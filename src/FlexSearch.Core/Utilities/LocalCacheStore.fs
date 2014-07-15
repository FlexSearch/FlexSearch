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
    type VersioningCacheStore() = 
        let cache = new ConcurrentDictionary<string, int>()
        interface IVersioningCacheStore with
            
            member this.GetVersion id = 
                match cache.TryGetValue(id) with
                | (true, x) -> Some(x)
                | _ -> None
            
            member this.AddVersion(id, version) = true
            member this.UpdateVersion(id, oldversion, newVersion) = true
            member this.DeleteVersion id = true
