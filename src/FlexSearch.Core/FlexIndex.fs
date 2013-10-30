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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core.Index
// ----------------------------------------------------------------------------

open FlexSearch.Api.Types
open FlexSearch.Utility
open FlexSearch.Core
open Common.Logging

open java.io
open java.util

open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.util
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.facet.search
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow


// ----------------------------------------------------------------------------
// Contains document building and indexing related operations
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<RequireQualifiedAccess>]
module Document =

    // Method to map a string based id to a lucene shard 
    let mapToShard (id:string) shardCount =
        let mutable total = 0 
        for i in id do
            total <- total + System.Convert.ToInt32(i)
        total % shardCount
    
    // Generates a lucene daocument from a flex document    
    let Generate (document: FlexSearch.Api.Types.Document) flexIndexSetting =
        let luceneDocument = new Document()
        luceneDocument.add(new StringField("id", document.Id, Field.Store.YES))
        luceneDocument.add(new StringField("type", document.Index, Field.Store.YES))
        luceneDocument.add(new LongField("lastmodified", GetCurrentTimeAsLong(), Field.Store.YES))
            
        for field in flexIndexSetting.Fields do
            match document.Fields.TryGetValue(field.FieldName) with
            | (true, value) -> luceneDocument.add(FlexField.CreateLuceneField field value)
            | _ -> luceneDocument.add(FlexField.CreateDefaultLuceneField field)

        luceneDocument    

    // Add a flex document to an index    
    let Add (document: FlexSearch.Api.Types.Document) flexIndex  =
        if (System.String.IsNullOrWhiteSpace(document.Id) = true) then
            failwith "Missing Id"
        let targetIndex = mapToShard document.Id flexIndex.Shards.Length
        let targetDocument = Generate document flexIndex.IndexSetting
        flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(targetDocument)

    // Update a flex document in an index    
    let Update(document: FlexSearch.Api.Types.Document) flexIndex =
        if (System.String.IsNullOrWhiteSpace(document.Id) = true) then
            failwith "Missing Id"
        let targetIndex = mapToShard document.Id flexIndex.Shards.Length
        let targetDocument = Generate document flexIndex.IndexSetting
        flexIndex.Shards.[targetIndex].TrackingIndexWriter.updateDocument(new Term("id",document.Id), targetDocument)

    // Delete a flex document in an index    
    let Delete(id: string) flexIndex  =
        if (System.String.IsNullOrWhiteSpace(id) = true) then
            failwith "Missing Id"
        let targetIndex = mapToShard id flexIndex.Shards.Length
        flexIndex.Shards.[targetIndex].TrackingIndexWriter.deleteDocuments(new Term("id",id)) 


