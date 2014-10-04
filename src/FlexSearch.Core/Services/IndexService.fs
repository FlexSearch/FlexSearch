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
open FlexSearch.Api.Messages
open FlexSearch.Api.Validation
open FlexSearch.Common
open FlexSearch.Core
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Linq
open org.apache.lucene.search

[<Sealed>]
/// <summary>
/// Service wrapper around all index related services
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
/// </summary>
/// <param name="state"></param>
type IndexService(settingsBuilder : ISettingsBuilder, logger : ILogService, regManager : RegisterationManager, formatter : IFormatter, serverSettings : ServerSettings) = 
    
    /// <summary>
    /// Get an existing index details
    /// </summary>
    /// <param name="indexName"></param>
    let GetIndex indexName = regManager.GetIndexInfo(indexName)
    
    /// <summary>
    /// Update an existing index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="nodeState"></param>
    let UpdateIndex(indexInfo : Index) = 
        maybe { 
            do! (indexInfo :> IValidator).MaybeValidator()
            let! registeration = regManager.GetRegisteration(indexInfo.IndexName)
            match registeration.IndexState with
            | IndexState.Online -> 
                let! settings = settingsBuilder.BuildSetting(indexInfo)
                do! regManager.UpdateStatus(indexInfo.IndexName, IndexState.Closing)
                Index.CloseIndex(registeration.Index.Value)
                do! regManager.UpdateStatus(indexInfo.IndexName, IndexState.Offline)
                let! index = Index.AddIndex(settings)
                do! regManager.UpdateStatus(registeration.IndexInfo.IndexName, IndexState.Offline)
                logger.AddIndex(registeration.IndexInfo.IndexName, indexInfo)
                return! Choice1Of2()
            | IndexState.Opening -> return! Choice2Of2(Errors.INDEX_IS_OPENING |> GenerateOperationMessage)
            | IndexState.Offline | IndexState.Closing -> 
                let! settings = settingsBuilder.BuildSetting(indexInfo)
                do! regManager.UpdateRegisteration(indexInfo.IndexName, registeration.IndexState, indexInfo, None)
                logger.AddIndex(indexInfo.IndexName, indexInfo)
                return! Choice1Of2()
            | _ -> return! Choice2Of2(Errors.INDEX_IS_IN_INVALID_STATE |> GenerateOperationMessage)
        }
    
    /// <summary>
    /// Delete an existing index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let DeleteIndex indexName = 
        maybe { 
            let! registeration = regManager.GetRegisteration(indexName)
            match registeration.IndexState with
            | IndexState.Online -> 
                CloseIndex(registeration.Index.Value)
                do! regManager.RemoveRegisteration(indexName)
                // It is possible that directory might not exist if the index has never been opened
                if Directory.Exists(Path.Combine(serverSettings.DataFolder, indexName)) then 
                    Directory.Delete(Path.Combine(serverSettings.DataFolder, indexName), true)
                logger.DeleteIndex(indexName)
                return! Choice1Of2()
            | IndexState.Opening -> return! Choice2Of2(Errors.INDEX_IS_OPENING |> GenerateOperationMessage)
            | IndexState.Offline | IndexState.Closing -> 
                do! regManager.RemoveRegisteration(indexName)
                // It is possible that directory might not exist if the index has never been opened
                if Directory.Exists(Path.Combine(serverSettings.DataFolder, indexName)) then 
                    Directory.Delete(Path.Combine(serverSettings.DataFolder, indexName), true)
                logger.DeleteIndex(indexName)
                return! Choice1Of2()
            | _ -> return! Choice2Of2(Errors.INDEX_IS_IN_INVALID_STATE |> GenerateOperationMessage)
        }
    
    /// <summary>
    /// Add a new index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="nodeState"></param>
    let AddIndex(indexInfo : Index) = 
        maybe { 
            do! (indexInfo :> IValidator).MaybeValidator()
            assert (System.String.IsNullOrWhiteSpace(indexInfo.IndexName) <> true)
            match regManager.GetRegisteration(indexInfo.IndexName) with
            | Choice1Of2(_) -> return! Choice2Of2(Errors.INDEX_ALREADY_EXISTS |> GenerateOperationMessage)
            | _ -> 
                let! settings = settingsBuilder.BuildSetting(indexInfo)
                logger.AddIndex(indexInfo.IndexName, indexInfo)
                if indexInfo.Online then 
                    let! index = AddIndex(settings)
                    do! regManager.UpdateRegisteration(indexInfo.IndexName, IndexState.Online, indexInfo, Some(index))
                else do! regManager.UpdateRegisteration(indexInfo.IndexName, IndexState.Offline, indexInfo, None)
                return! Choice1Of2(new CreateResponse(Id = indexInfo.IndexName))
        }
    
    /// <summary>
    /// Get all index information
    /// </summary>
    /// <param name="nodeState"></param>
    let GetAllIndex() = regManager.GetAllIndiceInfo().ToList()
    
    /// <summary>
    /// Check whether a given index exists in the system
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let IndexExists indexName = 
        match regManager.GetStatus(indexName) with
        | Choice1Of2(_) -> true
        | _ -> false
    
    /// <summary>
    /// Get the status of a given index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let GetIndexStatus indexName = regManager.GetStatus(indexName)
    
    /// <summary>
    /// Open an existing index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let OpenIndex indexName = 
        maybe { 
            let! registeration = regManager.GetRegisteration(indexName)
            match registeration.IndexState with
            | IndexState.Online | IndexState.Opening -> 
                return! Choice2Of2(Errors.INDEX_IS_OPENING |> GenerateOperationMessage)
            | IndexState.Offline | IndexState.Closing -> 
                let! settings = settingsBuilder.BuildSetting(registeration.IndexInfo)
                let! index = Index.AddIndex(settings)
                registeration.IndexInfo.Online <- true
                do! regManager.UpdateRegisteration(indexName, IndexState.Online, registeration.IndexInfo, Some(index))
                logger.OpenIndex(indexName)
                return! Choice1Of2()
            | _ -> return! Choice2Of2(Errors.INDEX_IS_IN_INVALID_STATE |> GenerateOperationMessage)
        }
    
    /// <summary>
    /// Close an existing index
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let CloseIndex indexName = 
        maybe { 
            let! registeration = regManager.GetRegisteration(indexName)
            match registeration.IndexState with
            | IndexState.Closing | IndexState.Offline -> 
                return! Choice2Of2(Errors.INDEX_IS_ALREADY_OFFLINE |> GenerateOperationMessage)
            | _ -> 
                CloseIndex(registeration.Index.Value)
                do! regManager.UpdateRegisteration(indexName, IndexState.Offline, registeration.IndexInfo, None)
                logger.CloseIndex(indexName)
                return! Choice1Of2()
        }
    
    /// <summary>
    /// Refresh an index reader
    /// Note: There should never be any need to use this directly.
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let Refresh indexName = 
        maybe { 
            let! registeration = regManager.GetRegisteration(indexName)
            match registeration.IndexState with
            | IndexState.Closing | IndexState.Offline -> 
                return! Choice2Of2(Errors.INDEX_IS_ALREADY_OFFLINE |> GenerateOperationMessage)
            | _ -> 
                RefreshIndexJob(registeration.Index.Value)
                return! Choice1Of2()
        }
    
    /// <summary>
    /// Get all the searchers associated with the index
    /// </summary>
    let GetIndexSearchers indexName = 
        maybe { 
            let! registeration = regManager.GetRegisteration(indexName)
            match registeration.IndexState with
            | IndexState.Closing | IndexState.Offline -> 
                return! Choice2Of2(Errors.INDEX_IS_ALREADY_OFFLINE |> GenerateOperationMessage)
            | _ -> 
                let indexSearchers = new List<IndexSearcher>()
                for i in 0..registeration.Index.Value.Shards.Length - 1 do
                    let searcher = 
                        (registeration.Index.Value.Shards.[i].SearcherManager :> ReferenceManager).acquire() :?> IndexSearcher
                    indexSearchers.Add(searcher)
                return! Choice1Of2(indexSearchers)
        }
    
    /// <summary>
    /// Commit changes to the disk
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="nodeState"></param>
    let Commit indexName = 
        maybe { 
            let! registeration = regManager.GetRegisteration(indexName)
            match registeration.IndexState with
            | IndexState.Closing | IndexState.Offline -> 
                return! Choice2Of2(Errors.INDEX_IS_ALREADY_OFFLINE |> GenerateOperationMessage)
            | _ -> 
                CommitJob(registeration.Index.Value)
                return! Choice1Of2()
        }
    
    /// <summary>
    /// Load all indices for the node
    /// </summary>
    /// <param name="nodeState"></param>
    let LoadAllIndex() = 
        for x in Directory.EnumerateDirectories(serverSettings.DataFolder) do
            let confPath = Path.Combine(x, "conf.yml")
            if File.Exists(confPath) then 
                use stream = new FileStream(confPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                let indexInfo = formatter.DeSerialize<Index>(stream)
                if indexInfo.Online then 
                    try 
                        match settingsBuilder.BuildSetting(indexInfo) with
                        | Choice1Of2(flexIndexSetting) -> 
                            match Index.AddIndex(flexIndexSetting) with
                            | Choice1Of2(index) -> 
                                regManager.UpdateRegisteration
                                    (indexInfo.IndexName, IndexState.Online, indexInfo, Some(index)) |> ignore
                            | _ -> ()
                        | _ -> ()
                    //indexLogger.Info(sprintf "Index: %s loaded successfully." x.IndexName)
                    with ex -> ()
                //indexLogger.Error("Loading index from file failed.", ex)
                else 
                    //indexLogger.Info(sprintf "Index: %s is not loaded as it is set to be offline." x.IndexName)
                    regManager.UpdateRegisteration(indexInfo.IndexName, IndexState.Online, indexInfo, None) |> ignore
    
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
        member this.Refresh indexName = Refresh indexName
        member this.GetIndexSearchers indexName = GetIndexSearchers indexName
