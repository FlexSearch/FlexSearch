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

open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Concurrent

// ----------------------------------------------------------------------------
/// Various cache implementation used by Flex
// ----------------------------------------------------------------------------
module Cache =
    

    // ----------------------------------------------------------------------------
    /// Version cache store used across the system. This helps in resolving 
    /// conflicts arising out of concrrent threads trying to update a lucene document.
    /// Every document update should go through version cache to ensure the update
    /// integrity and optimistic locking. 
    /// Initially Lucene's LiveFieldValues seemed like a good alternative but it
    /// complicates the design and requires thread management
    // ----------------------------------------------------------------------------
    type VersioningCacheStore() =
        let cache = new ConcurrentDictionary<(string * string), (int * DateTime)>()
        
        let removeInvalidItems() =
            let dateTime = DateTime.Now.AddMinutes(-2.0)
            for value in cache.OrderByDescending(fun x -> snd(x.Value))
                            .SkipWhile(fun x -> snd(x.Value) > dateTime) do
                let (_, _) = cache.TryRemove(value.Key)
                ()
        do
            let observer = Observable.Interval(TimeSpan.FromMinutes(2.0)).Subscribe(fun x -> removeInvalidItems())
            ()
               
        member this.GetVersion index id =
            match cache.TryGetValue((index, id)) with
            | (true, x) -> Some(x)
            | _ -> None
        
        member this.AddVersion index id version =
            cache.TryAdd((index, id), (version, DateTime.Now))
        
        member this.UpdateVersion index id oldversion oldDateTime newVersion =
            cache.TryUpdate((index, id), (newVersion, DateTime.Now), (oldversion, oldDateTime))
        
