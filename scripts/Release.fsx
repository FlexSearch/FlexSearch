// include Fake lib
#r @"..\src\packages\FAKE\tools\FakeLib.dll"
#r "System.Management.Automation"
#load "Helpers.fsx"

open Fake
open Fake.AssemblyInfoFile
open System.IO
open System.Linq
open System
open System.Diagnostics
open System.Management.Automation
open Helpers

//TraceEnvironmentVariables()

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
        |> Seq.iter (RestorePackage (fun p ->
            { p with
                OutputPath = "./src/packages"
                Sources = [@"https://nuget.org/api/v2/"; @"https://www.myget.org/F/aspnetvnext/api/v2/"]}))
)

if buildServer = BuildServer.AppVeyor then 
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers

// Version information
let majorVersion = 0
let minorVersion = 23
let patchLevel = 2
let buildVersion = System.DateTime.UtcNow.ToString("yyyyMMddhhmm")
let version = sprintf "%i.%i.%i-alpha+%s" majorVersion minorVersion patchLevel buildVersion
let productName = "FlexSearch"
let copyright = sprintf "Copyright (C) 2010 - %i - FlexSearch" DateTime.Now.Year


// Create necessary directories if they don't exist
Directory.CreateDirectory(buildDir)
Directory.CreateDirectory(testDir)
Directory.CreateDirectory(deployDir)

/// <summary>
/// Delete and move files to correct folders
/// </summary>
let packageFiles() = 
    let src = buildDir
    let dest = buildDir <!!> "lib"
    // Delete all pdb files
    for file in Directory.GetFiles(src, "*.pdb") do
        File.Delete(file)
    // Delete all xml files
    for file in Directory.GetFiles(src, "*.xml") do
        File.Delete(file)
    // Move all non flex related files to lib folder
    for file in Directory.GetFiles(src) do
        let fileName = Path.GetFileName(file).ToLowerInvariant()
        if fileName.Contains("test") then File.Delete(file)
        else 
            if fileName.StartsWith("flex") = false && fileName.StartsWith("install") = false
               && fileName.StartsWith("setup") = false
               && fileName.StartsWith("uninstall") = false && fileName.StartsWith("license") = false 
               && fileName.StartsWith("benchmark") = false then File.Move(file, Path.Combine(dest, fileName))
    let filesToDelete = 
        ([ "xunit.dll"; "xunit.extensions.dll"; "visualize.dll"; "ploeh.autofixture.dll"; "ploeh.autofixture.xunit.dll"; "mono.cecil.dll"; "mono.cecil.mdb.dll"; "mono.cecil.pdb.dll"; "mono.cecil.rocks.dll" ])
            .ToList()
    // Delete unnecessary files from the lib
    for file in Directory.GetFiles(dest) do
        let fileName = Path.GetFileName(file)
        if fileName.EndsWith(".xml") then File.Delete(file)
        else 
            if filesToDelete.Contains(fileName) then File.Delete(file)
    let filesToMove = 
        ([ "FlexSearch.Connectors.dll"; "FlexSearch.Connectors.dll.config"; "FlexSearch.DuplicateDetection.dll"; 
           "FlexSearch.DuplicateDetection.dll.config" ])
    // Move plug-in to plug-in folder
    for file in Directory.GetFiles(src) do
        let fileName = Path.GetFileName(file)
        if filesToMove.Contains(fileName) then File.Move(file, Path.Combine(src, "Plugins", fileName))

let AssemblyInfo path title = 
    CreateFSharpAssemblyInfo (sprintf @".\src\%s\AssemblyInfo.fs" path) [ Attribute.Title title
                                                                          Attribute.Description title
                                                                          Attribute.Product productName
                                                                          Attribute.Copyright copyright
                                                                          Attribute.FileVersion version
                                                                          Attribute.Version version ]

let AssemblyInfoCSharp path title = 
    CreateCSharpAssemblyInfo (sprintf @".\src\%s\Properties\AssemblyInfo.cs" path) [ Attribute.Title title
                                                                                     Attribute.Description title
                                                                                     Attribute.Product productName
                                                                                     Attribute.Copyright copyright
                                                                                     Attribute.FileVersion version
                                                                                     Attribute.Version version ]

let runPsScript scriptText =
    let ps = PowerShell.Create()
    let result = ps.AddScript(scriptText).Invoke()
        
    trace "PS Script Output:\n"
    result |> Seq.iter (sprintf "%A" >> trace)
    if result.Count > 0 then
        // Last exit code
        if (result |> Seq.last).ToString() <> "0" then 
            failwith "The powershell script exited with a non-success code. Please check previous error messages for details."

    if (ps.Streams.Error.Count > 0) then
        trace "PS Script non-fatal errors:\n"
        ps.Streams.Error |> Seq.iter (sprintf "%A" >> trace)

// Targets
Target "Clean" <| fun _ -> 
    [ buildDir; testDir; @"build\Conf"; @"build\Data"; @"build\Plugins"; @"build\Lib"; @"build\Web"; ]
    |> CleanDirs
    !! (deployDir @@ "*.zip")
    |> DeleteFiles
