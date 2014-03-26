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

[<AutoOpen>]
module Main = 
    open FlexSearch.Api
    open FlexSearch.Api.Message
    open FlexSearch.Core
    open FlexSearch.Utility
    open Microsoft.Owin
    open Newtonsoft.Json
    open Owin
    open System
    open System.Collections.Concurrent
    open System.IO
    open System.Linq
    open System.Net
    open System.Threading
    open System.Threading.Tasks
    
    /// <summary>
    /// A container used by OWIN to perform dependency injection
    /// </summary>
    let container = new ConcurrentDictionary<int, NodeState>()
    
    /// <summary>
    /// Default Owin method to process request
    /// </summary>
    /// <param name="owin">Owin Context</param>
    let exec (owin : IOwinContext) = 
        async { 
            let getModule moduleName indexName (owin : IOwinContext) = 
                match ServiceLocator.HttpModule.TryGetValue(moduleName) with
                | (true, x) -> 
                    match owin.Request.Method.ToUpperInvariant() with
                    | "GET" -> x.Get(indexName, owin, container.[owin.Request.Uri.Port])
                    | "POST" -> x.Post(indexName, owin, container.[owin.Request.Uri.Port])
                    | "PUT" -> x.Put(indexName, owin, container.[owin.Request.Uri.Port])
                    | "DELETE" -> x.Delete(indexName, owin, container.[owin.Request.Uri.Port])
                    | _ -> owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
                | _ -> owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
            
            let getIndexName (owin : IOwinContext) = 
                if owin.Request.Uri.Segments.[1].EndsWith("/") then 
                    owin.Request.Uri.Segments.[1].Substring(0, owin.Request.Uri.Segments.[1].Length - 1)
                else owin.Request.Uri.Segments.[1]
            
            try 
                match owin.Request.Uri.Segments.Length with
                // Server root
                | 1 -> getModule "/" "/" owin
                // Root index request
                | 2 -> 
                    let indexName = getIndexName owin
                    match container.[owin.Request.Uri.Port].IndexService.IndexExists(indexName) with
                    | true -> getModule "index" indexName owin
                    | false -> 
                        // This can be an index creation request
                        if owin.Request.Method = "POST" then getModule "index" indexName owin
                        else owin |> BAD_REQUEST MessageConstants.INDEX_NOT_FOUND
                // Index module request
                | x when x > 2 && x < 5 -> 
                    let indexName = getIndexName owin
                    match container.[owin.Request.Uri.Port].IndexService.IndexExists(indexName) with
                    | true -> 
                        let moduleName = 
                            if owin.Request.Uri.Segments.[2].EndsWith("/") then 
                                owin.Request.Uri.Segments.[2].Substring(0, owin.Request.Uri.Segments.[2].Length - 1)
                            else owin.Request.Uri.Segments.[2]
                        getModule moduleName indexName owin
                    | false -> owin |> BAD_REQUEST MessageConstants.INDEX_NOT_FOUND
                | _ -> owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
            with ex -> ()
        }
    
    /// <summary>
    /// Default owin handler to tranform C# functiom to F#
    /// </summary>
    let handler = Func<IOwinContext, Tasks.Task>(fun owin -> Async.StartAsTask(exec (owin)) :> Task)
    
    /// <summary>
    /// Owin startup class
    /// </summary>
    type OwinStartUp() = 
        member this.Configuration(app : IAppBuilder) = app.Run(handler)
    
    /// <summary>
    /// Generate server settings from the json text file
    /// </summary>
    /// <param name="path">File path to load settings from</param>
    let GetServerSettings(path) = 
        let fileText = Helpers.LoadFile(path)
        let parsedResult = JsonConvert.DeserializeObject<ServerSettings>(fileText)
        parsedResult.ConfFolder <- Helpers.GenerateAbsolutePath(parsedResult.ConfFolder)
        parsedResult.DataFolder <- Helpers.GenerateAbsolutePath(parsedResult.DataFolder)
        parsedResult.PluginFolder <- Helpers.GenerateAbsolutePath(parsedResult.PluginFolder)
        parsedResult
    
    /// <summary>
    /// Initialize all the service locator member
    /// </summary>
    let initServiceLocator (serverSettings, testServer) = 
        let pluginContainer = PluginContainer(not testServer).Value
        let factoryCollection = new FactoryCollection(pluginContainer) :> IFactoryCollection
        let settingBuilder = 
            SettingsBuilder.SettingsBuilder factoryCollection (new Validator.IndexValidator(factoryCollection))
        let persistanceStore = new PersistanceStore(Path.Combine(Constants.ConfFolder.Value, "Conf.db"), testServer)
        let searchService = new SearchService(GetQueryModules(factoryCollection), getParserPool (2)) :> ISearchService
        let indexService = 
            new IndexService(settingBuilder, persistanceStore, new VersioningCacheStore(), searchService) :> IIndexService
        ServiceLocator.HttpModule <- Factories.GetHttpModules().Value
        let nodeState = 
            { PersistanceStore = persistanceStore
              ServerSettings = serverSettings
              CacheStore = Unchecked.defaultof<_>
              IndexService = indexService
              SettingsBuilder = settingBuilder }
        nodeState
    
    /// <summary>
    /// Main entrypoint to load node
    /// </summary>
    let loadNode (serverSettings : ServerSettings, testServer : bool) = 
        // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
        // To verify that this works, run `netsh http show servicestate`.
        if testServer <> true then MaximizeThreads() |> ignore
        let nodeState = initServiceLocator (serverSettings, testServer)
        container.TryAdd(nodeState.ServerSettings.HttpPort, nodeState) |> ignore
    
    /// <summary>
    /// Used by windows service (top shelf) to start and stop windows service.
    /// </summary>
    type NodeService(serverSettings : ServerSettings, testServer : bool) = 
        let mutable server = Unchecked.defaultof<IDisposable>
        let mutable thread = Unchecked.defaultof<_>
        
        let startServer() = 
            server <- Microsoft.Owin.Hosting.WebApp.Start<OwinStartUp>(sprintf "http://*:%i" serverSettings.HttpPort)
            Console.ReadKey() |> ignore
        
        member this.Start() = 
            loadNode (serverSettings, testServer)
            try 
                thread <- Task.Factory.StartNew(startServer)
            with e -> ()
            ()
        
        member this.Stop() = 
            container.TryRemove(serverSettings.HttpPort) |> ignore
            server.Dispose()
