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
open System.IO
open System.Linq
open System.Threading
open System.Threading.Tasks
open Microsoft.Practices.EnterpriseLibrary.SemanticLogging
open FlexSearch.Logging
open System.Diagnostics.Tracing

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
    
    /// Register an instance as single instance in the builder
    let registerSingleInstance<'T, 'U> (builder : ContainerBuilder) = 
        builder.RegisterType<'T>().As<'U>().SingleInstance() |> ignore
    
    /// Factory implementation
    [<Sealed>]
    type FlexFactory<'T>(container : ILifetimeScope) = 
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
                            //Log.componentLoaded (moduleName, typeof<'T>.FullName)
                            Choice1Of3(pluginValue)
                        with e -> 
                            //Log.componentInitializationFailed (moduleName, typeof<'T>.FullName, exceptionPrinter e)
                            Choice3Of3 <| ModuleInitializationError(moduleName, moduleTypeName, e.Message)
        
        interface IFlexFactory<'T> with
            
            member __.GetModuleByName(moduleName) = 
                match getModuleByName (moduleName, false) with
                | Choice1Of3(x) -> ok x
                | _ -> fail <| ModuleNotFound(moduleName, moduleTypeName)
            
            member __.GetMetaData(moduleName) = 
                match getModuleByName (moduleName, true) with
                | Choice2Of3(x) -> ok (x)
                | _ -> fail <| ModuleNotFound(moduleName, moduleTypeName)
            
            member __.ModuleExists(moduleName) = 
                match getModuleByName (moduleName, true) with
                | Choice1Of3(_) -> false
                | _ -> true
            
            member __.GetAllModules() = 
                let modules = new Dictionary<string, 'T>(StringComparer.OrdinalIgnoreCase)
                let factory = container.Resolve<IEnumerable<Meta<Lazy<'T>>>>()
                let moduleNames = new ResizeArray<string>()
                for plugin in factory do
                    if plugin.Metadata.ContainsKey("Name") then 
                        let pluginName = plugin.Metadata.["Name"].ToString()
                        try 
                            let pluginValue = plugin.Value.Value
                            modules.Add(pluginName, pluginValue)
                            moduleNames.Add(pluginName)
                        with e -> Logger.Log <| PluginLoadFailure(pluginName, typeof<'T>.FullName, exceptionPrinter e)
                Logger.Log <| PluginsLoaded(typeof<'T>.FullName, moduleNames)
                modules
    
    let registerSingleFactoryInstance<'T> (builder : ContainerBuilder) = 
        builder.RegisterType<FlexFactory<'T>>().As<IFlexFactory<'T>>().SingleInstance() |> ignore

[<AutoOpen>]
module Main = 
    open Autofac
    open Autofac.Extras.Attributed
    open FlexSearch.Core
    open System.Reflection
    
    /// Get a container with all dependencies setup
    let getContainer (serverSettings : Settings.T, testServer : bool) = 
        let builder = new ContainerBuilder()
        // Register the service to consume with meta-data.
        // Since we're using attributed meta-data, we also
        // need to register the AttributedMetadataModule
        // so the meta-data attributes get read.
        builder.RegisterModule<AttributedMetadataModule>() |> ignore
        builder.RegisterInstance(serverSettings).SingleInstance().As<Settings.T>() |> ignore
        // Interface scanning
        builder |> FactoryService.registerInterfaceAssemblies<IFlexQuery>
        builder |> FactoryService.registerInterfaceAssemblies<IFlexQueryFunction>
        builder |> FactoryService.registerInterfaceAssemblies<IHttpHandler>
        // Factory registration
        builder |> FactoryService.registerSingleFactoryInstance<IFlexQuery>
        builder |> FactoryService.registerSingleFactoryInstance<IFlexQueryFunction>
        builder |> FactoryService.registerSingleFactoryInstance<IHttpHandler>
        builder |> FactoryService.registerSingleInstance<NewtonsoftJsonFormatter, IFormatter>
        builder |> FactoryService.registerSingleInstance<ThreadSafeFileWriter, ThreadSafeFileWriter>
        builder |> FactoryService.registerSingleInstance<FlexParser, IFlexParser>
        // Register services
        builder.RegisterType<AnalyzerService>().As<IAnalyzerService>().SingleInstance()
            .WithParameter("testMode", Some(testServer)) |> ignore
        builder.RegisterType<IndexService>().As<IIndexService>().SingleInstance()
            .WithParameter("testMode", Some(testServer)) |> ignore
        builder |> FactoryService.registerSingleInstance<ScriptService, IScriptService>
        builder |> FactoryService.registerSingleInstance<DocumentService, IDocumentService>
        builder |> FactoryService.registerSingleInstance<QueueService, IQueueService>
        builder |> FactoryService.registerSingleInstance<SearchService, ISearchService>
        builder |> FactoryService.registerSingleInstance<JobService, IJobService>
        builder |> FactoryService.registerSingleInstance<DemoIndexService, DemoIndexService>
        builder |> FactoryService.registerSingleInstance<EventAggregrator, EventAggregrator>
        builder.Build()

//            let indexService = container.Resolve<IIndexService>()
// Close all open indices
//            match indexService.GetAllIndex() with
//            | Ok(regs) -> 
//                for registeration in regs do
//                    indexService.CloseIndex(registeration.IndexName) |> ignore
//            | _ -> ()
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
    let container = getContainer (serverSettings, testServer)
    let mutable httpServer = Unchecked.defaultof<IServer>
    let port = serverSettings.GetInt(Settings.ServerKey, Settings.HttpPort, 9800)
    
    /// Perform all the clean up tasks to be run just before a shutdown request is 
    /// received by the server
    let shutdown() = 
        httpServer.Stop()
        // Get all types which implements IRequireNotificationForShutdown and issue shutdown command
        container.ComponentRegistry.Registrations
        |> Seq.where (fun x -> typeof<IRequireNotificationForShutdown>.IsAssignableFrom(x.Activator.LimitType))
        |> Seq.map (fun x -> x.Activator.LimitType |> container.Resolve :?> IRequireNotificationForShutdown)
        |> Seq.toArray
        |> Array.Parallel.iter (fun x -> x.Shutdown() |> Async.RunSynchronously)
    
    // do 
    // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
    // To verify that this works, run `netsh http show servicestate`.
    //            if testServer <> true then MaximizeThreads() |> ignore
    member __.Start() = 
        try 
            let handlerModules = container.Resolve<IFlexFactory<IHttpHandler>>().GetAllModules()
            httpServer <- new OwinServer(generateRoutingTable handlerModules, port)
            httpServer.Start()
        with e -> printfn "%A" e
    
    member __.Stop() = shutdown()

type EventTextFormatter() = 
    interface Formatters.IEventTextFormatter with
        member __.WriteEvent(eventEntry, writer) = 
            writer.WriteLine(sprintf "[%s] %s" (eventEntry.Schema.Level.ToString()) eventEntry.FormattedMessage)

module StartUp = 
    open System.Reflection
    
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
    
    let private consoleSink = ConsoleLog.CreateListener(new EventTextFormatter())
    
    let private rollingFileSink = 
        let fileName = Constants.LogsFolder +/ "startup-log.txt"
        if File.Exists(fileName) then File.Delete(fileName)
        File.AppendAllText(fileName, headerText)
        FlatFileLog.CreateListener(fileName, new EventTextFormatter())
    
    /// Initialize the listeners to be used across the application
    let initializeListeners() = 
        if isInteractive then 
            // Only use console listener in user interactive mode
            consoleSink.EnableEvents(LogService.GetLogger(), EventLevel.LogAlways)
        // Write all start up events to a specific file. This is helpful in case ETW is not
        // setup properly. The slight overhead of writing to two sinks is negligible.
        rollingFileSink.EnableEvents
            (LogService.GetLogger(), EventLevel.LogAlways, LogService.Keywords.Startup ||| LogService.Keywords.Node)
    
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
