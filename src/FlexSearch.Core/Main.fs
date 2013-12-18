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

module Main =
    open System
    open System.Collections.Concurrent
    open System.Net
    open System.Threading
    open FlexSearch.Api
    open FlexSearch.Core.Server
    open SuperWebSocket

    let startClusterMaster() = ()
    
    type Servers =
        {
            TcpServer           :   IServer
            HttpServer          :   IServer
        } 

    let loadNode() =
        let serverSettings = new Settings.SettingsStore(Constants.ConfFolder.Value + "Config.xml") :> IPersistanceStore
        let nodeState =
            {
                PersistanceStore = serverSettings
                ActiveConnections = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                IncomingSessions = new ConcurrentDictionary<string, SuperWebSocket.WebSocketSession>(StringComparer.OrdinalIgnoreCase)
                OutgoingConnections = new ConcurrentDictionary<string, ISocketClient>(StringComparer.OrdinalIgnoreCase)
                Indices = new ConcurrentDictionary<string, Index>(StringComparer.OrdinalIgnoreCase)
            }


        let httpServer = new Server.Http.HttpServer(serverSettings.Settings.HttpPort) :> IServer
        httpServer.Start()
        ()
        //let tcpServer = new SocketServer(serverSettings.Settings().TcpPort, )
//        match serverSettings.Settings().NodeRole with
//        | NodeRole.ClusterMaster -> 
//            
//            ()
//        | NodeRole.ClusterSlave -> startClusterSlave()
//        | NodeRole.Index -> startIndexNode()
//        | NodeRole.Query -> startIndexNode()
//        | _ -> failwithf "Invalid node role configured."




