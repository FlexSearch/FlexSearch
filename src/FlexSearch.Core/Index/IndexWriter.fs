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
open FlexSearch.Api.Constants
open FlexSearch.Api.Model
open FlexLucene.Codecs.Bloom
open System.Threading
open Microsoft.Extensions.ObjectPool
open System.IO
open System

/// An IndexWriter creates and maintains an index. It contains a list of ShardWriters,
/// each of which encapsulating the functionality of IndexWriter, TrackingIndexWriter and
/// SearcherManger through an easy to manage abstraction.
type IndexWriter = 
    { Template : ObjectPool<DocumentTemplate>
      Caches : VersionCache []
      ShardWriters : ShardWriter []
      Settings : IndexSetting
      TxWriterPool : ObjectPool<TxWriter>
      ModifyIndex : AtomicLong
      Generation : AtomicLong
      mutable Status : IndexStatus
      Token : CancellationTokenSource }
    member this.GetSchemaName(fieldName : string) = this.Settings.Fields.Item(fieldName).SchemaName

[<Compile(ModuleSuffix)>]
module IndexWriter = 
    ///  Method to map a string based id to a Lucene shard 
    /// Uses MurmurHash2 algorithm
    let inline mapToShard shardCount (id : string) = 
        if (shardCount = 1) then 0
        else 
            let byteArray = System.Text.Encoding.UTF8.GetBytes(id)
            MurmurHash2.Hash32(byteArray, 0, byteArray.Length) % shardCount
    
    /// Create index settings from the Index DTO
    let createIndexSetting (index : Index, analyzerService) = 
        try 
            withIndexName (index.IndexName, Constants.DataFolder +/ index.IndexName)
            |> withShardConfiguration (index.ShardConfiguration)
            |> withIndexConfiguration (index.IndexConfiguration)
            |> withFields (index.Fields, analyzerService)
            |> withPredefinedQueries (index.PredefinedQueries, new FlexParser())
            |> withScripts
            |> build
            |> ok
        with
        | :? ValidationException as e -> 
            Logger.Log <| IndexLoadingFailure(index.IndexName, index.ToString(), exceptionPrinter e)
            fail <| e.Data0
        | e -> 
            let error = IndexLoadingFailure(index.IndexName, index.ToString(), exceptionPrinter e)
            Logger.Log <| error
            fail <| error
    
    /// Close the index    
    let close (writer : IndexWriter) = 
        // Try cancelling the token. Ignore Object disposed exceptions
        try writer.Token.Cancel()
        with | :? ObjectDisposedException -> ()

        writer.ShardWriters |> Array.iter ShardWriter.close
        // Release the locks on the transaction log files
        writer.TxWriterPool 
        |> getPooledObjects
        |> Seq.where isNotNull
        |> Seq.iter (fun txw -> (txw :> IDisposable).Dispose())
        // Make sure all the files are removed from TxLog otherwise the index will
        // go into unnecessary recovery mode.
        if writer.Settings.IndexConfiguration.DeleteLogsOnClose then emptyDir <| writer.Settings.BaseFolder +/ Constants.TxLogsSuffix
    
    /// Refresh the index    
    let refresh (s : IndexWriter) = s.ShardWriters |> Array.iter ShardWriter.refresh
    
    /// Commit unsaved data to the index
    let commit (forceCommit : bool) (s : IndexWriter) = 
        // Increment the generation before committing so that all transactions start going to the
        // new log file
        let newGen = s.Generation.Increment()
        s.ShardWriters |> Array.iter (ShardWriter.commit forceCommit)
        // Delete all the Tx files which are older than two generation
        loopFiles (s.Settings.BaseFolder +/ Constants.TxLogsSuffix) 
        |> Seq.iter (fun filePath -> 
                let (success, gen) = 
                    Int64.TryParse(Path.GetFileNameWithoutExtension filePath)
                if success && (newGen - 2L) >= gen then File.Delete(filePath))
    
    /// Add or update a document
    let addOrUpdateDocument (document : Document, create : bool, addToTxLog : bool) (s : IndexWriter) = 
        maybe { 
            let shardNo = document.Id |> mapToShard s.ShardWriters.Length
            let modifyIndex = s.ModifyIndex.Increment()
            let! existingVersion = s.Caches.[shardNo] |> VersionCache.versionCheck (document, modifyIndex)
            // After version check is passed update the cache
            do! s.Caches.[shardNo]
                |> VersionCache.addOrUpdate (document.Id, modifyIndex, existingVersion)
                |> boolToResult UnableToUpdateMemory
            
            document.ModifyIndex <- modifyIndex
            // Create new binary document
            let template = s.Template.Get()
            let doc = template |> DocumentTemplate.updateTempate document s.Settings.Scripts
            if addToTxLog then 
                let opCode = 
                    if create then TxOperation.Create
                    else TxOperation.Update
                
                let txEntry = TransactionEntry.Create(modifyIndex, opCode, document.Fields, document.Id)
                let txWriter = s.TxWriterPool.Get()
                txWriter.AppendEntry(txEntry, s.Generation.Value)
                s.TxWriterPool.Return(txWriter)
            // Release the dictionary back to the pool so that it could be recycled
            dictionaryPool.Return(document.Fields)

            s.ShardWriters.[shardNo] 
            |> if create then ShardWriter.addDocument doc
               else ShardWriter.updateDocument (document.Id, (s.GetSchemaName(IdField.Name)), doc)

            /// !!!! Important !!!!
            /// The DocumentTemplate should only be returned to the pool at this point in the code.
            /// This is because even though the `template` variable is no longer used, references
            /// to fields within the variable are being used by the `doc` variable. It turns out that
            /// LuceneDocument (`doc` variable) doesn't make a copy of the field, it just keeps the 
            /// reference you've given it. And those references were coming from the `template` 
            /// variable. Returning the template to the pool any earlier would cause duplicate
            /// documents being created in the index.
            /// !!!! Important !!!!
            s.Template.Return(template)
        }
    
    /// Add a document to the index
    let addDocument (document : Document) (s : IndexWriter) = s |> addOrUpdateDocument (document, true, true)
    
    /// Add a document to the index
    let updateDocument (document : Document) (s : IndexWriter) = s |> addOrUpdateDocument (document, false, true)
    
    /// Delete all documents in the index
    let deleteAllDocuments (s : IndexWriter) = 
        s.ShardWriters |> Array.Parallel.iter (fun s -> ShardWriter.deleteAll (s))
    
    /// Deletes all documents returned by search query
    // TODO: maybe include this in transaction log
    let deleteAllDocumentsFromSearch q iw = iw.ShardWriters |> Array.Parallel.iter (ShardWriter.deleteFromSearch q)
    
    /// Delete a document from index
    let deleteDocument (id : string) (s : IndexWriter) = 
        maybe { 
            let shardNo = id |> mapToShard s.ShardWriters.Length
            do! s.Caches.[shardNo]
                |> VersionCache.delete (id, VersionCache.DeletedValue)
                |> boolToResult UnableToUpdateMemory
            let txId = s.ModifyIndex.Increment()
            let txEntry = TransactionEntry.Create(txId, id)
            let txWriter = s.TxWriterPool.Get()
            txWriter.AppendEntry(txEntry, s.Generation.Value)
            s.TxWriterPool.Return txWriter
            s.ShardWriters.[shardNo] |> ShardWriter.deleteDocument id (s.GetSchemaName(IdField.Name))
        }
    
    let getRealTimeSearchers (s : IndexWriter) = 
        Array.init s.ShardWriters.Length (fun x -> ShardWriter.getRealTimeSearcher <| s.ShardWriters.[x])
    
    let getRealTimeSearcher (shardNo : int) (s : IndexWriter) = 
        assert (s.ShardWriters.Length <= shardNo)
        ShardWriter.getRealTimeSearcher <| s.ShardWriters.[shardNo]
    
    /// Returns the total number of docs present in the index
    let getDocumentCount (s : IndexWriter) = 
        s.ShardWriters |> Array.fold (fun count shard -> ShardWriter.getDocumentCount (shard) + count) 0
    
    /// <summary>
    /// Creates a async timer which can be used to execute a function at specified
    /// period of time. This is used to schedule all recurring indexing tasks
    /// </summary>
    /// <param name="delay">Delay to be applied</param>
    /// <param name="work">Method to perform the work</param>
    /// <param name="indexWriter">Index on which the job is to be scheduled</param>
    let scheduleIndexJob delay (work : IndexWriter -> unit) indexWriter = 
        let rec loop time (cts : CancellationTokenSource) = 
            async { 
                do! Async.Sleep(time)
                if (cts.IsCancellationRequested) then cts.Dispose()
                else 
                    try 
                        work indexWriter
                    with _ -> cts.Dispose()
                return! loop delay cts
            }
        loop delay indexWriter.Token
    
    /// Replay all the uncommitted transactions from the logs
    let replayTransactionLogs (indexWriter : IndexWriter) = 
        indexWriter.Status <- IndexStatus.Opening
        let replayShardTransaction (path) = 
            // Read logs for the generation 1 higher than the last committed generation as
            // these represents the records which are not committed
            // TODO: Find a more memory efficient way of sorting the transaction log file
            let logEntries = 
                TxWriter.ReadLog(path) 
                // Exclude corrupted log entries
                |> Seq.filter (fun l -> if isNotNull l then true
                                        else Logger.Log("There was a corrupted Transaction Log entry in the following file " + path,
                                                        MessageKeyword.Index,
                                                        MessageLevel.Warning)
                                             false)
                |> Seq.sortBy (fun l -> l.ModifyIndex)
            for entry in logEntries do
                let shardNo = entry.Id |> mapToShard indexWriter.ShardWriters.Length
                match entry.Operation with
                | TxOperation.Create | TxOperation.Update -> 
                    let template = indexWriter.Template.Get()
                    let document = new Document(entry.Id, indexWriter.Settings.IndexName, Fields = entry.Data, ModifyIndex = entry.ModifyIndex)
                    let doc = template |> DocumentTemplate.updateTempate document indexWriter.Settings.Scripts
                    indexWriter.ShardWriters.[shardNo] 
                    |> ShardWriter.updateDocument (entry.Id, indexWriter.GetSchemaName(IdField.Name), doc)
                | TxOperation.Delete -> 
                    indexWriter.ShardWriters.[shardNo] 
                    |> ShardWriter.deleteDocument entry.Id (indexWriter.GetSchemaName(IdField.Name))
                | _ -> ()
            // Force commit after iterating through each file
            indexWriter |> commit true

            // Delete the current transaction log file
            // The reason we are doing it here, as opposed to letting the commit handle the deletion,
            // is because the TxLog generation will almost always be higher than the current 
            // generation. Generation counting restarts every time FlexSearch starts.
            File.Delete path
        indexWriter.Settings.BaseFolder +/ Constants.TxLogsSuffix
        |> loopFiles
        /// Sort the files by generation number so that we execute them in order
        |> Seq.sortBy (fun path -> let (success, gen) = Int64.TryParse <| Path.GetFileNameWithoutExtension path
                                   if success then gen 
                                   else (!>) "Couldn't get the generation number of TxLog %s" path
                                        Int64.MaxValue)
        |> Seq.iter replayShardTransaction
        // Just refresh the index so that the changes are picked up
        // in subsequent searches. We can also commit here but it will
        // introduce blank commits in case there are no logs to replay.
        indexWriter.ShardWriters |> Array.iter ShardWriter.refresh
        indexWriter.Status <- IndexStatus.Online
    
    /// Create a new index instance
    let create (settings : IndexSetting) = 
        let factory = fun _ -> DocumentTemplate.create (settings)
        let policy = new ObjectPoolPolicy<DocumentTemplate>(factory, fun _ -> true)
        // Intitialize TxWriter pool
        let txPath = settings.BaseFolder +/ Constants.TxLogsSuffix |> createDir
        let txFactory = fun _ -> new TxWriter(0L, txPath)
        let txPolicy = new ObjectPoolPolicy<TxWriter>(txFactory, fun _ -> true)
        
        // Create a shard for the index
        let createShard (n) = 
            let path = settings.BaseFolder +/ "shards" +/ n.ToString() +/ "index"
            let indexWriterConfig = IndexWriterConfigBuilder.buildWithSettings (settings)
            let dir = DirectoryType.getIndexDirectory (settings.IndexConfiguration.DirectoryType, path) |> extract
            ShardWriter.create (n, settings.IndexConfiguration, indexWriterConfig, settings.BaseFolder, dir)
        
        let shardWriters = Array.init settings.ShardConfiguration.ShardCount createShard
        let caches = shardWriters |> Array.map (VersionCache.create settings)
        
        let modifyIndex = 
            shardWriters
            |> Array.map ShardWriter.getMaxModifyIndex
            |> Array.max
            // Modify index should never be smaller than 2 as
            // it affect concurrency check criteria than when
            // modifyIndex is 1 then document should exist.
            // we set the index to 1L as it will be incremented
            // on the first index request.
            |> (fun x -> if x < 2L  then 1L else x)
        
        let indexWriter = 
            { Template = new DefaultObjectPool<DocumentTemplate>(policy)
              TxWriterPool = new DefaultObjectPool<TxWriter>(txPolicy)
              ShardWriters = shardWriters
              Caches = caches
              ModifyIndex = new AtomicLong(modifyIndex)
              Generation = new AtomicLong(0L)
              Settings = settings
              Status = IndexStatus.Offline
              Token = new System.Threading.CancellationTokenSource() }
        
        indexWriter |> replayTransactionLogs
        // Add the scheduler for the index
        // Commit Scheduler
        if settings.IndexConfiguration.AutoCommit then 
            Async.Start
                (indexWriter |> scheduleIndexJob (settings.IndexConfiguration.CommitTimeSeconds * 1000) (commit false))
        // NRT Scheduler
        if settings.IndexConfiguration.AutoRefresh then 
            Async.Start(indexWriter |> scheduleIndexJob settings.IndexConfiguration.RefreshTimeMilliseconds refresh)
        indexWriter
