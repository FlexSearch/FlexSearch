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

open System
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open FSharpx.Extras

/// Interface to be used by the services which require notification of
/// server shutdown so that they can start performing there internal 
/// cleanup
type IRequireNotificationForShutdown = 
    abstract Shutdown : unit -> Async<unit>

/// Interface which represents the functionality exposed by the queue. A queue
/// can be used to process any number of arbitrary items one at a time or many in
/// parallel. There are multiple implementations of the queue available in the
/// system
type IQueue<'T> = 
    inherit IRequireNotificationForShutdown
    
    /// The maximum number of items that the queue can hold 
    abstract Capacity : int
    
    /// The number of items still to be processed
    abstract Count : int
    
    /// Asynchronously block till the queue has capacity to accept the item.
    abstract Send : 'T -> unit
    
    /// Post the item to queue and return true if the item was accepted by the
    /// queue for processing.
    abstract Post : 'T -> bool

/// Represents a single consumer queue based around TPL data flow. In principle this is
/// similar to F# Mailbox but helps in overcoming a big limitation of mailbox that is 
/// to determine the total number of unprocessed items in the queue and also the ability
/// to wait for the queue to finish processing.
/// Design Notes
/// The reason to use AbstractClass instead of passing the process method to the constructor
/// is to avoid closures. An abstract class will allow the process method to use class level
/// variables without relying on to closures. This is an significant when the consumer is
/// used with objects which have a high creation cost.
/// This does make it more difficult to use compared to TPL Data flow blocks directly but the
/// the complexity can be reduced by using F# object expressions:
///     let _ = {new SingleConsumerQueue<int>() with
///                member __.Process(item) = ()
///             }
[<AbstractClassAttribute>]
type SingleConsumerQueue<'T>(?capacity0 : int) as self = 
    let capacity = defaultArg capacity0 -1
    let buffer = new BufferBlock<'T>(new DataflowBlockOptions(BoundedCapacity = capacity))
    
    let processItem() = 
        async { 
            let mutable cont = true
            while cont do
                let! value = Async.AwaitTask(buffer.ReceiveAsync())
                self.Process(value)
                let! cont1 = Async.AwaitTask(buffer.OutputAvailableAsync())
                cont <- cont1
        }
    
    do Async.Start(processItem())
    abstract Process : 'T -> unit
    
    interface IQueue<'T> with
        member __.Capacity = capacity
        member __.Count = buffer.Count
        
        member __.Send(item : 'T) = 
            Async.AwaitTask <| buffer.SendAsync(item)
            |> Async.RunSynchronously
            |> ignore
        
        member __.Post(item : 'T) = buffer.Post(item)
    
    interface IRequireNotificationForShutdown with
        member __.Shutdown() = 
            async { 
                buffer.Complete()
                do! Async.AwaitTask(buffer.Completion)
            }

/// Queue which supports processing of items in parallel. The degree of parallelism can be
/// set. The work function is expected to be pure so as to avoid any threading issues.
/// Note:
/// We can use this instead of SingleConsumerQueue with the degreeeOfParallelism = 1 when we 
/// don't there in no state in the work function.
type MultipleConsumerQueue<'T>(work : 'T -> unit, ?capacity0 : int, ?degreeeOfParallelism0 : int) = 
    let capacity = defaultArg capacity0 100
    let maxDegreeOfParallelism = defaultArg degreeeOfParallelism0 -1
    let buffer = 
        new ActionBlock<'T>(work, 
                            new ExecutionDataflowBlockOptions(BoundedCapacity = capacity, 
                                                              MaxDegreeOfParallelism = maxDegreeOfParallelism))
    
    interface IQueue<'T> with
        member __.Capacity = capacity
        member __.Count = buffer.InputCount
        
        member __.Send(item : 'T) = 
            Async.AwaitTask <| buffer.SendAsync(item)
            |> Async.RunSynchronously
            |> ignore
        
        member __.Post(item : 'T) = buffer.Post(item)
    
    interface IRequireNotificationForShutdown with
        member __.Shutdown() = 
            async { 
                buffer.Complete()
                do! Async.AwaitTask(buffer.Completion)
            }