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
open FlexSearch.Api
open FlexLucene.Analysis
open FlexSearch.Core
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime.Caching
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open System.ComponentModel.Composition

/// Index related operations
type IIndexService = 
    abstract GetIndex : indexName:string -> Result<Index>
    abstract UpdateIndexFields : indexName:string * fields:Field [] -> Result<unit>
    abstract AddOrUpdatePredefinedQuery : indexName:string * profile:SearchQuery -> Result<unit>
    abstract UpdateIndexConfiguration : indexName:string * indexConfiguration:IndexConfiguration -> Result<unit>
    abstract DeleteIndex : indexName:string -> Result<unit>
    abstract AddIndex : index:Index -> Result<CreationId>
    abstract GetAllIndex : unit -> Index array
    abstract IndexExists : indexName:string -> bool
    abstract IndexOnline : indexName:string -> Result<unit>
    abstract IsIndexOnline : indexName:string -> Result<IndexWriter>
    abstract GetIndexState : indexName:string -> Result<IndexStatus>
    abstract OpenIndex : indexName:string -> Result<unit>
    abstract CloseIndex : indexName:string -> Result<unit>
    abstract Commit : indexName:string -> Result<unit>
    abstract ForceCommit : indexName:string -> Result<unit>
    abstract Refresh : indexName:string -> Result<unit>
    abstract GetRealtimeSearchers : indexName:string -> Result<array<RealTimeSearcher>>
    abstract GetRealtimeSearcher : indexName:string * int -> Result<RealTimeSearcher>
    abstract GetDiskUsage : indexName:string -> Result<int64>

/// Document related operations
type IDocumentService = 
    abstract GetDocument : indexName:string * id:string -> Result<Document>
    abstract GetDocuments : indexName:string * count:int -> Result<SearchResults>
    abstract AddOrUpdateDocument : document:Document -> Result<unit>
    abstract DeleteDocument : indexName:string * id:string -> Result<unit>
    abstract DeleteDocumentsFromSearch : indexName:string * query:SearchQuery -> Result<SearchResults>
    abstract DeleteAllDocuments : indexName:string -> Result<unit>
    abstract AddDocument : document: Document -> Result<CreationId>
    abstract TotalDocumentCount : indexName:string -> Result<int>

/// Search related operations
type ISearchService = 
    abstract Search : searchQuery:SearchQuery -> Result<SearchResults>
    abstract GetLuceneQuery : searchQuery:SearchQuery -> Result<FlexLucene.Search.Query>

/// Queuing related operations
type IQueueService = 
    abstract AddDocumentQueue : document:Document -> unit
    abstract AddOrUpdateDocumentQueue : document:Document -> unit
    /// Waits until all the work items submitted are completed
    abstract Complete : unit -> unit

type IJobService = 
    abstract GetJob : string -> Result<Job>
    abstract DeleteAllJobs : unit -> Result<unit>
    abstract UpdateJob : Job -> Result<unit>
    abstract UpdateJob : jobId:string * JobStatus * count:int -> unit
    abstract UpdateJob : jobId:string * JobStatus * count:int * msg:string -> unit

///  Analyzer/Analysis related services

type IAnalyzerService = 
    abstract GetAnalyzer : analyzerName:string -> Result<LuceneAnalyzer>
    abstract GetAnalyzerInfo : analyzerName:string -> Result<Model.Analyzer>
    abstract DeleteAnalyzer : analyzerName:string -> Result<unit>
    abstract UpdateAnalyzer : analyzer:Model.Analyzer -> Result<unit>
    abstract GetAllAnalyzers : unit -> Model.Analyzer []
    abstract Analyze : analyzerName:string * input:string -> Result<string []>

