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
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime.Caching
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

/// General factory Interface for all MEF based factories
type IFlexFactory<'T> = 
    abstract GetModuleByName : string -> Choice<'T, IMessage>
    abstract ModuleExists : string -> bool
    abstract GetAllModules : unit -> Dictionary<string, 'T>
    abstract GetMetaData : string -> Choice<IDictionary<string, obj>, IMessage>

/// Index related operations
type IIndexService = 
    abstract GetIndex : indexName:string -> Choice<Index.Dto, IMessage>
    abstract UpdateIndexFields : fields:Field.Dto [] -> Choice<unit, IMessage>
    abstract DeleteIndex : indexName:string -> Choice<unit, IMessage>
    abstract AddIndex : index:Index.Dto -> Choice<CreateResponse, IMessage>
    abstract GetAllIndex : unit -> Index.Dto array
    abstract IndexExists : indexName:string -> bool
    abstract IndexOnline : indexName:string -> bool
    abstract IsIndexOnline : indexName:string -> Choice<IndexWriter.T, IMessage>
    abstract GetIndexState : indexName:string -> Choice<IndexStatus, IMessage>
    abstract OpenIndex : indexName:string -> Choice<unit, IMessage>
    abstract CloseIndex : indexName:string -> Choice<unit, IMessage>
    abstract Commit : indexName:string -> Choice<unit, IMessage>
    abstract ForceCommit : indexName:string -> Choice<unit, IMessage>
    abstract Refresh : indexName:string -> Choice<unit, IMessage>
    abstract GetRealtimeSearchers : indexName:string -> Choice<array<RealTimeSearcher>, IMessage>
    abstract GetRealtimeSearcher : indexName:string * int -> Choice<RealTimeSearcher, IMessage>

/// Document related operations
type IDocumentService = 
    abstract GetDocument : indexName:string * id:string -> Choice<Document.Dto, IMessage>
    abstract GetDocuments : indexName:string * count:int -> Choice<SearchResults, IMessage>
    abstract AddOrUpdateDocument : document:Document.Dto -> Choice<unit, IMessage>
    abstract DeleteDocument : indexName:string * id:string -> Choice<unit, IMessage>
    abstract AddDocument : document:Document.Dto -> Choice<CreateResponse, IMessage>
    abstract DeleteAllDocuments : indexName:string -> Choice<unit, IMessage>
    abstract TotalDocumentCount : indexName:string -> Choice<int, IMessage>

/// Search related operations
type ISearchService = 
    abstract Search : searchQuery:SearchQuery.Dto * inputFields:Dictionary<string, string>
     -> Choice<SearchResults<SearchResultComponents.T>, IMessage>
    abstract Search : searchQuery:SearchQuery.Dto -> Choice<SearchResults<SearchResultComponents.T>, IMessage>
    abstract Search : searchQuery:SearchQuery.Dto * searchProfileString:string
     -> Choice<SearchResults<SearchResultComponents.T>, IMessage>

/// Queuing related operations
type IQueueService = 
    abstract AddDocumentQueue : document:Document.Dto -> unit
    abstract AddOrUpdateDocumentQueue : document:Document.Dto -> unit

type IJobService = 
    abstract GetJob : string -> Choice<Job, IMessage>
    abstract DeleteAllJobs : unit -> Choice<unit, IMessage>
    abstract UpdateJob : Job -> Choice<unit, IMessage>
    abstract UpdateJob : jobId:string * JobStatus * count:int -> unit
    abstract UpdateJob : jobId:string * JobStatus * count:int * msg:string -> unit

///  Analyzer/Analysis related services
type IAnalyzerService = 
    abstract GetAnalyzer : analyzerName:string -> Choice<Analyzer, IMessage>
    abstract GetAnalyzerInfo : analyzerName:string -> Choice<Analyzer.Dto, IMessage>
    abstract DeleteAnalyzer : analyzerName:string -> Choice<unit, IMessage>
    abstract UpdateAnalyzer : analyzer:Analyzer.Dto -> Choice<unit, IMessage>
    abstract GetAllAnalyzers : unit -> Analyzer.Dto []
    abstract Analyze : analyzerName:string * input:string -> Choice<string [], IMessage>

