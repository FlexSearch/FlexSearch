// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open Autofac
open Autofac.Features.Metadata
open FlexSearch.Core
open Microsoft.Owin
open Microsoft.Owin.Hosting
open Owin
open System
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Threading.Tasks

// ----------------------------------------------------------------------------
// Contains container and other factory implementation
// ----------------------------------------------------------------------------
[<AutoOpen>]
module FactoryService = 
    /// Register all the interface assemblies
    let registerInterfaceAssemblies<'T> (builder : ContainerBuilder) = 
        builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies())
               .Where(fun t -> t.GetInterfaces().Any(fun i -> i.IsAssignableFrom(typeof<'T>))).AsImplementedInterfaces() 
        |> ignore
    
    /// Register all the abstract class assemblies
    let registerAbstractClassAssemblies<'T> (builder : ContainerBuilder) = 
        builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(fun t -> t.BaseType = typeof<'T>).As<'T>
            () |> ignore
    
    /// Register an instance as single instance in the builder
    let registerSingleInstance<'T, 'U> (builder : ContainerBuilder) = 
        builder.RegisterType<'T>().As<'U>().SingleInstance() |> ignore
    
    /// Factory implementation
    [<Sealed>]
    type FlexFactory<'T>(container : ILifetimeScope, logger : ILogService) = 
        let moduleTypeName = typeof<'T>.Name
        
        /// Returns a module by name.
        /// Choice1of3 -> instance of T
        /// Choice2of3 -> meta-data of T
        /// Choice3of3 -> error
        let getModuleByName (moduleName, metaOnly) = 
            if (System.String.IsNullOrWhiteSpace(moduleName)) then 
                Choice3Of3 <| ModuleNotFound(moduleName, moduleTypeName)
            else 
                // We cannot use a global instance of factory as it will cache the instances. We
                // need a new instance of T per request 
                let factory = container.Resolve<IEnumerable<Meta<Lazy<'T>>>>()
                let injectMeta = 
                    factory.FirstOrDefault
                        (fun a -> 
                        a.Metadata.Keys.Contains("Name") 
                        && String.Equals(a.Metadata.["Name"].ToString(), moduleName, StringComparison.OrdinalIgnoreCase))
                match injectMeta with
                | null -> Choice3Of3 <| ModuleNotFound(moduleName, moduleTypeName)
                | _ -> 
                    if metaOnly then Choice2Of3(injectMeta.Metadata)
                    else 
                        try 
                            let pluginValue = injectMeta.Value.Value
                            logger.ComponentLoaded(moduleName, typeof<'T>.FullName)
                            Choice1Of3(pluginValue)
                        with e -> 
                            logger.ComponentInitializationFailed(moduleName, typeof<'T>.FullName, e)
                            Choice3Of3 <| ModuleInitializationError(moduleName, moduleTypeName, e.Message)
        
        interface IFlexFactory<'T> with
            
            member __.GetModuleByName(moduleName) = 
                match getModuleByName (moduleName, false) with
                | Choice1Of3(x) -> ok x
                | _ -> fail <| ModuleNotFound(moduleName, moduleTypeName)
            
            member __.GetMetaData(moduleName) = 
                match getModuleByName (moduleName, true) with
                | Choice2Of3(x) -> Choice1Of2(x)
                | _ -> fail <| ModuleNotFound(moduleName, moduleTypeName)
            
            member __.ModuleExists(moduleName) = 
                match getModuleByName (moduleName, true) with
                | Choice1Of3(_) -> false
                | _ -> true
            
            member __.GetAllModules() = 
                let modules = new Dictionary<string, 'T>(StringComparer.OrdinalIgnoreCase)
                let factory = container.Resolve<IEnumerable<Meta<Lazy<'T>>>>()
                for plugin in factory do
                    if plugin.Metadata.ContainsKey("Name") then 
                        let pluginName = plugin.Metadata.["Name"].ToString()
                        try 
                            let pluginValue = plugin.Value.Value
                            modules.Add(pluginName, pluginValue)
                            logger.ComponentLoaded(pluginName, typeof<'T>.FullName)
                        with e -> logger.ComponentInitializationFailed(pluginName, typeof<'T>.FullName, e)
                modules
    
    let registerSingleFactoryInstance<'T> (builder : ContainerBuilder) = 
        builder.RegisterType<FlexFactory<'T>>().As<IFlexFactory<'T>>().SingleInstance() |> ignore

[<AutoOpen>]
module Main = 
    open Autofac
    open Autofac.Extras.Attributed
    open FlexSearch.Core
    open FlexSearch.Core.Formatters
    
    /// <summary>
    /// Get a container with all dependencies setup
    /// </summary>
    /// <param name="serverSettings"></param>
    /// <param name="testServer"></param>
    let getContainer (serverSettings : ServerSettings.T, logService : ILogService, testServer : bool) = 
        let builder = new ContainerBuilder()
        // Register the service to consume with meta-data.
        // Since we're using attributed meta-data, we also
        // need to register the AttributedMetadataModule
        // so the meta-data attributes get read.
        builder.RegisterModule<AttributedMetadataModule>() |> ignore
        builder.RegisterInstance(serverSettings).SingleInstance().As<ServerSettings.T>() |> ignore
        builder.RegisterInstance(logService).As<ILogService>() |> ignore
        // Interface scanning
        builder |> FactoryService.registerInterfaceAssemblies<IFlexQuery>
        builder |> FactoryService.registerInterfaceAssemblies<IHttpResource>
        // Abstract class scanning
        builder |> FactoryService.registerAbstractClassAssemblies<HttpHandlerBase<_, _>>
        // Factory registration
        builder |> FactoryService.registerSingleFactoryInstance<IHttpResource>
        builder |> FactoryService.registerSingleFactoryInstance<IFlexQuery>
        //        builder |> FactoryService.registerSingleInstance<SettingsBuilder, ISettingsBuilder>
        builder |> FactoryService.registerSingleInstance<YamlFormatter, IFormatter>
        //builder |> FactoryService.registerSingleInstance<ThreadSafeFileWiter, IThreadSafeWriter>
        //builder |> FactoryService.registerSingleInstance<RegisterationManager, RegisterationManager>
        // Register services
        builder |> FactoryService.registerSingleInstance<FlexParser, IFlexParser>
        builder |> FactoryService.registerSingleInstance<IndexService.Service, IIndexService>
        //        builder |> FactoryService.registerSingleInstance<DocumentService, IDocumentService>
        //        builder |> FactoryService.registerSingleInstance<QueueService, IQueueService>
        //        builder |> FactoryService.registerSingleInstance<SearchService, ISearchService>
        //        builder |> FactoryService.registerSingleInstance<JobService, IJobService>
        //        builder |> FactoryService.registerSingleInstance<AnalyzerService, IAnalyzerService>
        //        builder |> FactoryService.registerSingleInstance<DemoIndexService, DemoIndexService>
        //        builder |> FactoryService.registerSingleInstance<ResourceService, IResourceService>
        //        builder |> FactoryService.registerSingleInstance<FactoryService.FactoryCollection, IFactoryCollection>
        builder.Build()
    
    /// <summary>
    /// Used by windows service (top shelf) to start and stop windows service.
    /// </summary>
    [<Sealed>]
    type NodeService(serverSettings : ServerSettings.T, logService : ILogService, testServer : bool) = 
        let container = getContainer (serverSettings, logService, testServer)
        let mutable httpServer = Unchecked.defaultof<IServer>
        
        //        do 
        // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
        // To verify that this works, run `netsh http show servicestate`.
        //            if testServer <> true then MaximizeThreads() |> ignore
        member __.Start() = 
            try 
                let indexExists = container.Resolve<IIndexService>().IndexExists
                let httpModules = container.Resolve<IFlexFactory<IHttpResource>>().GetAllModules()
                let loggerService = container.Resolve<ILogService>()
                httpServer <- new OwinServer(indexExists, httpModules, loggerService, serverSettings.HttpPort)
                httpServer.Start()
            with e -> printfn "%A" e
        
        member __.Stop() = httpServer.Stop()
//            let indexService = container.Resolve<IIndexService>()
// Close all open indices
//            match indexService.GetAllIndex() with
//            | Choice1Of2(regs) -> 
//                for registeration in regs do
//                    indexService.CloseIndex(registeration.IndexName) |> ignore
//            | _ -> ()
