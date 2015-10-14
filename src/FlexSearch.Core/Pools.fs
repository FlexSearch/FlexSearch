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
open System.Collections.Concurrent
open System.Threading

/// Abstract base class to be implemented by all pool able object
[<AbstractClass>]
type PooledObject() as self = 
    let mutable disposed = false
    
    /// Internal method to cleanup resources
    let cleanup (reRegisterForFinalization : bool) = 
        if not disposed then 
            if self.AllowRegeneration = true then 
                if reRegisterForFinalization then GC.ReRegisterForFinalize(self)
                self.ReturnToPool(self)
            else disposed <- true
    
    /// Responsible for returning object back to the pool. This will be set automatically by the
    /// object pool
    member val ReturnToPool = Unchecked.defaultof<_> with get, set
    
    /// This should be set to true to allow automatic return to the pool in case dispose is called
    member val AllowRegeneration = false with get, set
    
    // implementation of IDisposable
    interface IDisposable with
        member this.Dispose() = cleanup (false)
    
    // override of finalizer
    override this.Finalize() = cleanup (true)
    member this.Release() = cleanup (false)

/// A generic object pool which can be used for connection pooling etc.
[<Sealed>]
type ObjectPool<'T when 'T :> PooledObject>(factory : unit -> 'T, poolSize : int, ?onAcquire : 'T -> bool, ?onRelease : 'T -> bool) as self = 
    let pool = new ConcurrentQueue<'T>()
    let mutable disposed = false
    let mutable itemCount = 0L
    
    let createNewItem() = 
        // Since this method will be passed to the pool able object we have to pass the reference 
        // to the underlying queue for the items to be returned back cleanly
        let returnToPool (item : PooledObject) = pool.Enqueue(item :?> 'T)
        let instance = factory()
        instance.ReturnToPool <- returnToPool
        instance.AllowRegeneration <- true
        Interlocked.Increment(&itemCount) |> ignore
        instance
    
    let getItem() = 
        match pool.TryDequeue() with
        | true, a -> a
        | _ -> createNewItem()
    
    /// Internal method to cleanup resources
    let cleanup (disposing : bool) = 
        if not disposed then 
            if disposing then 
                disposed <- true
                while not pool.IsEmpty do
                    match pool.TryDequeue() with
                    // This will stop the regeneration of the pooled items
                    | true, a -> 
                        a.AllowRegeneration <- false
                        (a :> IDisposable).Dispose()
                        Interlocked.Decrement(&itemCount) |> ignore
                    | _ -> ()
    
    do 
        for i = 1 to poolSize do
            pool.Enqueue(createNewItem())
    
    // implementation of IDisposable
    interface IDisposable with
        member this.Dispose() = 
            cleanup (true)
            GC.SuppressFinalize(self)
    
    // override of finalizer
    override this.Finalize() = cleanup (false)
    member this.Available() = pool.Count
    member this.Total() = itemCount
    /// Acquire an instance of 'T
    member this.Acquire() = 
        // if onAcquire id defined then keep on finding the pool able object till onAcquire is satisfied
        match onAcquire with
        | Some(a) -> 
            let mutable item = getItem()
            let mutable success = a (item)
            while success <> true do
                item <- getItem()
                success <- a (item)
                // Dispose the item which failed the onAcquire condition
                if not success then 
                    item.AllowRegeneration <- false
                    (item :> IDisposable).Dispose()
            item
        | None -> getItem()

/// An alternative implementation of object pool which does not require the pooled item to implement
/// any interface. So, this pool requires the caller to explicitly release items back to the pool.
/// Failing to return objects back to the pool won't affect the performance but would be counter
/// productive.
/// TODO: We can improve this design further by using array as an backing store as done by Roslyn 
/// pools but that should be confirmed by CPU/Memory profiling
type SimpleObjectPool<'T>(factory : unit -> 'T, poolSize : int, onRelease : 'T -> bool) = 
    let pool = new ConcurrentQueue<'T>()
    let mutable itemCount = 0L
    
    let createNewItem() = 
        let instance = factory()
        Interlocked.Increment(&itemCount) |> ignore
        instance
    
    do 
        for i = 1 to poolSize do
            pool.Enqueue(createNewItem())
    
    /// Total number of items available in the pool
    member this.Available() = pool.Count
    
    /// Total number of items created by the pool
    member this.Total() = itemCount
    
    /// Acquire an instance of 'T
    member this.Acquire() = 
        match pool.TryDequeue() with
        | true, a -> a
        | _ -> createNewItem()
    
    /// Release an instance of 'T
    member this.Release(instance : 'T) = 
        // Ignore the failed item as it wasn't released successfully
        if onRelease (instance) then pool.Enqueue(instance)

[<AutoOpen>]
module Pools = 
    open System.Collections.Generic
    open System
    
    /// A reusable dictionary pool
    let dictionaryPool = 
        let initialCapacity = 50
        let itemsInPool = 300
        let factory() = new Dictionary<string, string>(initialCapacity, StringComparer.OrdinalIgnoreCase)
        
        let onRelease (dict : Dictionary<string, string>) = 
            dict.Clear()
            true
        new SimpleObjectPool<Dictionary<string, string>>(factory, itemsInPool, onRelease)

[<AutoOpen>]
module BytePool = 
    type BufferSize = 
        | Small = 0
        | Large = 1
    
    let utf = Text.UTF8Encoding.UTF8
    
    let private generatePool (sizeInKB : int, itemsInPool : int) = 
        let factory() = Array.create (sizeInKB * 1024) (0uy)
        let onRelease (item : _) = true
        new SimpleObjectPool<byte array>(factory, itemsInPool, onRelease)
    
    let smallPoolSize = 128
    let largePoolSize = 4096
    let smallPoolSizeBytes = smallPoolSize * 1024
    let largePoolSizeBytes = largePoolSize * 1024
    let private smallPool = generatePool (smallPoolSize, Environment.ProcessorCount * 2)
    let private largePool = generatePool (largePoolSize, Environment.ProcessorCount * 2)
    
    let requestBuffer (bufferSize : BufferSize) = 
        match bufferSize with
        | BufferSize.Small -> smallPool.Acquire()
        | BufferSize.Large -> largePool.Acquire()
        | _ -> failwithf "Internal : Unsupported buffer size requested."
    
    let releaseBuffer (item : array<byte>) = 
        if item.Length = smallPoolSizeBytes then smallPool.Release(item)
        else 
            if item.Length = largePoolSizeBytes then largePool.Release(item)
