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
open FlexSearch.Core
open System
open System.Runtime.Caching

/// <summary>
/// Job service class which will be dynamically injected using IOC.
/// </summary>
[<Sealed>]
type JobService(persistenceStore : IPersistanceStore) = 
    let cache = MemoryCache.Default
    interface IJobService with
        
        member x.UpdateJob(job : Job) : Choice<unit, OperationMessage> = 
            let item = new CacheItem(job.JobId, job)
            let policy = new CacheItemPolicy()
            policy.AbsoluteExpiration <- DateTimeOffset.Now.AddHours(5.00)
            cache.Set(item, new CacheItemPolicy())
            Choice1Of2()
        
        member this.GetJob(jobId : string) = 
            assert (jobId <> null)
            let item = cache.GetCacheItem(jobId)
            if item <> null then Choice1Of2(item.Value :?> Job)
            else 
                Choice2Of2("JOB_NOT_FOUND:Job with the specified JobId does not exist."
                           |> GenerateOperationMessage
                           |> Append("JobId", jobId))
        
        member this.DeleteAllJobs() = 
            // Not implemented
            Choice1Of2()
