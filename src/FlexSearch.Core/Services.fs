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

module IndexService = 
    open System
    open System.Collections.Concurrent
    open System.IO
    open System.Linq
    open System.Threading.Tasks
    
    type T = 
        { IndexStates : ConcurrentDictionary<string, IndexState>
          IndexWriters : ConcurrentDictionary<string, IndexWriter.T>
          AnalyzerService : LazyFactory<FlexLucene.Analysis.Analyzer, Analyzer.T>
          Store : State.Store }
        member this.GetIndices() = this.Store.GetItems<Index.T>()
        member this.GetIndex(indexName) = this.Store.GetItem<Index.T>(indexName)
    
    let create (service, store) = 
        { IndexStates = conDict<IndexState>()
          IndexWriters = conDict<IndexWriter.T>()
          AnalyzerService = service
          Store = store }
    
    let indexNotFound (indexName) = IndexNotFound <| indexName
    let indexExists (indexName) (state : T) = state.IndexStates |> keyExists (indexName, indexNotFound)
    let getIndexWriter (indexName) (state : T) = state.IndexWriters |> keyExists (indexName, indexNotFound)
    let getIndexState (indexName) (state : T) = state.IndexStates |> keyExists (indexName, indexNotFound)
    
    let updateState (indexName, indexState : IndexState) (state : T) = 
        match state.IndexStates.TryGetValue(indexName) with
        | true, v -> state.IndexStates.TryUpdate(indexName, indexState, v) |> ignore
        | _ -> raise (ValidationException(IndexNotFound(indexName)))
    
    /// Load a index
    let loadIndex (index : Index.T) (state : T) = 
        let setting = IndexWriter.createIndexSetting (index, state.AnalyzerService)
        if Args.getBool index.Online then 
            state.IndexStates |> addOrUpdate (index.IndexName, IndexState.Opening)
            state.IndexWriters |> add (index.IndexName, IndexWriter.create (setting))
        else state.IndexStates |> addOrUpdate (index.IndexName, IndexState.Offline)
    
    let openIndex (indexName) (state : T) = 
        maybe { 
            let! indexState = state |> getIndexState indexName
            let! index = state.GetIndex indexName
            let updatedIndex = { index with Online = Nullable(true) }
            state.Store.UpdateItem(index.IndexName, updatedIndex)
            let setting = IndexWriter.createIndexSetting (updatedIndex, state.AnalyzerService)
            state |> loadIndex updatedIndex
        }
    
    let closeIndex (indexName) (state : T) = 
        maybe { 
            let! indexState = state |> getIndexState indexName
            let! index = state.GetIndex(indexName)
            let updatedIndex = { index with Online = Nullable(false) }
            state.Store.UpdateItem(index.IndexName, updatedIndex)
            state.IndexWriters.[indexName] |> IndexWriter.close
            state.IndexWriters |> remove indexName
        }
    
    /// Load all index from the store
    let loadAllIndex (state : T) = 
        state.GetIndices()
        |> Seq.map (fun i -> new Task(fun _ -> loadIndex i state))
        |> Seq.toArray
        |> Task.WaitAll
    
    /// Add a new index
    let addIndex (index : Index.T) (state : T) = 
        maybe { 
            do! (index :> IValidate).Validate()
            let _ = state |> indexExists index.IndexName
            state |> loadIndex index
            return CreateResponse(index.IndexName)
        }
    
    /// Get index by name
    let getIndex (indexName) (state : T) = state.GetIndex(indexName)
    
    /// Get all available index
    let getAllIndices (state : T) = state.GetIndices().ToArray()
    
    /// Delete an existing index    
    let deleteIndex (indexName) (s : T) = 
        IndexWriter.close (s.IndexWriters.[indexName])
        let info = s.IndexWriters.[indexName]
        s.Store.DeleteItem(indexName)
        s.IndexStates |> remove indexName
        s.IndexWriters |> remove indexName
        delDir (info.Settings.BaseFolder)
        ok()
    
    let updateIndex (index : Index.T) (state : T) = 
        maybe { 
            let! iw = state |> getIndexWriter index.IndexName
            let! indexState = state |> getIndexState index.IndexName
            match indexState with
            | IndexState.OnlineMaster -> 
                state |> updateState (index.IndexName, IndexState.Closing)
                IndexWriter.close <| iw
                state |> loadIndex index
                state.Store.UpdateItem(index.IndexName, index)
                state |> updateState (index.IndexName, IndexState.OnlineMaster)
                return! ok()
            | IndexState.Opening -> return! fail <| IndexInOpenState index.IndexName
            | IndexState.Offline | IndexState.Closing -> state.Store.UpdateItem(index.IndexName, index)
            | _ -> return! fail <| IndexInInvalidState index.IndexName
        }
    
    [<Sealed>]
    type Service(analyzerService, store) = 
        let state = create (analyzerService, store)
        do state |> loadAllIndex
        interface IIndexService with
            member __.GetIndex(indexName) = state |> getIndex (indexName)
            member __.UpdateIndex(index) = state |> updateIndex (index)
            member __.DeleteIndex(indexName) = state |> deleteIndex (indexName)
            member __.AddIndex(index) = state |> addIndex (index)
            member __.GetAllIndex() = state |> getAllIndices
            member __.IndexExists(indexName) = succeeded <| (state |> indexExists indexName)
            member __.GetIndexStatus(indexName) = state |> indexExists (indexName)
            member __.OpenIndex(indexName) = state |> openIndex indexName
            member __.CloseIndex(indexName) = state |> closeIndex indexName
            member __.Commit(indexName) = maybe { let! _ = state |> getIndexState indexName
                                                  state.IndexWriters.[indexName] |> IndexWriter.commit }
            member __.Refresh(indexName) = maybe { let! _ = state |> getIndexState indexName
                                                   state.IndexWriters.[indexName] |> IndexWriter.refresh }
            
            member __.GetRealtimeSearchers(indexName) = 
                maybe { 
                    let! _ = state |> getIndexState indexName
                    let searchers = state.IndexWriters.[indexName] |> IndexWriter.getRealTimeSearchers
                    return searchers
                }
            
            member __.GetRealtimeSearcher(indexName, shardNo) = 
                maybe { let! _ = state |> getIndexState indexName
                        return (state.IndexWriters.[indexName] |> IndexWriter.getRealTimeSearcher shardNo) }
