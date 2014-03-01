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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------
module Pool =
    open System.Collections.Concurrent
    open System
    open System.Threading

    // ----------------------------------------------------------------------------
    /// Abstract base class to be implemented by all poolable object
    // ----------------------------------------------------------------------------
    [<AbstractClass>]  
    type PooledObject() as self = 
        let mutable disposed = false

        /// Internal method to cleanup resources
        let cleanup(reRegisterForFinalization: bool) = 
            if not disposed then
                if self.AllowRegeneration = true then
                    if reRegisterForFinalization then 
                        GC.ReRegisterForFinalize(self)
                    self.ReturnToPool(self)
                else
                    disposed <- true
        
        /// Responsible for returning object back to the pool. This will be set automatically by the
        /// object pool
        member val ReturnToPool: PooledObject -> unit = Unchecked.defaultof<_> with get, set
        
        /// This should be set to true to allow automatic return to the pool in case dispose is called
        member val AllowRegeneration = false with get, set

        // implementation of IDisposable
        interface IDisposable with
            member this.Dispose() =
                cleanup(false)

        // override of finalizer
        override this.Finalize() = 
            cleanup(true)     
    
        member this.Release() =
            cleanup(false)

    // ----------------------------------------------------------------------------
    /// A generic object pool which can be used for connection pooling etc.
    // ----------------------------------------------------------------------------
    type ObjectPool<'T when 'T :> PooledObject>(factory: unit -> 'T, poolSize: int, ?onAcquire: 'T -> bool, ?onRelease: 'T -> bool) as self =
        let pool = new ConcurrentQueue<'T>()
        let mutable disposed = false
        let mutable itemCount = 0L
              
        let createNewItem() =            
            // Since this method will be passed to the poolable object we have to pass the reference 
            // to the underlyinh queue for the items to be returned back cleanly
            let returnToPool (item: PooledObject) =
                pool.Enqueue(item :?> 'T)

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
        let cleanup(disposing: bool) = 
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
                cleanup(true)
                GC.SuppressFinalize(self)

        // override of finalizer
        override this.Finalize() = 
            cleanup(false)     
        
        member this.Available() = pool.Count
        member this.Total() = itemCount

        /// Acquire an instance of 'T
        member this.Acquire() =
            // if onAcquire id defined then keep on finding the poolable object till onAcquire is satisfied

            match onAcquire with
            | Some(a) ->
                let mutable item = getItem()
                let mutable success = a(item)
                while success <> true do
                    item <- getItem()
                    success <- a(item)

                    // Dispose the item which failed the onAcquire condition
                    if not success then
                        item.AllowRegeneration <- false
                        (item :> IDisposable).Dispose() 
                item
            | None -> getItem()

//        member this.Release(item: PooledObject) =
//            pool.Enqueue(item :?> 'T)

