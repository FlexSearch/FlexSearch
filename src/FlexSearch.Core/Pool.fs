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
    
    /// Abstract base class to be implemented by all poolable object
    [<AbstractClass>]  
    type PooledObject() as self = 
        let mutable disposed = false

        /// Internal method to cleanup resources
        let cleanup(disposing: bool) = 
            if not disposed then
                if disposing then
                    if self.AllowRegeneration = true then
                        self.ReturnToPool(self)
                    else
                        disposed <- true
        
        /// Responsible for returning object back to the pool. This will be set automatically by the
        /// object pool
        member val ReturnToPool: PooledObject -> unit = Unchecked.defaultof<_> with get, set
        
        /// This should be set to true to allow automatic return to the pool in case dispose is called
        member val AllowRegeneration = true with get, set

        // implementation of IDisposable
        interface IDisposable with
            member this.Dispose() =
                cleanup(true)
                GC.SuppressFinalize(self)

        // override of finalizer
        override this.Finalize() = 
            cleanup(false)     
    

    /// A generic object pool which can be used for connection pooling etc.
    type ObjectPool<'T when 'T :> PooledObject>(factory: unit -> 'T, poolSize: int) =
        let pool = new ConcurrentQueue<'T>()
        let createNewItem() =
            let returnToPool(item: PooledObject) =
                pool.Enqueue(item :?> 'T)

            let instance = factory()
            instance.ReturnToPool <- returnToPool
            instance

        do
            for i = 0 to poolSize do     
                pool.Enqueue(createNewItem())


        /// Acquire an instance of 'T
        member this.Acquire() =
            match pool.TryDequeue() with
            | true, a -> a
            | _ -> createNewItem()


        /// Release the instance. Poolable objects implement dispose which can automatically
        /// return the object to the object pool
        member this.Release(item : 'T) =
            pool.Enqueue(item)


