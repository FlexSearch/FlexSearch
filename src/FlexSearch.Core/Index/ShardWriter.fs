// ----------------------------------------------------------------------------
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

open FlexSearch.Api.Constants
open FlexSearch.Api.Model
open FlexSearch.Core
open FlexLucene.Index
open FlexLucene.Search
open System
open System.IO

/// A SharWriter creates and maintains a shard of an index.
/// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
/// SearcherManager through an easy to manage abstraction.
type ShardWriter = 
    { IndexWriter : IndexWriter
      TrackingIndexWriter : TrackingIndexWriter
      SearcherManager : SearcherManager
      Settings : IndexConfiguration
      ShardNo : int }

[<Compile(ModuleSuffix)>]
module ShardWriter = 
    /// Get the highest modified index value from the shard   
    let getMaxModifyIndex (sw : ShardWriter) = 
        let mutable max = 0L
        use searcher = new RealTimeSearcher(sw.SearcherManager)
        let r = searcher.IndexReader
        for i = 0 to r.Leaves().size() - 1 do
            let ctx = r.Leaves().get(i) :?> LeafReaderContext
            let reader = ctx.Reader()
            let nDocs = reader.GetNumericDocValues(ModifyIndexField.Name)
            for j = 0 to reader.MaxDoc() do
                // The last document in the index might have got corrupted, so just try getting the maximum.
                // Values that were corrupted will get overridden.
                try max <- Math.Max(max, nDocs.Get(j))
                with | :? java.lang.IndexOutOfBoundsException -> ()
        max
    
    /// Commits all pending changes (added & deleted documents, segment merges, added indexes, etc.) to the index, 
    /// and syncs all referenced index files, such that a reader will see the changes and the index updates will 
    /// survive an OS or machine crash or power loss. Note that this does not wait for any running background 
    /// merges to finish. This may be a costly operation, so you should test the cost in your application and 
    /// do it only when really necessary.
    let commit (forceCommit : bool) (sw : ShardWriter) = 
        if forceCommit || sw.IndexWriter.HasUncommittedChanges() then sw.IndexWriter.Commit()
    
    /// Commits all changes to an index, waits for pending merges to complete, closes all 
    /// associated files and releases the write lock.
    let close (sw : ShardWriter) = 
        try 
            sw.SearcherManager.Close()
            if sw.Settings.CommitOnClose then
                sw |> commit false
            sw.IndexWriter.Close()
        with e -> Logger.Log(e, MessageKeyword.Index, MessageLevel.Warning)
    
    /// Adds a document to this index.
    let addDocument (document : LuceneDocument) (sw : ShardWriter) = 
        sw.TrackingIndexWriter.AddDocument(document) |> ignore
    
    /// Deletes the document with the given id.
    let deleteDocument (id : string) (idFieldName : string) (sw : ShardWriter) = 
        sw.TrackingIndexWriter.DeleteDocuments(id.Term(idFieldName)) |> ignore
    
    /// Delete all documents in the index.
    let deleteAll (sw : ShardWriter) = sw.TrackingIndexWriter.DeleteAll() |> ignore
    
    /// Delete all documents returned by search query
    let deleteFromSearch (query : FlexLucene.Search.Query) (sw : ShardWriter) = 
        sw.TrackingIndexWriter.DeleteDocuments query |> ignore
    
    /// Updates a document by id by first deleting the document containing term and then 
    /// adding the new document.
    let updateDocument (id : string, idFieldName : string, document : LuceneDocument) (sw : ShardWriter) = 
        sw.TrackingIndexWriter.UpdateDocument(id.Term(idFieldName), document) |> ignore
    
    /// Returns real time searcher. 
    /// Note: Use it with 'use' keyword to automatically return the searcher to the pool
    let getRealTimeSearcher (sw : ShardWriter) = new RealTimeSearcher(sw.SearcherManager)
    
    /// You must call this periodically, if you want that GetRealTimeSearcher() will return refreshed instances.
    let refresh (sw : ShardWriter) = sw.SearcherManager.MaybeRefresh() |> ignore
    
    /// Adds a listener, to be notified when a reference is refreshed/swapped.
    let addRefreshListener (item : ReferenceManagerRefreshListener) (sw : ShardWriter) = 
        sw.SearcherManager.AddListener(item)
    
    /// Remove a listener added with AddRefreshListener.
    let removeRefreshListener (item : ReferenceManagerRefreshListener) (sw : ShardWriter) = 
        sw.SearcherManager.RemoveListener(item)
    
    /// Returns the total number of docs present in the index
    let getDocumentCount (sw : ShardWriter) = sw.IndexWriter.NumDocs()
    
    /// Create a new shard
    let create (shardNumber : int, settings : IndexConfiguration, config : IndexWriterConfig, basePath : string, 
                directory : FlexLucene.Store.Directory) = 
        let iw = new IndexWriter(directory, config)
        let trackingWriter = new TrackingIndexWriter(iw)
        let searcherManager = new SearcherManager(iw, true, new SearcherFactory())
        { IndexWriter = iw
          TrackingIndexWriter = trackingWriter
          SearcherManager = searcherManager
          Settings = settings
          ShardNo = shardNumber }
