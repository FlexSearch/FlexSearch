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

[<Export(typeof<HttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "index")>]
type IndexModule() = 
    inherit HttpModule()
    let routes = [||]
    override this.Routes() = routes
    override this.Get(indexName: string , owin : IOwinContext) = owin.Response.ContentType <- "text/html"
    override this.Post(indexName, owin : IOwinContext) = owin.Response.ContentType <- "text/html"
    override this.Delete(indexName, owin : IOwinContext) = owin.Response.ContentType <- "text/html"
    override this.Put(indexName, owin : IOwinContext) = owin.Response.ContentType <- "text/html"

[<Export(typeof<HttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "/")>]
type RootModule() = 
    inherit HttpModule()
    let routes = [||]
    override this.Routes() = routes
    
    override this.Get(indexName, owin : IOwinContext) = 
        owin.Response.ContentType <- "text/html"
        owin.Response.StatusCode <- 200
        await (owin.Response.WriteAsync("FlexSearch 0.21"))
    
    override this.Post(indexName, owin : IOwinContext) = owin.Response.ContentType <- "text/html"
    override this.Delete(indexName, owin : IOwinContext) = owin.Response.ContentType <- "text/html"
    override this.Put(indexName, owin : IOwinContext) = owin.Response.ContentType <- "text/html"
