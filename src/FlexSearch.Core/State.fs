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

module State = 
    open FlexSearch.Api
    open FlexSearch.Core
    open FlexSearch.Core.Interface
    open System.Collections.Concurrent
    open System.Net
    open Microsoft.Owin
    open Owin

    type NodeProperties = 
        { /// Name of the node  
          Name : string
          /// Ip Address of the server
          Address : System.Net.IPAddress
          /// Port on which the server is listening
          Port : int
          /// Connection pool to the server
          Pool : Pool.ObjectPool<Socket.TcpClient> }
    
    /// This will hold all the mutable data related to the node. Everything outside will be
    /// immutable. This will be passed around. 
    type NodeState = 
        { PersistanceStore : IPersistanceStore
          ServerSettings : ServerSettings
          Indices : ConcurrentDictionary<string, Index>
          ConnectedSlaves : BlockingCollection<NodeProperties>
          SlaveNodes : BlockingCollection<NodeProperties>
          MasterNode : NodeProperties }
        member this.IndexExists(indexName : string) = 
            match this.Indices.TryGetValue(indexName) with
            | true, x -> Some(x)
            | _ -> None
    
    type ServiceRoute = 
        { RequestType : System.Type
          RestPath : string
          Verbs : string
          Summary : string
          Notes : string }
    
    // ----------------------------------------------------------------------------     
    /// Http module to handle to incoming requests
    // ----------------------------------------------------------------------------   
    [<AbstractClass>]
    type HttpModule() = 
        abstract Routes : unit -> ServiceRoute []
        abstract Get : string * IOwinContext -> unit
        abstract Post : string * IOwinContext -> unit
        abstract Put : string * IOwinContext -> unit
        abstract Delete : string * IOwinContext -> unit
