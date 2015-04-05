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
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks

/// Index related operations
type IIndexService = 
    abstract GetIndex : string -> Choice<Index.Dto, Error>
    abstract UpdateIndex : Index.Dto -> Choice<unit, Error>
    abstract DeleteIndex : string -> Choice<unit, Error>
    abstract AddIndex : Index.Dto -> Choice<CreateResponse, Error>
    abstract GetAllIndex : unit -> Index.Dto array
    abstract IndexExists : string -> bool
    abstract IndexOnline : string -> bool
    abstract GetIndexStatus : string -> Choice<IndexState, Error>
    abstract OpenIndex : string -> Choice<unit, Error>
    abstract CloseIndex : string -> Choice<unit, Error>
    abstract Commit : string -> Choice<unit, Error>
    abstract Refresh : string -> Choice<unit, Error>
    abstract GetRealtimeSearchers : string -> Choice<array<RealTimeSearcher>, Error>
    abstract GetRealtimeSearcher : string * int -> Choice<RealTimeSearcher, Error>

/// Document related operations
type IDocumentService = 
    abstract GetDocument : indexName:string * id:string -> Choice<Document.Dto, Error>
    abstract GetDocuments : indexName:string * count:int -> Choice<Document.Dto, Error>
    abstract AddOrUpdateDocument : document:Document.Dto -> Choice<unit, Error>
    abstract DeleteDocument : indexName:string * id:string -> Choice<unit, Error>
    abstract AddDocument : document:Document.Dto -> Choice<CreateResponse, Error>
    abstract DeleteAllDocuments : indexName:string -> Choice<unit, Error>

/// Search related operations
type ISearchService = 
    abstract Search : SearchQuery.Dto -> Choice<SearchResults, Error>
    abstract SearchAsDocmentSeq : query:SearchQuery.Dto -> Choice<seq<Document.Dto> * int * int, Error>
    abstract SearchAsDictionarySeq : query:SearchQuery.Dto -> Choice<seq<Dictionary<string, string>> * int * int, Error>
    abstract SearchUsingProfile : query:SearchQuery.Dto * inputFields:Dictionary<string, string>
     -> Choice<SearchResults, Error>

/// Queuing related operations
type IQueueService = 
    abstract AddDocumentQueue : document:Document.Dto -> unit
    abstract AddOrUpdateDocumentQueue : document:Document.Dto -> unit

module State = 
    /// Default state to be used by items in the 
    type ItemState = 
        | Active = 0
        | Inactive = 1
    
    /// Factory used for index creation
    type IndexFactory = LazyFactory.T<IndexWriter.T, Index.Dto, IndexState>
    
    let getIndexFactory (store) = LazyFactory.create<IndexWriter.T, Index.Dto, IndexState> None store
    
    /// Factory used for analyzer creation
    type AnalyzerFactory = LazyFactory.T<Analyzer, Analyzer.Dto, ItemState>
    
    let getAnalyzerFactory (store) = LazyFactory.create<Analyzer, Analyzer.Dto, ItemState> (Some(Analyzer.build)) store
    
    /// Represents the node state. All the state related data should be part 
    /// of this.
    type T = 
        { IndexFactory : IndexFactory
          AnalyzerFactory : AnalyzerFactory
          Store : LazyFactory.Store }
    
    /// Create a new node state
    let create (inMemory) = 
        let store = new LazyFactory.Store(inMemory)
        { IndexFactory = getIndexFactory store
          AnalyzerFactory = getAnalyzerFactory store
          Store = store }
    
    /// Returns IndexNotFound error
    let indexNotFound (indexName) = IndexNotFound <| indexName
    
    /// Check if a given index exists
    let indexExists (indexName) (state : T) = state.IndexFactory |> LazyFactory.exists indexName indexNotFound

