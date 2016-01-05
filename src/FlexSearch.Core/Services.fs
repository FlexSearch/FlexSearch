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
    abstract AddOrUpdateSearchProfile : indexName:string * profile:SearchQuery -> Result<unit>
    abstract UpdateIndexConfiguration : indexName:string * indexConfiguration:IndexConfiguration -> Result<unit>
    abstract DeleteIndex : indexName:string -> Result<unit>
    abstract AddIndex : index:Index -> Result<CreateResponse>
    abstract GetAllIndex : unit -> Index array
    abstract IndexExists : indexName:string -> bool
    abstract IndexOnline : indexName:string -> bool
    abstract IsIndexOnline : indexName:string -> Result<IndexWriter.T>
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
    abstract DeleteDocumentsFromSearch : indexName:string * query:SearchQuery -> Result<SearchResults<T>>
    abstract DeleteAllDocuments : indexName:string -> Result<unit>
    abstract AddDocument : document: Document -> Result<CreateResponse>
    abstract TotalDocumentCount : indexName:string -> Result<int>

/// Search related operations
type ISearchService = 
    abstract Search : searchQuery:SearchQuery * inputFields:Dictionary<string, string>
     -> Result<SearchResults<SearchResultComponents.T>>
    abstract Search : searchQuery:SearchQuery -> Result<SearchResults<SearchResultComponents.T>>
    abstract Search : searchQuery:SearchQuery * searchProfileString:string
     -> Result<SearchResults<SearchResultComponents.T>>
    abstract GetLuceneQuery : searchQuery:SearchQuery -> Result<FlexLucene.Search.Query>

/// Queuing related operations
type IQueueService = 
    abstract AddDocumentQueue : document:Document -> unit
    abstract AddOrUpdateDocumentQueue : document:Document -> unit

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

/// Script related services
type IScriptService = 
    
    /// Signature : fun (indexName, fieldName, source, options) -> string
    abstract GetScript : scriptName:string * scriptType:ScriptType -> Result<Scripts.T>
    
    /// This methods verifies that the script call itself is valid and return the funtion along
    /// with the paramters that can be passed to the funtion
    /// Usually a script call looks like below
    /// function('param1','param2','param3',....)
    abstract GetScriptSig : scriptSig:string -> Result<string * string []>
    abstract GetComputedScript : scriptSig:string -> Result<ComputedDelegate * string []>
    abstract GetSearchProfileScript : scriptSig:string -> Result<SearchProfileDelegate>

[<Sealed>]
type ScriptService() = 
    let scripts = conDict<Scripts.T>()
    let addScripts (s : seq<string * Scripts.T>) = 
        s |> Seq.iter (fun (scriptName, script) -> scripts.TryAdd(scriptName, script) |> ignore)
    
    do 
        Compiler.compileAllScripts (ScriptType.Computed) |> addScripts
        Compiler.compileAllScripts (ScriptType.PostSearch) |> addScripts
        Compiler.compileAllScripts (ScriptType.SearchProfile) |> addScripts
    
    let getScript (scriptName, scriptType) = 
        match scripts.TryGetValue(scriptName + (scriptType.ToString())) with
        | true, func -> ok <| func
        | _ -> fail <| ScriptNotFound(scriptName, String.Empty)
    
    interface IScriptService with
        member __.GetScript(scriptName, scriptType) = getScript (scriptName, scriptType)
        member __.GetScriptSig(scriptSig) = ParseFunctionCall(scriptSig)
        
        member __.GetComputedScript(scriptSig) = 
            maybe { 
                let! (functionName, parameters) = ParseFunctionCall(scriptSig)
                let! script = getScript (functionName, ScriptType.Computed)
                return! match script with
                        | ComputedScript(s) -> ok <| (s, parameters)
                        | _ -> fail <| ScriptNotFound(functionName, ScriptType.Computed.ToString())
            }
        
        member __.GetSearchProfileScript(scriptName) = 
            maybe { 
                let! script = getScript (scriptName, ScriptType.SearchProfile)
                return! match script with
                        | SearchProfileScript(s) -> ok <| s
                        | _ -> fail <| ScriptNotFound(scriptName, ScriptType.Computed.ToString())
            }

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
                    return parseTextUsingAnalyzer(analyzer, "", input).ToArray() }

