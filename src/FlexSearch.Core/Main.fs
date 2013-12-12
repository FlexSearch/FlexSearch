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
    open System.Net
    open System.Threading
    open FlexSearch.Api
    open FlexSearch.Core.Server

    let startClusterMaster() = ()


    let loadNode() =
        let serverSettings = new Settings.SettingsStore(Constants.ConfFolder.Value + "Config.xml") :> IPersistanceStore
        //let httpServer = new HttpServer(serverSettings.Settings.HttpPort)
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




