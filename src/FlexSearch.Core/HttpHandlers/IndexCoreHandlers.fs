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

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Utility
open Microsoft.Owin
open Newtonsoft.Json
open Owin
open System
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.IO
open System.Linq
open System.Net
open System.Net.Http

[<Name("GET-/")>]
[<Sealed>]
type GetRootHandler() = 
    
    let htmlPage = 
        let filePath = System.IO.Path.Combine(Constants.ConfFolder, "WelcomePage.html")
        if File.Exists(filePath) then 
            let pageText = System.IO.File.ReadAllText(filePath)
            pageText.Replace
                ("{version}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
        else sprintf "FlexSearch %s" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
    
    interface IHttpHandler with
        member this.Process(owin) = 
            owin.Response.ContentType <- "text/html"
            owin.Response.StatusCode <- 200
            await (owin.Response.WriteAsync htmlPage)

[<Name("GET-/indices")>]
[<Sealed>]
type GetAllIndexHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = owin |> ResponseProcessor (indexService.GetAllIndex()) OK BAD_REQUEST

[<Name("GET-/indices/:id")>]
[<Sealed>]
type GetIndexByIdHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = owin |> ResponseProcessor (indexService.GetIndex(GetIndexName(owin))) OK BAD_REQUEST

[<Name("POST-/indices/:id")>]
[<Sealed>]
type PostIndexByIdHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            match GetRequestBody<Index>(owin.Request) with
            | Choice1Of2(index) -> 
                // Index name passed in URL takes precedence
                index.IndexName <- GetIndexName(owin)
                owin |> ResponseProcessor (indexService.AddIndex(index)) OK BAD_REQUEST
            | Choice2Of2(error) -> 
                if error.ErrorCode = 6002 then 
                    // In case the error is no body defined then still try to create the index based on index name
                    let index = new Index()
                    index.IndexName <- GetIndexName(owin)
                    owin |> ResponseProcessor (indexService.AddIndex(index)) OK BAD_REQUEST
                else owin |> BAD_REQUEST error

[<Name("DELETE-/indices/:id")>]
[<Sealed>]
type DeleteIndexByIdHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            owin |> ResponseProcessor (indexService.DeleteIndex(GetIndexName(owin))) OK BAD_REQUEST

[<Name("PUT-/indices/:id")>]
[<Sealed>]
type PutIndexByIdHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            match GetRequestBody<Index>(owin.Request) with
            | Choice1Of2(index) -> 
                // Index name passed in URL takes precedence
                index.IndexName <- GetIndexName(owin)
                owin |> ResponseProcessor (indexService.UpdateIndex(index)) OK BAD_REQUEST
            | Choice2Of2(error) -> owin |> BAD_REQUEST error

[<Name("GET-/indices/:id/status")>]
[<Sealed>]
type GetStatusHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            let processRequest = 
                match indexService.GetIndexStatus(GetIndexName(owin)) with
                | Choice1Of2(status) -> Choice1Of2(new IndexStatusResponse(status))
                | Choice2Of2(e) -> Choice2Of2(e)
            owin |> ResponseProcessor processRequest OK BAD_REQUEST

[<Name("PUT-/indices/:id/status/:id")>]
[<Sealed>]
type PostStatusHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            let processRequest = 
                match SubId(owin) with
                | InvariantEqual "online" -> indexService.OpenIndex(GetIndexName(owin))
                | InvariantEqual "offline" -> indexService.CloseIndex(GetIndexName(owin))
                | _ -> Choice2Of2(Errors.HTTP_NOT_SUPPORTED  |> GenerateOperationMessage)
            owin |> ResponseProcessor processRequest OK BAD_REQUEST

[<Name("GET-/indices/:id/exists")>]
[<Sealed>]
type GetExistsHandler(indexService : IIndexService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            let processRequest = 
                match indexService.IndexExists(GetIndexName(owin)) with
                | true -> Choice1Of2()
                | false -> Choice2Of2(Errors.INDEX_NOT_FOUND  |> GenerateOperationMessage)
            owin |> ResponseProcessor processRequest OK BAD_REQUEST
