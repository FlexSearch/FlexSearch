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
open FlexSearch.Api.Validation
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
open FlexSearch.Common
[<Name("GET-/indices/:id/documents")>]
[<Sealed>]
type GetDocumentsHandler(documentService : IDocumentService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            let count = GetIntValueFromQueryString "count" 10 owin
            owin |> ResponseProcessor (documentService.GetDocuments(GetIndexName(owin), count)) OK BAD_REQUEST

[<Name("GET-/indices/:id/documents/:id")>]
[<Sealed>]
type GetDocumentByIdHandler(documentService : IDocumentService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            owin |> ResponseProcessor (documentService.GetDocument(GetIndexName(owin), SubId(owin))) OK BAD_REQUEST

[<Name("POST-/indices/:id/documents")>]
[<Sealed>]
type PostDocumentByIdHandler(documentService : IDocumentService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            let processRequest = 
                maybe { 
                    let! document = GetRequestBody<FlexDocument>(owin.Request)
                    document.IndexName <- GetIndexName(owin)
                    do! (document :> IValidator).MaybeValidator()
                    return! documentService.AddDocument(document)
                }
            owin |> ResponseProcessor processRequest CREATED BAD_REQUEST

[<Name("DELETE-/indices/:id/documents/:id")>]
[<Sealed>]
type DeleteDocumentByIdHandler(documentService : IDocumentService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            owin |> ResponseProcessor (documentService.DeleteDocument(GetIndexName(owin), SubId(owin))) OK BAD_REQUEST

[<Name("PUT-/indices/:id/documents/:id")>]
[<Sealed>]
type PutDocumentByIdHandler(documentService : IDocumentService) = 
    interface IHttpHandler with
        member this.Process(owin) = 
            let processRequest = 
                maybe { 
                    let! document = GetRequestBody<FlexDocument>(owin.Request)
                    document.IndexName <- GetIndexName(owin)
                    do! (document :> IValidator).MaybeValidator()
                    return! documentService.AddOrUpdateDocument(document)
                }
            owin |> ResponseProcessor processRequest OK BAD_REQUEST