module IndexService = 
    /// Returns IndexNotFound error
    let indexNotFound (indexName) = IndexNotFound <| indexName
    
    /// Load a index
    let loadIndex (index : Index.Dto) (state : State.T) = 
        maybe { 
            let setting = IndexWriter.createIndexSetting (index, state.AnalyzerFactory)
            if index.Online then 
                do! state.IndexFactory |> LazyFactory.updateState (index.IndexName, IndexState.Opening)
                do! state.IndexFactory |> LazyFactory.updateInstance (index.IndexName, IndexWriter.create (setting))
            else do! state.IndexFactory |> LazyFactory.updateState (index.IndexName, IndexState.Offline)
        }
    
    let openIndex (indexName) (state : State.T) = 
        maybe { 
            let! item = state.IndexFactory |> LazyFactory.getItemOrError indexName indexNotFound
            item.MetaData.Online <- true
            state.Store.UpdateItem(indexName, item.MetaData)
            do! state |> loadIndex item.MetaData
        }
    
    let closeIndex (indexName) (state : State.T) = 
        maybe { 
            let! item = state.IndexFactory |> LazyFactory.getItemOrError indexName indexNotFound
            item.MetaData.Online <- false
            state.Store.UpdateItem(indexName, item.MetaData)
            item.Value.Value |> IndexWriter.close
            do! state.IndexFactory |> LazyFactory.updateState (indexName, IndexState.Offline)
        }
    
    /// Load all index from the store
    let loadAllIndex (state : State.T) = 
        state.Store.GetItems<Index.Dto>()
        |> Seq.map (fun i -> new Task(fun _ -> loadIndex i state |> ignore))
        |> Seq.toArray
        |> Task.WaitAll
    
    /// Add a new index
    let addIndex (index : Index.Dto) (state : State.T) = 
        maybe { 
            do! index.Validate()
            do! state |> State.indexExists index.IndexName
            do! state |> loadIndex index
            return CreateResponse(index.IndexName)
        }
    
    /// Get index by name
    let getIndex (indexName) (state : State.T) = state.Store.GetItem(indexName)
    
    /// Get all available index
    let getAllIndices (state : State.T) = state.Store.GetItems<Index.Dto>().ToArray()
    
    /// Delete an existing index    
    let deleteIndex (indexName) (s : State.T) = 
        maybe { 
            let! (_, _, writer) = s.IndexFactory |> LazyFactory.getAsTuple indexName indexNotFound
            writer |> IndexWriter.close
            s.IndexFactory |> LazyFactory.deleteItem indexName
            delDir (writer.Settings.BaseFolder)
        }
    
    let updateIndex (index : Index.Dto) (s : State.T) = 
        maybe { 
            let! (index, state, writer) = s.IndexFactory |> LazyFactory.getAsTuple index.IndexName indexNotFound
            match state with
            | IndexState.OnlineMaster -> 
                do! s.IndexFactory |> LazyFactory.updateState (index.IndexName, IndexState.Closing)
                writer |> IndexWriter.close
                do! s |> loadIndex index
                s.Store.UpdateItem(index.IndexName, index)
                do! s.IndexFactory |> LazyFactory.updateState (index.IndexName, IndexState.OnlineMaster)
                return! ok()
            | IndexState.Opening -> return! fail <| IndexInOpenState index.IndexName
            | IndexState.Offline | IndexState.Closing -> s.Store.UpdateItem(index.IndexName, index)
            | _ -> return! fail <| IndexInInvalidState index.IndexName
        }
    
    [<Sealed>]
    type Service(state) = 
        do state |> loadAllIndex
        interface IIndexService with
            member __.IndexOnline(indexName : string) = failwith "Not implemented yet"
            member __.GetIndex(indexName) = state |> getIndex (indexName)
            member __.UpdateIndex(index) = state |> updateIndex (index)
            member __.DeleteIndex(indexName) = state |> deleteIndex (indexName)
            member __.AddIndex(index) = state |> addIndex (index)
            member __.GetAllIndex() = state |> getAllIndices
            member __.IndexExists(indexName) = 
                succeeded <| (state.IndexFactory |> LazyFactory.exists indexName indexNotFound)
            member __.GetIndexStatus(indexName) = state.IndexFactory |> LazyFactory.getState indexName
            member __.OpenIndex(indexName) = state |> openIndex indexName
            member __.CloseIndex(indexName) = state |> closeIndex indexName
            member __.Commit(indexName) = maybe { let! instance = state.IndexFactory 
                                                                  |> LazyFactory.getInstance indexName
                                                  instance |> IndexWriter.commit }
            member __.Refresh(indexName) = maybe { let! instance = state.IndexFactory 
                                                                   |> LazyFactory.getInstance indexName
                                                   instance |> IndexWriter.refresh }
            
            member __.GetRealtimeSearchers(indexName) = 
                maybe { 
                    let! (index, state, writer) = state.IndexFactory |> LazyFactory.getAsTuple indexName indexNotFound
                    let searchers = writer |> IndexWriter.getRealTimeSearchers
                    return searchers
                }
            
            member __.GetRealtimeSearcher(indexName, shardNo) = 
                maybe { let! (index, state, writer) = state.IndexFactory 
                                                      |> LazyFactory.getAsTuple indexName indexNotFound
                        return writer |> IndexWriter.getRealTimeSearcher shardNo }