/// Script related services
type IScriptService = 
    
    /// Signature : fun (indexName, fieldName, source, options) -> string
    abstract GetScript : scriptName:string * scriptType:ScriptType -> Choice<Scripts.T, IMessage>
    
    /// This methods verifies that the script call itself is valid and return the funtion along
    /// with the paramters that can be passed to the funtion
    /// Usually a script call looks like below
    /// function('param1','param2','param3',....)
    abstract GetScriptSig : scriptSig:string -> Choice<string * string [], IMessage>
    
    abstract GetComputedScript : scriptSig:string -> Choice<ComputedDelegate * string [], IMessage>
    abstract GetSearchProfileScript : scriptSig:string -> Choice<SearchProfileDelegate, IMessage>

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
        let filters = new List<TokenFilter.Dto>()
        filters.Add(new TokenFilter.Dto(FilterName = "phonetic", Parameters = filterParams))
        let analyzerDefinition = 
            new Analyzer.Dto(AnalyzerName = encoder.ToLowerInvariant(), 
                             Tokenizer = new Tokenizer.Dto(TokenizerName = "whitespace"), Filters = filters)
        (analyzerDefinition, Analysis.buildFromAnalyzerDto (analyzerDefinition) |> extract)
    
    let path = 
        Constants.ConfFolder +/ "Analyzers"
        |> Directory.CreateDirectory
        |> fun x -> x.FullName
    
    let store = conDict<Analyzer.Dto * Analyzer>()
    
    let updateAnalyzer (analyzer : Analyzer.Dto) = 
        maybe { 
            do! analyzer.Validate()
            let! instance = Analysis.buildFromAnalyzerDto (analyzer)
            do! threadSafeWriter.WriteFile(path +/ analyzer.AnalyzerName, analyzer)
            do! store
                |> tryUpdate (analyzer.AnalyzerName, (analyzer, instance))
                |> boolToResult UnableToUpdateMemory
        }
    
    let loadAllAnalyzers() = 
        Directory.EnumerateFiles(path) |> Seq.iter (fun x -> 
                                              match threadSafeWriter.ReadFile<Analyzer.Dto>(x) with
                                              | Choice1Of2(dto) -> 
                                                  updateAnalyzer (dto)
                                                  |> logErrorChoice
                                                  |> ignore
                                              | Choice2Of2(error) -> Logger.Log(error))
    
    let getAnalyzer (analyzerName) = 
        match store.TryGetValue(analyzerName) with
        | true, (_, instance) -> ok <| instance
        | _ -> fail <| AnalyzerNotFound(analyzerName)
    
    do 
        // Add prebuilt analyzers
        let standardAnalyzer = new Analyzer.Dto(AnalyzerName = "standard")
        let instance = new FlexLucene.Analysis.Standard.StandardAnalyzer() :> Analyzer
        store |> add ("standard", (standardAnalyzer, instance))
        store |> add ("keyword", (new Analyzer.Dto(AnalyzerName = "keyword"), CaseInsensitiveKeywordAnalyzer))
        store |> add ("refinedsoundex", getPhoneticFilter ("refinedsoundex"))
        store |> add ("doublemetaphone", getPhoneticFilter ("doublemetaphone"))
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
type IndexService(eventAggregrator : EventAggregrator, threadSafeWriter : ThreadSafeFileWriter, analyzerService : IAnalyzerService, scriptService : IScriptService, ?testMode : bool) = 
    let testMode = defaultArg testMode true
    let im = 
        IndexManager.create 
            (eventAggregrator, threadSafeWriter, analyzerService.GetAnalyzer, scriptService.GetComputedScript)
    
    do 
        if not testMode then im |> IndexManager.loadAllIndex
    
    interface IIndexService with
        member __.IsIndexOnline(indexName : string) = im |> IndexManager.indexOnline (indexName)
        member __.AddIndex(index : Index.Dto) = im |> IndexManager.addIndex (index)
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
            | Choice1Of2(state) -> ok <| state.IndexStatus
            | Choice2Of2(error) -> fail <| error
        
        member __.DeleteIndex(indexName : string) = im |> IndexManager.deleteIndex (indexName)
        member __.GetAllIndex() = im.Store.Values.ToArray() |> Array.map (fun x -> x.IndexDto)
        member __.UpdateIndexFields(_ : Field.Dto []) = failwith "Not implemented yet"

