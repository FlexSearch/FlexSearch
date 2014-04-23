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
    open FlexSearch.Api
    open FlexSearch.Api.Message
    open FlexSearch.Core
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
    
    /// <summary>
    /// Get a container with all dependencies setup
    /// </summary>
    /// <param name="serverSettings"></param>
    /// <param name="testServer"></param>
    let GetContainer(serverSettings : ServerSettings, testServer : bool) = 
        let builder = new ContainerBuilder()
        // Interface scanning
        builder |> FactoryService.RegisterInterfaceAssemblies<IImportHandler>
        builder |> FactoryService.RegisterInterfaceAssemblies<IFlexFilterFactory>
        builder |> FactoryService.RegisterInterfaceAssemblies<IFlexTokenizerFactory>
        builder |> FactoryService.RegisterInterfaceAssemblies<IFlexQuery>
        // Abstract class scanning
        builder |> FactoryService.RegisterAbstractClassAssemblies<HttpModuleBase>
        builder |> FactoryService.RegisterAbstractClassAssemblies<Analyzer>
        // Factory registration
        builder |> FactoryService.RegisterSingleFactoryInstance<IImportHandler>
        builder |> FactoryService.RegisterSingleFactoryInstance<IFlexFilterFactory>
        builder |> FactoryService.RegisterSingleFactoryInstance<IFlexTokenizerFactory>
        builder |> FactoryService.RegisterSingleFactoryInstance<IFlexQuery>
        builder |> FactoryService.RegisterSingleFactoryInstance<HttpModuleBase>
        builder |> FactoryService.RegisterSingleFactoryInstance<Analyzer>
        builder |> FactoryService.RegisterSingleInstance<SettingsBuilder, ISettingsBuilder>
        builder |> FactoryService.RegisterSingleInstance<ResourceLoader, IResourceLoader>
        builder |> FactoryService.RegisterSingleInstance<PersistanceStore, IPersistanceStore>
        let indicesState = 
            { IndexStatus = new ConcurrentDictionary<string, IndexState>(StringComparer.OrdinalIgnoreCase)
              IndexRegisteration = new ConcurrentDictionary<string, FlexIndex>(StringComparer.OrdinalIgnoreCase)
              ThreadLocalStore = 
                  new ThreadLocal<ConcurrentDictionary<string, ThreadLocalDocument>>(fun () -> 
                  new ConcurrentDictionary<string, ThreadLocalDocument>(StringComparer.OrdinalIgnoreCase)) }
        builder.RegisterInstance(serverSettings).SingleInstance() |> ignore
        builder.RegisterInstance(indicesState).SingleInstance() |> ignore
        builder |> FactoryService.RegisterSingleInstance<NodeState, INodeState>
        // Register server
        builder.RegisterType<Owin.Server>().As<IServer>().SingleInstance().Named("http") |> ignore
        builder.Build()
    
    /// <summary>
    /// Used by windows service (top shelf) to start and stop windows service.
    /// </summary>
    type NodeService(serverSettings : ServerSettings, testServer : bool) = 
        let container = GetContainer(serverSettings, testServer)
        
        do 
            // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
            // To verify that this works, run `netsh http show servicestate`.
            if testServer <> true then MaximizeThreads() |> ignore
        
        member this.Start() = container.ResolveNamed<IServer>("http").Start()
        member this.Stop() = container.ResolveNamed<IServer>("http").Start()
