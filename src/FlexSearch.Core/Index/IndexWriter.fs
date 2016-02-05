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
    { Template : DefaultObjectPool<DocumentTemplate>
      Caches : VersionCache []
      ShardWriters : ShardWriter []
      Settings : IndexSetting
      TxWriterPool : DefaultObjectPool<TxWriter>
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
    let createIndexSetting (index : Index, analyzerService, scriptService) = 
        try 
            withIndexName (index.IndexName, Constants.DataFolder +/ index.IndexName)
            |> withShardConfiguration (index.ShardConfiguration)
            |> withIndexConfiguration (index.IndexConfiguration)
            |> withFields (index.Fields, analyzerService, scriptService)
            |> withSearchProfiles (index.SearchProfiles, new FlexParser())
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
        writer.Token.Cancel()
        writer.ShardWriters |> Array.iter ShardWriter.close
        // Make sure all the files are removed from TxLog otherwise the index will
        // go into unnecessary recovery mode.
        if writer.Settings.IndexConfiguration.DeleteLogsOnClose then emptyDir <| writer.Settings.BaseFolder +/ "txlogs"
    
    /// Refresh the index    
    let refresh (s : IndexWriter) = s.ShardWriters |> Array.iter ShardWriter.refresh
    
    /// Commit unsaved data to the index
    let commit (forceCommit : bool) (s : IndexWriter) = 
        // Increment the generation before committing so that all transactions start going to the
        // new log file
        let newGen = s.Generation.Increment()
        s.ShardWriters |> Array.iter (ShardWriter.commit forceCommit)
        // Delete all the Tx files which are older than two generation
        loopDir (s.Settings.BaseFolder +/ "txlog") |> Seq.iter (fun filePath -> 
                                                          let (success, gen) = 
                                                              Int64.TryParse(Path.GetFileNameWithoutExtension filePath)
                                                          if success && (newGen - 2L) <= gen then File.Delete(filePath))
    
    /// Add or update a document
    let addOrUpdateDocument (document : Document, create : bool, addToTxLog : bool) (s : IndexWriter) = 
        maybe { 
            let shardNo = document.Id |> mapToShard s.ShardWriters.Length
            let newVersion = GetCurrentTimeAsLong()
            let! existingVersion = s.Caches.[shardNo] |> VersionCache.versionCheck (document, newVersion)
            // After version check is passed update the cache
            document.TimeStamp <- newVersion
            do! s.Caches.[shardNo]
                |> VersionCache.addOrUpdate (document.Id, newVersion, existingVersion)
                |> boolToResult UnableToUpdateMemory
            let txId = s.ModifyIndex.Increment()
            // Create new binary document
            let template = s.Template.Get()
            let doc = template |> DocumentTemplate.updateTempate document txId
            if addToTxLog then 
                let opCode = 
                    if create then TxOperation.Create
                    else TxOperation.Update
                
                let txEntry = TransactionEntry.Create(txId, opCode, document.Fields, document.Id)
                let txWriter = s.TxWriterPool.Get()
                txWriter.AppendEntry(txEntry, s.Generation.Value)
                s.TxWriterPool.Return(txWriter)
            // Release the dictionary back to the pool so that it could be recycled
            dictionaryPool.Return(document.Fields)
            s.Template.Return(template)
            s.ShardWriters.[shardNo] 
            |> if create then ShardWriter.addDocument doc
               else ShardWriter.updateDocument (document.Id, (s.GetSchemaName(IdField.Name)), doc)
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
                |> VersionCache.delete (id, VersionCache.deletedValue)
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
        let txWriter = indexWriter.TxWriterPool.Get()
        indexWriter.Status <- IndexStatus.Opening
        let replayShardTransaction (gen : int64) = 
            // Read logs for the generation 1 higher than the last committed generation as
            // these represents the records which are not committed
            // TODO: Find a more memory efficient way of sorting the transaction log file
            let logEntries = txWriter.ReadLog(gen) |> Seq.sortBy (fun l -> l.ModifyIndex)
            for entry in logEntries do
                let shardNo = entry.Id |> mapToShard indexWriter.ShardWriters.Length
                match entry.Operation with
                | TxOperation.Create | TxOperation.Update -> 
                    let template = indexWriter.Template.Get()
                    let document = new Document(entry.Id, indexWriter.Settings.IndexName, Fields = entry.Data)
                    let doc = template |> DocumentTemplate.updateTempate document entry.ModifyIndex
                    let doc = template |> DocumentTemplate.updateTempate document entry.ModifyIndex
                    indexWriter.ShardWriters.[shardNo] 
                    |> ShardWriter.updateDocument (entry.Id, indexWriter.GetSchemaName(IdField.Name), doc)
                | TxOperation.Delete -> 
                    indexWriter.ShardWriters.[shardNo] 
                    |> ShardWriter.deleteDocument entry.Id (indexWriter.GetSchemaName(IdField.Name))
                | _ -> ()
            // Force commit after iterating through each file
            indexWriter |> commit true
        indexWriter.Settings.BaseFolder +/ "txlogs"
        |> loopFiles
        |> Seq.iter (Path.GetFileName
                     >> Int64.Parse
                     >> replayShardTransaction)
        // Just refresh the index so that the changes are picked up
        // in subsequent searches. We can also commit here but it will
        // introduce blank commits in case there are no logs to replay.
        indexWriter.ShardWriters |> Array.iter ShardWriter.refresh
        indexWriter.Status <- IndexStatus.Online
    
    /// Create a new index instance
    let create (settings : IndexSetting) = 
        let factory = fun _ -> DocumentTemplate.create (settings)
        let policy = new DefaultObjectPoolPolicy<DocumentTemplate>(factory, fun _ -> true)
        // Intitialize TxWriter pool
        let txPath = settings.BaseFolder +/ "txlogs" |> createDir
        let txFactory = fun _ -> new TxWriter(0L, txPath)
        let txPolicy = new DefaultObjectPoolPolicy<TxWriter>(txFactory, fun _ -> true)
        
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