// ----------------------------------------------------------------------------
// Contains lucene writer IO and infracture related operations
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<RequireQualifiedAccess>]
module IO =        
    
    // Logger 
    let logger = LogManager.GetLogger("Settings")  
     
    // ----------------------------------------------------------------------------     
    // Creates lucene index writer config from flex index setting 
    // ---------------------------------------------------------------------------- 
    let GetIndexWriterConfig (flexIndexSetting: FlexIndexSetting) =
        let iwc = new IndexWriterConfig(Constants.LuceneVersion, flexIndexSetting.IndexAnalyzer) 
        iwc.setOpenMode(org.apache.lucene.index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
        iwc.setRAMBufferSizeMB(System.Double.Parse(flexIndexSetting.IndexConfig.RamBufferSizeMb.ToString())) |> ignore
        Some(iwc)


    // ----------------------------------------------------------------------------                  
    // Create a lucene filesystem lock over a directory    
    // ---------------------------------------------------------------------------- 
    let GetIndexDirectory(directoryPath: string, directoryType: DirectoryType) =
        // Note: Might move to SingleInstanceLockFactory to provide other services to open
        // the index in readonly mode
        let lockFactory = new NativeFSLockFactory()
        let file = new java.io.File(directoryPath)
        try
            match directoryType with
            | DirectoryType.FileSystem -> Some(FSDirectory.``open``(file, lockFactory) :> org.apache.lucene.store.Directory)
            | DirectoryType.MemoryMapped -> Some(MMapDirectory.``open``(file, lockFactory) :> org.apache.lucene.store.Directory)
            | DirectoryType.Ram -> Some(new RAMDirectory() :> org.apache.lucene.store.Directory)  
            | _ -> failwithf "Unknown directory type."          
        with
            | e -> logger.Error(sprintf "Unable to open index at location: %s" directoryPath, e); None

    // ---------------------------------------------------------------------------- 
    // Creates lucene index writer from flex index setting  
    // ----------------------------------------------------------------------------                    
    let GetIndexWriter(indexSetting: FlexIndexSetting, directoryPath: string) = 
        let iwc = GetIndexWriterConfig indexSetting
        let indexDirectory = GetIndexDirectory(directoryPath, indexSetting.IndexConfig.DirectoryType)
        let indexWriter = new IndexWriter(indexDirectory.Value, iwc.Value)
        let trackingIndexWriter = new TrackingIndexWriter(indexWriter)
        Some(indexWriter, trackingIndexWriter) 


[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FlexIndex =
    
    // ----------------------------------------------------------------------------   
    // List of exceptions which can be thrown by manager
    // ----------------------------------------------------------------------------   
    let indexAlreadyExistsMessage = "The requested index already exist."
    exception IndexAlreadyExistsException of string

    let indexDoesNotExistMessage = "The requested index does not exist."
    exception IndexDoesNotExistException of string

    let indexIsOfflineMessage = "The index is offline or closing. Please bring the index online to use it."
    exception IndexIsOfflineException of string

    let indexIsOpeningMessage = "The index is in opening state. Please wait some time before making another request."
    exception IndexIsOpeningException of string

    let indexRegisterationMissingMessage = "Registeration information associated with the index is missing."
    exception IndexRegisterationMissingException of string

    // ----------------------------------------------------------------------------   


    // Represents a dummy lucene document. There will be one per index stored in a
    // dictionary
    type threadLocalDocument =
        {
            Document        :   Document
            FieldsLookup    :   Dictionary<string, Field>
            LastGeneration  :   int
        }   


    // ----------------------------------------------------------------------------   
    // Concerete implementation of the index service interface. This class will be 
    // injected using DI thus exposing the necessary
    // functionality at any web service
    // loadAllIndex - This is used to bypass loading of index at initialization time.
    // Helpful for testing
    // ----------------------------------------------------------------------------   
    type IndexService(settingsParser: ISettingsBuilder, searchService: ISearchService, keyValueStore: IKeyValueStore, loadAllIndex: bool) =
        
        let indexLogger = LogManager.GetLogger("IndexService")

        // Dictionary to hold all the information about currently active index and their status
        let indexRegisteration: ConcurrentDictionary<string, FlexIndex>  
            = new ConcurrentDictionary<string, FlexIndex>(StringComparer.OrdinalIgnoreCase)


        // Dictionary to hold the current status of the indices. This is a thread 
        // safe dictionary so it is easier to update it compared to a
        // mutable field on index setting 
        let indexStatus: ConcurrentDictionary<string, IndexState>  
            = new ConcurrentDictionary<string, IndexState>(StringComparer.OrdinalIgnoreCase)


        // ----------------------------------------------------------------------------  
        // For optimal indexing performance, re-use the Field and Document 
        // instance for more than one document. But that is not easily possible
        // in a multi-threaded scenario using TPL dataflow as we don't know which 
        // thread it is using to execute each task. The easiest way
        // is to use ThreadLocal value to create a local copy of the index document.

        // The implication of creating one lucene document class per document to 
        // be indexed is the penalty it has in terms of garbage collection. Also,
        // lucene's document and index classes can't be shared across threads.
        // ----------------------------------------------------------------------------          
        let threadLocalStore: ThreadLocal<ConcurrentDictionary<string , threadLocalDocument>> 
            = new ThreadLocal<ConcurrentDictionary<string , threadLocalDocument>>(fun() -> 
                new ConcurrentDictionary<string , threadLocalDocument>(StringComparer.OrdinalIgnoreCase))


        // ----------------------------------------------------------------------------               
        // Function to check if the requested index is available. If yes then tries to 
        // retrieve the dcument template associated with the index from threadlocal store.
        // If there is no template document for the requested index then goes ahead
        // and creates one. 
        // ----------------------------------------------------------------------------   
        let indexExists(indexName) = 
            match indexRegisteration.TryGetValue(indexName) with
            | (true, flexIndex) -> 
                match threadLocalStore.Value.TryGetValue(indexName) with
                | (true, a) -> Some(flexIndex, a)
                | _ -> 
                    let luceneDocument = new Document()
                    let fieldLookup = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase)
                    
                    let idField = new StringField("id", "", Field.Store.YES)
                    luceneDocument.add(idField)
                    fieldLookup.Add("id", idField)

                    let typeField = new StringField("type", indexName, Field.Store.YES)
                    luceneDocument.add(typeField)
                    fieldLookup.Add("type", typeField)

                    let lastModifiedField = new LongField("lastmodified", GetCurrentTimeAsLong(), Field.Store.YES)
                    luceneDocument.add(lastModifiedField)
                    fieldLookup.Add("lastmodified", lastModifiedField)

                    for field in flexIndex.IndexSetting.Fields do
                        // Ignore these 3 fields here.
                        if (field.FieldName = "id" || field.FieldName = "type" || field.FieldName = "lastmodified") then
                            ()
                        else
                            let defaultField = FlexField.CreateDefaultLuceneField field
                            luceneDocument.add(defaultField)
                            fieldLookup.Add(field.FieldName, defaultField)

                    let documentTemplate = 
                        {
                            Document = luceneDocument
                            FieldsLookup = fieldLookup
                            LastGeneration = 0
                        }

                    threadLocalStore.Value.TryAdd(indexName, documentTemplate) |> ignore
                    Some(flexIndex, documentTemplate)                
            | _ -> None


        // ----------------------------------------------------------------------------     
        // Updates the current thread local index document with the incoming data
        // ----------------------------------------------------------------------------     
        let UpdateDocument(flexIndex: FlexIndex, documentTemplate: threadLocalDocument , documentId: string, fields: Dictionary<string, string>) =            
            documentTemplate.FieldsLookup.["id"].setStringValue(documentId)
            documentTemplate.FieldsLookup.["lastmodified"].setLongValue(GetCurrentTimeAsLong())
            
            for field in flexIndex.IndexSetting.Fields do
                // Ignore these 3 fields here.
                if (field.FieldName = "id" || field.FieldName = "type" || field.FieldName = "lastmodified") then
                    ()
                else
                    // If it is computed field then generate and add it otherwise follow standard path
                    match field.Source with
                    | Some(s) -> 
                        try
                            // Wrong values for the data type will still be handled as update lucene field will
                            // check the data type
                            let value = s fields
                            FlexField.UpdateLuceneField field documentTemplate.FieldsLookup.[field.FieldName] value
                        with
                        | e -> FlexField.UpdateLuceneFieldToDefault field documentTemplate.FieldsLookup.[field.FieldName]

                    | None -> 
                        match fields.TryGetValue(field.FieldName) with
                        | (true, value) -> FlexField.UpdateLuceneField field documentTemplate.FieldsLookup.[field.FieldName] value
                        | _ -> FlexField.UpdateLuceneFieldToDefault field documentTemplate.FieldsLookup.[field.FieldName]
            
            let targetIndex = 
                if(flexIndex.Shards.Length = 1) then 
                    0
                else    
                    Document.mapToShard documentId flexIndex.Shards.Length

            (flexIndex, targetIndex, documentTemplate)
            
            
        // ----------------------------------------------------------------------------     
        // Function to process the 
        // ----------------------------------------------------------------------------                                         
        let processItem(indexMessage: IndexCommand, flexIndex: FlexIndex) = 
            match indexMessage with
            | Create(documentId, fields) -> 
                match indexExists(flexIndex.IndexSetting.IndexName) with
                | Some(flexIndex, documentTemplate) -> 
                    let (flexIndex, targetIndex, documentTemplate) = UpdateDocument(flexIndex, documentTemplate, documentId, fields)
                    flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) |> ignore
                    (true, "")
                | _ -> (false, "Index does not exist")

            | Update(documentId, fields) -> 
                match indexExists(flexIndex.IndexSetting.IndexName) with
                | Some(flexIndex, documentTemplate) -> 
                    let (flexIndex, targetIndex, documentTemplate) = UpdateDocument(flexIndex, documentTemplate, documentId, fields)
                    flexIndex.Shards.[targetIndex].TrackingIndexWriter.updateDocument(new Term("id",documentId), documentTemplate.Document) |> ignore
                    (true, "")
                | _ -> (false, "Index does not exist")

            | Delete(documentId) -> 
                let targetIndex = Document.mapToShard documentId flexIndex.Shards.Length - 1
                flexIndex.Shards.[targetIndex].TrackingIndexWriter.deleteDocuments(new Term("id",documentId))  |> ignore
                (true, "")

            | BulkDeleteByIndexName -> 
                for shard in flexIndex.Shards do
                    shard.TrackingIndexWriter.deleteAll()  |> ignore
                (true, "")
                
            | Commit -> 
                for i in 0 .. flexIndex.Shards.Length - 1 do
                    flexIndex.Shards.[i].IndexWriter.commit() 
                (true, "")


        // Default buffering queue
        // This is TPL Dataflow based approach. Can replace it with parallel.foreach
        // on blocking collection. 
        // Advantages - Faster, EnumerablePartitionerOptions.NoBuffering takes care of the
        // older .net partitioner bug, Can reduce the number of lucene documents which will be
        // generated 
        let mutable queue : ActionBlock<string * IndexCommand> = null

        // Index auto commit changes job
        let commitJob(flexIndex: FlexIndex) = 
            // Looping over array by index number is usually the fastest
            // iteration method
            for i in 0 .. flexIndex.Shards.Length - 1 do
                // Lucene 4.4.0 feature to check for uncommitted changes
                if flexIndex.Shards.[i].IndexWriter.hasUncommittedChanges() then
                    flexIndex.Shards.[i].IndexWriter.commit()                

        // Index auto commit changes job
        let refreshIndexJob(flexIndex) = 
            // Looping over array by index number is usually the fastest
            // iteration method
            for i in 0 .. flexIndex.Shards.Length - 1 do
                flexIndex.Shards.[i].NRTManager.maybeRefresh() |> ignore    

        // Creates a async timer which can be used to execute a funtion at specified
        // period of time. This is used to schedule all recurring indexing tasks
        let ScheduleIndexJob delay (work : FlexIndex -> unit) flexIndex =
            let rec loop time (cts: CancellationTokenSource) = async { 
                do! Async.Sleep(time)
                if (cts.IsCancellationRequested) then cts.Dispose()
                else work(flexIndex)
                return! loop delay cts}
            loop delay flexIndex.Token 

        // Add index to the registeration
        let addIndex (flexIndexSetting: FlexIndexSetting) =    
            let flexIndexConfig = flexIndexSetting.IndexConfig
            
            // Add index status
            indexStatus.TryAdd(flexIndexSetting.IndexName, IndexState.Opening) |> ignore

            // Initialize shards
            let shards = Array.init flexIndexConfig.Shards (fun a -> 
                let writers = GetIndexWriter(flexIndexSetting, flexIndexSetting.BaseFolder + "\\shards\\" + a.ToString())
                if writers.IsNone then 
                    logger.Error("Unable to create the requested index writer.")
                    failwith "Unable to create the requested index writer."
                
                let (indexWriter, trackingIndexWriter) = writers.Value

                // Based on Lucene 4.4 the nrtmanager is replaced with ControlledRealTimeReopenThread which can take any
                // reference manager
                let nrtManager = new SearcherManager(indexWriter, true, new SearcherFactory())
                let shard = 
                    {
                        ShardNumber = a
                        NRTManager = nrtManager
                        ReopenThread = new ControlledRealTimeReopenThread(trackingIndexWriter, nrtManager, float(25), float(5)) 
                        IndexWriter = indexWriter
                        TrackingIndexWriter = trackingIndexWriter
                    }
                
                shard
                ) 
            
            let flexIndex = {
                IndexSetting  =  flexIndexSetting
                Shards = shards
                Token = new System.Threading.CancellationTokenSource() 
            }

            // Add the scheduler for the index
            // Commit Scheduler
            Async.Start(ScheduleIndexJob (flexIndexConfig.CommitTimeSec * 1000) commitJob flexIndex)
            
            // NRT Scheduler
            Async.Start(ScheduleIndexJob flexIndexConfig.RefreshTimeMilliSec refreshIndexJob flexIndex)

            // Add the index to the registeration
            indexRegisteration.TryAdd(flexIndexSetting.IndexName, flexIndex) |> ignore                 
            indexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Online
            ()


        // ----------------------------------------------------------------------------
        // Close an open index
        // ----------------------------------------------------------------------------
        let closeIndex (flexIndex: FlexIndex) =
            try
                indexRegisteration.TryRemove(flexIndex.IndexSetting.IndexName) |> ignore

                // Update status from online to closing
                indexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Closing

                flexIndex.Token.Cancel()
                flexIndex.Shards |> Array.iter(fun x -> 
                    x.NRTManager.close()
                    x.IndexWriter.commit()
                    x.IndexWriter.close()
                    )
            with
            | e -> logger.Error("Error while closing index:" + flexIndex.IndexSetting.IndexName, e)
            
            indexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Offline
        

        // ----------------------------------------------------------------------------
        // Utility method to return index registeration information
        // ----------------------------------------------------------------------------
        let getIndexRegisteration(indexName) =
            match indexStatus.TryGetValue(indexName) with
            | (true, status) ->
                match status with
                | IndexState.Online ->
                    match indexRegisteration.TryGetValue(indexName) with
                    | (true, flexIndex) -> flexIndex
                    | _ -> raise(IndexRegisterationMissingException indexRegisterationMissingMessage)
                | IndexState.Opening ->
                    raise(IndexIsOpeningException indexIsOpeningMessage)
                | IndexState.Offline 
                | IndexState.Closing ->
                    raise(IndexIsOfflineException indexIsOfflineMessage)
            | _ -> raise(IndexDoesNotExistException  indexDoesNotExistMessage)


        // Process index queue requests
        let processQueueItem(indexName, indexMessage: IndexCommand) = 
            let flexIndex = getIndexRegisteration(indexName)
            processItem(indexMessage, flexIndex) |> ignore
        
        // ----------------------------------------------------------------------------
        // Load all index configuration data on start of application
        // ----------------------------------------------------------------------------
        do
            indexLogger.Info("Index loading: Operation Start")
            let executionBlockOption = new ExecutionDataflowBlockOptions()
            executionBlockOption.MaxDegreeOfParallelism <- -1
            executionBlockOption.BoundedCapacity <- 1000
            queue <- new ActionBlock<string * IndexCommand>(processQueueItem, executionBlockOption)
            
            if loadAllIndex then
                keyValueStore.GetAllIndexSettings()
                |> Seq.iter(fun x ->
                    if x.Online then
                        try
                            let flexIndexSetting = settingsParser.BuildSetting(x)
                            addIndex(flexIndexSetting) 
                            indexLogger.Info(sprintf "Index: %s loaded successfully." x.IndexName)
                         with
                            | ex -> 
                                indexLogger.Error("Loading index from file failed.", ex)
                    else
                        indexLogger.Info(sprintf "Index: %s is not loaded as it is set to be offline." x.IndexName)
                        indexStatus.TryAdd(x.IndexName, IndexState.Offline) |> ignore    
                )                
            else
                indexLogger.Info("Index loading bypassed. LoadIndex parameter is false. (Testing mode)")
            

        // ----------------------------------------------------------------------------
        // Interface implementation
        // ----------------------------------------------------------------------------
        interface IIndexService with
            member this.PerformCommandAsync(indexName, indexMessage, replyChannel) = 
                let flexIndex = getIndexRegisteration(indexName)
                replyChannel.Reply(processItem(indexMessage, flexIndex))
                

            member this.PerformCommand(indexName, indexMessage) = 
                let flexIndex = getIndexRegisteration(indexName)
                processItem(indexMessage, flexIndex)
                
            member this.CommandQueue() = queue
