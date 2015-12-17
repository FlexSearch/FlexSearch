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

open FlexSearch.Core
open FlexLucene.Index
open FlexLucene.Search
open System
open System.IO

module ShardWriter = 
    /// Returns the user commit data to be stored with the index
    let getCommitData (gen : int64) (modifyIndex : int64) = 
        hashMap()
//        |> putC (MetaFields.generationLabel, gen)
//        |> putC (MetaFields.modifyIndex, modifyIndex)
    
    type FileWriter(directory, config) = 
        inherit IndexWriter(directory, config)
        let mutable state = Unchecked.defaultof<T>
        member __.SetState(s : T) = state <- s
        /// A hook for extending classes to execute operations after pending 
        /// added and deleted documents have been flushed to the Directory but 
        /// before the change is committed (new segments_N file written).
        override __.doAfterFlush() = 
            /// State can be null when the index writer is opened for the
            /// very first time
            if not (isNull state) then state.IncrementFlushCount() |> ignore
    
    /// A SharWriter creates and maintains a shard of an index.
    /// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
    /// SearcherManager through an easy to manage abstraction.
    and T = 
        { IndexWriter : FileWriter
          TrackingIndexWriter : TrackingIndexWriter
          SearcherManager : SearcherManager
          TxWriter : TransactionLog.TxWriter
          CommitDuration : int
          /// Shows the status of the current Shard
          mutable Status : ShardStatus
          /// Represents the generation of commit
          Generation : AtomicLong
          /// Represents the last commit time. This is used by the
          /// time based commit to check if auto-commit should take
          /// place or not.
          mutable LastCommitTime : DateTime
          /// Represents the total outstanding flushes that have occurred
          /// since the last commit
          mutable OutstandingFlushes : AtomicLong
          /// Represents the current modify index. This is used by for
          /// recovery and shard sync from transaction logs.
          ModifyIndex : AtomicLong
          Settings : IndexConfiguration
          /// Transaction log path to be used
          TxLogPath : string
          ShardNo : int
          Lock : obj }
        member this.GetNextIndex() = this.ModifyIndex.Increment()
        member this.GetNextGen() = this.Generation.Increment()
        member this.IncrementFlushCount() = this.OutstandingFlushes.Increment()
        member this.ResetFlushCount() = this.OutstandingFlushes.Reset()
    
    /// Get the highest modified index value from the shard   
    let getMaxModifyIndex (r : IndexReader) = 
        let mutable max = 0L
        for i = 0 to r.Leaves().size() - 1 do
            let ctx = r.Leaves().get(i) :?> LeafReaderContext
            let reader = ctx.Reader()
            let nDocs = reader.getNumericDocValues (MetaFields.ModifyIndex)
            let liveDocs = reader.getLiveDocs()
            for j = 0 to reader.maxDoc() do
                if (liveDocs <> null || liveDocs.get (j)) then max <- Math.Max(max, nDocs.get (j))
        max
    
    /// Commits all pending changes (added & deleted documents, segment merges, added indexes, etc.) to the index, 
    /// and syncs all referenced index files, such that a reader will see the changes and the index updates will 
    /// survive an OS or machine crash or power loss. Note that this does not wait for any running background 
    /// merges to finish. This may be a costly operation, so you should test the cost in your application and 
    /// do it only when really necessary.
    let commit (forceCommit : bool) (sw : T) = 
        !>"Checking Commit Condition"
        (!>) "Generation: %i" sw.Generation.Value
        (!>) "Outstanding Flushes: %i" sw.OutstandingFlushes.Value
        (!>) "Force Commit: %b" forceCommit
        let internalCommit() = 
            !>"Starting Commit"
            lock sw.Lock (fun _ -> sw.LastCommitTime <- DateTime.Now)
            sw.OutstandingFlushes.Reset()
            (!>) "Outstanding Flushes: %i" sw.OutstandingFlushes.Value
            let generation = sw.Generation
            // Increment the generation before committing so that the 
            // newly added items go to the next log file
            let newGen = sw.Generation.Increment()
            (!>) "New Generation: %i" newGen
            // Set the new commit data
            !>"Performing Commit"
            getCommitData generation.Value sw.ModifyIndex.Value
            |> sw.IndexWriter.SetCommitData
            |> sw.IndexWriter.Commit
            !>"Deleting older commit files"
            try 
                loopFiles (sw.TxLogPath) |> Seq.iter (fun filePath -> 
                                                let (success, gen) = 
                                                    Int64.TryParse(Path.GetFileNameWithoutExtension filePath)
                                                // Delete files going back up to last 2 generations
                                                if success && (newGen - 2L) <= gen then File.Delete(filePath)
                                                else 
                                                    // File name does not follow our naming convention
                                                    // so delete it as it should not be here anyhow.
                                                    File.Delete(filePath))
            with _ -> ()
        if forceCommit then internalCommit()
        else 
            if sw.IndexWriter.HasUncommittedChanges() 
               && ((DateTime.Now - sw.LastCommitTime).Seconds >= sw.CommitDuration 
                   || sw.OutstandingFlushes.Value >= int64 sw.Settings.CommitEveryNFlushes) then internalCommit()
    
    /// Commits all changes to an index, waits for pending merges to complete, closes all 
    /// associated files and releases the write lock.
    let close (sw : T) = 
        try 
            sw.SearcherManager.Close()
            sw.IndexWriter.Close()
            sw.TxWriter :> IRequireNotificationForShutdown 
            |> fun x -> x.Shutdown()
            |> Async.RunSynchronously
            sw.TxWriter :> IDisposable
            |> fun x -> x.Dispose()
        with AlreadyClosedException -> ()
    
    /// Adds a document to this index.
    let addDocument (document : LuceneDocument) (sw : T) = sw.TrackingIndexWriter.AddDocument(document) |> ignore
    
    /// Deletes the document with the given id.
    let deleteDocument (id : string) (idFieldName : string) (sw : T) = 
        sw.TrackingIndexWriter.DeleteDocuments(id.Term(idFieldName)) |> ignore
    
    /// Delete all documents in the index.
    let deleteAll (sw : T) = sw.TrackingIndexWriter.DeleteAll() |> ignore
    
    /// Delete all documents returned by search query
    let deleteFromSearch (query : FlexLucene.Search.Query) (sw : T) = 
        sw.TrackingIndexWriter.DeleteDocuments query |> ignore
    
    /// Updates a document by id by first deleting the document containing term and then 
    /// adding the new document.
    let updateDocument (id : string, idFieldName : string, document : LuceneDocument) (sw : T) = 
        sw.TrackingIndexWriter.UpdateDocument(id.Term(idFieldName), document) |> ignore
    
    /// Returns real time searcher. 
    /// Note: Use it with 'use' keyword to automatically return the searcher to the pool
    let getRealTimeSearcher (sw : T) = new RealTimeSearcher(sw.SearcherManager)
    
    /// You must call this periodically, if you want that GetRealTimeSearcher() will return refreshed instances.
    let refresh (sw : T) = sw.SearcherManager.MaybeRefresh() |> ignore
    
    /// Adds a listener, to be notified when a reference is refreshed/swapped.
    let addRefreshListener (item : ReferenceManager.RefreshListener) (sw : T) = sw.SearcherManager.AddListener(item)
    
    /// Remove a listener added with AddRefreshListener.
    let removeRefreshListener (item : ReferenceManager.RefreshListener) (sw : T) = 
        sw.SearcherManager.RemoveListener(item)
    
    /// Returns the total number of docs present in the index
    let getDocumentCount (sw : T) = sw.IndexWriter.NumDocs()
    
    /// Create a new shard
    let create (shardNumber : int, settings : IndexConfiguration, config : IndexWriterConfig, basePath : string, 
                directory : FlexLucene.Store.Directory) = 
        let iw = new FileWriter(directory, config)
        let commitData = iw.GetCommitData()
        
        let generation = 
            let gen = 1L//pLong 1L (commitData.getOrDefault (MetaFields.generationLabel, "1") :?> string)
            // It is a newly created index. 
            if gen = 1L then 
                // Add a dummy commit so that searcher Manager could be initialized
                getCommitData 1L 1L
                |> iw.SetCommitData
                |> iw.Commit
            // Increment the generation as it is used to write the TxLog
            gen + 1L
        
        let trackingWriter = new TrackingIndexWriter(iw)
        let searcherManager = new SearcherManager(iw, true, new SearcherFactory())
        let modifyIndex = pLong 1L (commitData.getOrDefault (MetaFields.ModifyIndex, "1") :?> string)
        let logPath = basePath +/ "shards" +/ shardNumber.ToString() +/ "txlogs"
        Directory.CreateDirectory(logPath) |> ignore
        let state = 
            { IndexWriter = iw
              TrackingIndexWriter = trackingWriter
              SearcherManager = searcherManager
              Generation = AtomicLong.Create(generation)
              CommitDuration = settings.CommitTimeSeconds
              LastCommitTime = DateTime.Now
              OutstandingFlushes = AtomicLong.Create()
              Status = ShardStatus.Opening
              ModifyIndex = AtomicLong.Create(modifyIndex)
              TxWriter = new TransactionLog.TxWriter(logPath, generation)
              Settings = settings
              TxLogPath = logPath
              Lock = new Object()
              ShardNo = shardNumber }
        iw.SetState(state)
        state
