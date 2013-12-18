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
    open Newtonsoft.Json

    let httpModules = Factories.GetHttpModules.Value


    /// Write http response
    let writeResponse (res: obj) (request: HttpListenerRequest) (response: HttpListenerResponse) =
        let matchType format res =
            match format with
            | "text/json"
            | "application/json" ->
                response.ContentType <- "text/json" 
                let result = JsonConvert.SerializeObject(res)
                Some(Encoding.UTF8.GetBytes(result))
            | "application/x-protobuf" 
            | "application/octet-stream" ->
                response.ContentType <- "application/x-protobuf"
                Some(ClusterMessage.serialize(res))
            | _ -> None

        let result =
            if request.AcceptTypes.Length = 0 then
                matchType request.ContentType res
            else
                matchType request.AcceptTypes.[0] res

        match result with
        | None -> 
            response.StatusCode <- int HttpStatusCode.NotAcceptable
        | Some(x) -> 
            response.ContentLength64 <- int64 x.Length 
            response.OutputStream.Write(x, 0, x.Length)   
        response.Close()


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
            match httpModules.TryGetValue(request.Url.LocalPath) with
            | (true, proc) -> 
                try
                    let result = proc.Process(request)
                    writeResponse result request response
                with
                | x -> ()
            | _ -> response.StatusCode <- int HttpStatusCode.NotFound
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

    type SocketClient(address: string, port: int, state: NodeState) as self =
        let socket = new WebSocket(sprintf "ws://%s:%i/" address port)
        
        let socketOpened (event: EventArgs) = 
            // Add it if it doesn't exist
            state.OutgoingConnections.TryAdd(address, self) |> ignore
        
        let dataReceived (event: DataReceivedEventArgs) = ()
        let messageReceived (event: MessageReceivedEventArgs) = ()
        let socketClosed (event: EventArgs) = ()
        let socketErrored (event: ErrorEventArgs) = ()

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
                


            

    