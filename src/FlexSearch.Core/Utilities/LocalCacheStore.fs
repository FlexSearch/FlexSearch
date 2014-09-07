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
namespace FlexSearch.Core

open System
open System.Collections.Concurrent
open System.Linq
open CSharpTest.Net.Collections

// ----------------------------------------------------------------------------
/// Various cache implementation used by Flex
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Cache = 
    // ----------------------------------------------------------------------------
    /// Version cache store used across the system. This helps in resolving 
    /// conflicts arising out of concurrent threads trying to update a Lucene document.
    /// Every document update should go through version cache to ensure the update
    /// integrity and optimistic locking.
    /// In order to reduce contention there will be one CacheStore per shard. 
    /// Initially Lucene's LiveFieldValues seemed like a good alternative but it
    /// complicates the design and requires thread management
    // ----------------------------------------------------------------------------
    [<Sealed>]
    type VersioningCacheStore(shards : FlexShardWriter []) = 
        let cache = new LurchTable<string, Int64>(LurchTableOrder.Insertion, 10000)
        interface IVersioningCacheStore with
            
            member x.AddOrUpdate(id : string, version : int64, comparison : int64) = 
                match cache.TryGetValue(id) with
                | true, oldValue -> 
                    if comparison = 0L then 
                        // It is an unconditional update
                        cache.TryUpdate(id, version)
                    else cache.TryUpdate(id, version, comparison)
                | _ -> cache.TryAdd(id, version)
            
            member x.TryGetValue(id : string) = cache.TryGetValue(id)
            member x.Delete(id : string, version : Int64) = cache.TryRemove(id)
