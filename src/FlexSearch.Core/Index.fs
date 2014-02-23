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
open FlexSearch.Api.Exception
open FlexSearch.Core
open FlexSearch.Core.State
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
open org.apache.lucene.facet.search
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Index = 
    let maybe = new ValidationBuilder()
    
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
                else work (flexIndex)
                return! loop delay cts
            }
        loop delay flexIndex.Token
    
    // Add index to the registeration
    let private addIndex (state : IndicesState, flexIndexSetting : FlexIndexSetting) = 
        // Add index status
        state.IndexStatus.TryAdd(flexIndexSetting.IndexName, IndexState.Opening) |> ignore
        // Initialize shards
        let shards = 
            Array.init flexIndexSetting.ShardConfiguration.ShardCount (fun a -> 
                let writers = 
                    IO.GetIndexWriter(flexIndexSetting, flexIndexSetting.BaseFolder + "\\shards\\" + a.ToString())
                if writers.IsNone then 
                    //logger.Error("Unable to create the requested index writer.")
                    failwith "Unable to create the requested index writer."
                let (indexWriter, trackingIndexWriter) = writers.Value
                // Based on Lucene 4.4 the nrtmanager is replaced with ControlledRealTimeReopenThread which can take any
                // reference manager
                let nrtManager = new SearcherManager(indexWriter, true, new SearcherFactory())
                
                let shard = 
                    { ShardNumber = a
                      NRTManager = nrtManager
                      ReopenThread = 
                          new ControlledRealTimeReopenThread(trackingIndexWriter, nrtManager, float (25), float (5))
                      IndexWriter = indexWriter
                      TrackingIndexWriter = trackingIndexWriter }
                shard)
        
        let flexIndex = 
            { IndexSetting = flexIndexSetting
              Shards = shards
              Token = new System.Threading.CancellationTokenSource() }
        
        // Add the scheduler for the index
        // Commit Scheduler
        Async.Start(scheduleIndexJob (flexIndexSetting.IndexConfiguration.CommitTimeSec * 1000) commitJob flexIndex)
        // NRT Scheduler
        Async.Start(scheduleIndexJob flexIndexSetting.IndexConfiguration.RefreshTimeMilliSec refreshIndexJob flexIndex)
        // Add the index to the registeration
        state.IndexRegisteration.TryAdd(flexIndexSetting.IndexName, flexIndex) |> ignore
        state.IndexStatus.[flexIndex.IndexSetting.IndexName] <- IndexState.Online
    
    let private loadAllIndex (state : IndicesState, persistanceStore : IPersistanceStore, 
                              flexIndexSetting : FlexIndexSetting) = 
        for x in persistanceStore.GetAll<Index>() do
            if x.Online then 
                try 
                    let flexIndexSetting = ServiceLocator.SettingsBuilder.BuildSetting(x)
                    addIndex (state, flexIndexSetting)
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
            flexIndex.Shards |> Array.iter (fun x -> 
                                    x.NRTManager.close()
                                    x.IndexWriter.commit()
                                    x.IndexWriter.close())
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
                | (true, flexIndex) -> flexIndex
                | _ -> raise (IndexRegisterationMissingException indexRegisterationMissingMessage)
            | IndexState.Opening -> raise (IndexIsOpeningException indexIsOpeningMessage)
            | IndexState.Offline | IndexState.Closing -> raise (IndexIsOfflineException indexIsOfflineMessage)
        | _ -> raise (IndexDoesNotExistException indexDoesNotExistMessage)
    
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
                let versionField = new IntField(Constants.VersionField, 0, Field.Store.YES)
                luceneDocument.add (typeField)
                fieldLookup.Add(Constants.VersionField, typeField)
                let lastModifiedField = 
                    new LongField(Constants.LastModifiedField, GetCurrentTimeAsLong(), Field.Store.YES)
                luceneDocument.add (lastModifiedField)
                fieldLookup.Add(Constants.LastModifiedField, lastModifiedField)
                for field in flexIndex.IndexSetting.Fields do
                    // Ignore these 4 fields here.
                    if (field.FieldName = Constants.IdField || field.FieldName = Constants.TypeField 
                        || field.FieldName = Constants.LastModifiedField || field.FieldName = Constants.VersionField) then 
                        ()
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
        | _ -> Choice2Of2(ExceptionConstants.INDEX_NOT_FOUND)
    
    // ----------------------------------------------------------------------------     
    // Updates the current thread local index document with the incoming data
    // ----------------------------------------------------------------------------     
    let private updateDocument (flexIndex : FlexIndex, documentTemplate : ThreadLocalDocument, documentId : string, 
                                version : int, fields : Dictionary<string, string>) = 
        documentTemplate.FieldsLookup.[Constants.IdField].setStringValue(documentId)
        documentTemplate.FieldsLookup.[Constants.LastModifiedField].setLongValue(GetCurrentTimeAsLong())
        documentTemplate.FieldsLookup.[Constants.VersionField].setIntValue(version)
        for field in flexIndex.IndexSetting.Fields do
            // Ignore these 3 fields here.
            if (field.FieldName = Constants.IdField || field.FieldName = Constants.TypeField 
                || field.FieldName = Constants.LastModifiedField || field.FieldName = Constants.VersionField) then ()
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
                versionCache.AddVersion flexIndex.IndexSetting.IndexName documentId 1 |> ignore
                flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) |> ignore
                return! Choice1Of2()
            | Update(documentId, fields) -> 
                // It is a simple update so get the version number and increment it
                match versionCache.GetVersion flexIndex.IndexSetting.IndexName documentId with
                | Some(x) -> 
                    let (version, dateTime) = x
                    match versionCache.UpdateVersion flexIndex.IndexSetting.IndexName documentId version dateTime 
                                (version + 1) with
                    | true -> 
                        // Version was updated successfully so let's update the document
                        let (flexIndex, targetIndex, documentTemplate) = 
                            UpdateDocument(flexIndex, documentTemplate, documentId, (version + 1), fields)
                        flexIndex.Shards.[targetIndex]
                            .TrackingIndexWriter.updateDocument(new Term("id", documentId), 
                                                                documentTemplate.Document) |> ignore
                    | false -> failwithf "Version mismatch"
                | None -> 
                    // Document was not found in version cache so retrieve it from the index
                    let (flexIndex, targetIndex, documentTemplate) = 
                        UpdateDocument(flexIndex, documentTemplate, documentId, 1, fields)
                    let query = new TermQuery(new Term("id", documentId))
                    let searcher = 
                        (flexIndex.Shards.[targetIndex].NRTManager :> ReferenceManager).acquire() :?> IndexSearcher
                    let topDocs = searcher.search (query, 1)
                    let hits = topDocs.scoreDocs
                    if hits.Length = 0 then 
                        // It is actually a create as the document does not exist
                        versionCache.AddVersion flexIndex.IndexSetting.IndexName documentId 1 |> ignore
                        flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) 
                        |> ignore
                    else 
                        let version = int (searcher.doc(hits.[0].doc).get("version"))
                        versionCache.AddVersion flexIndex.IndexSetting.IndexName documentId (version + 1) |> ignore
                        flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) 
                        |> ignore
                (true, "")         
            | _ -> return! Choice2Of2(ExceptionConstants.INDEX_NOT_FOUND)
        }