[<Sealed>]
type SearchService(parser : IFlexParser, scriptService : IScriptService, queryFactory : IFlexFactory<IFlexQuery>, indexService : IIndexService) = 
    
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
                                  | Some(values) -> Choice1Of2(values)
                                  | None -> Parsers.ParseQueryString(search.QueryString, false)
                    // Check if search profile script is defined. If yes then execute it.
                    do! if isNotBlank sq.SearchProfileScript then 
                            match scriptService.GetSearchProfileScript(sq.SearchProfileScript) with
                            | Choice1Of2(script) -> 
                                try 
                                    script.Invoke(sq, values)
                                    ok()
                                with e -> 
                                    Logger.Log
                                        ("SearchProfile Query execution error", e, MessageKeyword.Search, 
                                         MessageLevel.Warning)
                                    ok()
                            | Choice2Of2(err) -> fail <| err
                        else ok()
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
    
    let searchWrapper (writers, query, searchQuery) = 
        try 
            ok <| SearchDsl.search (writers, query, searchQuery)
        with e -> fail <| SearchError(exceptionPrinter e)
    
    let search (searchQuery : SearchQuery.Dto, inputFields : Dictionary<string, string> option) = 
        maybe { let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                let! query = generateSearchQuery (writers, searchQuery, inputFields, queryTypes)
                return! searchWrapper (writers, query, searchQuery) }
    interface ISearchService with
        
        member __.Search(searchQuery : SearchQuery.Dto, searchProfileString : string) = 
            maybe { 
                let! writers = indexService.IsIndexOnline <| searchQuery.IndexName
                // Parse the search profile to see if it is a valid query
                let! predicate = parser.Parse(searchProfileString)
                let! searchData = Parsers.ParseQueryString(searchQuery.QueryString, false)
                match predicate with
                | NotPredicate(_) -> return! fail <| PurelyNegativeQueryNotSupported
                | _ -> let! query = SearchDsl.generateQuery 
                                        (writers.Settings.FieldsLookup, predicate, searchQuery, Some(searchData), 
                                         queryTypes)
                       return! searchWrapper (writers, query, searchQuery)
            }
        
        member __.Search(searchQuery : SearchQuery.Dto, inputFields : Dictionary<string, string>) = 
            search (searchQuery, Some <| inputFields)
        member __.Search(searchQuery : SearchQuery.Dto) = search (searchQuery, None)

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
                let job = new Job(JobId = jobId, Status = jobStatus, Message = "", ProcessedItems = itemCount)
                let item = new CacheItem(jobId, job)
                cache.Set(item, getCachePolicy())
        
        member __.UpdateJob(jobId, jobStatus, itemCount, message) = 
            if isNotBlank jobId then 
                let job = new Job(JobId = jobId, Status = jobStatus, Message = message, ProcessedItems = itemCount)
                let item = new CacheItem(jobId, job)
                cache.Set(item, getCachePolicy())
        
        member __.UpdateJob(job : Job) : Choice<unit, IMessage> = 
            let item = new CacheItem(job.JobId, job)
            cache.Set(item, getCachePolicy())
            ok()
        
        member __.GetJob(jobId : string) = 
            assert (jobId <> null)
            let item = cache.GetCacheItem(jobId)
            if item <> null then Choice1Of2(item.Value :?> Job)
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
    let addQueue : ActionBlock<Document.Dto> = 
        new ActionBlock<Document.Dto>(processAddQueueItems, executionBlockOptions())
    
    /// <summary>
    /// Queue for add or update operation 
    /// </summary>
    let addOrUpdateQueue : ActionBlock<Document.Dto> = 
        new ActionBlock<Document.Dto>(processAddOrUpdateQueueItems, executionBlockOptions())
    
    interface IQueueService with
        
        member __.AddDocumentQueue(document) = 
            Async.AwaitTask(addQueue.SendAsync(document))
            |> Async.RunSynchronously
            |> ignore
        
        member __.AddOrUpdateDocumentQueue(document) = 
            Async.AwaitTask(addOrUpdateQueue.SendAsync(document))
            |> Async.RunSynchronously
            |> ignore
