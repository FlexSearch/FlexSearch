﻿// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api.Model
open FlexSearch.Core
open System.Collections.Concurrent
open System
open FlexLucene.Index
open FlexLucene.Search

/// Cache store represented using two concurrent dictionaries
/// The reason to use two dictionary instead of one is to avoid calling clear method
/// on the dictionary as it acquires all locks. Also, there is a small span of time
/// between before and after refresh when we won't have the values in the index
type VersionCache = 
    { mutable Current : ConcurrentDictionary<string, int64>
      mutable Old : ConcurrentDictionary<string, int64>
      IdFieldName : string
      ModifyIndexFieldName : string
      ShardWriter : ShardWriter }
    
    interface ReferenceManagerRefreshListener with
        member this.AfterRefresh(_ : bool) : unit = 
            // Now drop all the old values because they are now
            // visible via the searcher that was just opened; if
            // didRefresh is false, it's possible old has some
            // entries in it, which is fine: it means they were
            // actually already included in the previously opened
            // reader.  So we can safely clear old here:
            this.Old <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
        member this.BeforeRefresh() : unit = 
            this.Old <- this.Current
            // Start sending all updates after this point to the new
            // dictionary.  While reopen is running, any lookup will first
            // try this new dictionary, then fall back to old, then to the
            // current searcher:
            this.Current <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    interface IDisposable with
        member this.Dispose() = 
            // Remove the listener when disposing the object
            if not (isNull this.ShardWriter.SearcherManager) then 
                this.ShardWriter |> ShardWriter.removeRefreshListener (this)

/// Version cache store used across the system. This helps in resolving 
/// conflicts arising out of concurrent threads trying to update a Lucene document.
/// Every document update should go through version cache to ensure the update
/// integrity and optimistic locking.
/// In order to reduce contention there will be one CacheStore per shard. 
/// Initially Lucene's LiveFieldValues seemed like a good alternative but it
/// complicates the design and requires thread management
[<Compile(ModuleSuffix)>]
module VersionCache = 
    let create (settings : IndexSetting) (shardWriter : ShardWriter) = 
        let store = 
            { Current = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
              Old = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
              IdFieldName = settings.Fields.[IdField.Name].SchemaName
              ModifyIndexFieldName = settings.Fields.[ModifyIndexField.Name].SchemaName
              ShardWriter = shardWriter }
        shardWriter |> ShardWriter.addRefreshListener (store)
        store
    
    /// Will be used to represent the deleted document version
    [<LiteralAttribute>]
    let DeletedValue = 0L
    
    /// An optimized key based lookup to get the version value using Lucene's DocValues
    let primaryKeyLookup (id : string, r : IndexReader) (cache : VersionCache) = 
        let term = new Term(cache.IdFieldName, id)
        
        let rec loop counter = 
            let readerContext = r.Leaves().get(counter) :?> LeafReaderContext
            let reader = readerContext.Reader()
            let terms = reader.Terms(cache.IdFieldName)
            assert (notNull terms)
            let termsEnum = terms.Iterator()
            match termsEnum.SeekExact(term.Bytes()) with
            | true -> 
                let docsEnums = termsEnum.Docs(null, null, 0)
                let nDocs = reader.GetNumericDocValues(cache.ModifyIndexFieldName)
                nDocs.Get(docsEnums.DocID())
            | false -> 
                if counter - 1 > 0 then loop (counter - 1)
                else 0L
        if r.Leaves().size() > 0 then loop (r.Leaves().size() - 1)
        else 0L
    
    /// Add or update a key in the cache store
    let addOrUpdate (id : string, version : int64, comparison : int64) (cache : VersionCache) = 
        match cache.Current.TryGetValue(id) with
        | true, oldValue -> 
            if comparison = 0L then 
                // It is an unconditional update
                cache.Current.TryUpdate(id, version, oldValue)
            else cache.Current.TryUpdate(id, version, comparison)
        | _ -> cache.Current.TryAdd(id, version)
    
    let delete (id : string, version : Int64) (cache : VersionCache) = addOrUpdate (id, DeletedValue, version) cache
    
    let getValue (id : string) (cache : VersionCache) = 
        match cache.Current.TryGetValue(id) with
        | true, value -> value
        | _ -> 
            // Search old
            match cache.Old.TryGetValue(id) with
            | true, value -> value
            | _ -> 
                // Go to the searcher to get the latest value
                use s = ShardWriter.getRealTimeSearcher (cache.ShardWriter)
                let value = cache |> primaryKeyLookup (id, s.IndexReader)
                cache.Current.TryAdd(id, value) |> ignore
                value
    
    /// Check and returns the current version number of the document
    let versionCheck (doc : Document, newVersion) (cache : VersionCache) = 
        match doc.ModifyIndex with
        | 0L -> 
            // We don't care what the version is let's proceed with normal operation
            // and bypass id check.
            ok <| 0L
        | -1L -> // Ensure that the document does not exists. Perform Id check
            let existingVersion = cache |> getValue (doc.Id)
            if existingVersion <> 0L then fail <| DocumentIdAlreadyExists(doc.IndexName, doc.Id)
            else ok <| existingVersion
        | 1L -> 
            // Ensure that the document exists
            let existingVersion = cache |> getValue (doc.Id)
            if existingVersion <> 0L then ok <| existingVersion
            else fail <| DocumentIdNotFound(doc.IndexName, doc.Id)
        | x when x > 1L -> 
            // Perform a version check and ensure that the provided version matches the version of 
            // the document
            let existingVersion = cache |> getValue (doc.Id)
            if existingVersion <> 0L then 
                if existingVersion <> doc.ModifyIndex || existingVersion > newVersion then 
                    fail <| IndexingVersionConflict(doc.IndexName, doc.Id, existingVersion.ToString())
                else ok <| existingVersion
            else fail <| DocumentIdNotFound(doc.IndexName, doc.Id)
        | _ -> 
            // This condition should never get executed unless the user has passed a negative version number
            // smaller than -1. In this case we will ignore version number.
            ok <| 0L
