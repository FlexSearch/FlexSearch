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

    let nodeState =
        {
            PersistanceStore = Unchecked.defaultof<_>
            ServerSettings = Unchecked.defaultof<ServerSettings>
            HttpConnections = new ConcurrentDictionary<string, System.Net.Http.HttpClient>(StringComparer.OrdinalIgnoreCase)
            IncomingSessions = new ConcurrentDictionary<string, SuperWebSocket.WebSocketSession>(StringComparer.OrdinalIgnoreCase)
            OutgoingConnections = new ConcurrentDictionary<string, ISocketClient>(StringComparer.OrdinalIgnoreCase)
            Indices = new ConcurrentDictionary<string, Index>(StringComparer.OrdinalIgnoreCase)
        }


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
                | (true, proc) -> 
                    try
                        proc.Process request response nodeState
                    with
                    | x -> 
                        HttpHelpers.writeResponse HttpStatusCode.InternalServerError x request response
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
    open SuperSocket.SocketBase
    open SuperWebSocket
    open System.Collections.Generic
    

    // ----------------------------------------------------------------------------
    /// WebSocket server
    // ----------------------------------------------------------------------------

    let newDataReceived (session: WebSocketSession) (data: byte[]) = ()
    let newMessageReceived (session: WebSocketSession) (data: string) = ()
    let newSessionConnected (session: WebSocketSession) = ()
    let sessionClosed (session: WebSocketSession) (reason: CloseReason) = ()

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


    open WebSocket4Net
    open SuperSocket.ClientEngine
    open ProtoBuf
    open System
    open System.Reactive.Linq

    type SocketClient(address: string, port: int, state: NodeState) as self =
        let socket = new WebSocket(sprintf "ws://%s:%i/" address port)
        
        let socketOpened (event: EventArgs) = 
            // Add it if it doesn't exist
            state.OutgoingConnections.TryAdd(address, self) |> ignore
        
        let dataReceived (event: DataReceivedEventArgs) = ()
        let messageReceived (event: MessageReceivedEventArgs) = ()
        let socketClosed (event: EventArgs) = 
            state.OutgoingConnections.TryRemove(address) |> ignore
        let socketErrored (event: ErrorEventArgs) = 
            state.OutgoingConnections.TryRemove(address) |> ignore
            let timer = Observable.Timer(TimeSpan.FromSeconds(15))
            let subscribe = timer.Subscribe(fun x ->
                socket.Open()
            )

        do
            socket.Opened.Add(socketOpened)
            socket.DataReceived.Add(dataReceived)
            socket.MessageReceived.Add(messageReceived)
            socket.Closed.Add(socketClosed)
            socket.Error.Add(socketErrored)
            
        interface ISocketClient with
            member this.Open() = socket.Open()
            member this.Connected() = true
            member this.Send(msg) = socket.Send(msg, 0, msg.Length)
                


            

    