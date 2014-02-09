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
namespace FlexSearch.Core.HttpModule

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Core.State
open Newtonsoft.Json
open ServiceStack
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.Linq
open System.Net
open System.Net.Http

type Hello() = 
    
    [<Description("Hello world")>] member val Name = "" with get, set
    
    member val Place = "" with get, set

type HelloResponse = 
    { mutable Result : string }

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "indices")>]
type IndexModule() = 
    inherit Service()
    
    let routes = 
        [| { RequestType = typeof<Hello>
             RestPath = "/indices"
             Verbs = "GET"
             Summary = ""
             Notes = "" } |]
    
    interface IHttpModule with
        member this.Routes() = routes
    
    member this.Get(req : Hello) = { Result = "Hello, " + req.Name }
