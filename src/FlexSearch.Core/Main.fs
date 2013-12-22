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
    open FlexSearch.Utility
    open FlexSearch.Core
    open FlexSearch
    open FlexSearch.Core.Server
    open SuperWebSocket
    open FSharp.Data

    /// Xml setting provider for server config
    type private FlexServerSetting = JsonProvider<"""
        {
            "HttpPort" : 9800,
            "TcpPort" : 9900,
            "IsMaster" : false,
            "DataFolder" : "./data"
        }
    """
    >

    let getServerSettings(path) =
        let fileXml = Helpers.LoadFile(path)
        let parsedResult = FlexServerSetting.Parse(fileXml)          

        let setting =
            {
                LuceneVersion = Constants.LuceneVersion
                HttpPort = parsedResult.HttpPort
                TcpPort = parsedResult.TcpPort
                DataFolder = Helpers.GenerateAbsolutePath(parsedResult.DataFolder)
                PluginFolder = Constants.PluginFolder.Value
                ConfFolder = Constants.ConfFolder.Value
                NodeName = ""
                NodeRole = NodeRole.UnDefined
                MasterNode = IPAddress.None
            }

        setting

    /// Initialize all the service locator member
    let initServiceLocator() =
        let pluginContainer = Factories.PluginContainer(true).Value
        ServiceLocator.FactoryCollection <- new Factories.FactoryCollection(pluginContainer)
        ServiceLocator.HttpModule <- Factories.GetHttpModules().Value
        ServiceLocator.SettingsBuilder <- SettingsBuilder.SettingsBuilder ServiceLocator.FactoryCollection (new Validator.IndexValidator(ServiceLocator.FactoryCollection))
        

    let loadNode() =
        initServiceLocator()
        let settings = getServerSettings(Constants.ConfFolder.Value + "Config.json")
        let nodeState =
            {
                PersistanceStore = new Store.PersistanceStore(Constants.ConfFolder.Value + "Conf.db", false)
                ServerSettings = settings
                HttpConnections = new ConcurrentDictionary<string, System.Net.Http.HttpClient>(StringComparer.OrdinalIgnoreCase)
                IncomingSessions = new ConcurrentDictionary<string, SuperWebSocket.WebSocketSession>(StringComparer.OrdinalIgnoreCase)
                OutgoingConnections = new ConcurrentDictionary<string, ISocketClient>(StringComparer.OrdinalIgnoreCase)
                Indices = new ConcurrentDictionary<string, Index>(StringComparer.OrdinalIgnoreCase)
            }

        let httpServer = new Server.Http.HttpServer(9800) :> IServer
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