Target "BuildApp" <| fun _ -> 
    AssemblyInfo "FlexSearch.Server" "FlexSearch Server"
    AssemblyInfo "FlexSearch.Core" "FlexSearch Core Library"
    AssemblyInfoCSharp "FlexSearch.API" "FlexSearch API Library"
    MSBuildRelease buildDir "Build" [ @"src\FlexSearch.sln" ] |> Log "BuildApp-Output: "
    // Copy the files from build to build-test necessary for Testing
    FileHelper.CopyRecursive buildDir testDir true |> ignore
Target "Test" <| fun _ -> 
    !! (testDir @@ "FlexSearch.Tests.dll") 
    |> (fun includes ->
            try FixieHelper.Fixie 
                    (fun p -> { p with CustomOptions = [ "xUnitXml", "TestResult.xml" :> obj 
                                                         "requestlogpath", dataDir <!!> "..\nonHttpTests" :> obj ] })
                    includes
            // Upload test results to Appveyor even if tests failed
            finally AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.Xunit __SOURCE_DIRECTORY__
                    trace "Uploaded to AppVeyor")

Target "HttpTests" <| fun _ ->
    !! (testDir @@ "FlexSearch.HttpTests.dll") 
    |> (fun includes ->
            try FixieHelper.Fixie 
                    (fun p -> { p with CustomOptions = [ "xUnitXml", "TestResult.xml" :> obj 
                                                         "requestlogpath", dataDir :> obj ] })
                    includes
            // Upload test results to Appveyor even if tests failed
            finally AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.Xunit __SOURCE_DIRECTORY__
                    trace "Uploaded HttpTests to AppVeyor")
            
Target "Default" (fun _ -> trace "FlexSearch Compilation")
Target "MoveFiles" (fun _ -> packageFiles())
Target "Zip" 
    (fun _ -> 
        // Zip FlexSearch.Core
        !!(buildDir + "/**/*.*") -- "*.zip" |> Zip buildDir (deployDir <!!> "FlexSearch." + version + ".zip")
        // Zip clients
        !!(deployDir + "/clients/**/*.*") -- "*.zip" |> Zip deployDir (deployDir <!!> "FlexSearch.Clients." + version + ".zip"))

// Portal related
Target "BuildPortal" <| fun _ ->
    FileUtils.cd portalDir

    runPsScript <| File.ReadAllText "build.ps1"

    FileUtils.cd @"..\"

Target "MovePortal" <| fun _ ->
    trace "Moving Portal"
    let source = portalDir + @"\src\apps"
    let target = webDir <!!> "apps"
    ensureDirectory target
    
    // Copy the apps
    loopDir source
    |> Seq.iter (fun dir -> 
        let appName = (directoryInfo dir).Name
        trace ("Moving files for app " + appName)
        let targetAppPath = target <!!> appName
        ensureDirectory targetAppPath
        FileHelper.CopyRecursive (dir <!!> "dist\\") targetAppPath true |> ignore
        ensureDirectory(targetAppPath <!!> @"\styles")
        File.Copy(dir <!!> @"dist\fonts\ui-grid.ttf", targetAppPath <!!> @"styles\ui-grid.ttf", true)
        File.Copy(dir <!!> @"dist\fonts\ui-grid.woff", targetAppPath <!!> @"styles\ui-grid.woff", true))

    // Copy the homepage templates
    File.Copy(portalDir <!!> @"src\homeTemplate.html", webDir <!!> "homeTemplate.html")
    File.Copy(portalDir <!!> @"src\cardTemplate.html", webDir <!!> "cardTemplate.html")
    // Copy the assets, fonts and styles
    ["assets"; "fonts"; "styles"]
    |> Seq.iter (fun folder -> 
            ensureDirectory (webDir <!!> folder)
            FileHelper.CopyRecursive (loopDir source |> Seq.head <!!> "dist\\" + folder) (webDir <!!> folder) true |> ignore)

    // Copy the Web folder from build to build-debug
    ensureDirectory (debugDir <!!> "Web")
    FileHelper.CopyRecursive (buildDir <!!> "Web") (debugDir <!!> "Web") true |> ignore

Target "DeployTypeScriptClient" <| fun _ ->
    let target = deployDir <!!> "clients\\ts"
    ensureDirectory target
    emptyDir target
    FileHelper.CopyRecursive (portalDir <!!> "src\\common\\client\\") target true |> ignore

Target "DeployJavaScriptClient" <| fun _ ->
    let target = deployDir <!!> "clients\\js"
    ensureDirectory target
    emptyDir target
    FileHelper.CopyRecursive (scriptDir <!!> @"obj\src") target true |> ignore

Target "DeployCSharpClient" <| fun _ ->
    let target = deployDir <!!> "clients\\cs"
    ensureDirectory target
    emptyDir target
    !! (buildDir <!!> "FlexSearch.Api.dll*") |> CopyFiles target

// Documentation related
//Target "GenerateSwagger" <| fun _ ->
//    trace "Generating Swagger"
//    FileUtils.cd documentationDir
//
//    runPsScript <| File.ReadAllText "build.ps1"
//
//    FileUtils.cd @"..\src"

// Dependencies
"Clean" 
==> "RestorePackages" 
==> "BuildApp" 
==> "Default" 
==> "MoveFiles" 
==> "MovePortal"
==> "DeployTypeScriptClient"
==> "DeployJavaScriptClient"
==> "DeployCSharpClient"
==> "Zip"
==> "Test"

"BuildPortal"
==> "MovePortal"

// start building core FlexSearch
RunTargetOrDefault "Zip"