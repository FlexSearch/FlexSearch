open FlexSearch.Core
open System
open System.IO
open Topshelf

[<EntryPoint>]
let main argv = 
    // Capture all unhandled errors
    AppDomain.CurrentDomain.UnhandledException.Subscribe
        (fun x -> Logger.Log(sprintf "%A" x.ExceptionObject, MessageKeyword.Node, MessageLevel.Critical)) |> ignore
    // Load server settings
    let settings = 
        match ServerSettings.createFromFile (Path.Combine(Constants.ConfFolder, "Config.json"), jsonFormatter) with
        | Ok(s) -> s
        | Fail(e) -> 
            Logger.Log
                ("Error parsing 'Config.json' file.", ValidationException(e), MessageKeyword.Node, MessageLevel.Critical)
            failwithf "%s" (e.ToString())
    // Load all plug-in DLLs
    for file in Directory.EnumerateFiles(Constants.PluginFolder, "*.dll", SearchOption.TopDirectoryOnly) do
        try 
            System.Reflection.Assembly.LoadFile(file) |> ignore
        with e -> Logger.Log("Error loading plug-in library.", e, MessageKeyword.Node, MessageLevel.Warning)
    let TopShelfConfiguration() = 
        HostFactory.Run(fun conf -> 
            conf.RunAsLocalSystem() |> ignore
            conf.SetDescription("FlexSearch Server")
            conf.SetDisplayName("FlexSearch Server")
            conf.SetServiceName("FlexSearch-Server")
            conf.StartAutomatically() |> ignore
            conf.EnableServiceRecovery(fun rc -> rc.RestartService(1) |> ignore) |> ignore
            conf.Service<NodeService>(fun factory -> 
                ServiceConfiguratorExtensions.ConstructUsing(factory, fun () -> new NodeService(settings, false)) 
                |> ignore
                ServiceConfiguratorExtensions.WhenStarted(factory, fun tc -> tc.Start()) |> ignore
                ServiceConfiguratorExtensions.WhenStopped(factory, fun tc -> tc.Stop()) |> ignore)
            |> ignore)
        |> int
    TopShelfConfiguration()
