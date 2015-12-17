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
        builder.RegisterModule<'T>() |> ignore; builder
        
    let registerInstance<'Interface> implementation (builder : ContainerBuilder) =
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
        try (value.GetType() |> getTypeNameFromAttribute, value) |> Some
        with e -> Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Critical); None

    let updateContainer (registrationFunction : ContainerBuilder -> ContainerBuilder)  (container : IContainer) =
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
        |> Seq.iter (fun instance -> builder.RegisterInstance(instance).SingleInstance().As<'Interface>() |> ignore)
        
        builder.Update container
        container


    let registerImportGroup<'Interface> (mefContainer : CompositionContainer) (builder : ContainerBuilder) =
        try
            let importGroup = mefContainer.GetExportedValues<'Interface>()
                              // Since MEF doesn't recognize attributes for the derived types (when using InheritedExport),
                              // we'll just going to use reflection to get the Name from a specific type.
                              |> Seq.choose mapImportsToNames
                              |> fun x -> x.ToDictionary(fst, snd)
        
            builder.RegisterInstance(importGroup).SingleInstance().As<Dictionary<string,'Interface>>() |> ignore
        
            Logger.Log <| PluginsLoaded(typedefof<'Interface>.FullName, importGroup.Keys.ToList())
        with e -> Logger.Log <| PluginLoadFailure("Unknown", typedefof<'Interface>.FullName, exceptionPrinter e);

        builder

    let injectServicesToMef (mefContainer : CompositionContainer) (diContainer : IContainer) =
        diContainer.ComponentRegistry.Registrations
        |> Seq.collect (fun r -> r.Services)
        |> fun s -> s.OfType<IServiceWithType>()
        |> Seq.map (fun r -> r.ServiceType) // Get the interface type
        |> Seq.iter (fun interfaceType -> 
                        // Get an instance from that interface
                        let instance = diContainer.Resolve interfaceType
                        // Add the instance to MEF
                        mefContainer.ComposeParts(instance))
        
        diContainer

    let getMefContainer () =
        let catalog = new AggregateCatalog(new DirectoryCatalog(@".\Plugins"),
                                           new AssemblyCatalog(Assembly.GetExecutingAssembly()))
        new CompositionContainer(catalog)

    let injectFromAspDi (services : IServiceCollection) (builder : ContainerBuilder) =
        builder.Populate <| services.AsEnumerable(); builder

[<AutoOpen>]
module Main = 
    open Autofac.Extras.AttributeMetadata
    open FlexSearch.Core

    /// Get a container with all dependencies setup
    let setupDependencies (testServer : bool) (serverSettings : Settings.T) (services : IServiceCollection) = 
        let mefContainer = getMefContainer()
        let builder = new ContainerBuilder()
        // Register the service to consume with meta-data.
        // Since we're using attributed meta-data, we also
        // need to register the AttributedMetadataModule
        // so the meta-data attributes get read.
        builder
        |> registerModule<AttributedMetadataModule>
        |> injectFromAspDi services
        |> registerInstance<Settings.T>(serverSettings)
        // Register the groups/factories of services
        |> registerImportGroup<IFlexQuery> mefContainer
        |> registerImportGroup<IFlexQueryFunction> mefContainer
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
        |> registerSingleton<EventAggregrator, EventAggregrator>
        // Build the container
        |> fun b -> b.Build()
        // We need to add the resolved instances as exports to MEF so that it 
        // can use them in the HttpHandlers
        |> injectServicesToMef mefContainer
        // Register the HttpHandlers
        |> updateContainer (registerImportGroup<IHttpHandler> mefContainer)
        // Register the services required for shutdown
        |> registerExistingInstanceAs<IRequireNotificationForShutdown>
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

/// Used by windows service (top shelf) to start and stop windows service.
[<Sealed>]
type NodeService(serverSettings : Settings.T, testServer : bool) = 
    let mutable httpServer = Unchecked.defaultof<IServer>
    
    /// Perform all the clean up tasks to be run just before a shutdown request is 
    /// received by the server
    let shutdown() = 
        httpServer.Stop()
        // Get all types which implement IRequireNotificationForShutdown and issue shutdown command
        match httpServer.Services with
        | Some(container) -> 
            container.GetServices<IRequireNotificationForShutdown>()
            |> Seq.toArray
            |> Array.Parallel.iter (fun x -> x.Shutdown() |> Async.RunSynchronously)
        | _ -> 
            Logger.Log("Couldn't get access to the web server's service provider", MessageKeyword.Default, MessageLevel.Warning)
    
    // do 
    // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
    // To verify that this works, run `netsh http show servicestate`.
    //            if testServer <> true then MaximizeThreads() |> ignore
    member __.Start() = 
        try 
            httpServer <- new WebServer(setupDependencies testServer, serverSettings)
            httpServer.Start()
        with e -> printfn "%A" e
    
    member __.Stop() = shutdown()

module StartUp = 
    open Logging
    open System.Reflection
    open System.Diagnostics
    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.Logging.Console
    open Microsoft.Extensions.Logging.TraceSource
    
    let version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
    
    /// Header text to be displayed at the beginning of the program
    let headerText = 
        let text = """FlexSearch Server
Flexible and fast search engine for the .Net Platform
------------------------------------------------------------------
Version : {version}
Copyright (C) 2010 - {year} - FlexSearch
------------------------------------------------------------------

"""
        text.Replace("{version}", version).Replace("{year}", DateTime.Now.Year.ToString())
    
    let (!>) (msg : string) = Logger.Log(msg, MessageKeyword.Startup, MessageLevel.Verbose)
    
    /// Checks if the application is in interactive user mode?
    let isInteractive = Environment.UserInteractive
    
    /// Initialize the listeners to be used across the application
    let initializeListeners() = 
        if isInteractive then 
            // Only use console listener in user interactive mode
            Logging._loggerFactory.AddTraceSource(_sourceSwitch, new ConsoleTraceListener(false)) |> ignore
            
        // Write all start up events to a specific file. This is helpful in case ETW is not
        // setup properly. The slight overhead of writing to two sinks is negligible.
        let twl = new TextWriterTraceListener(Constants.LogsFolder +/ "startup-log.txt", "FlexSearch")
        twl.Filter <- { new TraceFilter() with
                member this.ShouldTrace(cache, source, eventType, id, format, args, data1, data) =
                    // Startup events
                    (id >= 7000 && id < 8000 )
                    // Node events
                    || (id >= 1000 && id < 2000) }
        Logging._loggerFactory.AddTraceSource(_sourceSwitch, twl) |> ignore

    /// To improve CPU utilization, increase the number of threads that the .NET thread pool expands by when
    /// a burst of requests come in. We could do this by editing machine.config/system.web/processModel/minWorkerThreads,
    /// but that seems too global a change, so we do it in code for just our AppPool. More info: 
    /// http://support.microsoft.com/kb/821268
    /// http://blogs.msdn.com/b/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx
    /// http://blogs.msdn.com/b/perfworld/archive/2010/01/13/how-can-i-improve-the-performance-of-asp-net-by-adjusting-the-clr-thread-throttling-properties.aspx
    let maximizeThreads() = 
        let newMinWorkerThreads = 10
        let (minWorkerThreads, minCompletionPortThreads) = ThreadPool.GetMinThreads()
        ThreadPool.SetMinThreads(Environment.ProcessorCount * newMinWorkerThreads, minCompletionPortThreads)
    
    /// Capture all un-handled exceptions
    let subscribeToUnhandledExceptions() = 
        AppDomain.CurrentDomain.UnhandledException.Subscribe(fun x -> 
            Logger.Log(sprintf "%A" x.ExceptionObject, MessageKeyword.Node, MessageLevel.Critical)
            if isInteractive then 
                printfn 
                    "The application has encountered a critical error and will shutdown. Please refer to the Startup-Log.txt under logs folder to get more information."
                printfn "Press any key to continue . . ."
                Console.ReadKey() |> ignore)
        |> ignore
    
    /// Load server settings
    let loadSettings() = 
        match Settings.create (Constants.ConfFolder +/ "Config.ini") with
        | Ok(s) -> new Settings.T(s)
        | Fail(e) -> 
            Logger.Log
                ("Error parsing 'Config.ini' file.", ValidationException(e), MessageKeyword.Startup, 
                 MessageLevel.Critical)
            failwithf "%s" (e.ToString())
    
    /// Load all plug-ins from the plug-ins folder
    let loadAllPlugins() = 
        for file in Directory.EnumerateFiles(Constants.PluginFolder, "*.dll", SearchOption.TopDirectoryOnly) do
            try 
                System.Reflection.Assembly.LoadFile(file) |> ignore
            with e -> Logger.Log("Error loading plug-in library.", e, MessageKeyword.Startup, MessageLevel.Warning)
    
    /// Load all the server components and return settings
    let load() = 
        // NOTE: The order of operations below is very important 
        // It is important to initialize Listeners first otherwise logging services will not be available 
        initializeListeners()
        subscribeToUnhandledExceptions()
        loadAllPlugins()
        loadSettings()