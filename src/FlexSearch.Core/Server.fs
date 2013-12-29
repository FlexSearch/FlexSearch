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


module Socket =
    open System
    open SuperSocket.SocketBase
    open SuperWebSocket
    open System.Collections.Generic
    open SuperSocket.ClientEngine
    open SuperSocket.SocketBase.Protocol
    open SuperSocket.SocketBase
    open SuperSocket.SocketBase.Config
    open SuperSocket.SocketBase.Logging
    open SuperSocket.Facility.Protocol
    open SuperSocket.SocketBase.Protocol

    // ----------------------------------------------------------------------------
    /// TCP Socket server
    // ----------------------------------------------------------------------------

    let newDataReceived (session: WebSocketSession) (data: byte[]) = ()
    let newMessageReceived (session: WebSocketSession) (data: string) = ()
    let newSessionConnected (session: WebSocketSession) = ()
    let sessionClosed (session: WebSocketSession) (reason: CloseReason) = ()


    /// Custom Flex based protocol implemetation on top of TCP
    /// It's a protocol like that:
    /// +-------+---+-------------------------------+
    /// |length | m |                               |
    /// |       | c |    request body               |
    /// |       |   |                               |
    /// |  (2)  |(1)|                               |
    /// +-------+---+-------------------------------+
    /// length -> length of the packet body
    ///     +------------------+------------------+
    ///     | bodylength / 256 | bodylength % 256 |
    ///     +------------------+------------------+
    /// mc -> method code
    type ProtoBufferReceiveFilter() =
        inherit FixedHeaderReceiveFilter<BinaryRequestInfo>(3)
            
            /// Returns the body length from the header 
            member this.GetBodyLengthFromHeader(header: byte[], offset: int, length: int) =
                (int header.[offset] * 256) + (int header.[offset + 1])

            /// Returns binaryrequest for the handler
            /// Key -> message code
            /// Body -> Protobuffer encoded message
            member this.ResolveRequestInfo(header: ArraySegment<byte>, bodyBuffer: byte[], offset: int, length: int) =
                new BinaryRequestInfo(header.Array.[2].ToString(), bodyBuffer.CloneRange(offset, length))
                

    /// Protobuffer based session wrapper around AppSession
    type ProtoBufferSession() =
        inherit AppSession<ProtoBufferSession, BinaryRequestInfo>()


    /// Wrapper around socket server to handle protobuffer based communication 
    type ProtoBufferServer() =
        inherit AppServer<ProtoBufferSession, BinaryRequestInfo>(new DefaultReceiveFilterFactory<ProtoBufferReceiveFilter, BinaryRequestInfo>())
    
    
    type TcpSocketServer(port: int) =
        let config = new ServerConfig(Port = port, Ip = "Any", MaxConnectionNumber = 1000, Mode = SocketMode.Tcp, Name = "CustomProtocolServer")
        let server = new ProtoBufferServer()

        do
            server.Setup(config, new ConsoleLogFactory())
        
        interface IServer with 
            member this.Start() =
                server.Start() |> ignore
                    
            member this.Stop() =
                server.Stop()


    type SocketServer(port: int) =
        let listener = new WebSocketServer()

        do
            if listener.Setup(port) = false then
                failwithf "Failed to initialize socket server."

            listener.add_NewDataReceived(new SessionHandler<WebSocketSession, byte[]>(newDataReceived))
            listener.add_NewMessageReceived(new SessionHandler<WebSocketSession, string>(newMessageReceived))
            listener.add_NewSessionConnected(new SessionHandler<WebSocketSession>(newSessionConnected))
            listener.add_SessionClosed(new SessionHandler<WebSocketSession, CloseReason>(sessionClosed))

        interface IServer with 
            member this.Start() =
                listener.Start() |> ignore
                    
            member this.Stop() =
                listener.Stop()



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

        
        /// Thrift based client pool
        type ClientPool(ipAddress: System.Net.IPAddress, port: int, connectionCount: int) =
            let queue = new BlockingCollection<FlexSearchService.Client>(connectionCount)
            
            interface IConnectionPool with
                member this.PoolSize = connectionCount
                
                member this.Initialize() =
                    let mutable success = true
                    let mutable clientCount = 0
                    while (success = true && clientCount < connectionCount) do
                        try
                            let transport = new TSocket(ipAddress.ToString(), port)
                            let framedTransport = new TFramedTransport(transport)
                            let protocol = new TBinaryProtocol(framedTransport, true, true)
                            framedTransport.Open()
                            queue.Add(new FlexSearchService.Client(protocol)) |> ignore
                            clientCount <- clientCount + 1        
                        with | ex -> success <- false
                    success

                member this.TryExecute (action: FlexSearchService.Iface -> unit) =
                    try
                        let (success, client) =  queue.TryTake()
                        if success then action(client)
                        true
                    with
                        | ex -> false
                        
                
        let ElectLeader(state: State.NodeState) =
            if state.ConnectedNodes.Count < state.TotalNodes / 2 then
                failwith "Cannot initiate a leader election without having active connection to half the nodes."
            state.ConnectedNodes.ToArray() |> Array.iter(fun x -> x.Connection.se
                
                )


            

            

    