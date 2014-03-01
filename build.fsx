// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open System.IO
open System.Linq

RestorePackages()

// Version information
let version = "0.21.1.0"
let fileVersion = "0.21.1.0"
// Properties
let buildDir = @".\build\"
let testDir = @".\build\"
let deployDir = @".\deploy\"

let moveFiles() = 
    let src = __SOURCE_DIRECTORY__ + @"\build"
    let dest = __SOURCE_DIRECTORY__ + @"\build\lib\"
    // Delete all pdb files
    for file in Directory.GetFiles(src, "*.pdb") do
        File.Delete(file)
    //    // Delete unit test files
    File.Delete(src + @"\FlexSearch.Tests.exe")
    File.Delete(src + @"\FlexSearch.Tests.exe.config")
    File.Delete(src + @"\FlexSearch.Tests.XML")
    // Move all non flex related files to lib folder
    for file in Directory.GetFiles(src) do
        let fileName = Path.GetFileName(file)
        if fileName.StartsWith("Flex") = false 
           && fileName.StartsWith("Service") = false 
           && fileName.StartsWith("LoupeViewer") = false 
           && fileName.StartsWith("Install") = false 
           && fileName.StartsWith("Uninstall") = false 
           && fileName.StartsWith("LICENSE") = false then 
            printfn "%s" file
            File.Move(file, dest + fileName)
    
    let filesToDelete = (["FsCheck.dll"; "Fuchu.dll"; "FsUnit.NUnit.dll"; "nunit.framework.dll"; "Fuchu.FsCheck.dll" ]).ToList()
    // Delete unnecessary files from the lib
    for file in Directory.GetFiles(dest) do
        let fileName = Path.GetFileName(file)
        if fileName.EndsWith(".xml") then
            File.Delete(file)
        else if filesToDelete.Contains(fileName) then
            File.Delete(file)

// Targets
Target "Clean" 
    (fun _ -> 
    CleanDirs 
        [ buildDir; testDir; @"build\Conf"; @"build\Data"; 
          @"build\Plugins"; @"build\Lib" ])
Target "BuildApp" 
    (fun _ -> 
    CreateFSharpAssemblyInfo @".\src\FlexSearch.Core\AssemblyInfo.fs" 
        [ Attribute.Title "FlexSearch Core Library"
          Attribute.Description "FlexSearch Core Library"
          Attribute.Product "FlexSearch"
          Attribute.Copyright "(c) Seemant Rajvanshi, 2012 - 2014"
          Attribute.FileVersion fileVersion
          Attribute.Version version ]
    CreateFSharpAssemblyInfo @".\src\FlexSearch.Tests\AssemblyInfo.fs" 
        [ Attribute.Title "FlexSearch Tests Library"
          Attribute.Description "FlexSearch Tests Library"
          Attribute.Product "FlexSearch Tests"
          Attribute.Copyright "(c) Seemant Rajvanshi, 2012 - 2014"
          Attribute.FileVersion fileVersion
          Attribute.Version version ]
    CreateCSharpAssemblyInfo 
        @".\src\FlexSearch.Server\Properties\AssemblyInfo.cs" 
        [ Attribute.Title "FlexSearch Server"
          Attribute.Description "FlexSearch Server"
          Attribute.Product "FlexSearch"
          Attribute.Copyright "(c) Seemant Rajvanshi, 2012 - 2014"
          Attribute.FileVersion fileVersion
          Attribute.Version version ]
    CreateCSharpAssemblyInfo @".\src\FlexSearch.Api\Properties\AssemblyInfo.cs" 
        [ Attribute.Title "FlexSearch Api"
          Attribute.Description "FlexSearch Api"
          Attribute.Product "FlexSearch"
          Attribute.Copyright "(c) Seemant Rajvanshi, 2012 - 2014"
          Attribute.FileVersion fileVersion
          Attribute.Version version ]
    MSBuildRelease buildDir "Build" [ @"src\FlexSearch.sln" ] 
    |> Log "AppBuild-Output: ")
Target "Test" (fun _ -> 
    let errorCode = 
        [ Path.Combine(testDir, "FlexSearch.Tests.exe") ]
        |> Seq.map (fun p -> asyncShellExec { defaultParams with Program = p })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.sum
    if errorCode <> 0 then failwith "Error in tests")
Target "Default" (fun _ -> trace "FlexSearch Compilation")
Target "MoveFiles" (fun _ -> moveFiles())
Target "Zip" 
    (fun _ -> !!(buildDir + "/**/*.*") -- "*.zip"
              |> Zip buildDir (deployDir + "FlexSearch." + version + ".zip"))
// Dependencies
"Clean" ==> "BuildApp" // ==> "Test"
                       ==> "Default" ==> "MoveFiles" ==> "Zip"
// start build
Run "Zip"
