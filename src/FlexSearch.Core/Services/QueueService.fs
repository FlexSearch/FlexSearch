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

/// <summary>
/// Service wrapper around all document queuing services
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
/// </summary>
/// <param name="state"></param>
[<Sealed>]
type QueueService(state : INodeState, documentService : IDocumentService) = 
    
    let executionBlockOptions() = 
        let executionBlockOption = new ExecutionDataflowBlockOptions()
        executionBlockOption.MaxDegreeOfParallelism <- -1
        executionBlockOption.BoundedCapacity <- 100
        executionBlockOption
    
    /// <summary>
    /// Add queue processing method
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="documentId"></param>
    /// <param name="fields"></param>
    /// <param name="nodeState"></param>
    let processAddQueueItems (indexName, documentId, fields) = 
        documentService.AddDocument(indexName, documentId, fields) |> ignore
    
    /// <summary>
    /// Add or update processing queue method
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="documentId"></param>
    /// <param name="fields"></param>
    /// <param name="nodeState"></param>
    let processAddOrUpdateQueueItems (indexName, documentId, fields) = 
        documentService.AddOrUpdateDocument(indexName, documentId, fields) |> ignore
    
    /// <summary>
    /// Queue for add operation 
    /// </summary>
    let addQueue : ActionBlock<string * string * Dictionary<string, string>> = 
        new ActionBlock<string * string * Dictionary<string, string>>(processAddQueueItems, executionBlockOptions())
    
    /// <summary>
    /// Queue for add or update operation 
    /// </summary>
    let addOrUpdateQueue : ActionBlock<string * string * Dictionary<string, string>> = 
        new ActionBlock<string * string * Dictionary<string, string>>(processAddOrUpdateQueueItems, 
                                                                      executionBlockOptions())
    
    interface IQueueService with
        member this.AddDocumentQueue(indexName, documentId, fields) = 
            await (addQueue.SendAsync((indexName, documentId, fields)))
        member this.AddOrUpdateDocumentQueue(indexName, documentId, fields) = 
            await (addOrUpdateQueue.SendAsync((indexName, documentId, fields)))
