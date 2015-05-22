#r "UnionArgParser.dll"

open Nessos.UnionArgParser
open System.IO

[<AutoOpen>]
module Helpers =
    open System.Diagnostics

    let basePath = __SOURCE_DIRECTORY__ + "\\..\\"
    let (+/) (path1 : string) (path2 : string) = Path.Combine([| path1; path2 |])
    let toQuotedString (s : string) = if System.String.IsNullOrEmpty s then s
                                      elif s.Chars 0 = '"' then s
                                      else "\"" + s + "\""
    let out s = printfn "[FlexSearch.Install] %s" s

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
    "Registering the FlexSearch service..." |> out
    exec (basePath +/ "FlexSearch Server.exe") "install"

    "Installing the ETW manifest..." |> out
    exec "wevtutil.exe" <| "im " + (basePath +/ "FlexSearch.Logging.FlexSearch.etwManifest.man" |> toQuotedString) 
                            + " /rf:" + (basePath +/ "FlexSearch.Logging.FlexSearch.etwManifest.dll" |> toQuotedString)
                            + " /mf:" + (basePath +/ "FlexSearch.Logging.FlexSearch.etwManifest.dll" |> toQuotedString)

let upgrade() =
    "Upgrade not supported at the moment" |> out

let uninstall() =
    "Unregistering the FlexSearch service..." |> out
    exec (basePath +/ "FlexSearch Server.exe") "uninstall"
    
    "Uninstalling the ETW manifest..." |> out
    exec "wevtutil.exe" <| "um " + (basePath +/ "FlexSearch.Logging.FlexSearch.etwManifest.man" |> toQuotedString) 


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
        | Upgrade -> upgrade()
        | Uninstall -> uninstall()
    with e -> printfn "%s" e.Message
