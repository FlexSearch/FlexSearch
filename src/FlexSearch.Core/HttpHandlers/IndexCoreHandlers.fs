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
open FlexSearch.Core.HttpHelpers
open FlexSearch.Utility
open System.Collections.Generic
open System.IO

[<Sealed>]
[<Name("GET-/ping")>]
type PingHandler() = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = (Choice1Of2(), Ok, BadRequest)

[<Name("GET-/")>]
[<Sealed>]
type GetRootHandler() = 
    inherit HttpHandlerBase<unit, unit>(fullControl0 = true)
    
    let htmlPage = 
        let filePath = System.IO.Path.Combine(Constants.ConfFolder, "WelcomePage.html")
        if File.Exists(filePath) then 
            let pageText = System.IO.File.ReadAllText(filePath)
            pageText.Replace
                ("{version}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
        else sprintf "FlexSearch %s" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
    
    override this.Process(owin) = 
        owin.Response.ContentType <- "text/html"
        owin.Response.StatusCode <- 200
        await (owin.Response.WriteAsync htmlPage)

/// <summary>
///  Get all indices
/// </summary>
/// <remarks>
/// Returns all the indices present.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices</uri>
/// <resource>index</resource>
/// <id>get-all-index</id>
[<Name("GET-/indices")>]
[<Sealed>]
type GetAllIndexHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<unit, List<Index>>()
    override this.Process(id, subId, body, context) = (indexService.GetAllIndex(), Ok, BadRequest)

/// <summary>
///  Get an index
/// </summary>
/// <remarks>
/// Returns an index by name.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName</uri>
/// <resource>index</resource>
/// <id>get-index-by-id</id>
[<Name("GET-/indices/:id")>]
[<Sealed>]
type GetIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<unit, Index>()
    override this.Process(id, subId, body, context) = (indexService.GetIndex(id.Value), Ok, BadRequest)

/// <summary>
/// Create an index
/// </summary>
/// <remarks>
/// Create a new index.
/// </remarks>
/// <method>POST</method>
/// <uri>/indices</uri>
/// <resource>index</resource>
/// <id>create-index-by-id</id>
[<Name("POST-/indices")>]
[<Sealed>]
type PostIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<Index, CreateResponse>()
    override this.Process(id, subId, body, context) = 
        match indexService.AddIndex(body.Value) with
        | Choice1Of2(response) -> (Choice1Of2(response), Created, BadRequest)
        | Choice2Of2(error) -> 
            if Errors.INDEX_ALREADY_EXISTS.Contains(error.ErrorCode) then (Choice2Of2(error), Created, Conflict)
            else (Choice2Of2(error), Created, BadRequest)

/// <summary>
/// Delete an index
/// </summary>
/// <remarks>
/// Deletes an index by name.
/// </remarks>
/// <method>DELETE</method>
/// <uri>/indices/:indexName</uri>
/// <resource>index</resource>
/// <id>delete-index-by-id</id>
[<Name("DELETE-/indices/:id")>]
[<Sealed>]
type DeleteIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = (indexService.DeleteIndex(id.Value), Ok, BadRequest)

/// <summary>
/// Update an index
/// </summary>
/// <remarks>
/// Updates an index by name.
/// </remarks>
/// <method>PUT</method>
/// <uri>/indices/:indexName</uri>
/// <resource>index</resource>
/// <id>update-index-by-id</id>
[<Name("PUT-/indices/:id")>]
[<Sealed>]
type PutIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<Index, unit>()
    override this.Process(id, subId, body, context) = 
        // Index name passed in URL takes precedence
        body.Value.IndexName <- id.Value
        (indexService.UpdateIndex(body.Value), Ok, BadRequest)

/// <summary>
/// Get index status
/// </summary>
/// <remarks>
/// Returns an index status.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName/status</uri>
/// <resource>index</resource>
/// <id>get-index-status</id>
[<Name("GET-/indices/:id/status")>]
[<Sealed>]
type GetStatusHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<unit, IndexStatusResponse>()
    override this.Process(id, subId, body, context) = 
        let response = 
            match indexService.GetIndexStatus(id.Value) with
            | Choice1Of2(state) -> Choice1Of2(new IndexStatusResponse(Status = state))
            | Choice2Of2(error) -> Choice2Of2(error)
        (response, Ok, BadRequest)

/// <summary>
/// Update index status
/// </summary>
/// <remarks>
/// Update index status. An index should be online before it can be used for any operation.
/// </remarks>
/// <method>PUT</method>
/// <uri>/indices/:indexName/status/:online|offline</uri>
/// <resource>index</resource>
/// <id>update-index-status</id>
[<Name("PUT-/indices/:id/status/:id")>]
[<Sealed>]
type PutStatusHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = 
        match subId.Value with
        | InvariantEqual "online" -> (indexService.OpenIndex(id.Value), Ok, BadRequest)
        | InvariantEqual "offline" -> (indexService.CloseIndex(id.Value), Ok, BadRequest)
        | _ -> (Choice2Of2(Errors.HTTP_NOT_SUPPORTED |> GenerateOperationMessage), Ok, BadRequest)

/// <summary>
/// Check if an index exists
/// </summary>
/// <remarks>
/// Check if an index exists. Even Get Index endpoint can be used to check an index 
/// availability but this is a quicker method with minimal payload.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName/exists</uri>
/// <resource>index</resource>
/// <id>check-index-exists</id>
[<Name("GET-/indices/:id/exists")>]
[<Sealed>]
type GetExistsHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<unit, IndexExistsResponse>()
    override this.Process(id, subId, body, context) = 
        let indexExists = 
            match indexService.IndexExists(id.Value) with
            | true -> Choice1Of2(new IndexExistsResponse(Exists = true))
            | false -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
        (indexExists, Ok, NotFound)