[<Sealed>]
type AnalyzerService(threadSafeWriter : ThreadSafeFileWriter, ?testMode : bool) = 
    let testMode = defaultArg testMode true
    
    let getPhoneticFilter (encoder) = 
        let filterParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        filterParams.Add("encoder", encoder)
        filterParams.Add("inject", "false")
        let filters = new List<Filter>()
        filters.Add(new Filter(FilterName = "phonetic", Parameters = filterParams))
        let analyzerDefinition = 
            new Model.Analyzer(AnalyzerName = encoder.ToLowerInvariant(), 
                             Tokenizer = new Model.Tokenizer(TokenizerName = "whitespace"), Filters = filters.ToArray())
        (analyzerDefinition, Analysis.buildFromAnalyzerDto (analyzerDefinition) |> extract)
    
    let path = 
        Constants.ConfFolder +/ "Analyzers"
        |> Directory.CreateDirectory
        |> fun x -> x.FullName
    
    let store = conDict<Model.Analyzer * LuceneAnalyzer>()
    
    let updateAnalyzer (analyzer : Model.Analyzer) = 
        maybe { 
            do! validate analyzer
            let! instance = Analysis.buildFromAnalyzerDto (analyzer)
            do! threadSafeWriter.WriteFile(path +/ analyzer.AnalyzerName, analyzer)
            do! store
                |> tryUpdate (analyzer.AnalyzerName, (analyzer, instance))
                |> boolToResult UnableToUpdateMemory
        }
    
    let loadAllAnalyzers() = 
        Directory.EnumerateFiles(path) |> Seq.iter (fun x -> 
                                              match threadSafeWriter.ReadFile<Model.Analyzer>(x) with
                                              | Ok(dto) -> 
                                                  updateAnalyzer (dto)
                                                  |> Logger.Log
                                                  |> ignore
                                              | Fail(error) -> Logger.Log(error))
    
    let getAnalyzer (analyzerName) = 
        match store.TryGetValue(analyzerName) with
        | true, (_, instance) -> ok <| instance
        | _ -> fail <| AnalyzerNotFound(analyzerName)
    
    do 
        // Add prebuilt analyzers
        let standardAnalyzer = new Model.Analyzer(AnalyzerName = "standard")
        let instance = new FlexLucene.Analysis.Standard.StandardAnalyzer() :> LuceneAnalyzer
        store |> add ("standard", (standardAnalyzer, instance))
        store |> add ("keyword", (new Model.Analyzer(AnalyzerName = "keyword"), CaseInsensitiveKeywordAnalyzer))
        store |> add ("refinedsoundex", getPhoneticFilter ("refinedsoundex"))
        store |> add ("doublemetaphone", getPhoneticFilter ("doublemetaphone"))
        if not testMode then loadAllAnalyzers()
    
    interface IAnalyzerService with
        
        /// Create or update an existing analyzer
        member __.UpdateAnalyzer(analyzer : Model.Analyzer) = updateAnalyzer (analyzer)
        
        /// Delete an analyzer. This 
        member __.DeleteAnalyzer(analyzerName : string) = 
            maybe { 
                match store.TryGetValue(analyzerName) with
                | true, _ -> 
                    do! threadSafeWriter.DeleteFile(path +/ analyzerName)
                    do! store
                        |> tryRemove (analyzerName)
                        |> boolToResult UnableToUpdateMemory
                    return! okUnit
                | _ -> return! okUnit
            }
        
        member __.GetAllAnalyzers() = store.Values.ToArray() |> Array.map fst
        member __.GetAnalyzer(analyzerName : string) = getAnalyzer (analyzerName)
        
        member __.GetAnalyzerInfo(analyzerName : string) = 
            match store.TryGetValue(analyzerName) with
            | true, (dto, _) -> ok <| dto
            | _ -> fail <| AnalyzerNotFound(analyzerName)
        
        member __.Analyze(analyzerName : string, input : string) = 
            maybe { let! analyzer = getAnalyzer (analyzerName)
                    let tokens = new List<string>()
                    parseTextUsingAnalyzer(analyzer, "", input, tokens)
                    return tokens.ToArray() }

