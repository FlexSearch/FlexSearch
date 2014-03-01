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
open FlexSearch.Api.Message
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
    interface IHttpModule with
        member this.Get(indexName, owin, state) = 
            owin |> responseProcessor (state.IndexService.GetIndex(indexName)) OK BAD_REQUEST
        
        member this.Post(indexName, owin, state) = 
            match getRequestBody<Index> (owin.Request) with
            | Choice1Of2(index) -> owin |> responseProcessor (state.IndexService.AddIndex(index)) OK BAD_REQUEST
            | Choice2Of2(error) -> owin |> BAD_REQUEST error
        
        member this.Delete(indexName, owin, state) = 
            owin |> responseProcessor (state.IndexService.DeleteIndex(indexName)) OK BAD_REQUEST
        member this.Put(indexName, owin, state) = 
            match getRequestBody<Index> (owin.Request) with
            | Choice1Of2(index) -> owin |> responseProcessor (state.IndexService.AddIndex(index)) OK BAD_REQUEST
            | Choice2Of2(error) -> owin |> BAD_REQUEST error

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "documents")>]
type DocumentModule() = 
    interface IHttpModule with
        
        member this.Get(indexName, owin, state) = 
            let processRequest = 
                maybe { 
                    match checkIdPresent (owin) with
                    | Some(id) -> 
                        // documents/{id}
                        // Return the requested document
                        return! state.IndexService.PerformQuery
                                    (indexName, new SearchQuery(indexName, (sprintf "%s = '%s'" Constants.IdField id)))
                    | None -> 
                        // documents
                        // Return top 10 documents
                        let q = new SearchQuery(indexName, (sprintf "%s = '" Constants.IdField))
                        q.Columns.Add("*")
                        q.MissingValueConfiguration.Add(Constants.IdField, MissingValueOption.Ignore)
                        return! state.IndexService.PerformQuery(indexName, q)
                }
            owin |> responseProcessor processRequest OK BAD_REQUEST
        
        member this.Post(indexName, owin, state) = 
            let processRequest = 
                maybe { 
                    match checkIdPresent (owin) with
                    | Some(id) -> 
                        // documents/{id}
                        // Add the document by id
                        let! fields = getRequestBody<Dictionary<string, string>> (owin.Request)
                        return! state.IndexService.PerformCommand(indexName, IndexCommand.Create(id, fields))
                    | None -> 
                        // documents
                        // Bulk addition of the documents
                        return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
                }
            owin |> responseProcessor processRequest OK BAD_REQUEST
        
        member this.Delete(indexName, owin, state) = 
            let processRequest = 
                maybe { 
                    match checkIdPresent (owin) with
                    | Some(id) -> 
                        // documents/{id}
                        // Delete the document by id
                        return! state.IndexService.PerformCommand(indexName, IndexCommand.Delete(id))
                    | None -> 
                        // documents
                        // Bulk delete of the documents
                        return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
                }
            owin |> responseProcessor processRequest OK BAD_REQUEST
        
        member this.Put(indexName, owin, state) = 
            let processRequest = 
                maybe { 
                    match checkIdPresent (owin) with
                    | Some(id) -> 
                        // documents/{id}
                        // Add or update the document by id
                        let! fields = getRequestBody<Dictionary<string, string>> (owin.Request)
                        return! state.IndexService.PerformCommand(indexName, IndexCommand.Update(id, fields))
                    | None -> 
                        // documents
                        // Bulk addition of the documents
                        return! Choice2Of2(MessageConstants.HTTP_NOT_SUPPORTED)
                }
            owin |> responseProcessor processRequest OK BAD_REQUEST

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "search")>]
type SearchModule() = 
    let processRequest (indexName, owin, state) = maybe { return! Choice1Of2() }
    interface IHttpModule with
        member this.Get(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Post(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Put(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE
        member this.Delete(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "exists")>]
type ExistsModule() = 
    let processRequest (indexName, owin, state) = maybe { return! Choice1Of2() }
    interface IHttpModule with
        member this.Get(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Post(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Put(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE
        member this.Delete(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "status")>]
type StatusModule() = 
    let processRequest (indexName, owin, state) = maybe { return! Choice1Of2() }
    interface IHttpModule with
        member this.Get(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Post(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Put(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE
        member this.Delete(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "analysis")>]
type AnalysisModule() = 
    let processRequest (indexName, owin, state) = maybe { return! Choice1Of2() }
    interface IHttpModule with
        member this.Get(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Post(indexName, owin, state) = 
            owin |> responseProcessor (processRequest (indexName, owin, state)) OK BAD_REQUEST
        member this.Put(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE
        member this.Delete(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_UNSUPPORTED_CONTENT_TYPE

[<Export(typeof<IHttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "/")>]
type RootModule() = 
    let routes = [||]
    interface IHttpModule with
        
        member this.Get(indexName, owin, state) = 
            owin.Response.ContentType <- "text/html"
            owin.Response.StatusCode <- 200
            await (owin.Response.WriteAsync("FlexSearch 0.21"))
        
        member this.Post(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Delete(indexName, owin, state) = owin.Response.ContentType <- "text/html"
        member this.Put(indexName, owin, state) = owin.Response.ContentType <- "text/html"
