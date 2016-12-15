// include Fake lib
#r @"..\src\packages\FAKE\tools\FakeLib.dll"
#r "System.Management.Automation"
#load "Helpers.fsx"
#load "SwaggerInjector.fsx"
#load "GenerateAPI.fsx"

open Fake
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open GenerateAPI
open Helpers
open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Management.Automation

(*

Version information

*)
//let release = LoadReleaseNotes "RELEASE_NOTES.md"
let majorVersion = 0
let minorVersion = 8
let patchLevel = 0
let beta = ""
let nugetVersion = sprintf "%i.%i.%i" majorVersion minorVersion patchLevel
//let buildVersion = System.DateTime.UtcNow.ToString("yyyyMMddhhmm")
//let version = sprintf "%i.%i.%i+%s" majorVersion minorVersion patchLevel buildVersion
let productName = "FlexSearch"
let copyright = sprintf "Copyright (C) 2010 - %i - FlexSearch" DateTime.Now.Year
(*

API Generation Section

*)
let injectSwaggerTarget = "InjectSwagger"

Target injectSwaggerTarget <| fun _ -> SwaggerInjector.injectSwagger()
Target "CleanClients" <| fun _ -> emptyDir deployDir
Target "CsClient" <| fun _ -> CSharp.generateCSharp()
Target "Api" <| fun _ -> CSharp.generateCSharp()
Target "TsClient" <| fun _ -> TypeScript.generateTypeScript()
Target "JsClient" <| fun _ -> JavaScript.generateJavaScript()
Target "HtmlClient" <| fun _ -> Html.generateHtml()
Target "AllClients" TargetHelper.DoNothing
"CleanClients" ==> injectSwaggerTarget
injectSwaggerTarget ==> "CsClient"
injectSwaggerTarget ==> "Api"
injectSwaggerTarget ==> "TsClient"
injectSwaggerTarget ==> "JsClient"
injectSwaggerTarget ==> "HtmlClient"
"CsClient" ==> "TsClient" ==> "JsClient" ==> "HtmlClient" ==> "AllClients"

(*

FlexSearch core build Section

*)
let restore() =
    /// Restore nuget packages
    !!"./**/packages.config" |> Seq.iter (RestorePackage(fun p -> { p with OutputPath = "./src/packages" }))
    /// Restore Paket packages
    trace "Restoring packages using Packet"
    Paket.PaketRestoreDefaults()
    |> sprintf "%A"
    |> trace

Target "RestorePackages" restore
if buildServer = BuildServer.AppVeyor then
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers
/// Create necessary directories if they don't exist
ensureDir buildDir
ensureDir testDir
ensureDir deployDir

let assemblyInfoAttributes title =
    [ Attribute.Title title
      Attribute.Description title
      Attribute.Product productName
      Attribute.Copyright copyright
      Attribute.FileVersion nugetVersion
      Attribute.Version nugetVersion ]

let assemblyInfo path title =
    CreateFSharpAssemblyInfo (sprintf @".\src\%s\AssemblyInfo.fs" path) (assemblyInfoAttributes title)
let assemblyInfoCSharp path title =
    CreateCSharpAssemblyInfo (sprintf @".\src\%s\Properties\AssemblyInfo.cs" path) (assemblyInfoAttributes title)

// Targets
Target "Clean" <| fun _ ->
    [ buildDir; testDir; @"build\Conf"; @"build\Data"; @"build\Plugins"; @"build\Lib"; @"build\Web"; "documentation" ]
    |> CleanDirs
    !!(deployDir @@ "*.zip") |> DeleteFiles

let buildApp() =
    assemblyInfo "FlexSearch.Server" "FlexSearch Server"
    assemblyInfo "FlexSearch.Core" "FlexSearch Core Library"
    assemblyInfoCSharp "FlexSearch.API" "FlexSearch API Library"
    MSBuildRelease buildDir "Build" [ @"src\FlexSearch.sln" ] |> Log "BuildApp-Output: "
    [ // Copy over dlls that are not included in project references
      "libuv.dll"; "System.Numerics.Vectors.dll"; "System.Reflection.dll"; "System.Runtime.InteropServices.RuntimeInformation.dll" ]
    |> Seq.iter (fun name -> CopyFile (buildDir @@ name) (debugDir @@ name))
    // Copy the files from build to build-test necessary for Testing
    FileHelper.CopyRecursive buildDir testDir true |> ignore

Target "BuildApp" buildApp
"CsClient" ==> "Clean" ==> "RestorePackages" ==> "BuildApp"

(*

FlexSearch core tests

*)
let fixie (fileName : string) (includes : FileIncludes) =
    try
        FixieHelper.Fixie (fun p ->
            { p with CustomOptions =
                         [ "xUnitXml", "TestResult.xml" :> obj
                           "requestlogpath", fileName :> obj ] }) includes
    finally
        // Upload test results to Appveyor even if tests failed
        if buildServer = BuildServer.AppVeyor then
            AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.Xunit __SOURCE_DIRECTORY__
            trace "Uploaded to AppVeyor"

