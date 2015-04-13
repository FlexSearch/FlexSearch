// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

///  Get all indices
[<Name("GET-/indices")>]
[<Sealed>]
type GetAllIndexHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, Index.Dto []>()
    override __.Process(_, _) = SuccessResponse(indexService.GetAllIndex(), Ok)

///  Get an index
[<Name("GET-/indices/:id")>]
[<Sealed>]
type GetIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, Index.Dto>(true)
    override __.Process(request, _) = SomeResponse(indexService.GetIndex(request.ResId.Value), Ok, NotFound)

/// Create an index
[<Name("POST-/indices")>]
[<Sealed>]
type PostIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<Index.Dto, CreateResponse>()
    override __.Process(request, body) = 
        match indexService.AddIndex(body.Value) with
        | Choice1Of2(response) -> SuccessResponse(response, Created)
        | Choice2Of2(error) -> 
            if error = IndexAlreadyExists(body.Value.IndexName) then FailureResponse(error, Conflict)
            else FailureResponse(error, BadRequest)

/// Delete an index
[<Name("DELETE-/indices/:id")>]
[<Sealed>]
type DeleteIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(request, body) = SomeResponse(indexService.DeleteIndex(request.ResId.Value), Ok, BadRequest)

///// Update an index
//[<Name("PUT-/indices/:id")>]
//[<Sealed>]
//type PutIndexByIdHandler(indexService : IIndexService) = 
//    inherit HttpHandlerBase<Index.Dto, unit>()
//    override __.Process(request, body) = 
//        // Index name passed in URL takes precedence
//        body.Value.IndexName <- request.ResId.Value
//        SomeResponse(indexService.UpdateIndex(body.Value), Ok, BadRequest)

type IndexStatusResponse() = 
    member val Status = Unchecked.defaultof<IndexState> with get, set

/// Get index status
[<Name("GET-/indices/:id/status")>]
[<Sealed>]
type GetStatusHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, IndexStatusResponse>()
    override __.Process(request, body) = 
        let response = 
            match indexService.GetIndexState(request.ResId.Value) with
            | Choice1Of2(state) -> Choice1Of2(new IndexStatusResponse(Status = state))
            | Choice2Of2(error) -> Choice2Of2(error)
        SomeResponse(response, Ok, BadRequest)

/// Update index status
[<Name("PUT-/indices/:id/status/:id")>]
[<Sealed>]
type PutStatusHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override this.Process(request, body) = 
        match request.SubResId.Value with
        | InvariantEqual "online" -> SomeResponse(indexService.OpenIndex(request.ResId.Value), Ok, BadRequest)
        | InvariantEqual "offline" -> SomeResponse(indexService.CloseIndex(request.ResId.Value), Ok, BadRequest)
        | _ -> FailureResponse(HttpNotSupported, BadRequest)

/// Check if an index exists
[<Name("GET-/indices/:id/exists")>]
[<Sealed>]
type GetExistsHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, IndexExistsResponse>()
    override this.Process(request, body) = 
        match indexService.IndexExists(request.ResId.Value) with
        | true -> SuccessResponse(new IndexExistsResponse(Exists = true), Ok)
        | false -> FailureResponse(IndexNotFound(request.ResName), NotFound)
