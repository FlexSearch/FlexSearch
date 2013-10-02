// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"
open Fake
open System.IO
 
RestorePackages()

// Version information
let version = "0.2.0.0"
let fileVersion = "0.2.0.0"
 
// Properties
let buildDir = @".\build\"
let testDir  = @".\build-test\"
let deployDir = @".\deploy\"
let packagesDir = @".\src\packages"


// tools
let xunitVersion = GetPackageVersion packagesDir "xunit.runners"
let xunitPath = sprintf @".\src\packages\xunit.runners.%s\tools\" xunitVersion


let moveFiles() =
    let src = __SOURCE_DIRECTORY__ + @"\build"
    let dest = __SOURCE_DIRECTORY__ + @"\build\lib\"

    // Delete all pdb files
    for file in Directory.GetFiles(src, "*.pdb") do
        File.Delete(file)

    // Delete unit test files
    File.Delete(src + @"\FlexTestDataPeople.csv")
    File.Delete(src + @"\FlexSearch.Specs.dll")
    File.Delete(src + @"\FlexSearch.Specs.dll.config")
    File.Delete(src + @"\FlexSearch.Benchmark.exe")
    File.Delete(src + @"\FlexSearch.Benchmark.exe.config")

    // Move all non flex related files to lib folder
    for file in Directory.GetFiles(src) do
        let fileName = Path.GetFileName(file)
        if fileName.StartsWith("Flex") = false 
            && fileName.StartsWith("Service") = false 
            && fileName.StartsWith("LoupeViewer") = false 
            && fileName.StartsWith("NLog.config") = false 
            && fileName.StartsWith("LICENSE") = false then
            printfn "%s" file
            File.Move(file , dest + fileName) 

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; deployDir; @"build\Conf"; @"build\Data"; @"build\Plugins"; @"build\Lib"; @"build-test\Conf"; @"build-test\Data"; @"build-test\Plugins"; @"build-test\Lib"]
)
 
Target "BuildApp" (fun _ ->
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = FSharp;
            AssemblyVersion = version;
            AssemblyTitle = "FlexSearch Core Library";
            AssemblyDescription = "FlexSearch Core Library";
            AssemblyProduct = "FlexSearch"
            AssemblyCopyright = "(c) Seemant Rajvanshi, 2013"
            AssemblyFileVersion = fileVersion
            OutputFileName = @".\src\FlexSearch.Core\AssemblyInfo.fs"})

    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "FlexSearch Server";
            AssemblyDescription = "FlexSearch Server";
            AssemblyProduct = "FlexSearch"
            AssemblyCopyright = "(c) Seemant Rajvanshi, 2013"
            AssemblyFileVersion = fileVersion
            OutputFileName = @".\src\FlexSearch.Server\Properties\AssemblyInfo.cs"})          

    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "FlexSearch Api";
            AssemblyDescription = "FlexSearch Api";
            AssemblyProduct = "FlexSearch"
            AssemblyCopyright = "(c) Seemant Rajvanshi, 2013"
            AssemblyFileVersion = fileVersion
            OutputFileName = @".\src\FlexSearch.Api\Properties\AssemblyInfo.cs"}) 

    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "FlexSearch Validators";
            AssemblyDescription = "FlexSearch Validators";
            AssemblyProduct = "FlexSearch"
            AssemblyCopyright = "(c) Seemant Rajvanshi, 2013"
            AssemblyFileVersion = fileVersion
            OutputFileName = @".\src\FlexSearch.Validators\Properties\AssemblyInfo.cs"}) 

//   @"src\FlexSearch.sln"
    let src = Seq.init 1 (fun x -> @"src\FlexSearch.sln")
    MSBuildRelease buildDir "Build" src 
    |> Log "AppBuild-Output: "
)
 
Target "BuildTest" (fun _ ->
    !! @"src\FlexSearch.Specs\*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "TestBuild-Output: "
)
 
Target "Test" (fun _ ->
    !! (testDir + @"\FlexSearch.Specs.dll") 
      |> xUnit (fun p ->
          {p with             
            ShadowCopy = false;
            //HtmlOutput = true;
            XmlOutput = true;
            OutputDir = testDir })
)
 
Target "Default" (fun _ ->
    trace "FlexSearch Compilation"
)

Target "MoveFiles" (fun _ -> moveFiles()) 


Target "Zip" (fun _ ->
    !+ (buildDir + "\**\*.*") 
        -- "*.zip" 
        |> Scan
        |> Zip buildDir (deployDir + "FlexSearch." + version + ".zip")
)
// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "Default"
  ==> "MoveFiles"
  ==> "Zip"
     
// start build
Run "Zip"
