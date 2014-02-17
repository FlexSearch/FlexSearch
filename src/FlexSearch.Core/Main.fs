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
namespace FlexSearch.Core

module Main = 
    open FSharp.Data
    open FlexSearch
    open FlexSearch.Api
    open FlexSearch.Core
    open FlexSearch.Core.Server
    open FlexSearch.Core.State
    open FlexSearch.Utility
    open Microsoft.Owin
    open Owin
    open System
    open System.Collections.Concurrent
    open System.Linq
    open System.Net
    open System.Threading
    open System.Threading.Tasks
    
    /// Xml setting provider for server config
    type private FlexServerSetting = JsonProvider< """
        {
            "HttpPort" : 9800,
            "TcpPort" : 9900,
            "IsMaster" : false,
            "DataFolder" : "./data"
        }
    """ >
    
    let container = new ConcurrentDictionary<int, NodeState>()
    
    let exec (owin : IOwinContext) = 
        async { 
            let getModule moduleName indexName (owin : IOwinContext) = 
                match ServiceLocator.HttpModule.TryGetValue(moduleName) with
                | (true, x) -> 
                    match owin.Request.Method with
                    | "GET" -> x.Get(indexName, owin, container.[owin.Request.Uri.Port])
                    | "POST" -> x.Post(indexName, owin, container.[owin.Request.Uri.Port])
                    | "PUT" -> x.Put(indexName, owin, container.[owin.Request.Uri.Port])
                    | "DELETE" -> x.Delete(indexName, owin, container.[owin.Request.Uri.Port])
                    | _ -> owin.Response.StatusCode <- 500
                | _ -> owin.Response.StatusCode <- 500
            try 
                let dataType = 
                    if owin.Request.Uri.Segments.Last().Contains(".") then 
                        owin.Request.Uri.Segments.Last().Substring(owin.Request.Uri.Segments.Last().IndexOf("."))
                    else "json"
                // Root path
                if owin.Request.Uri.Segments.Length = 1 then getModule "/" "/" owin
                else 
                    let indexName = 
                        if owin.Request.Uri.Segments.[1].EndsWith("/") then 
                            owin.Request.Uri.Segments.[1].Substring(0, owin.Request.Uri.Segments.[1].Length - 1)
                        else owin.Request.Uri.Segments.[1]
                    if indexName.EndsWith(".ico") <> true then 
                        // Check if the passed index exists     
                        match container.[owin.Request.Uri.Port].IndexExists(indexName) with
                        | Some(index) -> 
                            // This is the root index module request
                            if owin.Request.Uri.Segments.Length = 2 then 
                                // Check if the requested module exists
                                getModule "index" indexName owin
                            else 
                                // This is a specialized reuest to an existing index
                                // Check if the requested module exists 
                                getModule owin.Request.Uri.Segments.[2] indexName owin
                        | _ -> owin.Response.StatusCode <- 500
                    else owin.Response.StatusCode <- 500
            with ex -> ()
        }
    
    let handler = Func<IOwinContext, Tasks.Task>(fun owin -> Async.StartAsTask(exec (owin)) :> Task)
    
    type OwinStartUp() = 
        member this.Configuration(app : IAppBuilder) = 
            app.Run(handler)
            ()
    
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
        FlexSearch.Logging.FlexLogger.Logger.AddIndex("test", "test")
        //ServiceLocator.FactoryCollection <- new Factories.FactoryCollection(pluginContainer)
        ServiceLocator.HttpModule <- Factories.GetHttpModules().Value
        //ServiceLocator.SettingsBuilder <- SettingsBuilder.SettingsBuilder ServiceLocator.FactoryCollection 
        //                                      (new Validator.IndexValidator(ServiceLocator.FactoryCollection))
    
    let loadNode() = 
        initServiceLocator()
        let settings = getServerSettings (Constants.ConfFolder.Value + "Config.json")
        
        let nodeState = 
            { PersistanceStore = new Store.PersistanceStore(Constants.ConfFolder.Value + "Conf.db", false)
              ServerSettings = settings
              Indices = Unchecked.defaultof<_>
              SlaveNodes = Unchecked.defaultof<_>
              MasterNode = Unchecked.defaultof<_>
              ConnectedSlaves = Unchecked.defaultof<_> }
        container.TryAdd(9000, nodeState) |> ignore
        Microsoft.Owin.Hosting.WebApp.Start<OwinStartUp>("http://localhost:9000") |> ignore
        Console.ReadKey() |> ignore
        ()