//            member this.SendCommandToQueue(indexName, indexMessage) =  
//                let flexIndex = getIndexRegisteration(indexName)
//                Async.AwaitTask(queue.SendAsync((indexMessage, flexIndex)))
                

            member this.PerformQuery(indexName, indexQuery) =
                let flexIndex = getIndexRegisteration(indexName)      
                match indexQuery with
                | SearchQuery(a) -> searchService.Search(flexIndex, a)
                | SearchProfileQuery(a) -> searchService.SearchProfile(flexIndex, a)


            member this.PerformQueryAsync(indexName, indexQuery, replyChannel) =
                let flexIndex = getIndexRegisteration(indexName)           
                match indexQuery with
                | SearchQuery(a) -> replyChannel.Reply(searchService.Search(flexIndex, a))
                | SearchProfileQuery(a) -> replyChannel.Reply(searchService.SearchProfile(flexIndex, a))


            member this.IndexExists(indexName) =
                match indexStatus.TryGetValue(indexName) with
                | (true, _) -> true
                | _ -> false


            member this.IndexStatus(indexName) = 
                match indexStatus.TryGetValue(indexName) with
                | (true, status) -> status
                | _ -> raise(IndexDoesNotExistException  indexDoesNotExistMessage)
            

            member this.AddIndex flexIndex = 
                match indexStatus.TryGetValue(flexIndex.IndexName) with
                | (true, _) -> raise(IndexAlreadyExistsException indexAlreadyExistsMessage)
                | _ ->
                    let settings = settingsParser.BuildSetting(flexIndex)
                    keyValueStore.UpdateIndexSetting(flexIndex)  
                    if flexIndex.Online then
                        addIndex(settings)  
                    else
                        indexStatus.TryAdd(flexIndex.IndexName, IndexState.Offline) |> ignore
                ()


            member this.UpdateIndex index = 
                match indexStatus.TryGetValue(index.IndexName) with
                | (true, status) ->
                    match status with
                    | IndexState.Online ->
                        match indexRegisteration.TryGetValue(index.IndexName) with
                        | (true, flexIndex) -> 
                            let settings = settingsParser.BuildSetting(index)
                            closeIndex(flexIndex)
                            addIndex(settings)
                            keyValueStore.UpdateIndexSetting(index)  |> ignore     
                        | _ -> raise(IndexRegisterationMissingException indexRegisterationMissingMessage)

                    | IndexState.Opening ->
                        raise(IndexIsOpeningException indexIsOpeningMessage)
                    | IndexState.Offline 
                    | IndexState.Closing ->
                        let settings = settingsParser.BuildSetting(index)
                        keyValueStore.UpdateIndexSetting(index)  |> ignore     
                | _ -> raise(IndexDoesNotExistException  indexDoesNotExistMessage)
                

            member this.DeleteIndex indexName = 
                match indexStatus.TryGetValue(indexName) with
                | (true, status) ->
                    match status with
                    | IndexState.Online ->
                        match indexRegisteration.TryGetValue(indexName) with
                        | (true, flexIndex) -> 
                            closeIndex(flexIndex) 
                            keyValueStore.DeleteIndexSetting(indexName)
                            // It is possible that directory might not exist if the index has never been opened
                            if Directory.Exists(Constants.DataFolder.Value + "\\" + indexName) then
                                Directory.Delete(flexIndex.IndexSetting.BaseFolder, true)

                        | _ -> raise(IndexRegisterationMissingException indexRegisterationMissingMessage)

                    | IndexState.Opening ->
                        raise(IndexIsOpeningException indexIsOpeningMessage)
                    | IndexState.Offline 
                    | IndexState.Closing -> 
                        keyValueStore.DeleteIndexSetting(indexName)
                        
                        // It is possible that directory might not exist if the index has never been opened
                        if Directory.Exists(Constants.DataFolder.Value + "\\" + indexName) then
                            Directory.Delete(Constants.DataFolder.Value + "\\" + indexName, true)

                | _ -> raise(IndexDoesNotExistException  indexDoesNotExistMessage)


            member this.CloseIndex indexName = 
                let flexIndex = getIndexRegisteration(indexName) 
                closeIndex(flexIndex)
                let index = keyValueStore.GetIndexSetting(indexName) 
                index.Value.Online <- false
                keyValueStore.UpdateIndexSetting(index.Value) |> ignore


            member this.OpenIndex indexName = 
                match indexStatus.TryGetValue(indexName) with
                    | (true, status) ->
                        match status with
                        | IndexState.Online
                        | IndexState.Opening ->
                            raise(IndexIsOpeningException indexIsOpeningMessage)
                        | IndexState.Offline 
                        | IndexState.Closing ->
                            let index = keyValueStore.GetIndexSetting(indexName)
                            let settings = settingsParser.BuildSetting(index.Value)
                            let res = addIndex(settings)  
                            index.Value.Online <- true
                            keyValueStore.UpdateIndexSetting(index.Value)  |> ignore    
                    | _ -> raise(IndexDoesNotExistException  indexDoesNotExistMessage)
                

            member this.ShutDown() = 
                for index in indexRegisteration do
                    closeIndex(index.Value)
                true

