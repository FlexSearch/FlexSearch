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
open FlexSearch.Api.Messages
open FlexSearch.Api.Validation
open FlexSearch.Common
open FlexSearch.Core
open Microsoft.Owin
open Owin
open System
open System.Collections.Generic

[<Name("GET-/indices/:id/documents")>]
[<Sealed>]
type GetDocumentsHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<unit, SearchResults>()
    override this.Process(id, subId, body, context) = 
        let count = GetIntValueFromQueryString "count" 10 context
        (documentService.GetDocuments(id.Value, count), Ok, BadRequest)

[<Name("GET-/indices/:id/documents/:id")>]
[<Sealed>]
type GetDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<unit, ResultDocument>()
    override this.Process(id, subId, body, context) = 
        (documentService.GetDocument(id.Value, subId.Value), Ok, NotFound)

[<Name("POST-/indices/:id/documents")>]
[<Sealed>]
type PostDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<FlexDocument, CreateResponse>()
    override this.Process(id, subId, body, context) = 
        match documentService.AddDocument(body.Value) with
        | Choice1Of2(response) -> (Choice1Of2(response), Created, BadRequest)
        | Choice2Of2(error) -> 
            if Errors.INDEXING_DOCUMENT_ID_ALREADY_EXISTS.Contains(error.ErrorCode) then 
                (Choice2Of2(error), Created, Conflict)
            else (Choice2Of2(error), Created, BadRequest)

[<Name("DELETE-/indices/:id/documents/:id")>]
[<Sealed>]
type DeleteDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = 
        (documentService.DeleteDocument(id.Value, subId.Value), Ok, BadRequest)

[<Name("PUT-/indices/:id/documents/:id")>]
[<Sealed>]
type PutDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<FlexDocument, unit>()
    override this.Process(id, subId, body, context) = (documentService.AddOrUpdateDocument(body.Value), Ok, BadRequest)
