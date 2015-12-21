/// Contains various helpers to be used across scripts
[<AutoOpen>]
module Helpers =
    open System
    open System.ComponentModel
    open System.Diagnostics
    open System.IO
    open System.IO.Compression
    open System.Linq
    open System.Text.RegularExpressions
    
    let (<!!>) (path1 : string) (path2 : string) = Path.Combine([| path1; path2 |])
    let loopDir (dir : string) = Directory.EnumerateDirectories(dir)
    let loopFiles (dir : string) = Directory.EnumerateFiles(dir)
    let createDir (dir : string) = Directory.CreateDirectory(dir) |> ignore
    
    let emptyDir (dir : string) = 
        if Directory.Exists(dir) then
            loopDir dir |> Seq.iter (fun x -> Directory.Delete(x, true))
            loopFiles dir |> Seq.iter (fun x -> File.Delete(x))
    
    let (!>) (content : string) = printfn "[INFO] %s" content |> ignore
    let (!>>) (content : string) = printfn "\t%s" content |> ignore
    let brk() = !>"------------------------------------------------------------------------"
    let buildDir = __SOURCE_DIRECTORY__
    let srcDir = Directory.GetParent(buildDir).FullName <!!> "src"
    let specDir = Directory.GetParent(buildDir).FullName <!!> "spec"
    let toolsDir = buildDir <!!> "tools"
    let codeFormatterExe = toolsDir <!!> @"CodeFormatter\CodeFormatter.exe"
    let modelsTempDir = buildDir <!!> @"obj\src\main\csharp\IO\Swagger\Model"
    let modelsDir = srcDir <!!> @"FlexSearch.Api\Models"
    let javaHome = Environment.GetEnvironmentVariable("JAVA_HOME")

    /// Execute a file from the path with the provided reference
    let exec(path, argument) = 
        let psi = new ProcessStartInfo()
        psi.FileName <- path
        psi.Arguments <- argument
        psi.WorkingDirectory <- __SOURCE_DIRECTORY__
        psi.RedirectStandardOutput <- false
        psi.UseShellExecute <- false
        use p = Process.Start(psi)
        p.WaitForExit()
    
    let javaExec (args : string) = exec(javaHome <!!> @"bin\java.exe", args)

    brk()
    !> "FlexSearch Build Variables"
    brk()
    !>> (sprintf "Build Directory: %s" buildDir)
    !>> (sprintf "Source Directory: %s" srcDir)
    !>> (sprintf "Tools Directory: %s" toolsDir)
    !>> (sprintf "Specs Directory: %s" specDir)
    !>> (sprintf "Models Directory: %s" modelsDir)
    !>> (sprintf "Models Temp Directory: %s" modelsTempDir)
    !>> (sprintf "Code formatter Directory: %s" codeFormatterExe)
    !>> (sprintf "Java Home Directory: %s" javaHome)
    brk()