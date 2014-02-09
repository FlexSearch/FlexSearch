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
    open FSharp.Data
    open FlexSearch
    open FlexSearch.Api
    open FlexSearch.Core
    open FlexSearch.Core.Server
    open FlexSearch.Core.State
    open FlexSearch.Utility
    open System
    open System.Collections.Concurrent
    open System.Net
    open System.Threading           
    
    /// Xml setting provider for server config
    type private FlexServerSetting = JsonProvider< """
        {
            "HttpPort" : 9800,
            "TcpPort" : 9900,
            "IsMaster" : false,
            "DataFolder" : "./data"
        }
    """ >
    
    let getServerSettings (path) = 
        let fileXml = Helpers.LoadFile(path)
        let parsedResult = FlexServerSetting.Parse(fileXml)
        
        let setting = 
            { LuceneVersion = Constants.LuceneVersion
              HttpPort = parsedResult.HttpPort
              TcpPort = parsedResult.TcpPort
              DataFolder = Helpers.GenerateAbsolutePath(parsedResult.DataFolder)
              PluginFolder = Constants.PluginFolder.Value
              ConfFolder = Constants.ConfFolder.Value
              NodeName = ""
              NodeRole = NodeRole.UnDefined
              MasterNode = IPAddress.None }
        setting
    
    /// Initialize all the service locator member
    let initServiceLocator() = 
        let pluginContainer = Factories.PluginContainer(true).Value
        ServiceLocator.FactoryCollection <- new Factories.FactoryCollection(pluginContainer)
        ServiceLocator.HttpModule <- Factories.GetHttpModules().Value
        ServiceLocator.SettingsBuilder <- SettingsBuilder.SettingsBuilder ServiceLocator.FactoryCollection 
                                              (new Validator.IndexValidator(ServiceLocator.FactoryCollection))
    
    open ServiceStack
    type AppHost() =
        inherit AppHostHttpListenerPoolBase("FlexSearch", typeof<FlexSearch.Core.HttpModule.Hello>.Assembly)
        override this.Configure container =
            let httpModules = Factories.GetHttpModules().Value
            for httpModule in httpModules do
                for route in httpModule.Value.Routes() do
                    base.Routes.Add(route.RequestType, route.RestPath, route.Verbs, route.Summary, route.Notes) |> ignore
                

    let loadNode() = 
        //initServiceLocator()
        let settings = getServerSettings (Constants.ConfFolder.Value + "Config.json")
        
        let nodeState = 
            { PersistanceStore = new Store.PersistanceStore(Constants.ConfFolder.Value + "Conf.db", false)
              ServerSettings = settings
              Indices = Unchecked.defaultof<_>
              SlaveNodes = Unchecked.defaultof<_>
              MasterNode = Unchecked.defaultof<_>
              ConnectedSlaves = Unchecked.defaultof<_> }
        
        let appHost = new AppHost()
        appHost.Init() |> ignore
        appHost.Start "http://localhost:9900/" |> ignore
        Console.ReadKey() |> ignore
        ()