module SearchService = 
    let parser = new FlexParser() :> IFlexParser
    
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
    
    let search (searchQuery : SearchQuery.Dto, queryTypes) (state : State.T) = 
        maybe { 
            let! (index, state, writers) = state.IndexFactory 
                                           |> LazyFactory.getAsTuple searchQuery.IndexName IndexService.indexNotFound
            let! query = generateSearchQuery (writers, searchQuery, None, queryTypes)
            let! (results, recordsReturned, totalAvailable) = SearchDsl.searchDocumentSeq (writers, query, searchQuery)
            let searchResults = new SearchResults()
            searchResults.Documents <- results.ToList()
            searchResults.TotalAvailable <- totalAvailable
            searchResults.RecordsReturned <- recordsReturned
            return! Choice1Of2(searchResults)
        }
    
    [<Sealed>]
    type Service(state : State.T, queryFactory : IFlexFactory<IFlexQuery>) = 
        
        // Generate query types from query factory. This is necessary as a single query can support multiple
        // query names
        let queryTypes = 
            let result = new Dictionary<string, IFlexQuery>(StringComparer.OrdinalIgnoreCase)
            for pair in queryFactory.GetAllModules() do
                for queryName in pair.Value.QueryName() do
                    result.Add(queryName, pair.Value)
            result
        
        interface ISearchService with
            member __.Search(query) = state |> search (query, queryTypes)
            member __.SearchAsDocmentSeq(searchQuery) = 
                maybe { let! writers = state.IndexFactory |> LazyFactory.getInstance searchQuery.IndexName
                        let! query = generateSearchQuery (writers, searchQuery, None, queryTypes)
                        return! SearchDsl.searchDocumentSeq (writers, query, searchQuery) }
            member __.SearchAsDictionarySeq(searchQuery : SearchQuery.Dto) = 
                maybe { let! writers = state.IndexFactory |> LazyFactory.getInstance searchQuery.IndexName
                        let! query = generateSearchQuery (writers, searchQuery, None, queryTypes)
                        return! SearchDsl.searchDictionarySeq (writers, query, searchQuery) }
            member __.SearchUsingProfile(searchQuery : SearchQuery.Dto, inputFields : Dictionary<string, string>) = 
                maybe { 
                    let! writers = state.IndexFactory |> LazyFactory.getInstance searchQuery.IndexName
                    let! query = generateSearchQuery (writers, searchQuery, Some(inputFields), queryTypes)
                    let! (results, recordsReturned, totalAvailable) = SearchDsl.searchDocumentSeq 
                                                                          (writers, query, searchQuery)
                    let searchResults = new SearchResults()
                    searchResults.Documents <- results.ToList()
                    searchResults.TotalAvailable <- totalAvailable
                    searchResults.RecordsReturned <- recordsReturned
                    return! Choice1Of2(searchResults)
                }

module DocumentService = 
    /// Get a document by Id
    let getDocument (indexName) (id) (state : State.T) = ()
    
    /// Get top 10 document from the index
    let getDocuments (indexName) (state : State.T) = ()
    
    /// Add or update an existing document
    let addOrUpdateDocument (document : Document.Dto) (state : State.T) = 
        maybe { 
            do! document.Validate()
            do! state.IndexFactory |> LazyFactory.exists document.IndexName IndexService.indexNotFound
            let! indexWriter = state.IndexFactory |> LazyFactory.getInstance document.IndexName
            indexWriter |> IndexWriter.updateDocument document
        }
    
    /// Add a new document to the index
    let addDocument (document : Document.Dto) (state : State.T) = 
        maybe { 
            do! document.Validate()
            if document.TimeStamp > 0L then 
                return! fail <| IndexingVersionConflict(document.IndexName, document.Id, document.TimeStamp.ToString())
            let! writer = state.IndexFactory |> LazyFactory.getInstance document.IndexName
            writer |> IndexWriter.addDocument document
            return new CreateResponse(document.Id)
        }
    
    /// Delete a document by Id
    let deleteDocument (indexName) (id) (state : State.T) = maybe { let! writer = state.IndexFactory 
                                                                                  |> LazyFactory.getInstance indexName
                                                                    writer |> IndexWriter.deleteDocument id }
    
    /// Delete all documents of an index
    let deleteAllDocument (indexName) (state : State.T) = maybe { let! writer = state.IndexFactory 
                                                                                |> LazyFactory.getInstance indexName
                                                                  writer |> IndexWriter.deleteAllDocuments }