let test() = !!(testDir <!!> "FlexSearch.Tests.dll") |> fixie (dataDir <!!> "nonHttpTests")

Target "Test" test
"BuildApp" ==> "Test"

Target "UnitTest" test
"BuildApp" ==> "UnitTest"

let httpTests() = !!(testDir <!!> "FlexSearch.HttpTests.dll") |> fixie dataDir

Target "HttpTests" httpTests
"BuildApp" ==> "HttpTests"
//Target "AllTests" TargetHelper.DoNothing
//"Test" ==> "HttpTests" ==> "AllTests"

(*

FlexSearch website related target

*)
Target "Website" TargetHelper.DoNothing
"AllClients" ==> "HttpTests" ==> "Website"

(*

FlexSearch packaging related tasks

*)
/// Delete and move files to correct folders
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
               && fileName.StartsWith("setup") = false && fileName.StartsWith("uninstall") = false
               && fileName.StartsWith("license") = false && fileName.StartsWith("benchmark") = false then
                File.Move(file, Path.Combine(dest, fileName))
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

Target "MoveFiles" (fun _ -> packageFiles())
Target "DeployCsClient" <| fun _ ->
    let target = deployDir <!!> "clients\\cs"
    ensureDirectory target
    emptyDir target
    !!(buildDir <!!> "FlexSearch.Api.dll*") |> CopyFiles target
Target "Zip"
    (fun _ ->
    // Zip FlexSearch.Core
    !!(buildDir + "/**/*.*") -- "*.zip" |> Zip buildDir (deployDir <!!> "FlexSearch." + nugetVersion + ".zip")
    // Zip clients
    !!(deployDir + "/clients/**/*.*") -- "*.zip"
    |> Zip deployDir (deployDir <!!> "FlexSearch.Clients." + nugetVersion + ".zip"))
(*

FlexSearch Portal build Section

*)
Target "BuildPortal" <| fun _ ->
    FileUtils.cd portalDir
    Shell.Exec "build.bat" |> ignore
    FileUtils.cd @"..\"

/// Moves all portal related artifacts to the release package
let movePortal() =
    trace "Moving Portal"
    let source = portalDir + @"\src\apps"
    let target = webDir <!!> "apps"
    ensureDirectory target
    let copy (dir) =
        let appName = (directoryInfo dir).Name
        trace ("Moving files for app " + appName)
        let targetAppPath = target <!!> appName
        ensureDirectory targetAppPath
        FileHelper.CopyRecursive (dir <!!> "dist\\") targetAppPath true |> ignore
        ensureDirectory (targetAppPath <!!> @"\styles")
        File.Copy(dir <!!> @"dist\fonts\ui-grid.ttf", targetAppPath <!!> @"styles\ui-grid.ttf", true)
        File.Copy(dir <!!> @"dist\fonts\ui-grid.woff", targetAppPath <!!> @"styles\ui-grid.woff", true)
    // Copy the applications
    loopDir source |> Seq.iter copy
    // Copy the homepage templates
    File.Copy(portalDir <!!> @"src\homeTemplate.html", webDir <!!> "homeTemplate.html")
    File.Copy(portalDir <!!> @"src\cardTemplate.html", webDir <!!> "cardTemplate.html")
    [ // Copy the assets, fonts and styles
      "assets"; "fonts"; "styles" ]
    |> Seq.iter (fun folder ->
           ensureDirectory (webDir <!!> folder)
           FileHelper.CopyRecursive (loopDir source
                                     |> Seq.head
                                     <!!> "dist\\" + folder) (webDir <!!> folder) true |> ignore)
    // Copy the Web folder from build to build-debug
    ensureDirectory (debugDir <!!> "Web")
    FileHelper.CopyRecursive (buildDir <!!> "Web") (debugDir <!!> "Web") true |> ignore

Target "MovePortal" movePortal
"TsClient" ==> "BuildPortal" ==> "MovePortal"

(*

FlexSearch GitHub release Section

*)
#load "Octokit.fsx"

open Fake.Git
open Octokit
open System.Text.RegularExpressions

type ChangeLogEntry =
    { Sha : string
      ShortSha : string
      MessageType : string
      Area : string
      Message : string }

