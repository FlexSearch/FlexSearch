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
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open Microsoft.Owin
open Owin
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Net
open org.apache.lucene.analysis

///// This will hold all the mutable data related to the node. Everything outside will be
///// immutable. This will be passed around.
//type INodeState = 
//    abstract PersistanceStore : IPersistanceStore
//    abstract ServerSettings : ServerSettings
//    abstract IndicesState : IndicesState
//
//[<Sealed>]
//type NodeState(persistanceStore, serversettings, indicesState) = 
//    interface INodeState with
//        member this.PersistanceStore = persistanceStore
//        member this.ServerSettings = serversettings
//        member this.IndicesState = indicesState

// ----------------------------------------------------------------------------     
/// HTTP module to handle to incoming requests
// ----------------------------------------------------------------------------   
type IHttpHandler = 
    abstract Process : context:IOwinContext -> unit

type IHttpResource = 
    abstract TakeFullControl : bool
    abstract HasBody : bool
    abstract FailOnMissingBody : bool
    abstract Execute : id:option<string> * subid:option<string> * context:IOwinContext -> unit

[<AbstractClass>]
type HttpHandlerBase<'T, 'U>(?failOnMissingBody0 : bool, ?fullControl0 : bool) = 
    let failOnMissingBody = defaultArg failOnMissingBody0 true
    let fullControl = defaultArg fullControl0 false
    
    let hasBody = 
        if typeof<'T> = typeof<unit> then false
        else true
    
    member this.Deserialize(request : IOwinRequest) = GetRequestBody<'T>(request)
    
    member this.Serialize (response : Choice<'U, OperationMessage>) (successStatus : HttpStatusCode) 
           (failureStatus : HttpStatusCode) (owinContext : IOwinContext) = 
        // For parameter less constructor the performance of Activator is as good as direct initialization. Based on the 
        // finding of http://geekswithblogs.net/mrsteve/archive/2012/02/11/c-sharp-performance-new-vs-expression-tree-func-vs-activator.createinstance.aspx
        // In future we can cache it if performance is found to be an issue.
        let instance = Activator.CreateInstance<Response<'U>>()
        match response with
        | Choice1Of2(r) -> 
            instance.Data <- r
            WriteResponse successStatus instance owinContext
        | Choice2Of2(r) -> 
            instance.Error <- r
            WriteResponse failureStatus instance owinContext
    
    abstract Process : id:option<string> * subId:option<string> * body:Option<'T> * context:IOwinContext
     -> Choice<'U, OperationMessage> * HttpStatusCode * HttpStatusCode
    override this.Process(id, subId, body, context) = 
        (Choice2Of2(Errors.HTTP_NOT_SUPPORTED |> GenerateOperationMessage), HttpStatusCode.OK, HttpStatusCode.BadRequest)
    abstract Process : context:IOwinContext -> unit
    override this.Process(context) = context |> BAD_REQUEST Errors.HTTP_NOT_SUPPORTED
    interface IHttpResource with
        member this.TakeFullControl = fullControl
        member this.FailOnMissingBody = failOnMissingBody
        member this.HasBody = hasBody
        member this.Execute(id, subId, context) = 
            if fullControl then this.Process(context)
            else if hasBody then 
                match this.Deserialize(context.Request) with
                | Choice1Of2(body) -> 
                    let (response, successCode, failureCode) = this.Process(id, subId, (Some(body)), context)
                    context |> this.Serialize (response) successCode failureCode
                | Choice2Of2(e) -> 
                    if failOnMissingBody then 
                        context |> this.Serialize (Choice2Of2(e)) HttpStatusCode.OK HttpStatusCode.BadRequest
                    else 
                        let (response, successCode, failureCode) = this.Process(id, subId, None, context)
                        context |> this.Serialize (response) successCode failureCode
            else 
                let (response, successCode, failureCode) = this.Process(id, subId, None, context)
                context |> this.Serialize (response) successCode failureCode

/// <summary>
/// Import handler interface to support
/// </summary>
type IImportHandler = 
    abstract SupportsBulkIndexing : unit -> bool
    abstract SupportsIncrementalIndexing : unit -> bool
    abstract ProcessBulkRequest : string * ImportRequest -> unit
    abstract ProcessIncrementalRequest : string * ImportRequest -> Choice<unit, OperationMessage>

/// <summary>
/// Interface which exposes all top level factories
/// Could have exposed all these through a simple dictionary over IFlexFactory
/// but then we would have to perform a look up to get each factory instance.
/// This is fairly easy to manage as all the logic is in IFlexFactory.
/// Also reduces passing of parameters.
/// </summary>
type IFactoryCollection = 
    abstract FilterFactory : IFlexFactory<IFlexFilterFactory>
    abstract TokenizerFactory : IFlexFactory<IFlexTokenizerFactory>
    abstract AnalyzerFactory : IFlexFactory<Analyzer>
    abstract SearchQueryFactory : IFlexFactory<IFlexQuery>
    abstract ImportHandlerFactory : IFlexFactory<IImportHandler>
    abstract HttpModuleFactory : IFlexFactory<IHttpHandler>
    abstract ResourceLoader : IResourceLoader
