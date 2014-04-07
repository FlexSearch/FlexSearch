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
    open System.Net
    
    /// This will hold all the mutable data related to the node. Everything outside will be
    /// immutable. This will be passed around. 
    type NodeState = 
        { PersistanceStore : IPersistanceStore
          ServerSettings : ServerSettings
          CacheStore : IVersioningCacheStore
          IndexService : IIndexService
          SettingsBuilder : ISettingsBuilder }
    
    type ServiceRoute = 
        { RequestType : System.Type
          RestPath : string
          Verbs : string
          Summary : string
          Notes : string }
    
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
        abstract SupportsBulkIndexing           :   unit -> bool
        abstract SupportsIncrementalIndexing    :   unit -> bool
        abstract ProcessBulkRequest             :   (string * IReadableStringCollection) -> unit
        abstract ProcessIncrementalRequest      :   (string * string * IReadableStringCollection) -> Choice<ImporterResponse, OperationMessage>