/// Parser for parsing FlexSearch commit messages
(*
Group 1: Long SHA
Group 2: Small SHA
Group 3: Message type
Group 5: Area
Group 6: Message
*)
let commitParser =
    new Regex("([a-z0-9]+)\|([a-z0-9]+)\|([a-z ]+)(\(([ a-z ]+)\))?[\s]*:([a-zA-Z 0-9]+)",
              RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

let toTitleCase (str : string) = sprintf "%c%s" (Char.ToUpper(str.[0])) (str.Substring(1))
let mutable releaseNotes = [||]

let releaseNotesGenerator (entries : ChangeLogEntry []) =
    let notes = new ResizeArray<string>()
    notes.Add
        (sprintf "### Release - %s (%s)" nugetVersion (DateTime.Now.ToString("dd-MM-yyyy")))
    entries
    |> Array.groupBy (fun k -> k.MessageType)
    |> Array.filter
           (fun (groupName, _) ->
           String.Equals(groupName, "build", StringComparison.OrdinalIgnoreCase)
           || String.Equals(groupName, "chore", StringComparison.OrdinalIgnoreCase) |> not)
    |> Array.iter
           (fun (groupName, groupEntries) ->
           notes.Add("")
           notes.Add(sprintf "#### %s" <| toTitleCase groupName)
           groupEntries
           |> Array.iter
                  (fun e ->
                  notes.Add
                      (sprintf "* [[%s]](https://github.com/flexsearch/flexsearch/commit/%s) %s" e.ShortSha e.Sha
                           (toTitleCase e.Message))))
    notes.Add("")
    releaseNotes <- notes.ToArray()
    trace (String.Join(Environment.NewLine, notes))
    // Append notes to RELEASE_NOTES.md
    let lines = File.ReadAllLines(rootDir <!!> "RELEASE_NOTES.md")
    notes.AddRange(lines)
    File.WriteAllLines(rootDir <!!> "RELEASE_NOTES.md", notes)

let releaseNotesParser() =
    let lastTagCmd = "describe --tags --abbrev=0"
    let (success, result, err) = Git.CommandHelper.runGitCommand rootDir lastTagCmd
    if success then
        trace <| sprintf "Last tag was: %s" result.[0]
        let tag = result.[0]
        let logCmd = "log " + tag + "..HEAD --oneline --pretty=format:%H|%h|%s"
        let (success, result, err) = Git.CommandHelper.runGitCommand rootDir logCmd
        let entries = new ResizeArray<ChangeLogEntry>()
        if success then
            for c in result do
                let m = commitParser.Match(c)
                if m.Success then
                    entries.Add({ Sha = m.Groups.[1].Value
                                  ShortSha = m.Groups.[2].Value
                                  MessageType = m.Groups.[3].Value.Trim()
                                  Area = m.Groups.[5].Value.Trim()
                                  Message = m.Groups.[6].Value.Trim() })
        releaseNotesGenerator (entries.ToArray())

Target "ReleaseNoteGenerator" releaseNotesParser

let releaseToGithub() =
    let releaseFiles = !!(deployDir @@ "FlexSearch.*.zip")
    // First check if the deployment package has been built
    if releaseFiles
       |> Seq.length
       <> 2 then
        failwithf "Couldn't find the 2 deployment files to be included in the release: %A. Please run ./build"
            releaseFiles
    let gitHome = "https://github.com/FlexSearch/FlexSearch.git"
    let gitOwner = "FlexSearch"
    let gitName = "FlexSearch"
    let user = "flexsearch-bot"

    let pw =
        match getBuildParam "github-pw" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserPassword <| "Please enter the GitHub password for the account " + user

    let remote =
        Git.CommandHelper.getGitResult "" "remote -v"
        |> Seq.filter (fun (s : string) -> s.EndsWith("(push)"))
        |> Seq.tryFind (fun (s : string) -> s.Contains(gitOwner + "/" + gitName))
        |> function
        | None -> gitHome + "/" + gitName
        | Some(s : string) -> s.Split().[0]

    StageAll ""
    Git.Commit.Commit "" (sprintf "chore(release): bump version to %s" nugetVersion)
    Branches.pushBranch "" remote (Information.getBranchName "")
    Branches.tag "" nugetVersion
    Branches.pushTag "" remote nugetVersion
    // release on GitHub
    createClient user pw
    |> createDraft gitOwner gitName nugetVersion (beta = "beta") releaseNotes
    |> fun draft -> releaseFiles |> Seq.fold (fun acc value -> acc |> uploadFile value) draft
    |> releaseDraft
    |> Async.RunSynchronously

Target "Release" releaseToGithub
"ReleaseNoteGenerator" ==> "Release"
/// Dependencies to build the whole ecosystem
"AllClients" ==> "MovePortal" ==> "Test" ==> "MoveFiles" ==> "DeployCsClient" ==> "Zip"
"Zip" ==> "Release"
Target "Help" <| fun _ ->
    trace ""
    trace "---------------------------------------------------------------------"
    trace "The following command line switches can be passed to the build.bat."
    trace ""
    trace "api          - Generates C# api client."
    trace "allclients   - Generates all clients."
    trace "zip          - Generates the zip release package. This is the default option when no switch is passed."
    trace "release      - Release package on Github."
    trace "---------------------------------------------------------------------"
    trace "All available targets:"
    TargetHelper.PrintTargets()
    trace ""
RunTargetOrDefault "Zip"
