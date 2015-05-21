#r "UnionArgParser.dll"

open Nessos.UnionArgParser
open System.IO

let basePath = @"C:\git\FlexSearch\build-debug"//"..\"
let (+/) (path1 : string) (path2 : string) = Path.Combine([| path1; path2 |])

[<AutoOpen>]
module Helpers =
    open System.Diagnostics

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


/// --------------------------------------
/// Command logic
/// --------------------------------------

let install() =
    // Register the FlexSearch service
    exec (basePath +/ "FlexSearch Server.exe") "install"

    // Install ETW manifest
    // TODO

let uninstall() =
    // Unregister the FlexSearch service
    exec (basePath +/ "FlexSearch Server.exe") "uninstall"
    
    // Uninstall ETW manifest
    // TODO


/// --------------------------------------
/// Command parsing and execution
/// --------------------------------------

type Arguments =
    | Install
    | Upgrade
    | Uninstall
with
    interface IArgParserTemplate with
        member s.Usage = match s with
                         | Install -> "Installs the FlexSearch Service and ETW logger"
                         | Upgrade -> "Upgrades the FlexSearch Service"
                         | Uninstall -> "Uninstalls the FlexSearch Service and ETW logger"

let parser = UnionArgParser.Create<Arguments>()

// Display the usage text
if fsi.CommandLineArgs.Length <= 1 then 
    parser.Usage() |> printfn "%s" 
else 
    try 
        // Parse the command
        let command = parser.Parse(fsi.CommandLineArgs |> Array.tail)
        

        // Execute the appropriate command
        match command.GetAllResults().Head with
        | Install -> install()
        | Upgrade -> printfn "Upgrade not supported at the moment."
        | Uninstall -> uninstall()
    with e -> printfn "%s" e.Message

