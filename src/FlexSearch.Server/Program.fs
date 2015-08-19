open FlexSearch.Core
open Nessos.UnionArgParser
open System
open System.IO
open Topshelf

module Installers =
    open System.Diagnostics

    let out s = printfn "[FlexSearch.Install] %s" s

    let toQuotedString (s : string) = if System.String.IsNullOrEmpty s then s
                                      elif s.Chars 0 = '"' then s
                                      else "\"" + s + "\""
    
    // Executes a given exe along with the passed argument 
    let exec path argument = 
        let psi = new ProcessStartInfo()
        psi.FileName <- path
        psi.Arguments <- argument
        psi.WorkingDirectory <- __SOURCE_DIRECTORY__
        psi.RedirectStandardOutput <- false
        psi.UseShellExecute <- false
        use p = Process.Start(psi)
        p.WaitForExit()
    
    let private manifestManFileName = "FlexSearch.Logging.FlexSearch.etwManifest.man"
    let private manifestFileName = "FlexSearch.Logging.FlexSearch.etwManifest.dll"
     
    let installManifest() =
        printfn "Installing the ETW manifest..."
        exec "wevtutil.exe" <| "im " + (Constants.rootFolder +/ manifestManFileName |> toQuotedString) 
                                + " /rf:" + (Constants.rootFolder +/ manifestFileName |> toQuotedString)
                                + " /mf:" + (Constants.rootFolder +/ manifestFileName |> toQuotedString)
    
    let uninstallManifest() =
        printfn "Un-installing the ETW manifest..."
        exec "wevtutil.exe" <| "um " + (Constants.rootFolder +/ manifestManFileName |> toQuotedString)

let mutable topShelfCommand = Unchecked.defaultof<string>
let mutable loadTopShelf = true

let topShelfConfiguration(conf : HostConfigurators.HostConfigurator) = 
    if notNull topShelfCommand then
        conf.ApplyCommandLine(topShelfCommand)
    conf.RunAsLocalSystem() |> ignore
    conf.SetDescription("FlexSearch Server")
    conf.SetDisplayName("FlexSearch Server")
    conf.SetServiceName("FlexSearch-Server")
    conf.StartAutomatically() |> ignore
    conf.EnableServiceRecovery(fun rc -> rc.RestartService(1) |> ignore) |> ignore
    conf.Service<NodeService>(fun factory -> 
        ServiceConfiguratorExtensions.ConstructUsing(factory, fun () -> StartUp.start()) 
        |> ignore
        ServiceConfiguratorExtensions.WhenStarted(factory, fun tc -> tc.Start()) |> ignore
        ServiceConfiguratorExtensions.WhenStopped(factory, fun tc -> tc.Stop()) |> ignore)
    |> ignore

let runTopShelf() =
    if loadTopShelf then
        HostFactory.Run(fun x -> topShelfConfiguration(x)) |> int
    else 0

type CLIArguments =
    | [<AltCommandLine("-i")>] Install
    | [<AltCommandLine("-u")>] UnInstall
    | Start
    | Stop
    | [<AltCommandLine("-im")>] InstallManifest
    | [<AltCommandLine("-um")>] UnInstallManifest
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Install -> "Installs the Windows Service"
            | UnInstall -> "Un-install the Windows Service"
            | Start -> "Starts the service if it is not already running"
            | Stop -> "Stops the service if it is running"
            | InstallManifest -> "Install the ETW manifest"
            | UnInstallManifest -> "Un-install the ETW manifest"

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
             
    // Only parse arguments in case of interactive mode
    if notNull argv && argv.Length > 0 && StartUp.isInteractive then
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
            | InstallManifest -> ()
            | UnInstallManifest -> ()
        with e ->
            printUsage()
    
    let result = runTopShelf()
    if StartUp.isInteractive then
        printfn "Press any key to continue . . ."
        Console.ReadKey() |> ignore
    result
    