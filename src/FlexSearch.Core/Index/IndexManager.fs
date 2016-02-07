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

open FlexSearch.Core
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open System.Collections.Concurrent
open System.IO
open System.Threading

/// Represent the internal representation of the index state
type IndexState = 
    { IndexDto : Index
      IndexStatus : IndexStatus
      IndexWriter : IndexWriter option }

/// Index Manager module is responsible for the life cycle of an index on the node. 
/// Life cycle management will include state management also.
/// Note: The logical hierarchy of objects will be
///     IndexManager : Manage life cycle of multiple indices
///             -> has Many -> 
///     IndexWriter : Manage a index and all its shards
///             -> has Many -> 
///     ShardWriter : Responsible for managing single shard of an index
type IndexManager = 
    { Store : ConcurrentDictionary<string, IndexState>
      EventAggregrator : EventAggregator
      ThreadSafeFileWriter : ThreadSafeFileWriter
      GetAnalyzer : string -> Result<LuceneAnalyzer>
      GetComputedScript : string -> Result<ComputedDelegate * string []> }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module IndexManager = 
    let path = 
        Constants.ConfFolder +/ "Indices"
        |> Directory.CreateDirectory
        |> fun x -> x.FullName
    
    /// Returns IndexNotFound error
    let indexNotFound (indexName) = IndexNotFound <| indexName
    
    let createIndexState (dto, status) = 
        { IndexDto = dto
          IndexStatus = status
          IndexWriter = None }
    
    let createIndexStateWithWriter (dto, status, writer) = 
        { IndexDto = dto
          IndexStatus = status
          IndexWriter = Some(writer) }
    
    /// Updates an index status to the given value
    let updateState (newState : IndexState) (t : IndexManager) = 
        match t.Store.TryGetValue(newState.IndexDto.IndexName) with
        | true, state -> 
            match t.Store.TryUpdate(state.IndexDto.IndexName, newState, state) with
            | true -> 
                t.EventAggregrator.Push(IndexStatusChange(state.IndexDto.IndexName, newState.IndexStatus.ToString()))
                okUnit
            | false -> 
                fail 
                <| UnableToUpdateIndexStatus
                       (state.IndexDto.IndexName, state.IndexStatus.ToString(), newState.IndexStatus.ToString())
        | _ -> 
            match t.Store.TryAdd(newState.IndexDto.IndexName, newState) with
            | true -> 
                t.EventAggregrator.Push(IndexStatusChange(newState.IndexDto.IndexName, newState.IndexStatus.ToString()))
                okUnit
            | false -> 
                fail <| UnableToUpdateIndexStatus(newState.IndexDto.IndexName, "None", newState.IndexDto.ToString())
    
    /// Check if the given index exists
    let indexExists (indexName) (t : IndexManager) = 
        match t.Store.TryGetValue(indexName) with
        | true, _ -> okUnit
        | _ -> fail <| indexNotFound indexName
    
    /// Checks if a given index is online or not. If it is 
    /// online then return the index writer
    let indexOnline (indexName) (t : IndexManager) = 
        match t.Store.TryGetValue(indexName) with
        | true, state -> 
            match state.IndexStatus with
            | IndexStatus.Online when state.IndexWriter.IsSome -> ok <| state.IndexWriter.Value
            | IndexStatus.Online -> failwithf "Internal Error: Index is in invalid state."
            | _ -> fail <| IndexShouldBeOnline indexName
        | _ -> fail <| indexNotFound indexName
    
    let indexState (indexName) (t : IndexManager) = 
        match t.Store.TryGetValue(indexName) with
        | true, state -> ok <| state
        | _ -> fail <| indexNotFound indexName
    
    /// Load a index from the given index DTO
    let loadIndex (dto : Index) (t : IndexManager) = 
        maybe { 
            do! t |> updateState (createIndexState (dto, IndexStatus.Opening))
            if dto.Active then 
                let! setting = IndexWriter.createIndexSetting (dto, t.GetAnalyzer, t.GetComputedScript)
                let indexWriter = IndexWriter.create (setting)
                do! t |> updateState (createIndexStateWithWriter (dto, IndexStatus.Online, indexWriter))
            else do! t |> updateState (createIndexState (dto, IndexStatus.Offline))
        }
    
    /// Loads all indices from the given path
    let loadAllIndex (t : IndexManager) = 
        let loadFromFile (path) = 
            match t.ThreadSafeFileWriter.ReadFile<Index>(path) with
            | Ok(dto) -> Some(dto)
            | Fail(error) -> 
                Logger.Log(error)
                None
        
        let queueOnThreadPool (dto : Index) = 
            fun _ -> 
                try 
                    t
                    |> loadIndex dto
                    |> (Logger.Log >> ignore)
                with e -> 
                    Logger.Log
                        (sprintf "Index Loading Error. Index Name: %s" dto.IndexName, e, MessageKeyword.Node, 
                         MessageLevel.Error)
            |> ThreadPool.QueueUserWorkItem
            |> ignore
        
        loopFiles (path)
        |> Seq.map loadFromFile
        |> Seq.choose id
        |> Seq.iter queueOnThreadPool
    
    /// Add a new index to the node
    let addIndex (index : Index) (t : IndexManager) = 
        maybe { 
            do! validate index
            match t |> indexExists index.IndexName with
            | Ok(_) -> return! fail <| IndexAlreadyExists(index.IndexName)
            | _ -> 
                do! t.ThreadSafeFileWriter.WriteFile(path +/ index.IndexName, index)
                do! t |> loadIndex index
                return CreationId(index.IndexName)
        }
    
    /// Shut down an existing index without saving the configuration as Inactive
    let shutdownIndex (indexName : string) (t : IndexManager) = 
        maybe { 
            let! indexState = t |> indexState indexName
            match indexState.IndexStatus with
            | IndexStatus.Closing | IndexStatus.Offline -> return! fail <| IndexIsAlreadyOffline(indexName)
            | IndexStatus.Opening -> return! fail <| IndexIsOpening(indexName) 
            | _ -> 
                indexState.IndexDto.Active <- false
                match indexState.IndexWriter with 
                | Some(iw) -> iw |> IndexWriter.close
                | _ ->  return! fail <| IndexWriterNotCreatedYet(indexName)
                do! t |> updateState (createIndexState (indexState.IndexDto, IndexStatus.Offline))
            return indexState
        }
    
    /// Close an existing index by shutting it down and saving it as Inactive
    let closeIndex (indexName : string) (t : IndexManager) = 
        t
        |> shutdownIndex indexName
        >>= (fun indexState -> t.ThreadSafeFileWriter.WriteFile(path +/ indexName, indexState.IndexDto))
    
    /// open an existing index and set the status to online
    let openIndex (indexName : string) (t : IndexManager) = 
        maybe { 
            let! indexState = t |> indexState indexName
            match indexState.IndexStatus with
            | IndexStatus.Opening | IndexStatus.Online -> return! fail <| IndexIsAlreadyOnline(indexName)
            | _ -> 
                indexState.IndexDto.Active <- true
                do! t.ThreadSafeFileWriter.WriteFile(path +/ indexName, indexState.IndexDto)
                do! t |> loadIndex indexState.IndexDto
        }
    
    /// Update an existing index with the new provided details
    let updateIndex (index : Index) (t : IndexManager) = 
        let wasActive = index.Active
        t
        |> closeIndex index.IndexName
        >>= fun _ -> 
            index.Active <- wasActive
            t |> loadIndex index
    
    /// Deletes an existing index
    let deleteIndex (indexName : string) (t : IndexManager) = 
        maybe { 
            // Try closing the index
            do! match t |> closeIndex indexName with
                | Ok(_) -> okUnit
                | Fail(e) when e.OperationMessage().ErrorCode = "IndexIsAlreadyOffline" -> okUnit
                | Fail(e) -> fail <| e
            t.Store.TryRemove(indexName) |> ignore
            // Delete the index configuration file
            do! t.ThreadSafeFileWriter.DeleteFile(path +/ indexName)
            // Data might not be present for this index
            if (Directory.Exists(DataFolder +/ indexName)) then delDir (DataFolder +/ indexName)
        }
    
    /// Create a new 
    let create (eventAggregrator, threadSafeFileWriter, getAnalyzer, getComputedScript) = 
        { Store = conDict<IndexState>()
          EventAggregrator = eventAggregrator
          ThreadSafeFileWriter = threadSafeFileWriter
          GetAnalyzer = getAnalyzer
          GetComputedScript = getComputedScript }
    
    /// Returns the disk usage of an index
    let getDiskUsage (indexName : string) (t : IndexManager) = 
        match t.Store.ContainsKey indexName with
        | true -> ok <| getFolderSize (DataFolder +/ indexName)
        | _ -> fail <| IndexNotFound indexName