[<Sealed>]
type IndexService(eventAggregrator : EventAggregator, threadSafeWriter : ThreadSafeFileWriter, analyzerService : IAnalyzerService, ?testMode : bool) = 
    let testMode = defaultArg testMode true
    let im = 
        IndexManager.create 
            (eventAggregrator, threadSafeWriter, analyzerService.GetAnalyzer)
    
    let getAllIndex() = im.Store.Values.ToArray() |> Array.map (fun x -> x.IndexDto)
    do 
        if not testMode then im |> IndexManager.loadAllIndex
    
    interface IIndexService with
        member __.IsIndexOnline(indexName : string) = im |> IndexManager.indexOnline (indexName)
        member __.AddIndex(index : Index) = im |> IndexManager.addIndex (index)
        member __.CloseIndex(indexName : string) = im |> IndexManager.closeIndex (indexName)
        member __.OpenIndex(indexName : string) = im |> IndexManager.openIndex (indexName)
        member __.Commit(indexName : string) = maybe { let! writer = im |> IndexManager.indexOnline indexName
                                                       writer |> IndexWriter.commit false }
        member __.ForceCommit(indexName : string) = maybe { let! writer = im |> IndexManager.indexOnline indexName
                                                            writer |> IndexWriter.commit true }
        member __.Refresh(indexName : string) = maybe { let! writer = im |> IndexManager.indexOnline indexName
                                                        writer |> IndexWriter.refresh }
        member __.GetRealtimeSearcher(indexName : string, shardNo : int) = 
            maybe { let! writer = im |> IndexManager.indexOnline indexName
                    return writer |> IndexWriter.getRealTimeSearcher shardNo }
        member __.GetRealtimeSearchers(indexName : string) = maybe { let! writer = im 
                                                                                   |> IndexManager.indexOnline indexName
                                                                     return writer |> IndexWriter.getRealTimeSearchers }
        
        member __.GetIndex(indexName : string) = 
            match im.Store.TryGetValue(indexName) with
            | true, state -> ok <| state.IndexDto
            | _ -> fail <| IndexNotFound indexName
        
        member __.IndexExists(indexName : string) = 
            match im.Store.TryGetValue(indexName) with
            | true, _ -> true
            | _ -> false
        
        member __.IndexOnline(indexName : string) = 
            im
            |> IndexManager.indexOnline indexName
            >>= fun _ -> okUnit
        
        member __.GetIndexState(indexName : string) = 
            match im |> IndexManager.indexState indexName with
            | Ok(state) -> ok <| state.IndexStatus
            | Fail(error) -> fail <| error
        
        member __.UpdateIndexFields(indexName:string, fields : Field []) = 
            match im.Store.TryGetValue(indexName) with
            | true, state -> let index = state.IndexDto
                             index.Fields <- fields
                             im |> IndexManager.updateIndex index
            | _ -> fail <| IndexNotFound indexName

        member __.AddOrUpdatePredefinedQuery(indexName:string , profile:SearchQuery) = 
            match im.Store.TryGetValue(indexName) with
            | true, state -> 
                let index = state.IndexDto
                match index.PredefinedQueries |> Array.tryFindIndex (fun sp -> sp.QueryName = profile.QueryName) with
                | Some(spNo) -> index.PredefinedQueries.[spNo] <- profile
                | _ -> index.PredefinedQueries <- [| profile |] |> Array.append index.PredefinedQueries
                
                im |> IndexManager.updateIndex index
            | _ -> fail <| IndexNotFound indexName

        member __.UpdateIndexConfiguration(indexName:string, indexConfiguration:IndexConfiguration) =
            match im.Store.TryGetValue indexName with
            | true, state ->
                let index = state.IndexDto
                
                // Don't allow for index version to be modified
                if index.IndexConfiguration.IndexVersion <> indexConfiguration.IndexVersion
                then fail <| UnSupportedIndexVersion(indexConfiguration.IndexVersion.ToString())
                else index.IndexConfiguration <- indexConfiguration
                     im |> IndexManager.updateIndex index
            | _ -> fail <| IndexNotFound indexName

        member __.DeleteIndex(indexName : string) = im |> IndexManager.deleteIndex (indexName)
        member __.GetAllIndex() = getAllIndex()
        member __.GetDiskUsage(indexName : string) = im |> IndexManager.getDiskUsage indexName
    
    interface IRequireNotificationForShutdown with
        member __.Shutdown() = 
            async {
                eventAggregrator.Push(RegisterForShutdownCallback(__))
                getAllIndex() |> Array.Parallel.iter (fun i -> 
                                     im
                                     |> IndexManager.shutdownIndex i.IndexName
                                     |> ignore)
            }
                
