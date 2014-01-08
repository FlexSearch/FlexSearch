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
    open System
    open System.Net
    open System.Text
    open System.Threading
    open FlexSearch.Core
    open FlexSearch
    open FlexSearch.Api
    open Newtonsoft.Json
    open System.Collections.Concurrent
    open FlexSearch.Core.State

    /// Request callback for http server
    let requestCallback(state : obj) =
        let context = state :?> HttpListenerContext
        let request = context.Request
        let response = context.Response
        
        if request.ContentType = null then
            response.StatusCode <- int HttpStatusCode.InternalServerError
            response.ContentType <- "text/html"
            let content = Encoding.UTF8.GetBytes("Content-type is not defined.")
            response.OutputStream.Write(content, 0, content.Length)
        else
            if request.Url.Segments.Length > 1 then
                let segment =
                    if request.Url.Segments.[1].EndsWith("/") then
                        request.Url.Segments.[1].Substring(0, request.Url.Segments.[1].Length - 1)
                    else
                        request.Url.Segments.[1]

                match ServiceLocator.HttpModule.TryGetValue(segment) with
                | (true, proc) -> ()
//                    try
//                        //proc.Process request response nodeState
//                    with
//                    | x -> 
//                        HttpHelpers.writeResponse HttpStatusCode.InternalServerError x request response
                | _ -> 
                    HttpHelpers.writeResponse HttpStatusCode.NotFound "The request end point is not available." request response
            else
                response.StatusCode <- int HttpStatusCode.InternalServerError
                response.ContentType <- "text/html"
                let content = Encoding.UTF8.GetBytes("FlexSearch Server")
                response.OutputStream.Write(content, 0, content.Length)
        response.Close()

                
    // ----------------------------------------------------------------------------
    // Based on http://www.techempower.com/benchmarks/ 
    /// A reusable http server
    // ----------------------------------------------------------------------------
    type HttpServer(port: int) =
        let listener = new System.Net.HttpListener();
        do
            // This doesn't seem to ignore all write exceptions, so in WriteResponse(), we still have a catch block.
            listener.IgnoreWriteExceptions <- true
            listener.Prefixes.Add(sprintf "http://*:%i/" port)
        
        interface IServer with
            member this.Start() =
                try
                    listener.Start()
                    let mutable context: HttpListenerContext = null
                    
                    // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
                    // To verify that this works, run `netsh http show servicestate`.
                    Interop.SetRequestQueueLength(listener, 65535u)
                    
                    while true do
                        try
                            try
                                context <- listener.GetContext()
                    
                                // http://msdn.microsoft.com/en-us/library/0ka9477y(v=vs.110).aspx
                                ThreadPool.QueueUserWorkItem(new WaitCallback(requestCallback), context) |> ignore
                                context <- null
                            with
                            | :? System.Net.HttpListenerException -> ()
                        finally
                            if context <> null then context.Response.Close()                        
                with
                 | :? System.Net.HttpListenerException -> ()

            member this.Stop() =
                listener.Close()
 

    module Thrift =
        open Thrift
        open Thrift.Protocol
        open Thrift.Server
        open Thrift.Transport
        open FlexSearch.Api
        open System.Collections.Concurrent

        // ----------------------------------------------------------------------------
        /// Thrift server
        // ----------------------------------------------------------------------------
        type Server(port: int, processor: TProcessor, minThread, maxThread) =
            let mutable server: TThreadPoolServer option = None
            do
                let serverSocket = new TServerSocket(port, 0, false)
                let protocolFactory = new TBinaryProtocol.Factory(true, true)
                let transportFactory = new TFramedTransport.Factory()
                
                server <- Some(new TThreadPoolServer(processor, serverSocket, transportFactory, transportFactory, protocolFactory, protocolFactory, minThread, maxThread, null))
        
            interface IServer with
                member this.Start() = server.Value.Serve()
                member this.Stop() = server.Value.Stop()
                        
                
        let ElectLeader(state: State.NodeState) =
            if state.ConnectedNodes.Count < state.TotalNodes / 2 then
                failwith "Cannot initiate a leader election without having active connection to half the nodes."
//            state.ConnectedNodes.ToArray() |> Array.iter(fun x -> x.Connection.se
//                
//                )


            
//    type SocketServer(port: int) =
//        let listener = new WebSocketServer()
//
//        do
//            if listener.Setup(port) = false then
//                failwithf "Failed to initialize socket server."
//
//            listener.add_NewDataReceived(new SessionHandler<WebSocketSession, byte[]>(newDataReceived))
//            listener.add_NewMessageReceived(new SessionHandler<WebSocketSession, string>(newMessageReceived))
//            listener.add_NewSessionConnected(new SessionHandler<WebSocketSession>(newSessionConnected))
//            listener.add_SessionClosed(new SessionHandler<WebSocketSession, CloseReason>(sessionClosed))
//
//        interface IServer with 
//            member this.Start() =
//                listener.Start() |> ignore
//                    
//            member this.Stop() =
//                listener.Stop()
            

    