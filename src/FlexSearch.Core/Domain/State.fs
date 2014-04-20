﻿// ----------------------------------------------------------------------------
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

[<AutoOpen>]
module State = 
    open FlexSearch.Api
    open FlexSearch.Api.Message
    open FlexSearch.Core
    open FlexSearch.Core.HttpHelpers
    open FlexSearch.Core.Interface
    open Microsoft.Owin
    open Owin
    open System.Collections.Concurrent
    open System.Collections.Generic
    open System.Net
    open org.apache.lucene.analysis
    
    /// This will hold all the mutable data related to the node. Everything outside will be
    /// immutable. This will be passed around. 
    type NodeState = 
        { PersistanceStore : IPersistanceStore
          ServerSettings : ServerSettings
          CacheStore : IVersioningCacheStore
          IndexService : IIndexService
          SettingsBuilder : ISettingsBuilder }
    
    // ----------------------------------------------------------------------------     
    /// HTTP module to handle to incoming requests
    // ----------------------------------------------------------------------------   
    [<AbstractClass>]
    type HttpModuleBase() = 
        //abstract Routes : unit -> ServiceRoute []
        abstract Get : string * IOwinContext * NodeState -> unit
        override this.Get(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
        abstract Put : string * IOwinContext * NodeState -> unit
        override this.Put(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
        abstract Delete : string * IOwinContext * NodeState -> unit
        override this.Delete(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
        abstract Post : string * IOwinContext * NodeState -> unit
        override this.Post(indexName, owin, state) = owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
    
    /// <summary>
    /// Import handler interface to support
    /// </summary>
    type IImportHandler = 
        abstract SupportsBulkIndexing : unit -> bool
        abstract SupportsIncrementalIndexing : unit -> bool
        abstract ProcessBulkRequest : string * ImportRequest * NodeState -> unit
        abstract ProcessIncrementalRequest : string * ImportRequest * NodeState -> Choice<unit, OperationMessage>
    
    /// <summary>
    /// Interface which exposes all top level factories
    /// Could have exposed all these through a simple dictionary over IFlexFactory
    /// but then we would have to perform a look up to get each factory instance.
    /// This is fairly easy to manage as all the logic is in IFlexFactory.
    /// Also reduces passing of parameters.
    /// </summary>
    type IFactoryCollection = 
        abstract FilterFactory : IFlexFactory<IFlexFilterFactory> with get
        abstract TokenizerFactory : IFlexFactory<IFlexTokenizerFactory> with get
        abstract AnalyzerFactory : IFlexFactory<Analyzer> with get
        abstract SearchQueryFactory : IFlexFactory<IFlexQuery> with get
        abstract ComputationOperationFactory : IFlexFactory<IComputationOperation> with get
        abstract ImportHandlerFactory : IFlexFactory<IImportHandler> with get
        abstract HttpModuleFactory : IFlexFactory<HttpModuleBase> with get
        abstract ScriptFactoryCollection : IScriptFactoryCollection with get
        abstract ResourceLoader : IResourceLoader with get