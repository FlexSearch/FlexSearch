module QueueTests

open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic
open System

type SingleConsumerQueueTests() = 
    
    let getSut (counter : AtomicLong) = 
        { new SingleConsumerQueue<int>() with
              member __.Process(item) = 
                  test <@ item = 1 @>
                  counter.Increment() |> ignore } :> IQueue<int>
    
    member __.``Posting 5 items to the queue will results in 5 items getting processed by the queue``() = 
        let mutable counter = new AtomicLong(0L)
        let sut = getSut (counter)
        [| 1..5 |] |> Array.iter (fun _ -> sut.Post(1) |> ignore)
        sut.Shutdown() |> Async.RunSynchronously
        test <@ counter.Value = 5L @>
    
    member __.``Sending 5 items to the queue will results in 5 items getting processed by the queue``() = 
        let mutable counter = new AtomicLong(0L)
        let sut = getSut (counter)
        [| 1..5 |] |> Array.iter (fun _ -> sut.Send(1))
        sut.Shutdown() |> Async.RunSynchronously
        test <@ counter.Value = 5L @>
    
    member __.``Items can be send in parallel to the queue``() = 
        let mutable counter = new AtomicLong(0L)
        let sut = getSut (counter)
        [| 1..50 |] |> Array.Parallel.iter (fun _ -> sut.Send(1))
        sut.Shutdown() |> Async.RunSynchronously
        test <@ counter.Value = 50L @>

type MultipleConsumerQueueTests() = 
    
    let getSut (counter : AtomicLong) = 
        let work (item : int) = 
            test <@ item = 1 @>
            counter.Increment() |> ignore
        new MultipleConsumerQueue<int>(work) :> IQueue<int>
    
    member __.``Posting 5 items to the queue will results in 5 items getting processed by the queue``() = 
        let mutable counter = new AtomicLong(0L)
        let sut = getSut (counter)
        [| 1..5 |] |> Array.iter (fun _ -> sut.Post(1) |> ignore)
        sut.Shutdown() |> Async.RunSynchronously
        test <@ counter.Value = 5L @>
    
    member __.``Sending 5 items to the queue will results in 5 items getting processed by the queue``() = 
        let mutable counter = new AtomicLong(0L)
        let sut = getSut (counter)
        [| 1..5 |] |> Array.iter (fun _ -> sut.Send(1))
        sut.Shutdown() |> Async.RunSynchronously
        test <@ counter.Value = 5L @>
    
    member __.``Items can be send in parallel to the queue``() = 
        let mutable counter = new AtomicLong(0L)
        let sut = getSut (counter)
        [| 1..50 |] |> Array.Parallel.iter (fun _ -> sut.Send(1))
        sut.Shutdown() |> Async.RunSynchronously
        test <@ counter.Value = 50L @>