//        | Update(documentId, fields) -> 
//            match indexExists (state, flexIndex.IndexSetting.IndexName) with
//            | Some(flexIndex, documentTemplate) -> 
//                // It is a simple update so get the version number and increment it
//                match versionCache.GetVersion flexIndex.IndexSetting.IndexName documentId with
//                | Some(x) -> 
//                    let (version, dateTime) = x
//                    match versionCache.UpdateVersion flexIndex.IndexSetting.IndexName documentId version dateTime 
//                              (version + 1) with
//                    | true -> 
//                        // Version was updated successfully so let's update the document
//                        let (flexIndex, targetIndex, documentTemplate) = 
//                            updateDocument (flexIndex, documentTemplate, documentId, (version + 1), fields)
//                        flexIndex.Shards.[targetIndex]
//                            .TrackingIndexWriter.updateDocument(new Term("id", documentId), documentTemplate.Document) 
//                        |> ignore
//                    | false -> failwithf "Version mismatch"
//                | None -> 
//                    // Document was not found in version cache so retrieve it from the index
//                    let (flexIndex, targetIndex, documentTemplate) = 
//                        updateDocument (flexIndex, documentTemplate, documentId, 1, fields)
//                    let query = new TermQuery(new Term("id", documentId))
//                    let searcher = 
//                        (flexIndex.Shards.[targetIndex].NRTManager :> ReferenceManager).acquire() :?> IndexSearcher
//                    let topDocs = searcher.search (query, 1)
//                    let hits = topDocs.scoreDocs
//                    if hits.Length = 0 then 
//                        // It is actually a create as the document does not exist
//                        versionCache.AddVersion flexIndex.IndexSetting.IndexName documentId 1 |> ignore
//                        flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) 
//                        |> ignore
//                    else 
//                        let version = int (searcher.doc(hits.[0].doc).get("version"))
//                        versionCache.AddVersion flexIndex.IndexSetting.IndexName documentId (version + 1) |> ignore
//                        flexIndex.Shards.[targetIndex].TrackingIndexWriter.addDocument(documentTemplate.Document) 
//                        |> ignore
//                (true, "")
//            | _ -> (false, "Index does not exist")
//        | Delete(documentId) -> 
//            let targetIndex = Document.mapToShard documentId flexIndex.Shards.Length - 1
//            versionCache.DeleteVersion flexIndex.IndexSetting.IndexName documentId |> ignore
//            flexIndex.Shards.[targetIndex].TrackingIndexWriter.deleteDocuments(new Term("id", documentId)) |> ignore
//            (true, "")
//        | BulkDeleteByIndexName -> 
//            for shard in flexIndex.Shards do
//                shard.TrackingIndexWriter.deleteAll() |> ignore
//            (true, "")
//        | Commit -> 
//            for i in 0..flexIndex.Shards.Length - 1 do
//                flexIndex.Shards.[i].IndexWriter.commit()
//            (true, "")
