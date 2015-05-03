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

open FlexLucene.Analysis
open FlexSearch.Core
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime.Caching
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

/// General factory Interface for all MEF based factories
type IFlexFactory<'T> = 
    abstract GetModuleByName : string -> Choice<'T, Error>
    abstract ModuleExists : string -> bool
    abstract GetAllModules : unit -> Dictionary<string, 'T>
    abstract GetMetaData : string -> Choice<IDictionary<string, obj>, Error>

/// Index related operations
type IIndexService = 
    abstract GetIndex : indexName:string -> Choice<Index.Dto, Error>
    abstract UpdateIndexFields : fields:Field.Dto [] -> Choice<unit, Error>
    abstract DeleteIndex : indexName:string -> Choice<unit, Error>
    abstract AddIndex : index:Index.Dto -> Choice<CreateResponse, Error>
    abstract GetAllIndex : unit -> Index.Dto array
    abstract IndexExists : indexName:string -> bool
    abstract IndexOnline : indexName:string -> bool
    abstract IsIndexOnline : indexName:string -> Choice<IndexWriter.T, Error>
    abstract GetIndexState : indexName:string -> Choice<IndexState, Error>
    abstract OpenIndex : indexName:string -> Choice<unit, Error>
    abstract CloseIndex : indexName:string -> Choice<unit, Error>
    abstract Commit : indexName:string -> Choice<unit, Error>
    abstract Refresh : indexName:string -> Choice<unit, Error>
    abstract GetRealtimeSearchers : indexName:string -> Choice<array<RealTimeSearcher>, Error>
    abstract GetRealtimeSearcher : indexName:string * int -> Choice<RealTimeSearcher, Error>

/// Document related operations
type IDocumentService = 
    abstract GetDocument : indexName:string * id:string -> Choice<Document.Dto, Error>
    abstract GetDocuments : indexName:string * count:int -> Choice<SearchResults, Error>
    abstract AddOrUpdateDocument : document:Document.Dto -> Choice<unit, Error>
    abstract DeleteDocument : indexName:string * id:string -> Choice<unit, Error>
    abstract AddDocument : document:Document.Dto -> Choice<CreateResponse, Error>
    abstract DeleteAllDocuments : indexName:string -> Choice<unit, Error>
    abstract TotalDocumentCount : indexName:string -> Choice<int, Error>

/// Search related operations
type ISearchService = 
    abstract Search : searchQuery:SearchQuery.Dto * inputFields:Dictionary<string, string>
     -> Choice<SearchResults<SearchResultComponents.T>, Error>
    abstract Search : searchQuery:SearchQuery.Dto -> Choice<SearchResults<SearchResultComponents.T>, Error>

/// Queuing related operations
type IQueueService = 
    abstract AddDocumentQueue : document:Document.Dto -> unit
    abstract AddOrUpdateDocumentQueue : document:Document.Dto -> unit

type IJobService = 
    abstract GetJob : string -> Choice<Job, Error>
    abstract DeleteAllJobs : unit -> Choice<unit, Error>
    abstract UpdateJob : Job -> Choice<unit, Error>

///  Analyzer/Analysis related services
type IAnalyzerService = 
    abstract GetAnalyzer : analyzerName:string -> Choice<Analyzer, Error>
    abstract GetAnalyzerInfo : analyzerName:string -> Choice<Analyzer.Dto, Error>
    abstract DeleteAnalyzer : analyzerName:string -> Choice<unit, Error>
    abstract UpdateAnalyzer : analyzer:Analyzer.Dto -> Choice<unit, Error>
    abstract GetAllAnalyzers : unit -> Analyzer.Dto []
    abstract Analyze : analyzerName:string * input:string -> Choice<string, Error>