[<Sealed>]
type SearchService(parser : IFlexParser, queryFunctions : Dictionary<string, IQueryFunction>, indexService : IIndexService) = 
    let getSearchPredicate (writers : IndexWriter, search : SearchQuery) = 
        maybe { 
            if String.IsNullOrWhiteSpace(search.PredefinedQuery) <> true then 
                // Search profile based
                match writers.Settings.PredefinedQueries.TryGetValue(search.PredefinedQuery) with
                | true, p -> 
                    let (p', sq) = p
                    // This is a search profile based query. So copy over essential
                    // values from Search profile to query. Keep the search query
                    /// values if override is set to true
                    if not search.OverridePredefinedQueryOptions then 
                        search.Columns <- sq.Columns
                        search.DistinctBy <- sq.DistinctBy
                        search.Skip <- sq.Skip
                        search.OrderBy <- sq.OrderBy
                        search.CutOff <- sq.CutOff
                        search.Count <- sq.Count
                    // Check if search profile script is defined. If yes then execute it.
                    do! if isNotBlank sq.PreSearchScript then
                            okUnit 
//                            match scriptService.GetPreSearchScript(sq.PreSearchScript) with
//                            | Ok(script) -> 
//                                try 
//                                    script.Invoke(sq)
//                                    okUnit
//                                with e -> 
//                                    Logger.Log
//                                        ("Predefined Query execution error", e, MessageKeyword.Search, 
//                                         MessageLevel.Warning)
//                                    okUnit
//                            | Fail(err) -> fail <| err
                        else okUnit
                    return p'
                | _ -> return! fail <| UnknownPredefinedQuery(search.IndexName, search.PredefinedQuery)
            else let! predicate = parser.Parse(search.QueryString)
                 return predicate
        }
    
    let generateSearchQuery (writer : IndexWriter, searchQuery : SearchQuery) = 
        maybe { 
            let! predicate = getSearchPredicate (writer, searchQuery)
            match predicate with
            | NotPredicate(_) -> return! fail <| PurelyNegativeQueryNotSupported
            | _ -> 
                return!
                  { Fields = writer.Settings.Fields.ReadOnlyDictionary
                    QueryFunctions = queryFunctions }
                  |> generateQuery predicate searchQuery
        }
    
    let searchWrapper (writers, query, searchQuery) = 
        try 
            ok <| search (writers, query, searchQuery)
        with e -> fail <| SearchError(exceptionPrinter e)
    
    let search (searchQuery : SearchQuery) = 
        maybe { let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                let! query = generateSearchQuery (writers, searchQuery)
                return! searchWrapper (writers, query, searchQuery) }

    interface ISearchService with
        member __.Search(searchQuery : SearchQuery) = search searchQuery

        // Expose a member that generates a Lucene Query from a given FlexSearch SearchQuery
        member __.GetLuceneQuery(searchQuery: SearchQuery) =
            maybe { let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                    return! generateSearchQuery (writers, searchQuery) }

[<Sealed>]
type DocumentService(searchService : ISearchService, indexService : IIndexService) = 
    let deleteByQuery searchQuery indexName = 
        maybe { 
            let! query = searchService.GetLuceneQuery searchQuery
            let! writer = indexService.IsIndexOnline indexName

            writer |> IndexWriter.deleteAllDocumentsFromSearch query
        }

    interface IDocumentService with

        /// Returns the total number of documents present in the index
        member __.TotalDocumentCount(indexName : string) = maybe { let! writer = indexService.IsIndexOnline <| indexName
                                                                   return writer |> IndexWriter.getDocumentCount }
        
        /// Get a document by Id        
        member __.GetDocument(indexName, documentId) = 
            maybe { 
                let q = new SearchQuery(indexName, (sprintf "allof(%s, '%s')" IdField.Name documentId))
                q.ReturnScore <- false
                q.Columns <- [| "*" |]
                match searchService.Search(q) with
                | Ok(v') -> 
                    if v'.RecordsReturned <> 0 then return (v'.Documents.First())
                    else return! fail <| DocumentIdNotFound(indexName, documentId)
                | Fail(e) -> return! fail <| e
            }
        
        /// Get top 10 document from the index
        member __.GetDocuments(indexName, count) = 
            maybe { 
                let q = new SearchQuery(indexName, (sprintf "matchall(%s, 'x')" IdField.Name))
                q.ReturnScore <- false
                q.Columns <- [| "*" |]
                q.Count <- count
                return! searchService.Search(q)
            }
        
        /// Add or update an existing document
        member __.AddOrUpdateDocument(document) = 
            maybe { 
                do! validate document
                let! indexWriter = indexService.IsIndexOnline <| document.IndexName
                return! indexWriter |> IndexWriter.updateDocument document
            }
        
        /// Add a new document to the index
        member __.AddDocument(document) = 
            maybe { 
                do! validate document
                if document.TimeStamp > 0L then 
                    return! fail 
                            <| IndexingVersionConflict(document.IndexName, document.Id, document.TimeStamp.ToString())
                else 
                    let! writer = indexService.IsIndexOnline <| document.IndexName
                    do! writer |> IndexWriter.addDocument document
                    return new CreationId(document.Id)
            }
        
        /// Delete a document by Id
        member __.DeleteDocument(indexName, documentId) = 
            maybe { let! writer = indexService.IsIndexOnline <| indexName
                    return! writer |> IndexWriter.deleteDocument documentId }
        
        /// Delete all the documents present in an index
        member __.DeleteAllDocuments indexName = maybe { let! writer = indexService.IsIndexOnline <| indexName
                                                         writer |> IndexWriter.deleteAllDocuments }

        /// Deletes all the documents from the returned search query
        member __.DeleteDocumentsFromSearch(indexName, searchQuery) =
            maybe {
                // First run the search query to get the results
                let! searchResults = searchService.Search(searchQuery)

                // Then delete the documents
                do! indexName |> deleteByQuery searchQuery   

                // Finally return the search results
                return searchResults }
            

/// <summary>
/// Job service class which will be dynamically injected using IOC.
/// </summary>
[<Sealed>]
type JobService() = 
    let cache = MemoryCache.Default
    
    let getCachePolicy() = 
        let policy = new CacheItemPolicy()
        policy.AbsoluteExpiration <- DateTimeOffset.Now.AddHours(5.00)
        policy
    
    interface IJobService with
        
        member __.UpdateJob(jobId, jobStatus, itemCount) = 
            if isNotBlank jobId then 
                let job = new Job(JobId = jobId, JobStatus = jobStatus, Message = "", ProcessedItems = itemCount)
                let item = new CacheItem(jobId, job)
                cache.Set(item, getCachePolicy())
        
        member __.UpdateJob(jobId, jobStatus, itemCount, message) = 
            if isNotBlank jobId then 
                let job = new Job(JobId = jobId, JobStatus = jobStatus, Message = message, ProcessedItems = itemCount)
                let item = new CacheItem(jobId, job)
                cache.Set(item, getCachePolicy())
        
        member __.UpdateJob(job : Job) = 
            let item = new CacheItem(job.JobId, job)
            cache.Set(item, getCachePolicy())
            okUnit
        
        member __.GetJob(jobId : string) = 
            assert (jobId <> null)
            let item = cache.GetCacheItem(jobId)
            if item <> null then ok <| (item.Value :?> Job)
            else fail <| JobNotFound jobId
        
        member __.DeleteAllJobs() = 
            // Not implemented
            fail <| NotImplemented

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
    let addQueue = new MultipleConsumerQueue<Document>(processAddQueueItems)
    
    /// <summary>
    /// Queue for add or update operation 
    /// </summary>
    let addOrUpdateQueue = new MultipleConsumerQueue<Document>(processAddOrUpdateQueueItems)
    
    interface IQueueService with
        member __.AddDocumentQueue(document) = (addQueue :> IQueue<Document>).Send(document)
        member __.AddOrUpdateDocumentQueue(document) = (addOrUpdateQueue :> IQueue<Document>).Send(document)
        member __.Complete() = 
            let completeQueue (q : MultipleConsumerQueue<Document>) =
                (q :> IRequireNotificationForShutdown).Shutdown()
                |> Async.Catch
                |> Async.RunSynchronously
                |> handleShutdownExceptions

            completeQueue addQueue
            completeQueue addOrUpdateQueue