[<Sealed>]
type IndexService(eventAggregrator : EventAggregator, threadSafeWriter : ThreadSafeFileWriter, analyzerService : IAnalyzerService, scriptService : IScriptService, ?testMode : bool) = 
    let testMode = defaultArg testMode true
    let im = 
        IndexManager.create 
            (eventAggregrator, threadSafeWriter, analyzerService.GetAnalyzer, scriptService.GetComputedScript)
    
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
            |> resultToBool
        
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

        member __.AddOrUpdateSearchProfile(indexName:string , profile:SearchQuery) = 
            match im.Store.TryGetValue(indexName) with
            | true, state -> 
                let index = state.IndexDto
                match index.SearchProfiles |> Array.tryFindIndex (fun sp -> sp.QueryName = profile.QueryName) with
                | Some(spNo) -> index.SearchProfiles.[spNo] <- profile
                | _ -> index.SearchProfiles <- [| profile |] |> Array.append index.SearchProfiles
                
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
type SearchService(parser : IFlexParser, scriptService : IScriptService, flexQueries : Dictionary<string, IFlexQuery>, flexQueryFuncs : Dictionary<string, IFlexQueryFunction>,  indexService : IIndexService) = 
    // Generate query types from query factory. This is necessary as a single query can support multiple
    // query names
    let queryTypes = 
        let result = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
        for pair in flexQueries do
            for queryName in pair.Value.QueryName() do
                result.Add(queryName, pair.Value)
        result
    
    // Generate query function types from factory. This is necessary when passing functions in the query string
    let queryFunctionTypes =
        let result = new Dictionary<string, IFlexQueryFunction>(StringComparer.OrdinalIgnoreCase)
        for pair in flexQueryFuncs do
            result.Add(pair.Value.GetType() |> getTypeNameFromAttribute, pair.Value)
        result

    let getSearchPredicate (writers : IndexWriter.T, search : SearchQuery, 
                            inputValues : Dictionary<string, string> option) = 
        maybe { 
            if String.IsNullOrWhiteSpace(search.SearchProfile) <> true then 
                // Search profile based
                match writers.Settings.SearchProfiles.TryGetValue(search.SearchProfile) with
                | true, p -> 
                    let (p', sq) = p
                    // This is a search profile based query. So copy over essential
                    // values from Search profile to query. Keep the search query
                    /// values if override is set to true
                    if not search.OverrideProfileOptions then 
                        search.Columns <- sq.Columns
                        search.DistinctBy <- sq.DistinctBy
                        search.Skip <- sq.Skip
                        search.OrderBy <- sq.OrderBy
                        search.CutOff <- sq.CutOff
                        search.Count <- sq.Count
                    let! values = match inputValues with
                                  | Some(values) -> ok(values)
                                  | None -> Parsers.ParseQueryString(search.QueryString, false)
                    // Check if search profile script is defined. If yes then execute it.
                    do! if isNotBlank sq.SearchProfileScript then 
                            match scriptService.GetSearchProfileScript(sq.SearchProfileScript) with
                            | Ok(script) -> 
                                try 
                                    script.Invoke(sq, values)
                                    okUnit
                                with e -> 
                                    Logger.Log
                                        ("SearchProfile Query execution error", e, MessageKeyword.Search, 
                                         MessageLevel.Warning)
                                    okUnit
                            | Fail(err) -> fail <| err
                        else okUnit
                    return (p', Some(values))
                | _ -> return! fail <| UnknownSearchProfile(search.IndexName, search.SearchProfile)
            else let! predicate = parser.Parse(search.QueryString)
                 return (predicate, None)
        }
    
    let generateSearchQuery (writers : IndexWriter.T, searchQuery : SearchQuery, 
                             inputValues : Dictionary<string, string> option, queryTypes) = 
        maybe { 
            let! (predicate, searchProfile) = getSearchPredicate (writers, searchQuery, inputValues)
            match predicate with
            | NotPredicate(_) -> return! fail <| PurelyNegativeQueryNotSupported
            | _ -> 
                return! SearchDsl.generateQuery 
                            (writers.Settings.Fields.ReadOnlyDictionary, predicate, searchQuery, searchProfile, 
                             queryTypes, queryFunctionTypes)
        }
    
    let searchWrapper (writers, query, searchQuery) = 
        try 
            ok <| SearchDsl.search (writers, query, searchQuery)
        with e -> fail <| SearchError(exceptionPrinter e)
    
    let search (searchQuery : SearchQuery, inputFields : Dictionary<string, string> option) = 
        maybe { let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                let! query = generateSearchQuery (writers, searchQuery, inputFields, queryTypes)
                return! searchWrapper (writers, query, searchQuery) }

    interface ISearchService with
        
        member __.Search(searchQuery : SearchQuery, searchProfileString : string) = 
            maybe { 
                let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                // Parse the search profile to see if it is a valid query
                let! predicate = parser.Parse(searchProfileString)
                let! searchData = Parsers.ParseQueryString(searchQuery.QueryString, false)
                match predicate with
                | NotPredicate(_) -> return! fail <| PurelyNegativeQueryNotSupported
                | _ -> let! query = SearchDsl.generateQuery 
                                        (writers.Settings.Fields.ReadOnlyDictionary, predicate, searchQuery, Some(searchData), 
                                         queryTypes, queryFunctionTypes)
                       return! searchWrapper (writers, query, searchQuery)
            }
        
        member __.Search(searchQuery : SearchQuery, inputFields : Dictionary<string, string>) = 
            search (searchQuery, Some <| inputFields)
        member __.Search(searchQuery : SearchQuery) = search (searchQuery, None)

        // Expose a member that generates a Lucene Query from a given FlexSearch SearchQuery
        member __.GetLuceneQuery(searchQuery: SearchQuery) =
            maybe { let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                    return! generateSearchQuery (writers, searchQuery, None, queryTypes) }

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
                let q = new SearchQuery(indexName, (sprintf "%s = '%s'" MetaFields.IdField documentId))
                q.ReturnScore <- false
                q.ReturnFlatResult <- false
                q.Columns <- [| "*" |]
                match searchService.Search(q) with
                | Ok(v') -> 
                    if v'.Meta.RecordsReturned <> 0 then return (v'.Documents.First() |> toStructuredResult)
                    else return! fail <| DocumentIdNotFound(indexName, documentId)
                | Fail(e) -> return! fail <| e
            }
        
        /// Get top 10 document from the index
        member __.GetDocuments(indexName, count) = 
            maybe { 
                let q = new SearchQuery(indexName, (sprintf "%s matchall 'x'" MetaFields.IdField))
                q.ReturnScore <- false
                q.ReturnFlatResult <- false
                q.Columns <- [| "*" |]
                q.Count <- count
                let! result = searchService.Search(q)
                return result |> toSearchResults
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
                    return new CreateResponse(document.Id)
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
    let addQueue : ActionBlock<Document> = 
        new ActionBlock<Document>(processAddQueueItems, executionBlockOptions())
    
    /// <summary>
    /// Queue for add or update operation 
    /// </summary>
    let addOrUpdateQueue : ActionBlock<Document> = 
        new ActionBlock<Document>(processAddOrUpdateQueueItems, executionBlockOptions())
    
    interface IQueueService with
        
        member __.AddDocumentQueue(document) = 
            Async.AwaitTask(addQueue.SendAsync(document))
            |> Async.RunSynchronously
            |> ignore
        
        member __.AddOrUpdateDocumentQueue(document) = 
            Async.AwaitTask(addOrUpdateQueue.SendAsync(document))
            |> Async.RunSynchronously
            |> ignore
