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

//[<Name("GET-/filterlists/:id")>]
//[<Sealed>]
//type GetFilterListByIdHandler(state : INodeState) = 
//    interface IHttpHandler with
//        member this.Process(owin) = 
//            owin |> ResponseProcessor (state.PersistanceStore.Get<FilterList>(SubId(owin))) OK BAD_REQUEST
//
//[<Name("PUT-/filterlists/:id")>]
//[<Sealed>]
//type PutFilterListByIdIdHandler(state : INodeState) = 
//    interface IHttpHandler with
//        member this.Process(owin) = 
//            match GetRequestBody<FilterList>(owin.Request) with
//            | Choice1Of2(filterList) -> 
//                owin 
//                |> ResponseProcessor (state.PersistanceStore.Put<FilterList>(SubId(owin), filterList)) OK BAD_REQUEST
//            | Choice2Of2(error) -> owin |> BAD_REQUEST error
//
//[<Name("DELETE-/filterlists/:id")>]
//[<Sealed>]
//type DeleteFilterListByIdHandler(state : INodeState) = 
//    interface IHttpHandler with
//        member this.Process(owin) = 
//            owin |> ResponseProcessor (state.PersistanceStore.Delete<FilterList>(SubId(owin))) OK BAD_REQUEST
//
//[<Name("GET-/maplists/:id")>]
//[<Sealed>]
//type GetMapListByIdHandler(state : INodeState) = 
//    interface IHttpHandler with
//        member this.Process(owin) = 
//            owin |> ResponseProcessor (state.PersistanceStore.Get<MapList>(SubId(owin))) OK BAD_REQUEST
//
//[<Name("PUT-/maplists/:id")>]
//[<Sealed>]
//type PutMapListByIdIdHandler(state : INodeState) = 
//    interface IHttpHandler with
//        member this.Process(owin) = 
//            match GetRequestBody<MapList>(owin.Request) with
//            | Choice1Of2(filterList) -> 
//                owin |> ResponseProcessor (state.PersistanceStore.Put<MapList>(SubId(owin), filterList)) OK BAD_REQUEST
//            | Choice2Of2(error) -> owin |> BAD_REQUEST error
//
//[<Name("DELETE-/maplists/:id")>]
//[<Sealed>]
//type DeleteMapListByIdHandler(state : INodeState) = 
//    interface IHttpHandler with
//        member this.Process(owin) = 
//            owin |> ResponseProcessor (state.PersistanceStore.Delete<MapList>(SubId(owin))) OK BAD_REQUEST
