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
open FlexSearch.Core

/// <summary>
///  Get top documents
/// </summary>
/// <remarks>
/// Returns top 10 documents from the index. This is not the preferred 
/// way to retrieve documents from an index. This is provided
/// for quick testing only.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName/documents</uri>
/// <resource>document</resource>
/// <id>get-documents</id>
[<Name("GET-/indices/:id/documents")>]
[<Sealed>]
type GetDocumentsHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<unit, SearchResults>()
    override this.Process(id, subId, body, context) = 
        let count = GetIntValueFromQueryString "count" 10 context
        (documentService.GetDocuments(id.Value, count), Ok, BadRequest)

/// <summary>
///  Get document by Id
/// </summary>
/// <remarks>
/// Returns a document by id. This returns all the fields associated
/// with the current document. Use 'Search' endpoint to customize the 
/// fields to be returned.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName/documents/:documentId</uri>
/// <resource>document</resource>
/// <id>get-document-by-id</id>
[<Name("GET-/indices/:id/documents/:id")>]
[<Sealed>]
type GetDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<unit, ResultDocument>()
    override this.Process(id, subId, body, context) = (documentService.GetDocument(id.Value, subId.Value), Ok, NotFound)

/// <summary>
///  Create a new document
/// </summary>
/// <remarks>
/// Create a new document. By default this does not check if the id of the 
/// of the document is unique across the index. Use a timestamp of -1 to 
/// enforce unique id check.
/// </remarks>
/// <method>POST</method>
/// <uri>/indices/:indexName/documents</uri>
/// <resource>document</resource>
/// <id>create-document-by-id</id>
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

/// <summary>
///  Delete a document
/// </summary>
/// <remarks>
/// Delete a document by Id.
/// </remarks>
/// <method>DELETE</method>
/// <uri>/indices/:indexId/documents/:documentId</uri>
/// <resource>document</resource>
/// <id>delete-document-by-id</id>
[<Name("DELETE-/indices/:id/documents/:id")>]
[<Sealed>]
type DeleteDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = 
        (documentService.DeleteDocument(id.Value, subId.Value), Ok, BadRequest)

/// <summary>
///  Create or update a document
/// </summary>
/// <remarks>
/// Creates or updates an existing document. This is idempotent as repeated calls to the
/// endpoint will have the same effect. Many concurrency control parameters can be 
/// applied using timestamp field.
/// </remarks>
/// <method>PUT</method>
/// <uri>/indices/:indexId/documents/:documentId</uri>
/// <resource>document</resource>
/// <id>update-document-by-id</id>
[<Name("PUT-/indices/:id/documents/:id")>]
[<Sealed>]
type PutDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<FlexDocument, unit>()
    override this.Process(id, subId, body, context) = (documentService.AddOrUpdateDocument(body.Value), Ok, BadRequest)
