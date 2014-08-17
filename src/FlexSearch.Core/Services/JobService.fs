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

/// <summary>
/// Job service class which will be dynamically injected using IOC. This will
/// provide the interface for all kind of job related functionality in flex.
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
/// </summary>
[<Sealed>]
type JobService(persistenceStore : IPersistanceStore) = 
    interface IJobService with
        member this.GetJob(jobId : string) = persistenceStore.Get<Job>(jobId)
        member this.DeleteAllJobs() = persistenceStore.DeleteAll<Job>()
