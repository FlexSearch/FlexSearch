open FlexSearch.Core
open FlexSearch.Server
open Nessos.UnionArgParser
open System.Text
open System.IO
open Topshelf
open System.Collections.Generic
open System

module Management = 
    open System.Management
    
    let getObj (className : string) = 
        let query = "SELECT * FROM Win32_" + className
        use mgmtObj = new ManagementObjectSearcher(query)
        mgmtObj.Get()
    
    let private infoObjects = 
        [| ("Processor", [| "Name"; "Description"; "NumberOfCores"; "NumberOfLogicalProcessors"; "MaxClockSpeed" |])
           ("OperatingSystem", [| "Caption"; "TotalVisibleMemorySize" |])
           ("ComputerSystem", [| "TotalPhysicalMemory" |])
           ("PhysicalMemory", [| "ConfiguredClockSpeed" |])
           ("DiskDrive", [| "Manufacturer"; "Model"; "Size" |]) |]
    
    /// Generates basic system info like CPU etc. It is useful to record this information
    /// along with the performance test results    
    let generateSystemInfo() = 
        let info = new Dictionary<string, Dictionary<string, string>>()
        for (className, props) in infoObjects do
            let res = new Dictionary<string, string>()
            let stmt = getObj className
            let mutable i = 0
            for s in stmt do
                for p in props do
                    let v = s.GetPropertyValue(p)
                    
                    let propName = 
                        if i = 0 then p
                        else p + i.ToString()
                    if notNull v then res.[propName] <- v.ToString()
                i <- i + 1
            info.Add(className, res)
        info
    
    /// Generate printable report of system information
    let printSystemInfo() = 
        let sb = new StringBuilder()
        for className in generateSystemInfo() do
            for prop in className.Value do
                sb.AppendLine(sprintf "%s-%s : %s" className.Key prop.Key prop.Value) |> ignore
        sb.ToString()

module Installers = 
    open System.Diagnostics
    
    let out s = printfn "[FlexSearch.Install] %s" s
    
    let toQuotedString (s : string) = 
        if System.String.IsNullOrEmpty s then s
        elif s.Chars 0 = '"' then s
        else "\"" + s + "\""
    
    // Executes a given exe along with the passed argument 
    let exec path argument showOutput = 
        let psi = new ProcessStartInfo()
        psi.FileName <- path
        psi.Arguments <- argument
        psi.WorkingDirectory <- Constants.rootFolder
        psi.RedirectStandardOutput <- not showOutput
        psi.UseShellExecute <- false
        use p = Process.Start(psi)
        p.WaitForExit()
    
    let reservePort (port : int) = 
        printfn "Reserving the port %i" port
        exec "netsh.exe" <| sprintf "http add urlacl url=http://+:%i/ user=everyone listen=yes" port
                         <| true
    
    let resetPerformanceCounters () =
        try exec "lodctr" "/r" false
        with e -> printfn "Failed to reset performance counters: %s" e.Message

    let assignKeyContainerToUser() =
        let user = sprintf "%s\\%s" Environment.UserDomainName Environment.UserName
        printfn "Assigning the '%s' RSA Key container to the user %s" Constants.RsaKeyContainerName user

        try
            let frameworkV4 = loopDir "C:\\Windows\\Microsoft.NET\\Framework"
                              |> Seq.find(fun d -> d.Contains("v4.0"))

            let aspnet_regiis = (frameworkV4 +/ "aspnet_regiis") 

            // Assign the key to the user
            exec aspnet_regiis
            <| sprintf "-pa \"%s\" \"%s\"" Constants.RsaKeyContainerName user
            <| true
        with e -> printfn "Failed to assign the key: %s" <| exceptionPrinter e

    /// Gets executed after the service is installed by TopShelf
    let afterInstall (settings : Settings.T) = 
        new Action(fun _ -> 
        reservePort (settings.GetInt(Settings.ServerKey, Settings.HttpPort, 9800)))

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
    conf.AfterInstall(Installers.afterInstall (settings)) |> ignore

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

    // The reason we need to reset the performance counters is that sometimes 
    // the counters cache in the registry becomes corrupted.
    // http://stackoverflow.com/questions/17980178/cannot-load-counter-name-data-because-an-invalid-index-exception
    // The performance counters are used for the /memory endpoint.
    Installers.resetPerformanceCounters()

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
