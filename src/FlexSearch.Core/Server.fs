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
// ----------------------------------------------------------------------------
namespace FlexSearch.Core.Server

// ----------------------------------------------------------------------------
open FlexSearch.Core

module Http = 
    open FlexSearch
    open FlexSearch.Api
    open FlexSearch.Core
    open FlexSearch.Core.State
    open Newtonsoft.Json
    open System
    open System.Collections.Concurrent
    open System.Net
    open System.Text
    open System.Threading
    
    module Thrift = 
        open FlexSearch.Api
        open System.Collections.Concurrent
        open Thrift
        open Thrift.Protocol
        open Thrift.Server
        open Thrift.Transport
        
        // ----------------------------------------------------------------------------
        /// Thrift server
        // ----------------------------------------------------------------------------
        type Server(port : int, processor : TProcessor, minThread, maxThread) = 
            let mutable server : TThreadPoolServer option = None
            
            do 
                let serverSocket = new TServerSocket(port, 0, false)
                let protocolFactory = new TBinaryProtocol.Factory(true, true)
                let transportFactory = new TFramedTransport.Factory()
                server <- Some
                              (new TThreadPoolServer(processor, serverSocket, transportFactory, transportFactory, 
                                                     protocolFactory, protocolFactory, minThread, maxThread, null))
            
            interface IServer with
                member this.Start() = server.Value.Serve()
                member this.Stop() = server.Value.Stop()