[<Sealed>]
type AnalyzerService(threadSafeWriter : ThreadSafeFileWriter, ?testMode : bool) = 
    let testMode = defaultArg testMode true
    
    let path = 
        Constants.ConfFolder +/ "Analyzer"
        |> Directory.CreateDirectory
        |> fun x -> x.FullName
    
    let store = conDict<Analyzer.Dto * Analyzer>()
    
    let updateAnalyzer (analyzer : Analyzer.Dto) = 
        maybe { 
            do! analyzer.Validate()
            let! instance = Analyzer.build (analyzer)
            do! threadSafeWriter.WriteFile(path +/ analyzer.AnalyzerName, analyzer)
            do! store
                |> tryUpdate (analyzer.AnalyzerName, (analyzer, instance))
                |> boolToResult UnableToUpdateMemory
        }
    
    let loadAllAnalyzers() = 
        Directory.EnumerateFiles(path) |> Seq.iter (fun x -> 
                                              match threadSafeWriter.ReadFile<Analyzer.Dto>(x) with
                                              | Choice1Of2(dto) -> updateAnalyzer (dto) |> ignore
                                              | Choice2Of2(error) -> ())
    
    do 
        // Add prebuilt analyzers
        let standardAnalyzer = new Analyzer.Dto(AnalyzerName = "standard")
        let instance = new FlexLucene.Analysis.Standard.StandardAnalyzer() :> Analyzer
        store |> add ("standard", (standardAnalyzer, instance))
        store |> add ("keyword", (new Analyzer.Dto(AnalyzerName = "keyword"), CaseInsensitiveKeywordAnalyzer))
        if not testMode then loadAllAnalyzers()
    
    interface IAnalyzerService with
        
        /// Create or update an existing analyzer
        member __.UpdateAnalyzer(analyzer : Analyzer.Dto) = updateAnalyzer (analyzer)
        
        /// Delete an analyzer. This 
        member __.DeleteAnalyzer(analyzerName : string) = 
            maybe { 
                match store.TryGetValue(analyzerName) with
                | true, _ -> 
                    do! threadSafeWriter.DeleteFile(path +/ analyzerName)
                    do! store
                        |> tryRemove (analyzerName)
                        |> boolToResult UnableToUpdateMemory
                    return! ok()
                | _ -> return! ok()
            }
        
        member __.Analyze(analyzerName : string, input : string) = failwith "Not implemented yet"
        member __.GetAllAnalyzers() = store.Values.ToArray() |> Array.map fst
        
        member __.GetAnalyzer(analyzerName : string) = 
            match store.TryGetValue(analyzerName) with
            | true, (_, instance) -> ok <| instance
            | _ -> fail <| AnalyzerNotFound(analyzerName)
        
        member __.GetAnalyzerInfo(analyzerName : string) = 
            match store.TryGetValue(analyzerName) with
            | true, (dto, _) -> ok <| dto
            | _ -> fail <| AnalyzerNotFound(analyzerName)

