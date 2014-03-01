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
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Index = 
    /// <summary>
    /// Represents a dummy lucene document. There will be one per index stored in a dictionary
    /// </summary>
    type private ThreadLocalDocument = 
        { Document : Document
          FieldsLookup : Dictionary<string, Field>
          LastGeneration : int }
    
    /// <summary>
    /// Stores all mutable index realted state data. This will be passed around
    /// in a controlled manner and is thread-safe.
    /// </summary>
    type private IndicesState = 
        { /// Dictionary to hold the current status of the indices. This is a thread 
          /// safe dictionary so it is easier to update it compared to a
          /// mutable field on index setting 
          IndexStatus : ConcurrentDictionary<string, IndexState>
          /// Dictionary to hold all the information about currently active index and their status
          IndexRegisteration : ConcurrentDictionary<string, FlexIndex>
          /// For optimal indexing performance, re-use the Field and Document 
          /// instance for more than one document. But that is not easily possible
          /// in a multi-threaded scenario using TPL dataflow as we don't know which 
          /// thread it is using to execute each task. The easiest way
          /// is to use ThreadLocal value to create a local copy of the index document.
          /// The implication of creating one lucene document class per document to 
          /// be indexed is the penalty it has in terms of garbage collection. Also,
          /// lucene's document and index classes can't be shared across threads.
          ThreadLocalStore : ThreadLocal<ConcurrentDictionary<string, ThreadLocalDocument>> }
        
        member this.GetStatus(indexName) = 
            match this.IndexStatus.TryGetValue(indexName) with
            | (true, state) -> Choice1Of2(state)
            | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
        
        member this.GetRegisteration(indexName) = 
            match this.IndexRegisteration.TryGetValue(indexName) with
            | (true, state) -> Choice1Of2(state)
            | _ -> Choice2Of2(MessageConstants.INDEX_REGISTERATION_MISSING)
        
        member this.AddStatus(indexName, status) = 
            match this.IndexStatus.TryAdd(indexName, status) with
            | true -> Choice1Of2()
            | false -> Choice2Of2(MessageConstants.ERROR_ADDING_INDEX_STATUS)
    
    // Index auto commit changes job
    let private commitJob (flexIndex : FlexIndex) = 
        // Looping over array by index number is usually the fastest
        // iteration method
        for i in 0..flexIndex.Shards.Length - 1 do
            // Lucene 4.4.0 feature to check for uncommitted changes
            if flexIndex.Shards.[i].IndexWriter.hasUncommittedChanges() then flexIndex.Shards.[i].IndexWriter.commit()
    
    // Index auto commit changes job
    let private refreshIndexJob (flexIndex) = 
        // Looping over array by index number is usually the fastest
        // iteration method
        for i in 0..flexIndex.Shards.Length - 1 do
            flexIndex.Shards.[i].NRTManager.maybeRefresh() |> ignore
    
    /// <summary>
    /// Creates a async timer which can be used to execute a funtion at specified
    /// period of time. This is used to schedule all recurring indexing tasks
    /// </summary>
    /// <param name="delay">Dealy to be applied</param>
    /// <param name="work">Method to perform the work</param>
    /// <param name="flexIndex">Index on which the job is to be scheduled</param>
    let private scheduleIndexJob delay (work : FlexIndex -> unit) flexIndex = 
        let rec loop time (cts : CancellationTokenSource) = 
            async { 
                do! Async.Sleep(time)
                if (cts.IsCancellationRequested) then cts.Dispose()
                else 
                    try 
                        work (flexIndex)
                    with e -> cts.Dispose()
                return! loop delay cts
            }
        loop delay flexIndex.Token
    
    /// <summary>
    /// Add index to the registeration
    /// </summary>
    /// <param name="state">Index state</param>
    /// <param name="flexIndexSetting">Index setting</param>
    let private addIndex (state : IndicesState, flexIndexSetting : FlexIndexSetting) = 
        maybe { 
            /// Generate shards for the newly added index
            let generateShards flexIndexSetting = 
                try 
                    let shards = 
                        Array.init flexIndexSetting.ShardConfiguration.ShardCount (fun a -> 
                            let writers = 
                                IO.GetIndexWriter
                                    (flexIndexSetting, flexIndexSetting.BaseFolder + "\\shards\\" + a.ToString())
                            match writers with
                            | Choice2Of2(e) -> failwith e.UserMessage
                            | Choice1Of2(indexWriter, trackingIndexWriter) -> 
                                // Based on Lucene 4.4 the nrtmanager is replaced with ControlledRealTimeReopenThread which can take any
                                // reference manager
                                let nrtManager = new SearcherManager(indexWriter, true, new SearcherFactory())
                                
                                let shard = 
                                    { ShardNumber = a
                                      NRTManager = nrtManager
                                      ReopenThread = 
                                          new ControlledRealTimeReopenThread(trackingIndexWriter, nrtManager, float (25), 
                                                                             float (5))
                                      IndexWriter = indexWriter
                                      TrackingIndexWriter = trackingIndexWriter }
                                shard)
                    Choice1Of2(shards)
                with e -> 
                    Choice2Of2
                        (OperationMessage.WithDeveloperMessage(MessageConstants.ERROR_OPENING_INDEXWRITER, e.Message))
            // Add index status
            state.IndexStatus.TryAdd(flexIndexSetting.IndexName, IndexState.Opening) |> ignore
            let! shards = generateShards flexIndexSetting
            let flexIndex = 
                { IndexSetting = flexIndexSetting
                  Shards = shards
                  Token = new System.Threading.CancellationTokenSource() }
            // Add the scheduler for the index
            // Commit Scheduler
            Async.Start(scheduleIndexJob (flexIndexSetting.IndexConfiguration.CommitTimeSec * 1000) commitJob flexIndex)
            // NRT Scheduler
            Async.Start
                (scheduleIndexJob flexIndexSetting.IndexConfiguration.RefreshTimeMilliSec refreshIndexJob flexIndex)
            // Add the index to the registeration
            state.IndexRegisteration.TryAdd(flexIndexSetting.IndexName, flexIndex) |> ignore
            state.IndexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Online
        }
    
    let private loadAllIndex (state : IndicesState, persistanceStore : IPersistanceStore) = 
        for x in persistanceStore.GetAll<Index>() do
            if x.Online then 
                try 
                    match ServiceLocator.SettingsBuilder.BuildSetting(x) with
                    | Choice1Of2(flexIndexSetting) -> addIndex (state, flexIndexSetting) |> ignore
                    | Choice2Of2(e) -> ()
                //indexLogger.Info(sprintf "Index: %s loaded successfully." x.IndexName)
                with ex -> ()
            //indexLogger.Error("Loading index from file failed.", ex)
            else 
                //indexLogger.Info(sprintf "Index: %s is not loaded as it is set to be offline." x.IndexName)
                state.IndexStatus.TryAdd(x.IndexName, IndexState.Offline) |> ignore
    
    // ----------------------------------------------------------------------------
    // Close an open index
    // ----------------------------------------------------------------------------
    let private closeIndex (state : IndicesState, flexIndex : FlexIndex) = 
        try 
            state.IndexRegisteration.TryRemove(flexIndex.IndexSetting.IndexName) |> ignore
            // Update status from online to closing
            state.IndexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Closing
            flexIndex.Token.Cancel()
            for shard in flexIndex.Shards do
                try 
                    shard.NRTManager.close()
                    shard.IndexWriter.commit()
                    shard.IndexWriter.close()
                with e -> ()
        with e -> () //logger.Error("Error while closing index:" + flexIndex.IndexSetting.IndexName, e)
        state.IndexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Offline
    
    // ----------------------------------------------------------------------------
    // Utility method to return index registeration information
    // ----------------------------------------------------------------------------
    let private getIndexRegisteration (state : IndicesState, indexName) = 
        match state.IndexStatus.TryGetValue(indexName) with
        | (true, status) -> 
            match status with
            | IndexState.Online -> 
                match state.IndexRegisteration.TryGetValue(indexName) with
                | (true, flexIndex) -> Choice1Of2(flexIndex)
                | _ -> Choice2Of2(MessageConstants.INDEX_REGISTERATION_MISSING)
            | IndexState.Opening -> Choice2Of2(MessageConstants.INDEX_IS_OPENING)
            | IndexState.Offline | IndexState.Closing -> Choice2Of2(MessageConstants.INDEX_IS_OFFLINE)
            | _ -> Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
        | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
    
    // ----------------------------------------------------------------------------               
    // Function to check if the requested index is available. If yes then tries to 
    // retrieve the dcument template associated with the index from threadlocal store.
    // If there is no template document for the requested index then goes ahead
    // and creates one. 
    // ----------------------------------------------------------------------------   
    let private indexExists (state : IndicesState, indexName) = 
        match state.IndexRegisteration.TryGetValue(indexName) with
        | (true, flexIndex) -> 
            match state.ThreadLocalStore.Value.TryGetValue(indexName) with
            | (true, a) -> Choice1Of2(flexIndex, a)
            | _ -> 
                let luceneDocument = new Document()
                let fieldLookup = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase)
                let idField = new StringField(Constants.IdField, "", Field.Store.YES)
                luceneDocument.add (idField)
                fieldLookup.Add(Constants.IdField, idField)
                let typeField = new StringField(Constants.TypeField, indexName, Field.Store.YES)
                luceneDocument.add (typeField)
                fieldLookup.Add(Constants.TypeField, typeField)
                let lastModifiedField = 
                    new LongField(Constants.LastModifiedField, GetCurrentTimeAsLong(), Field.Store.YES)
                luceneDocument.add (lastModifiedField)
                fieldLookup.Add(Constants.LastModifiedField, lastModifiedField)
                for field in flexIndex.IndexSetting.Fields do
                    // Ignore these 4 fields here.
                    if (field.FieldName = Constants.IdField || field.FieldName = Constants.TypeField 
                        || field.FieldName = Constants.LastModifiedField) then ()
                    else 
                        let defaultField = FlexField.CreateDefaultLuceneField field
                        luceneDocument.add (defaultField)
                        fieldLookup.Add(field.FieldName, defaultField)
                let documentTemplate = 
                    { Document = luceneDocument
                      FieldsLookup = fieldLookup
                      LastGeneration = 0 }
                state.ThreadLocalStore.Value.TryAdd(indexName, documentTemplate) |> ignore
                Choice1Of2(flexIndex, documentTemplate)
        | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
    
    // ----------------------------------------------------------------------------     
    // Updates the current thread local index document with the incoming data
    // ----------------------------------------------------------------------------     
    let private updateDocument (flexIndex : FlexIndex, documentTemplate : ThreadLocalDocument, documentId : string, 
                                version : int, fields : Dictionary<string, string>) = 
        documentTemplate.FieldsLookup.[Constants.IdField].setStringValue(documentId)
        documentTemplate.FieldsLookup.[Constants.LastModifiedField].setLongValue(GetCurrentTimeAsLong())
        for field in flexIndex.IndexSetting.Fields do
            // Ignore these 3 fields here.
            if (field.FieldName = Constants.IdField || field.FieldName = Constants.TypeField 
                || field.FieldName = Constants.LastModifiedField) then ()
            else 
                // If it is computed field then generate and add it otherwise follow standard path
                match field.Source with
                | Some(s) -> 
                    try 
                        // Wrong values for the data type will still be handled as update lucene field will
                        // check the data type
                        let value = s fields
                        FlexField.UpdateLuceneField field documentTemplate.FieldsLookup.[field.FieldName] value
                    with e -> FlexField.UpdateLuceneFieldToDefault field documentTemplate.FieldsLookup.[field.FieldName]
                | None -> 
                    match fields.TryGetValue(field.FieldName) with
                    | (true, value) -> 
                        FlexField.UpdateLuceneField field documentTemplate.FieldsLookup.[field.FieldName] value
                    | _ -> FlexField.UpdateLuceneFieldToDefault field documentTemplate.FieldsLookup.[field.FieldName]
        let targetIndex = 
            if (flexIndex.Shards.Length = 1) then 0
            else Document.mapToShard documentId flexIndex.Shards.Length
        (targetIndex, documentTemplate)
    
    // ----------------------------------------------------------------------------     
    // Function to process the 
    // ----------------------------------------------------------------------------                                         
    let private processItem (state : IndicesState, indexMessage : IndexCommand, flexIndex : FlexIndex, 
                             versionCache : IVersioningCacheStore) = 
        maybe { 
            let! (flexIndex, documentTemplate) = indexExists (state, flexIndex.IndexSetting.IndexName)
            match indexMessage with
            | Create(documentId, fields) -> 
                let (targetIndex, documentTemplate) = 
                    updateDocument (flexIndex, documentTemplate, documentId, 1, fields)
                flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) |> ignore
                return! Choice1Of2()
            | Update(documentId, fields) -> 
                let (targetIndex, documentTemplate) = 
                    updateDocument (flexIndex, documentTemplate, documentId, 1, fields)
                flexIndex.Shards.[targetIndex]
                    .TrackingIndexWriter.updateDocument(new Term("id", documentId), documentTemplate.Document) |> ignore
                return! Choice1Of2()
            | Delete(documentId) -> 
                let targetIndex = Document.mapToShard documentId flexIndex.Shards.Length - 1
                flexIndex.Shards.[targetIndex].TrackingIndexWriter.deleteDocuments(new Term("id", documentId)) |> ignore
                return! Choice1Of2()
            | BulkDeleteByIndexName -> 
                flexIndex.Shards |> Array.iter (fun shard -> shard.TrackingIndexWriter.deleteAll() |> ignore)
                return! Choice1Of2()
            | Commit -> 
                flexIndex.Shards |> Array.iter (fun shard -> shard.IndexWriter.commit())
                return! Choice1Of2()
            | _ -> return! Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
        }
    
    // ----------------------------------------------------------------------------   
    // Concerete implementation of the index service interface. This class will be 
    // injected using DI thus exposing the necessary
    // functionality at any web service
    // loadAllIndex - This is used to bypass loading of index at initialization time.
    // Helpful for testing
    // ----------------------------------------------------------------------------   
    type IndexService(settingsParser : ISettingsBuilder, persistanceStore : IPersistanceStore, versionCache : IVersioningCacheStore, searchService : ISearchService) = 
        
        let state = 
            { IndexStatus = new ConcurrentDictionary<string, IndexState>(StringComparer.OrdinalIgnoreCase)
              IndexRegisteration = new ConcurrentDictionary<string, FlexIndex>(StringComparer.OrdinalIgnoreCase)
              ThreadLocalStore = 
                  new ThreadLocal<ConcurrentDictionary<string, ThreadLocalDocument>>(fun () -> 
                  new ConcurrentDictionary<string, ThreadLocalDocument>(StringComparer.OrdinalIgnoreCase)) }
        
        /// Default buffering queue
        /// This is TPL Dataflow based approach. Can replace it with parallel.foreach
        /// on blocking collection. 
        /// Advantages - Faster, EnumerablePartitionerOptions.NoBuffering takes care of the
        /// older .net partitioner bug, Can reduce the number of lucene documents which will be
        /// generated 
        let mutable queue : ActionBlock<string * IndexCommand> = null
        
        let processQueueItem (indexName, indexMessage : IndexCommand) = 
            let registeration = getIndexRegisteration (state, indexName)
            match registeration with
            | Choice1Of2(index) -> processItem (state, indexMessage, index, versionCache) |> ignore
            | Choice2Of2(_) -> ()
        
        // ----------------------------------------------------------------------------
        // Load all index configuration data on start of application
        // ----------------------------------------------------------------------------
        do 
            let executionBlockOption = new ExecutionDataflowBlockOptions()
            executionBlockOption.MaxDegreeOfParallelism <- -1
            executionBlockOption.BoundedCapacity <- 1000
            queue <- new ActionBlock<string * IndexCommand>(processQueueItem, executionBlockOption)
            loadAllIndex (state, persistanceStore)
        
        // ----------------------------------------------------------------------------
        // Interface implementation
        // ----------------------------------------------------------------------------        
        interface IIndexService with
            
            member this.PerformCommandAsync(indexName, indexMessage, replyChannel) = 
                match getIndexRegisteration (state, indexName) with
                | Choice1Of2(index) -> replyChannel.Reply(processItem (state, indexMessage, index, versionCache))
                | Choice2Of2(error) -> replyChannel.Reply(Choice2Of2(error))
            
            member this.PerformCommand(indexName, indexMessage) = 
                maybe { let! index = getIndexRegisteration (state, indexName)
                        return! processItem (state, indexMessage, index, versionCache) }
            member this.CommandQueue() = queue
            
            member this.IndexExists(indexName) = 
                match state.IndexStatus.TryGetValue(indexName) with
                | (true, _) -> true
                | _ -> false
            
            member this.IndexStatus(indexName) = 
                match state.IndexStatus.TryGetValue(indexName) with
                | (true, status) -> Choice1Of2(status)
                | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
            
            member this.GetIndex indexName = 
                match state.IndexStatus.TryGetValue(indexName) with
                | (true, _) -> 
                    match persistanceStore.Get<Index>(indexName) with
                    | Some(a) -> Choice1Of2(a)
                    | None -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
                | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
            
            member this.AddIndex flexIndex = 
                maybe { 
                    match state.IndexStatus.TryGetValue(flexIndex.IndexName) with
                    | (true, _) -> return! Choice2Of2(MessageConstants.INDEX_ALREADY_EXISTS)
                    | _ -> 
                        let! settings = settingsParser.BuildSetting(flexIndex)
                        persistanceStore.Put flexIndex.IndexName flexIndex |> ignore
                        Logger.AddIndex(flexIndex.IndexName, flexIndex)
                        if flexIndex.Online then do! addIndex (state, settings)
                        else do! state.AddStatus(flexIndex.IndexName, IndexState.Offline)
                }
            
            member this.UpdateIndex index = 
                maybe { 
                    let! status = state.GetStatus(index.IndexName)
                    match status with
                    | IndexState.Online -> 
                        let! flexIndex = state.GetRegisteration(index.IndexName)
                        let! settings = settingsParser.BuildSetting(index)
                        closeIndex (state, flexIndex)
                        do! addIndex (state, settings)
                        persistanceStore.Put index.IndexName index |> ignore
                        Logger.AddIndex(index.IndexName,index)
                        return! Choice1Of2()
                    | IndexState.Opening -> return! Choice2Of2(MessageConstants.INDEX_IS_OPENING)
                    | IndexState.Offline | IndexState.Closing -> 
                        let settings = settingsParser.BuildSetting(index)
                        persistanceStore.Put index.IndexName index |> ignore
                        Logger.AddIndex(index.IndexName,index)
                        return! Choice1Of2()
                    | _ -> return! Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
                }
            
            member this.DeleteIndex indexName = 
                maybe { 
                    let! status = state.GetStatus(indexName)
                    match status with
                    | IndexState.Online -> 
                        let! flexIndex = state.GetRegisteration(indexName)
                        closeIndex (state, flexIndex)
                        persistanceStore.Delete<Index> indexName |> ignore
                        state.IndexRegisteration.TryRemove(indexName) |> ignore
                        state.IndexStatus.TryRemove(indexName) |> ignore
                        // It is possible that directory might not exist if the index has never been opened
                        if Directory.Exists(Constants.DataFolder.Value + "\\" + indexName) then 
                            Directory.Delete(flexIndex.IndexSetting.BaseFolder, true)
                        Logger.DeleteIndex(indexName)
                        return! Choice1Of2()
                    | IndexState.Opening -> return! Choice2Of2(MessageConstants.INDEX_IS_OPENING)
                    | IndexState.Offline | IndexState.Closing -> 
                        persistanceStore.Delete<Index> indexName |> ignore
                        state.IndexRegisteration.TryRemove(indexName) |> ignore
                        state.IndexStatus.TryRemove(indexName) |> ignore
                        // It is possible that directory might not exist if the index has never been opened
                        if Directory.Exists(Constants.DataFolder.Value + "\\" + indexName) then 
                            Directory.Delete(Constants.DataFolder.Value + "\\" + indexName, true)
                        Logger.DeleteIndex(indexName)
                        return! Choice1Of2()
                    | _ -> return! Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
                }
            
            member this.CloseIndex indexName = 
                maybe { 
                    let! status = state.GetStatus(indexName)
                    match status with
                    | IndexState.Closing | IndexState.Offline -> 
                        return! Choice2Of2(MessageConstants.INDEX_IS_ALREADY_OFFLINE)
                    | _ -> 
                        let! index = state.GetRegisteration(indexName)
                        closeIndex (state, index)
                        let index' = persistanceStore.Get<Index>(indexName)
                        index'.Value.Online <- false
                        persistanceStore.Put indexName index'.Value |> ignore
                        Logger.CloseIndex(indexName)
                        return! Choice1Of2()
                }
            
            member this.OpenIndex indexName = 
                maybe { 
                    let! status = state.GetStatus(indexName)
                    match status with
                    | IndexState.Online | IndexState.Opening -> return! Choice2Of2(MessageConstants.INDEX_IS_OPENING)
                    | IndexState.Offline | IndexState.Closing -> 
                        let index = persistanceStore.Get<Index>(indexName)
                        let! settings = settingsParser.BuildSetting(index.Value)
                        do! addIndex (state, settings)
                        index.Value.Online <- true
                        persistanceStore.Put indexName index.Value |> ignore
                        Logger.OpenIndex(indexName)
                        return! Choice1Of2()
                    | _ -> return! Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
                }
            
            member this.ShutDown() = 
                Logger.Shutdown()
                for index in state.IndexRegisteration do
                    closeIndex (state, index.Value)
                    Logger.CloseIndex(index.Key)
                true
            
            member this.PerformQuery(indexName, query) = maybe { let! flexIndex = state.GetRegisteration(indexName)
                                                                 return! searchService.Search(flexIndex, query) }
