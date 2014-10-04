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

//[<Name("GET-/jobs/:id")>]
//[<Sealed>]
//type GetJobByIdHandler(jobService : IJobService) = 
//    interface IHttpHandler with
//        member this.Process(owin) = owin |> ResponseProcessor (jobService.GetJob(SubId(owin))) OK BAD_REQUEST
//
//[<Name("DELETE-/jobs")>]
//[<Sealed>]
//type DeleteJobsdHandler(jobService : IJobService) = 
//    interface IHttpHandler with
//        member this.Process(owin) = owin |> ResponseProcessor (jobService.DeleteAllJobs()) OK BAD_REQUEST
