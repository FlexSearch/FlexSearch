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
    open Autofac
    open Autofac.Extras.Attributed
    open FlexSearch.Api
    open FlexSearch.Common
    open FlexSearch.Core
    open FlexSearch.Core.Services
    open FlexSearch.Utility
    open Microsoft.Owin
    open Newtonsoft.Json
    open Owin
    open System
    open System.Collections.Concurrent
    open System.Collections.Generic
    open System.IO
    open System.Linq
    open System.Net
    open System.Threading
    open System.Threading.Tasks
    open org.apache.lucene.analysis
    
    /// <summary>
    /// Generate server settings from the JSON text file
    /// </summary>
    /// <param name="path">File path to load settings from</param>
    let GetServerSettings(path) = 
        let fileText = Helpers.LoadFile(path)
        let parsedResult = JsonConvert.DeserializeObject<ServerSettings>(fileText)
        parsedResult.ConfFolder <- Helpers.GenerateAbsolutePath(parsedResult.ConfFolder)
        parsedResult.DataFolder <- Helpers.GenerateAbsolutePath(parsedResult.DataFolder)
        parsedResult.PluginFolder <- Helpers.GenerateAbsolutePath(parsedResult.PluginFolder)
        parsedResult
    
    let GetLoggerService(serverSettings : ServerSettings) = 
        let builder = new ContainerBuilder()
        // Register the service to consume with meta-data.
        // Since we're using attributed meta-data, we also
        // need to register the AttributedMetadataModule
        // so the meta-data attributes get read.
        builder.RegisterModule<AttributedMetadataModule>() |> ignore
        // Interface scanning
        builder |> FactoryService.RegisterInterfaceAssemblies<ILogService>
        builder |> FactoryService.RegisterSingleFactoryInstance<ILogService>
        let container = builder.Build()
        let logFactory = container.Resolve<IFlexFactory<ILogService>>()
        match logFactory.GetModuleByName(serverSettings.Logger) with
        | Choice1Of2(logger) -> logger
        | _ -> 
            // Return the default log service
            new ConsoleLogService() :> ILogService
    
    /// <summary>
    /// Get a container with all dependencies setup
    /// </summary>
    /// <param name="serverSettings"></param>
    /// <param name="testServer"></param>
    let GetContainer(serverSettings : ServerSettings, testServer : bool) = 
        let builder = new ContainerBuilder()

        // Register the service to consume with meta-data.
        // Since we're using attributed meta-data, we also
        // need to register the AttributedMetadataModule
        // so the meta-data attributes get read.
        builder.RegisterModule<AttributedMetadataModule>() |> ignore
        // Interface scanning
        builder |> FactoryService.RegisterInterfaceAssemblies<IHttpHandler>
        builder |> FactoryService.RegisterInterfaceAssemblies<IImportHandler>
        builder |> FactoryService.RegisterInterfaceAssemblies<IFlexFilterFactory>
        builder |> FactoryService.RegisterInterfaceAssemblies<IFlexTokenizerFactory>
        builder |> FactoryService.RegisterInterfaceAssemblies<IFlexQuery>
        builder |> FactoryService.RegisterInterfaceAssemblies<IHttpResource>
        // Abstract class scanning
        builder |> FactoryService.RegisterAbstractClassAssemblies<HttpHandlerBase<_,_>>
        builder |> FactoryService.RegisterAbstractClassAssemblies<Analyzer>
        // Factory registration
        builder |> FactoryService.RegisterSingleFactoryInstance<IHttpResource>
        builder |> FactoryService.RegisterSingleFactoryInstance<IHttpHandler>
        builder |> FactoryService.RegisterSingleFactoryInstance<IImportHandler>
        builder |> FactoryService.RegisterSingleFactoryInstance<IFlexFilterFactory>
        builder |> FactoryService.RegisterSingleFactoryInstance<IFlexTokenizerFactory>
        builder |> FactoryService.RegisterSingleFactoryInstance<IFlexQuery>
        builder |> FactoryService.RegisterSingleFactoryInstance<Analyzer>
        builder |> FactoryService.RegisterSingleInstance<SettingsBuilder, ISettingsBuilder>
        builder |> FactoryService.RegisterSingleInstance<ResourceLoader, IResourceLoader>
        builder |> FactoryService.RegisterSingleInstance<NodeState, INodeState>
        let indicesState = 
            { IndexStatus = new ConcurrentDictionary<string, IndexState>(StringComparer.OrdinalIgnoreCase)
              IndexRegisteration = new ConcurrentDictionary<string, FlexIndex>(StringComparer.OrdinalIgnoreCase) }
        builder.RegisterInstance(new SqlLitePersistanceStore(testServer)).As<IPersistanceStore>().SingleInstance() 
        |> ignore
        builder.RegisterInstance(serverSettings).SingleInstance() |> ignore
        builder.RegisterInstance(indicesState).SingleInstance() |> ignore
        // Register services
        builder |> FactoryService.RegisterSingleInstance<FlexParser, IFlexParser>
        builder |> FactoryService.RegisterSingleInstance<IndexService, IIndexService>
        builder |> FactoryService.RegisterSingleInstance<DocumentService, IDocumentService>
        builder |> FactoryService.RegisterSingleInstance<QueueService, IQueueService>
        builder |> FactoryService.RegisterSingleInstance<SearchService, ISearchService>
        builder |> FactoryService.RegisterSingleInstance<JobService, IJobService>
        builder |> FactoryService.RegisterSingleInstance<FactoryService.FactoryCollection, IFactoryCollection>
        builder.RegisterInstance((GetLoggerService(serverSettings))).SingleInstance().As<ILogService>() |> ignore
        // Register server
        //builder.RegisterType<Owin.Server>().As<IServer>().SingleInstance().Named("http") |> ignore
        builder.Build()
    
    /// <summary>
    /// Load third party plug ins
    /// </summary>
    let LoadPlugins() = 
        // Load plug-in DLLs
        for file in Directory.EnumerateFiles(Constants.PluginFolder, "dll") do
            System.Reflection.Assembly.LoadFile(file) |> ignore
    
    /// <summary>
    /// Used by windows service (top shelf) to start and stop windows service.
    /// </summary>
    [<Sealed>]
    type NodeService(serverSettings : ServerSettings, testServer : bool) = 
        let container = GetContainer(serverSettings, testServer)
        let mutable httpServer = Unchecked.defaultof<IServer>
        
        do 
            // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
            // To verify that this works, run `netsh http show servicestate`.
            if testServer <> true then MaximizeThreads() |> ignore
        
        member this.Start() = 
            try 
                let indexService = container.Resolve<IIndexService>()
                let httpFactory = container.Resolve<IFlexFactory<IHttpResource>>()
                httpServer <- new OwinServer(indexService, httpFactory)
                httpServer.Start()
            with e -> printfn "%A" e
        
        member this.Stop() = 
            httpServer.Stop()
            let indexService = container.Resolve<IIndexService>()
            let state = container.Resolve<IndicesState>()
            // Close all open indices
            for registeration in state.IndexRegisteration do
                indexService.CloseIndex(registeration.Key) |> ignore
