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
namespace FlexSearch.Core.Services

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Linq
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
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

/// <summary>
/// Service wrapper around all index related services
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
/// </summary>
/// <param name="state"></param>
[<Sealed>]
type IndexService(nodeState : INodeState, settingsBuilder : ISettingsBuilder, logger: ILogService) = 
    
    /// <summary>
    /// Get an existing index details
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let GetIndex indexName = 
        match nodeState.IndicesState.IndexStatus.TryGetValue(indexName) with
        | (true, _) -> 
            match nodeState.PersistanceStore.Get<Index>(indexName) with
            | Choice1Of2(a) -> Choice1Of2(a)
            | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
        | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
    
    /// <summary>
    /// Update an existing index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="nodeState"></param>
    let UpdateIndex(index : Index) = 
        maybe { 
            let! status = nodeState.IndicesState.GetStatus(index.IndexName)
            match status with
            | IndexState.Online -> 
                let! flexIndex = nodeState.IndicesState.GetRegisteration(index.IndexName)
                let! settings = settingsBuilder.BuildSetting(index)
                Index.CloseIndex(nodeState.IndicesState, flexIndex)
                do! Index.AddIndex(nodeState.IndicesState, settings)
                nodeState.PersistanceStore.Put index.IndexName index |> ignore
                logger.AddIndex(index.IndexName, index)
                return! Choice1Of2()
            | IndexState.Opening -> return! Choice2Of2(MessageConstants.INDEX_IS_OPENING)
            | IndexState.Offline | IndexState.Closing -> 
                let settings = settingsBuilder.BuildSetting(index)
                nodeState.PersistanceStore.Put index.IndexName index |> ignore
                logger.AddIndex(index.IndexName, index)
                return! Choice1Of2()
            | _ -> return! Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
        }
    
    /// <summary>
    /// Delete an existing index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let DeleteIndex indexName = 
        maybe { 
            let! status = nodeState.IndicesState.GetStatus(indexName)
            match status with
            | IndexState.Online -> 
                let! flexIndex = nodeState.IndicesState.GetRegisteration(indexName)
                CloseIndex(nodeState.IndicesState, flexIndex)
                nodeState.PersistanceStore.Delete<Index> indexName |> ignore
                nodeState.IndicesState.IndexRegisteration.TryRemove(indexName) |> ignore
                nodeState.IndicesState.IndexStatus.TryRemove(indexName) |> ignore
                // It is possible that directory might not exist if the index has never been opened
                if Directory.Exists(Constants.DataFolder + "\\" + indexName) then 
                    Directory.Delete(flexIndex.IndexSetting.BaseFolder, true)
                logger.DeleteIndex(indexName)
                return! Choice1Of2()
            | IndexState.Opening -> return! Choice2Of2(MessageConstants.INDEX_IS_OPENING)
            | IndexState.Offline | IndexState.Closing -> 
                nodeState.PersistanceStore.Delete<Index> indexName |> ignore
                nodeState.IndicesState.IndexRegisteration.TryRemove(indexName) |> ignore
                nodeState.IndicesState.IndexStatus.TryRemove(indexName) |> ignore
                // It is possible that directory might not exist if the index has never been opened
                if Directory.Exists(Constants.DataFolder + "\\" + indexName) then 
                    Directory.Delete(Constants.DataFolder + "\\" + indexName, true)
                logger.DeleteIndex(indexName)
                return! Choice1Of2()
            | _ -> return! Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
        }
    
    /// <summary>
    /// Add a new index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="nodeState"></param>
    let AddIndex(index : Index) = 
        maybe { 
            match nodeState.IndicesState.IndexStatus.TryGetValue(index.IndexName) with
            | (true, _) -> return! Choice2Of2(MessageConstants.INDEX_ALREADY_EXISTS)
            | _ -> 
                let! settings = settingsBuilder.BuildSetting(index)
                nodeState.PersistanceStore.Put index.IndexName index |> ignore
                logger.AddIndex(index.IndexName, index)
                if index.Online then do! AddIndex(nodeState.IndicesState, settings)
                else do! nodeState.IndicesState.AddStatus(index.IndexName, IndexState.Offline)
        }
    
    /// <summary>
    /// Get all index information
    /// </summary>
    /// <param name="nodeState"></param>
    let GetAllIndex() = nodeState.PersistanceStore.GetAll<Index>().ToList()
    
    /// <summary>
    /// Check whether a given index exists in the system
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let IndexExists indexName = 
        match nodeState.IndicesState.IndexStatus.TryGetValue(indexName) with
        | (true, _) -> true
        | _ -> false
    
    /// <summary>
    /// Get the status of a given index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let GetIndexStatus indexName = 
        match nodeState.IndicesState.IndexStatus.TryGetValue(indexName) with
        | (true, status) -> Choice1Of2(status)
        | _ -> Choice2Of2(MessageConstants.INDEX_NOT_FOUND)
    
    /// <summary>
    /// Open an existing index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let OpenIndex indexName = 
        maybe { 
            let! status = nodeState.IndicesState.GetStatus(indexName)
            match status with
            | IndexState.Online | IndexState.Opening -> return! Choice2Of2(MessageConstants.INDEX_IS_OPENING)
            | IndexState.Offline | IndexState.Closing -> 
                let! index = nodeState.PersistanceStore.Get<Index>(indexName)
                let! settings = settingsBuilder.BuildSetting(index)
                do! Index.AddIndex(nodeState.IndicesState, settings)
                index.Online <- true
                nodeState.PersistanceStore.Put indexName index |> ignore
                logger.OpenIndex(indexName)
                return! Choice1Of2()
            | _ -> return! Choice2Of2(MessageConstants.INDEX_IS_IN_INVALID_STATE)
        }
    
    /// <summary>
    /// Close an existing index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let CloseIndex indexName = 
        maybe { 
            let! status = nodeState.IndicesState.GetStatus(indexName)
            match status with
            | IndexState.Closing | IndexState.Offline -> return! Choice2Of2(MessageConstants.INDEX_IS_ALREADY_OFFLINE)
            | _ -> 
                let! index = nodeState.IndicesState.GetRegisteration(indexName)
                CloseIndex(nodeState.IndicesState, index)
                let! index' = nodeState.PersistanceStore.Get<Index>(indexName)
                index'.Online <- false
                nodeState.PersistanceStore.Put indexName index' |> ignore
                logger.CloseIndex(indexName)
                return! Choice1Of2()
        }
    
    /// <summary>
    /// Commit changes to the disk
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let Commit indexName = maybe { let! (flexIndex, documentTemplate) = Index.IndexExists
                                                                            (nodeState.IndicesState, indexName)
                                   flexIndex.Shards |> Array.iter (fun shard -> shard.IndexWriter.commit()) }
    
    /// <summary>
    /// Load all indices for the node
    /// </summary>
    /// <param name="nodeState"></param>
    let LoadAllIndex() = 
        for x in nodeState.PersistanceStore.GetAll<Index>() do
            if x.Online then 
                try 
                    match settingsBuilder.BuildSetting(x) with
                    | Choice1Of2(flexIndexSetting) -> Index.AddIndex(nodeState.IndicesState, flexIndexSetting) |> ignore
                    | Choice2Of2(e) -> ()
                //indexLogger.Info(sprintf "Index: %s loaded successfully." x.IndexName)
                with ex -> ()
            //indexLogger.Error("Loading index from file failed.", ex)
            else 
                //indexLogger.Info(sprintf "Index: %s is not loaded as it is set to be offline." x.IndexName)
                nodeState.IndicesState.IndexStatus.TryAdd(x.IndexName, IndexState.Offline) |> ignore
    
    do LoadAllIndex()
    interface IIndexService with
        member this.GetIndex indexName = GetIndex indexName
        member this.UpdateIndex index = UpdateIndex index
        member this.DeleteIndex indexName = DeleteIndex indexName
        member this.AddIndex index = AddIndex index
        member this.GetAllIndex() = Choice1Of2(GetAllIndex())
        member this.IndexExists indexName = IndexExists indexName
        member this.GetIndexStatus indexName = GetIndexStatus indexName
        member this.OpenIndex indexName = OpenIndex indexName
        member this.CloseIndex indexName = CloseIndex indexName
        member this.Commit indexName = Commit indexName
