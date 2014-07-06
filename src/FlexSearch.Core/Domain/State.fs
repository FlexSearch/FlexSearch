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
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open Microsoft.Owin
open Owin
open System.Collections.Concurrent
open System.Collections.Generic
open System.Net
open org.apache.lucene.analysis

/// This will hold all the mutable data related to the node. Everything outside will be
/// immutable. This will be passed around.
type INodeState = 
    abstract PersistanceStore : IPersistanceStore
    abstract ServerSettings : ServerSettings
    abstract CahceStore : IVersioningCacheStore
    abstract IndicesState : IndicesState

[<Sealed>]
type NodeState(persistanceStore, serversettings, cacheStore, indicesState) = 
    interface INodeState with
        member this.PersistanceStore = persistanceStore
        member this.ServerSettings = serversettings
        member this.CahceStore = cacheStore
        member this.IndicesState = indicesState

// ----------------------------------------------------------------------------     
/// HTTP module to handle to incoming requests
// ----------------------------------------------------------------------------   
type IHttpHandler =
    abstract Process : context: IOwinContext -> unit
  
[<AbstractClass>]
type HttpModuleBase() = 
    //abstract Routes : unit -> ServiceRoute []
    abstract Get : string * IOwinContext -> unit
    override this.Get(indexName, owin) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
    abstract Put : string * IOwinContext -> unit
    override this.Put(indexName, owin) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
    abstract Delete : string * IOwinContext -> unit
    override this.Delete(indexName, owin) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
    abstract Post : string * IOwinContext -> unit
    override this.Post(indexName, owin) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED

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
