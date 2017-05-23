open FlexSearch.Core
open FlexSearch.Server
open Nessos.UnionArgParser
open System.Text
open System.IO
open Topshelf
open System.Collections.Generic
open System

let mutable topShelfCommand = Unchecked.defaultof<string>
let mutable loadTopShelf = true

let topShelfConfiguration (settings : Settings.T, conf : HostConfigurators.HostConfigurator) = 
    if isNotBlank topShelfCommand then conf.ApplyCommandLine(topShelfCommand)
    conf.RunAsLocalSystem() |> ignore
    conf.SetDescription("FlexSearch Server")
    conf.SetDisplayName("FlexSearch Server")
    conf.SetServiceName("FlexSearch-Server")
    conf.StartAutomatically() |> ignore
    conf.EnableServiceRecovery(fun rc -> rc.RestartService(1) |> ignore) |> ignore
    conf.Service<WebServerBuilder>(fun (factory : ServiceConfigurators.ServiceConfigurator<_>) -> 
        factory.ConstructUsing(new Func<_>(fun _ -> new WebServerBuilder(settings))) |> ignore
        factory.WhenStarted(fun tc -> tc.Start()) |> ignore
        factory.WhenStopped(fun tc -> tc.Stop()) |> ignore)
    |> ignore
    conf.AfterInstall(Installers.afterInstall settings) |> ignore

let runService() = 
    if loadTopShelf then 
        HostFactory.Run(fun configurator -> 
            let settings = Startup.load()
            topShelfConfiguration (settings, configurator))
        |> int
    else 0

type CLIArguments = 
    | [<AltCommandLine("-i")>] Install
    | [<AltCommandLine("-u")>] UnInstall
    | Start
    | Stop
    | SystemInfo
    | [<AltCommandLine("-ic")>] InstallCertificate
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Install -> "Installs the Windows Service"
            | UnInstall -> "Un-install the Windows Service"
            | Start -> "Starts the service if it is not already running"
            | Stop -> "Stops the service if it is running"
            | SystemInfo -> "Print basic information about the running system"
            | InstallCertificate -> "Installs the provided .pfx certificate"

/// Standard text to prefix before the help statement
let prefixUsageText = """
 Usage: FlexSearch-Server.exe [options]
 ------------------------------------------------------------------
 Options:"""

let parser = UnionArgParser.Create<CLIArguments>(usageText = prefixUsageText)

let printUsage() = 
    loadTopShelf <- false
    printfn "%s" (parser.Usage(prefixUsageText))



[<EntryPoint>]
let main argv = 
    printfn "%s" Startup.headerText

    // Only parse arguments in case of interactive mode
    if notNull argv && argv.Length > 0 && Startup.isInteractive then 
        try 
            let results = parser.Parse(argv).GetAllResults()
            // We don't want TopShelf to process the incoming arguments.
            // TopShelf will only be used to process certain arguments
            // supported by it.
            topShelfCommand <- String.Empty
            match results.Head with
            | Install -> topShelfCommand <- "install"
            | UnInstall -> topShelfCommand <- "uninstall"
            | Start -> topShelfCommand <- "start"
            | Stop -> topShelfCommand <- "stop"
            | SystemInfo -> 
                loadTopShelf <- false
                printf "%s" (Management.printSystemInfo())
            | InstallCertificate -> 
                loadTopShelf <- false
                installCertificate()
                Installers.assignKeyContainerToUser()
        with e -> printUsage()

    let result = runService()
    if Startup.isInteractive then 
        printfn "Press any key to continue . . ."
        Console.ReadKey() |> ignore
    result
