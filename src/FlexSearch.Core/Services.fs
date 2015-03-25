﻿// ----------------------------------------------------------------------------
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

open System
open System.Collections.Concurrent
open System.IO
open System.Linq
open System.Threading.Tasks
open FlexLucene.Analysis

module State = 
    /// Default state to be used by items in the 
    type ItemState = 
        | Active = 0
        | Inactive = 1
    
    /// Factory used for index creation
    type IndexFactory = LazyFactory.T<IndexWriter.T, Index.T, IndexState>
    
    let getIndexFactory (store) = LazyFactory.create<IndexWriter.T, Index.T, IndexState> None store
    
    /// Factory used for analyzer creation
    type AnalyzerFactory = LazyFactory.T<Analyzer, Analyzer.T, ItemState>
    
    let getAnalyzerFactory (store) = LazyFactory.create<Analyzer, Analyzer.T, ItemState> (Some(Analyzer.build)) store
    
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
    let loadIndex (index : Index.T) (state : State.T) = 
        maybe { 
            let setting = IndexWriter.createIndexSetting (index, state.AnalyzerFactory)
            if Args.getBool index.Online then 
                do! state.IndexFactory |> LazyFactory.updateState (index.IndexName, IndexState.Opening)
                do! state.IndexFactory |> LazyFactory.updateInstance (index.IndexName, IndexWriter.create (setting))
            else do! state.IndexFactory |> LazyFactory.updateState (index.IndexName, IndexState.Offline)
        }
    
    let openIndex (indexName) (state : State.T) = 
        maybe { 
            let! item = state.IndexFactory |> LazyFactory.getItemOrError indexName indexNotFound
            let updatedIndex = { item.MetaData with Online = Nullable(true) }
            state.Store.UpdateItem(indexName, updatedIndex)
            let setting = IndexWriter.createIndexSetting (updatedIndex, state.AnalyzerFactory)
            do! state |> loadIndex updatedIndex
        }
    
    let closeIndex (indexName) (state : State.T) = 
        maybe { 
            let! item = state.IndexFactory |> LazyFactory.getItemOrError indexName indexNotFound
            let updatedIndex = { item.MetaData with Online = Nullable(false) }
            state.Store.UpdateItem(indexName, updatedIndex)
            item.Value.Value |> IndexWriter.close
            do! state.IndexFactory |> LazyFactory.updateState (indexName, IndexState.Offline)
        }
    
    /// Load all index from the store
    let loadAllIndex (state : State.T) = 
        state.Store.GetItems<Index.T>()
        |> Seq.map (fun i -> new Task(fun _ -> loadIndex i state |> ignore))
        |> Seq.toArray
        |> Task.WaitAll
    
    /// Add a new index
    let addIndex (index : Index.T) (state : State.T) = 
        maybe { 
            do! (index :> IValidate).Validate()
            do! state |> State.indexExists index.IndexName
            do! state |> loadIndex index
            return CreateResponse(index.IndexName)
        }
    
    /// Get index by name
    let getIndex (indexName) (state : State.T) = state.Store.GetItem(indexName)
    
    /// Get all available index
    let getAllIndices (state : State.T) = state.Store.GetItems<Index.T>().ToArray()
    
    /// Delete an existing index    
    let deleteIndex (indexName) (s : State.T) = 
        IndexWriter.close (s.IndexWriters.[indexName])
        let info = s.IndexWriters.[indexName]
        s.Store.DeleteItem(indexName)
        s.IndexStates |> remove indexName
        s.IndexWriters |> remove indexName
        delDir (info.Settings.BaseFolder)
        ok()
    
    let updateIndex (index : Index.T) (state : State.T) = 
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
        let state = State.create (analyzerService, store)
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

module DocumentService = 
    /// Get a document by Id
    let getDocument (indexName) (id) = ()
    
    /// Get top 10 document from the index
    let getDocuments (indexName) = ()
    
    /// Add or update an existing document
    let addOrUpdateDocument (document : Document.T) (state : State.T) = 
        maybe { 
            do! (document :> IValidate).Validate()
            let _ = state |> IndexService.indexExists document.IndexName
            let! indexWriter = state |> IndexService.getIndexWriter (document.IndexName)
            indexWriter |> IndexWriter.updateDocument document
        }
    
    /// Add a new document to the index
    let addDocument (document : Document.T) = ()
    
    /// Delete a document by Id
    let deleteDocument (indexName) (id) = ()
    
    /// Delete all documents of an index
    let deleteAllDocument (indexName) = ()
