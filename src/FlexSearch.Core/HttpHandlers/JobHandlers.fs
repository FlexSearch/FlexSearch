// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core.HttpHandlers

open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Api

[<Name("GET-/jobs/:id")>]
[<Sealed>]
type GetJobByIdHandler(jobService : IJobService) = 
    inherit HttpHandlerBase<unit, Job>()
        override this.Process(id, subId, body, context) = (jobService.GetJob(id.Value), Ok, NotFound)