module Startup

open FlexSearch.Core
open FlexSearch.Core.Logging
open System
open System.Reflection
open System.Diagnostics
open System.Threading
open System.IO
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
                            (// Startup events
                                id >= 7000 && id < 8000) // Node events
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
    
open FlexSearch.Server.HomepageGenerator
let generateHomePage() =
    match buildHomePage() with
    | Ok() -> ()
    | Fail(e) -> Logger.Log(e, MessageKeyword.Startup, MessageLevel.Error)

/// Load all the server components and return settings
let load() = 
    // NOTE: The order of operations below is very important 
    // It is important to initialize Listeners first otherwise logging services will not be available 
    initializeListeners()
    subscribeToUnhandledExceptions()
    generateHomePage()
    loadSettings()
