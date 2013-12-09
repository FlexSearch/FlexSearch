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
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

module Server =
    open System
    open System.Net
    open System.Threading

    // ----------------------------------------------------------------------------
    // Based on http://www.techempower.com/benchmarks/ 
    /// A reusable http server
    // ----------------------------------------------------------------------------
    type HttpServer(port: int, requestCallback) =
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


    // ----------------------------------------------------------------------------
    /// NancyFx http server
    // ----------------------------------------------------------------------------
    open Nancy
    open Nancy.Hosting.Self

    type NancyServer(port: int) =
        let url = sprintf "http://*:%d" port
        let listener = new NancyHost(new Uri(url))
        
        interface IServer with
            member this.Start() =
                listener.Start()
                    
            member this.Stop() =
                listener.Stop()


    // ----------------------------------------------------------------------------
    /// WebSocket server
    // ----------------------------------------------------------------------------
    open SuperSocket.SocketBase
    open SuperWebSocket

    type SocketServer(port: int, newDataHandler, newMessageHandler, newRequestHandler, newSessionHandler, dropSessionHandler) =
        let listener = new WebSocketServer()
        do
            if listener.Setup(port) = false then
                failwithf "Failed to initialize socket server."
            listener.add_NewDataReceived(newDataHandler)
            listener.add_NewMessageReceived(newMessageHandler)
            listener.add_NewRequestReceived(newRequestHandler)
            listener.add_NewSessionConnected(newSessionHandler)
            listener.add_SessionClosed(dropSessionHandler)

        interface IServer with
            member this.Start() =
                listener.Start() |> ignore
                    
            member this.Stop() =
                listener.Stop()