[<Sealed>]
type IndexService(threadSafeWriter : ThreadSafeFileWriter, analyzerService : IAnalyzerService, ?testMode : bool) = 
    let testMode = defaultArg testMode true
    
    /// State information related to all the indices present in the
    /// system. An index can exist but be offline. In that case there
    /// won't be any associated index writer
    let state = conDict<Index.Dto * IndexWriter.T option>()
    
    /// Store to save all the index related information to the physical
    /// medium
    let dtoStore = new DtoStore<Index.Dto>(threadSafeWriter)
    
    /// Returns IndexNotFound error
    let indexNotFound (indexName) = IndexNotFound <| indexName
    
    /// Check if the given index exists
    let indexExists (indexName) = 
        match state.TryGetValue(indexName) with
        | true, _ -> ok()
        | _ -> fail <| indexNotFound indexName
    
    /// Checks if a given index is online or not. If it is 
    /// online then return the index writer
    let indexOnline (indexName) = 
        match state.TryGetValue(indexName) with
        | true, (_, writerOption) -> 
            match writerOption with
            | Some(writer) -> 
                match writer.State with
                | IndexState.Online -> ok <| writer
                | _ -> fail <| IndexShouldBeOnline indexName
            | None -> fail <| IndexShouldBeOnline indexName
        | _ -> fail <| indexNotFound indexName
    
    let indexState (indexName) = 
        match state.TryGetValue(indexName) with
        | true, (_, writerOption) -> 
            match writerOption with
            | Some(writer) -> ok <| (writer.State, Some(writer))
            | None -> ok <| (IndexState.Offline, None)
        | _ -> fail <| indexNotFound indexName
    
    /// Load a index
    let loadIndex (index : Index.Dto) = 
        maybe { 
            let! setting = IndexWriter.createIndexSetting (index, analyzerService.GetAnalyzer)
            if index.Online then 
                let indexWriter = IndexWriter.create (setting)
                state
                |> tryUpdate (index.IndexName, (index, Some(indexWriter)))
                |> ignore
            else 
                state
                |> tryUpdate (index.IndexName, (index, None))
                |> ignore
        }
    
    /// Load all index from the store
    let loadAllIndex() = 
        dtoStore.GetItems()
        |> Seq.map (fun i -> Task.Run(fun _ -> loadIndex i |> ignore))
        |> Seq.toArray
        |> Task.WaitAll
    
    do 
        if not testMode then loadAllIndex()
    
    interface IIndexService with
        member __.IsIndexOnline(indexName : string) = indexOnline (indexName)
        
        member __.AddIndex(index : Index.Dto) = 
            maybe { 
                do! index.Validate()
                match indexExists index.IndexName with
                | Choice1Of2(_) -> return! fail <| IndexAlreadyExists(index.IndexName)
                | _ -> 
                    do! dtoStore.UpdateItem(index.IndexName, index)
                    do! loadIndex <| index
                    return CreateResponse(index.IndexName)
            }
        
        member __.CloseIndex(indexName : string) = 
            maybe { 
                let! (indexState, indexWriter) = indexState indexName
                match indexState with
                | IndexState.Closing | IndexState.Offline -> return! fail <| IndexIsAlreadyOffline(indexName)
                | _ -> 
                    let item = fst state.[indexName]
                    item.Online <- false
                    do! dtoStore.UpdateItem(indexName, item)
                    indexWriter.Value |> IndexWriter.close
                    state
                    |> tryUpdate (indexName, (item, None))
                    |> ignore
            }
        
        member __.OpenIndex(indexName : string) = 
            maybe { 
                let! (indexState, indexWriter) = indexState indexName
                match indexState with
                | IndexState.Opening | IndexState.Online -> return! fail <| IndexIsAlreadyOnline(indexName)
                | _ -> 
                    let item = fst state.[indexName]
                    item.Online <- true
                    do! dtoStore.UpdateItem(indexName, item)
                    do! loadIndex item
            }
        
        member __.Commit(indexName : string) = maybe { let! writer = indexOnline indexName
                                                       writer |> IndexWriter.commit }
        member __.Refresh(indexName : string) = maybe { let! writer = indexOnline indexName
                                                        writer |> IndexWriter.refresh }
        member __.GetRealtimeSearcher(indexName : string, shardNo : int) = 
            maybe { let! writer = indexOnline indexName
                    return writer |> IndexWriter.getRealTimeSearcher shardNo }
        member __.GetRealtimeSearchers(indexName : string) = maybe { let! writer = indexOnline indexName
                                                                     return writer |> IndexWriter.getRealTimeSearchers }
        
        member __.GetIndex(indexName : string) = 
            match dtoStore.GetItem(indexName) with
            | Choice1Of2(_) as success -> success
            | Choice2Of2(_) -> fail <| Error.IndexNotFound indexName
        
        member __.IndexExists(indexName : string) = dtoStore.GetItem(indexName) |> resultToBool
        member __.IndexOnline(indexName : string) = indexOnline indexName |> resultToBool
        
        member __.GetIndexState(indexName : string) = 
            match indexState indexName with
            | Choice1Of2(state, _) -> ok <| state
            | Choice2Of2(error) -> fail <| error
        
        member __.DeleteIndex(indexName : string) = 
            maybe { 
                let! (_, writerOption) = indexState indexName
                match writerOption with
                | Some(writer) -> writer |> IndexWriter.close
                | None -> ()
                state.TryRemove(indexName) |> ignore
                do! dtoStore.DeleteItem(indexName)
                delDir (DataFolder +/ indexName)
            }
        
        member __.GetAllIndex() = dtoStore.GetItems()
        member __.UpdateIndexFields(fields : Field.Dto []) = failwith "Not implemented yet"

