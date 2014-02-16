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
open FlexSearch.Utility
open Microsoft.Owin
open Newtonsoft.Json
open Owin
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.Linq
open System.Net
open System.Net.Http

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "index")>]
type IndexModule() = 
    let routes = [||]
    interface IHttpModule with
        member this.Routes() = routes
        member this.Get(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Post(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Delete(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Put(indexName, owin, state) = owin.Response.ContentType <- "text/html"

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "/")>]
type RootModule() = 
    let routes = [||]
    interface IHttpModule with
        member this.Routes() = routes
        
        member this.Get(indexName, owin, state) = 
            owin.Response.ContentType <- "text/html"
            owin.Response.StatusCode <- 200
            await (owin.Response.WriteAsync("FlexSearch 0.21"))
        
        member this.Post(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Delete(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Put(indexName, owin, state) = owin.Response.ContentType <- "text/html"
