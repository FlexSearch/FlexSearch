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
open Autofac.Integration.Mef
open Autofac.Core
open Autofac.Builder
open Autofac.Features.Metadata
open Autofac.Extensions.DependencyInjection
open FlexSearch.Core
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open System.Threading
open System.Threading.Tasks
open System.ComponentModel.Composition
open System.ComponentModel.Composition.Hosting
open Microsoft.Extensions.DependencyInjection

// ----------------------------------------------------------------------------
// Contains container and other factory implementation
// ----------------------------------------------------------------------------
[<AutoOpen>]
module BootstrappingHelpers = 
    let registerModule<'T when 'T : (new : unit -> 'T) and 'T :> IModule> (builder : ContainerBuilder) = 
        builder.RegisterModule<'T>() |> ignore
        builder
    
    /// Register all the interface assemblies
    let registerInterfaceAssemblies<'T> (builder : ContainerBuilder) = 
        builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies())
               .Where(fun t -> t.GetInterfaces().Any(fun i -> i.IsAssignableFrom(typeof<'T>))).AsImplementedInterfaces() 
        |> ignore
        builder
    
    let registerInstance<'Interface> (implementation : obj) (builder : ContainerBuilder) = 
        builder.RegisterInstance(implementation).SingleInstance().As<'Interface>() |> ignore
        builder
    
    /// Register an instance as single instance in the builder
    let registerSingleton<'Implementation, 'Interface> (builder : ContainerBuilder) = 
        builder.RegisterType<'Implementation>().As<'Interface>().SingleInstance() |> ignore
        builder
    
    let registerSingletonWithParam<'Implementation, 'Interface> paramName paramValue (builder : ContainerBuilder) = 
        builder.RegisterType<'Implementation>().As<'Interface>().SingleInstance()
            .WithParameter(paramName, Some(paramValue)) |> ignore
        builder
    
    let private mapImportsToNames (value : 'T) = 
        try 
            (value.GetType() |> getTypeNameFromAttribute, value) |> Some
        with e -> 
            Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Critical)
            None
    
    let updateContainer (registrationFunction : ContainerBuilder -> ContainerBuilder) (container : IContainer) = 
        new ContainerBuilder()
        |> registrationFunction
        |> fun builder -> builder.Update container
        container
    
    let registerExistingInstanceAs<'Interface> (container : IContainer) = 
        let builder = new ContainerBuilder()
        container.ComponentRegistry.Registrations
        |> Seq.where (fun x -> typeof<'Interface>.IsAssignableFrom(x.Activator.LimitType))
        // Get the main interface it implements
        |> Seq.map (fun x -> x.Services |> Seq.head :?> TypedService |> fun s -> s.ServiceType)
        // Resolve to an instance
        |> Seq.map container.Resolve
        // Register that instance to the new interface
        |> Seq.iter (fun instance -> 
               builder
               |> registerInstance<'Interface> (instance)
               |> ignore)
        builder.Update container
        container
    
    let logEvents (eventAggregator : EventAggregator) = 
        fun event -> 
            match event with
            | IndexStatusChange(idxName, status) -> 
                Logger.Log
                    (sprintf "Index '%s' changed status to '%s'" idxName status, MessageKeyword.Node, MessageLevel.Info)
            | ShardStatusChange(idxName, sNo, status) -> 
                Logger.Log
                    (sprintf "Shard number %i of index '%s' changed status to '%s'" sNo idxName status, 
                     MessageKeyword.Node, MessageLevel.Info)
            | RegisterForShutdownCallback(service) -> 
                Logger.Log
                    (sprintf "Service %s has been called to shut down" <| service.GetType().FullName, 
                     MessageKeyword.Node, MessageLevel.Info)
        |> Event.add
        <| eventAggregator.Event()
        eventAggregator
    
    let registerGroup<'T when 'T : not struct> (container : IContainer) = 
        let instances = new Dictionary<string, 'T>(StringComparer.OrdinalIgnoreCase)
        let factory = container.Resolve<IEnumerable<Meta<Lazy<'T>>>>()
        let moduleNames = new ResizeArray<string>()
        for plugin in factory do
            if plugin.Metadata.ContainsKey("Name") then 
                let pluginName = plugin.Metadata.["Name"].ToString()
                try 
                    let pluginValue = plugin.Value.Value
                    instances.Add(pluginName, pluginValue)
                    moduleNames.Add(pluginName)
                with e -> Logger.Log <| PluginLoadFailure(pluginName, typeof<'T>.FullName, exceptionPrinter e)
        Logger.Log <| PluginsLoaded(typeof<'T>.FullName, moduleNames)

        let builder = new ContainerBuilder()
        builder.RegisterInstance<Dictionary<string, 'T>>(instances) |> ignore
        builder.Update container
        container
    
    let injectFromAspDi (services : IServiceCollection) (builder : ContainerBuilder) = 
        builder.Populate <| services.AsEnumerable()
        builder

[<AutoOpen>]
module Main = 
    open Autofac.Extras.AttributeMetadata
    open FlexSearch.Core
    
    /// Get a container with all dependencies setup
    let setupDependencies (testServer : bool) (serverSettings : Settings.T) (services : IServiceCollection) = 
        let builder = new ContainerBuilder()
        // The event aggregator logging needs to be instantiated here because log messages
        // begin to be pushed once services are instantiated/resolved
        let eventAggregator = new EventAggregator() |> logEvents
        // Register the service to consume with meta-data.
        // Since we're using attributed meta-data, we also
        // need to register the AttributedMetadataModule
        // so the meta-data attributes get read.
        builder
        |> registerModule<AttributedMetadataModule>
        |> injectFromAspDi services
        |> registerInstance<EventAggregator> (eventAggregator)
        |> registerInstance<Settings.T> (serverSettings)
        // Interface scanning
        |> registerInterfaceAssemblies<IFlexQuery>
        |> registerInterfaceAssemblies<IFlexQueryFunction>
        |> registerInterfaceAssemblies<IHttpHandler>
        // Register Utilities
        |> registerSingleton<NewtonsoftJsonFormatter, IFormatter>
        |> registerSingleton<ThreadSafeFileWriter, ThreadSafeFileWriter>
        |> registerSingleton<FlexParser, IFlexParser>
        // Register services
        |> registerSingletonWithParam<AnalyzerService, IAnalyzerService> "testMode" testServer
        |> registerSingletonWithParam<IndexService, IIndexService> "testMode" testServer
        |> registerSingleton<ScriptService, IScriptService>
        |> registerSingleton<DocumentService, IDocumentService>
        |> registerSingleton<QueueService, IQueueService>
        |> registerSingleton<SearchService, ISearchService>
        |> registerSingleton<JobService, IJobService>
        |> registerSingleton<DemoIndexService, DemoIndexService>
        // Build the container
        |> fun b -> b.Build()
        // Register the services required for shutdown
        |> registerExistingInstanceAs<IRequireNotificationForShutdown>
        // Register the groups/factories of services
        |> registerGroup<IFlexQuery>
        |> registerGroup<IFlexQueryFunction>
        |> registerGroup<IHttpHandler>
        // Return an IServiceProvider to be compatible with Microsoft's DI
        |> fun container -> container.Resolve<IServiceProvider>()

module Interop = 
    open System
    open System.Linq
    open System.Runtime.InteropServices
    open System.Reflection
    open System.Threading
    
    type HTTP_SERVER_PROPERTY = 
        | HttpServerAuthenticationProperty = 0
        | HttpServerLoggingProperty = 1
        | HttpServerQosProperty = 2
        | HttpServerTimeoutsProperty = 3
        | HttpServerQueueLengthProperty = 4
        | HttpServerStateProperty = 5
        | HttpServer503VerbosityProperty = 6
        | HttpServerBindingProperty = 7
        | HttpServerExtendedAuthenticationProperty = 8
        | HttpServerListenEndpointProperty = 9
        | HttpServerChannelBindProperty = 10
        | HttpServerProtectionLevelProperty = 11
    
    [<DllImport("httpapi.dll", CallingConvention = CallingConvention.StdCall)>]
    extern uint32 HttpSetRequestQueueProperty(CriticalHandle requestQueueHandle, HTTP_SERVER_PROPERTY serverProperty, uint32& pPropertyInfo, uint32 propertyInfoLength, uint32 reserved, IntPtr pReserved)
    
    // Adapted from:
    // http://stackoverflow.com/questions/15417062/changing-http-sys-kernel-queue-limit-when-using-net-httplistener
    /// Sets the request queue length of a HTTP listener
    let setRequestQueueLength (listener : System.Net.HttpListener, len : uint32) = 
        let listenerType = typeof<System.Net.HttpListener>
        let mutable length = len
        let requestQueueHandleProperty = 
            listenerType.GetProperties(BindingFlags.NonPublic ||| BindingFlags.Instance)
                        .First(fun p -> p.Name = "RequestQueueHandle")
        let requestQueueHandle = requestQueueHandleProperty.GetValue(listener) :?> CriticalHandle
        let result = 
            HttpSetRequestQueueProperty
                (requestQueueHandle, HTTP_SERVER_PROPERTY.HttpServerQueueLengthProperty, &length, 
                 Marshal.SizeOf(len) |> uint32, 0u, IntPtr.Zero)
        if result <> 0u then failwithf ""