[<Sealed>]
type SearchService(parser : IFlexParser, queryFactory : IFlexFactory<IFlexQuery>, indexService : IIndexService) = 
    
    // Generate query types from query factory. This is necessary as a single query can support multiple
    // query names
    let queryTypes = 
        let result = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
        for pair in queryFactory.GetAllModules() do
            for queryName in pair.Value.QueryName() do
                result.Add(queryName, pair.Value)
        result
    
    let getSearchPredicate (writers : IndexWriter.T, search : SearchQuery.Dto, 
                            inputValues : Dictionary<string, string> option) = 
        maybe { 
            if String.IsNullOrWhiteSpace(search.SearchProfile) <> true then 
                // Search profile based
                match writers.Settings.SearchProfiles.TryGetValue(search.SearchProfile) with
                | true, p -> 
                    let (p', sq) = p
                    search.MissingValueConfiguration <- sq.MissingValueConfiguration
                    let! values = match inputValues with
                                  | Some(values) -> Choice1Of2(values)
                                  | None -> Parsers.ParseQueryString(search.QueryString, false)
                    return (p', Some(values))
                | _ -> return! fail <| UnknownSearchProfile(search.IndexName, search.SearchProfile)
            else let! predicate = parser.Parse(search.QueryString)
                 return (predicate, None)
        }
    
    let generateSearchQuery (writers : IndexWriter.T, searchQuery : SearchQuery.Dto, 
                             inputValues : Dictionary<string, string> option, queryTypes) = 
        maybe { 
            let! (predicate, searchProfile) = getSearchPredicate (writers, searchQuery, inputValues)
            match predicate with
            | NotPredicate(_) -> return! fail <| PurelyNegativeQueryNotSupported
            | _ -> 
                return! SearchDsl.generateQuery 
                            (writers.Settings.FieldsLookup, predicate, searchQuery, searchProfile, queryTypes)
        }
    
    let search (searchQuery : SearchQuery.Dto, inputFields : Dictionary<string, string> option) = 
        maybe { let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                let! query = generateSearchQuery (writers, searchQuery, inputFields, queryTypes)
                return SearchDsl.search (writers, query, searchQuery) }
    interface ISearchService with
        member __.Search(searchQuery : SearchQuery.Dto, inputFields : Dictionary<string, string>) = 
            search (searchQuery, Some <| inputFields)
        member this.Search(searchQuery : SearchQuery.Dto) = search (searchQuery, None)

[<Sealed>]
type DocumentService(searchService : ISearchService, indexService : IIndexService) = 
    interface IDocumentService with
        
        /// Returns the total number of documents present in the index
        member __.TotalDocumentCount(indexName : string) = maybe { let! writer = indexService.IsIndexOnline <| indexName
                                                                   return writer |> IndexWriter.getDocumentCount }
        
        /// Get a document by Id        
        member __.GetDocument(indexName, documentId) = 
            maybe { 
                let q = new SearchQuery.Dto(indexName, (sprintf "%s = '%s'" Constants.IdField documentId))
                q.ReturnScore <- false
                q.ReturnFlatResult <- false
                q.Columns <- [| "*" |]
                match searchService.Search(q) with
                | Choice1Of2(v') -> 
                    if v'.Meta.RecordsReturned <> 0 then return (v'.Documents.First() |> toStructuredResult)
                    else return! fail <| DocumentIdNotFound(indexName, documentId)
                | Choice2Of2(e) -> return! fail <| e
            }
        
        /// Get top 10 document from the index
        member __.GetDocuments(indexName, count) = 
            maybe { 
                let q = new SearchQuery.Dto(indexName, (sprintf "%s matchall 'x'" Constants.IdField))
                q.ReturnScore <- false
                q.ReturnFlatResult <- false
                q.Columns <- [| "*" |]
                q.Count <- count
                q.MissingValueConfiguration.Add(Constants.IdField, MissingValueOption.Ignore)
                let! result = searchService.Search(q)
                return result |> toSearchResults
            }
        
        /// Add or update an existing document
        member __.AddOrUpdateDocument(document) = 
            maybe { 
                do! document.Validate()
                let! indexWriter = indexService.IsIndexOnline <| document.IndexName
                return! indexWriter |> IndexWriter.updateDocument document
            }
        
        /// Add a new document to the index
        member __.AddDocument(document) = 
            maybe { 
                do! document.Validate()
                if document.TimeStamp > 0L then 
                    return! fail 
                            <| IndexingVersionConflict(document.IndexName, document.Id, document.TimeStamp.ToString())
                let! writer = indexService.IsIndexOnline <| document.IndexName
                return! writer |> IndexWriter.addDocument document
                return new CreateResponse(document.Id)
            }
        
        /// Delete a document by Id
        member __.DeleteDocument(indexName, documentId) = 
            maybe { let! writer = indexService.IsIndexOnline <| indexName
                    return! writer |> IndexWriter.deleteDocument documentId }
        
        /// Delete all the documents present in an index
        member __.DeleteAllDocuments indexName = maybe { let! writer = indexService.IsIndexOnline <| indexName
                                                         writer |> IndexWriter.deleteAllDocuments }

/// <summary>
/// Job service class which will be dynamically injected using IOC.
/// </summary>
[<Sealed>]
type JobService() = 
    let cache = MemoryCache.Default
    interface IJobService with
        
        member __.UpdateJob(job : Job) : Choice<unit, Error> = 
            let item = new CacheItem(job.JobId, job)
            let policy = new CacheItemPolicy()
            policy.AbsoluteExpiration <- DateTimeOffset.Now.AddHours(5.00)
            cache.Set(item, new CacheItemPolicy())
            ok()
        
        member __.GetJob(jobId : string) = 
            assert (jobId <> null)
            let item = cache.GetCacheItem(jobId)
            if item <> null then Choice1Of2(item.Value :?> Job)
            else fail <| Error.JobNotFound jobId
        
        member __.DeleteAllJobs() = 
            // Not implemented
            fail <| Error.NotImplemented

/// <summary>
/// Service wrapper around all document queuing services
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
/// </summary>
/// <param name="state"></param>
[<Sealed>]
type QueueService(documentService : IDocumentService) = 
    
    let executionBlockOptions() = 
        let executionBlockOption = new ExecutionDataflowBlockOptions()
        executionBlockOption.MaxDegreeOfParallelism <- -1
        executionBlockOption.BoundedCapacity <- 100
        executionBlockOption
    
    /// <summary>
    /// Add queue processing method
    /// </summary>
    let processAddQueueItems (document) = documentService.AddDocument(document) |> ignore
    
    /// <summary>
    /// Add or update processing queue method
    /// </summary>
    let processAddOrUpdateQueueItems (document) = documentService.AddOrUpdateDocument(document) |> ignore
    
    /// <summary>
    /// Queue for add operation 
    /// </summary>
    let addQueue : ActionBlock<Document.Dto> = 
        new ActionBlock<Document.Dto>(processAddQueueItems, executionBlockOptions())
    
    /// <summary>
    /// Queue for add or update operation 
    /// </summary>
    let addOrUpdateQueue : ActionBlock<Document.Dto> = 
        new ActionBlock<Document.Dto>(processAddOrUpdateQueueItems, executionBlockOptions())
    
    interface IQueueService with
        
        member this.AddDocumentQueue(document) = 
            Async.AwaitTask(addQueue.SendAsync(document))
            |> Async.RunSynchronously
            |> ignore
        
        member this.AddOrUpdateDocumentQueue(document) = 
            Async.AwaitTask(addOrUpdateQueue.SendAsync(document))
            |> Async.RunSynchronously
            |> ignore
