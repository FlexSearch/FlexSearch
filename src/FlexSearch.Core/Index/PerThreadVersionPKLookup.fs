// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Common
open System
open System.Collections.Concurrent
open org.apache.lucene.analysis
open org.apache.lucene.codecs.idversion
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.sandbox
open org.apache.lucene.search

// ----------------------------------------------------------------------------
/// Version cache store used across the system. This helps in resolving 
/// conflicts arising out of concurrent threads trying to update a Lucene document.
/// Every document update should go through version cache to ensure the update
/// integrity and optimistic locking.
/// In order to reduce contention there will be one CacheStore per shard. 
/// Initially Lucene's LiveFieldValues seemed like a good alternative but it
/// complicates the design and requires thread management
// ----------------------------------------------------------------------------
[<Sealed>]
type VersionCacheStore(shard : FlexShardWriter, indexSettings : FlexIndexSetting) as self = 
    
    /// Will be used to represent the deleted document version
    static let deletedValue = 0L
    
    /// The reason to use two dictionary instead of one is to avoid calling clear method
    /// on the dictionary as it acquires all locks. Also, there is a small span of time
    /// between before and after refresh when we won't have the values in the index
    [<VolatileFieldAttribute>]
    let mutable current = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    [<VolatileFieldAttribute>]
    let mutable old = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    let PKLookup(id : string, r : IndexReader) = 
        let term = new Term(indexSettings.FieldsLookup.[Constants.IdField].SchemaName, id)
        
        let rec loop counter = 
            let readerContext = r.leaves().get(counter) :?> AtomicReaderContext
            let reader = readerContext.reader()
            let terms = reader.terms (indexSettings.FieldsLookup.[Constants.IdField].SchemaName)
            assert (terms <> null)
            let termsEnum = terms.iterator (null)
            match termsEnum.seekExact (term.bytes()) with
            | true -> 
                let docsEnums = termsEnum.docs (null, null, 0)
                let nDocs = 
                    reader.getNumericDocValues (indexSettings.FieldsLookup.[Constants.LastModifiedFieldDv].SchemaName)
                nDocs.get (docsEnums.nextDoc())
            | false -> 
                if counter - 1 > 0 then loop (counter - 1)
                else 0L
        if r.leaves().size() > 0 then loop (r.leaves().size() - 1)
        else 0L
    
    let AddOrUpdate(id : string, version : int64, comparison : int64) : bool = 
        match current.TryGetValue(id) with
        | true, oldValue -> 
            if comparison = 0L then 
                // It is an unconditional update
                current.TryUpdate(id, version, oldValue)
            else current.TryUpdate(id, version, comparison)
        | _ -> current.TryAdd(id, version)
    
    do shard.SearcherManager.addListener (self)
    
    /// <summary>
    /// Dispose method which will be called automatically through Fody inter-leaving 
    /// </summary>
    member this.DisposeManaged() = 
        if shard.SearcherManager <> null then shard.SearcherManager.removeListener (self)
    
    interface ReferenceManager.RefreshListener with
        member this.afterRefresh (b : bool) : unit = 
            // Now drop all the old values because they are now
            // visible via the searcher that was just opened; if
            // didRefresh is false, it's possible old has some
            // entries in it, which is fine: it means they were
            // actually already included in the previously opened
            // reader.  So we can safely clear old here:
            old <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
        member this.beforeRefresh() : unit = 
            old <- current
            // Start sending all updates after this point to the new
            // dictionary.  While reopen is running, any lookup will first
            // try this new dictionary, then fall back to old, then to the
            // current searcher:
            current <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    interface IVersioningCacheStore with
        member this.AddOrUpdate(id : string, version : int64, comparison : int64) : bool = 
            AddOrUpdate(id, version, comparison)
        member this.Delete(id : string, version : Int64) : bool = AddOrUpdate(id, deletedValue, version)
        member this.GetValue(id : string) : int64 = 
            match current.TryGetValue(id) with
            | true, value -> value
            | _ -> 
                // Search old
                match old.TryGetValue(id) with
                | true, value -> value
                | _ -> 
                    // Go to the searcher to get the latest value
                    let s = shard.SearcherManager.acquire() :?> IndexSearcher
                    let value = PKLookup(id, s.getIndexReader())
                    shard.SearcherManager.release (s)
                    value
    
    interface IDisposable with
        member x.Dispose() : unit = ()

[<Sealed>]
type VersioningManger(indexSettings : FlexIndexSetting, shards : FlexShardWriter []) = 
    let versionCaches = shards |> Array.map (fun s -> new VersionCacheStore(s, indexSettings) :> IVersioningCacheStore)
    
    let VersionCheck(document : FlexDocument, shardNumber : int, newVersion : int64) = 
        maybe { 
            match document.TimeStamp with
            | 0L -> 
                // We don't care what the version is let's proceed with normal operation
                // and bypass id check.
                return! Choice1Of2(0L)
            | -1L -> // Ensure that the document does not exists. Perform Id check
                     
                let existingVersion = versionCaches.[shardNumber].GetValue(document.Id)
                if existingVersion <> 0L then 
                    return! Choice2Of2(Errors.INDEXING_DOCUMENT_ID_ALREADY_EXISTS |> GenerateOperationMessage)
                else return! Choice1Of2(0L)
            | 1L -> 
                // Ensure that the document does exist
                let existingVersion = versionCaches.[shardNumber].GetValue(document.Id)
                if existingVersion <> 0L then return! Choice1Of2(existingVersion)
                else return! Choice2Of2(Errors.INDEXING_DOCUMENT_ID_NOT_FOUND |> GenerateOperationMessage)
            | x when x > 1L -> 
                // Perform a version check and ensure that the provided version matches the version of 
                // the document
                let existingVersion = versionCaches.[shardNumber].GetValue(document.Id)
                if existingVersion <> 0L then 
                    if existingVersion <> document.TimeStamp || existingVersion > newVersion then 
                        return! Choice2Of2(Errors.INDEXING_VERSION_CONFLICT |> GenerateOperationMessage)
                    else return! Choice1Of2(existingVersion)
                else return! Choice2Of2(Errors.INDEXING_DOCUMENT_ID_NOT_FOUND |> GenerateOperationMessage)
            | _ -> 
                System.Diagnostics.Debug.Fail("This condition should never get executed.")
                return! Choice1Of2(0L)
        }
    
    /// <summary>
    /// Dispose method which will be called automatically through Fody inter-leaving 
    /// </summary>   
    member this.DisposeManaged() = 
        // Explicitly dispose all caches
        versionCaches |> Array.iter (fun s -> (s :?> IDisposable).Dispose())
    
    interface IVersionManager with
        member x.VersionCheck(document : FlexDocument, newVersion : int64) : Choice<int64, OperationMessage> = 
            failwith "Not implemented yet"
        member x.VersionCheck(document : FlexDocument, shardNumber : int, newVersion : int64) : Choice<int64, OperationMessage> = 
            VersionCheck(document, shardNumber, newVersion)
        member x.AddOrUpdate(id : string, shardNumber : int, version : int64, comparison : int64) = 
            versionCaches.[shardNumber].AddOrUpdate(id, version, comparison)
        member x.Delete(id : string, shardNumber : int, version : int64) = 
            versionCaches.[shardNumber].Delete(id, version)
    
    interface IDisposable with
        member x.Dispose() : unit = ()
