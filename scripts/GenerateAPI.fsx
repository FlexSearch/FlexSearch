#load "Helpers.fsx"

open Helpers
open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Text.RegularExpressions

let private generateSwaggerModel() = 
    !>> "Cleaning Models, Api & Client directories"
    [modelsTempDir; modelsDir; apiDir; clientDir] |> Seq.iter emptyDir
    javaExec <| sprintf """-jar %s generate -i %s -l csharp -t %s -o obj -c %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-partial.json") (specDir <!!> "template") (toolsDir <!!> "codegen-config.json")
    let generatedFiles = Directory.GetFiles(modelsTempDir).Count()
    if generatedFiles > 0 then
        brk()
        !> (sprintf "%i files generated." generatedFiles)
        brk()
    else
        brk()
        !> "Swagger code generation failed." 
        brk()

let private cleanup() = 
    /// Perform specific code cleanups
    let codeCorrection(l : string) =
        // Correct constructors
        let mutable line = l.Replace(", )", ")")
        // Remove all null-able types
        line <- line.Replace("?", "")
        // Correct enums
        if line.Contains(".\"") then 
            let startPos = line.IndexOf(".\"")
            let endPos = line.LastIndexOf("\"")
            line <- line.Remove(startPos + 1, 1)
            line <- line.Remove(endPos - 1, 1)
        // Convert lists to array
        if line.Contains("List<") then
            line <- line.Replace("List<", "")
            line <- line.Replace(">>", "%%")
            line <- line.Replace(">", "[]")
            line <- line.Replace("%%", ">")
        // Covers the corner case when we have arrays of basic data type
        line <- line.Replace("= new string[]();", "= Array.Empty<string>();")
        // Specify the format type for ToString for date time field
        if line.EndsWith("//datetime") then
            line <- line.Replace(".ToString()", """.ToString("yyyyMMddHHmmss")""")
        line

        // Change OperationMessage.Properties to List<KeyValuePair<string, string>>

    let cleanupFile(f : string) =
        let mutable file = File.ReadAllLines(f)
        let lines = 
            file 
            |> Array.map codeCorrection
            |> Array.filter (String.IsNullOrWhiteSpace >> not)

        File.WriteAllLines(f, lines)
        
    modelsTempDir
    |> loopFiles
    |> Seq.iter cleanupFile
    
let private formatCode() =
    exec(codeFormatterExe, sprintf "%s/FlexSearch.Api/FlexSearch.Api.csproj /nocopyright" srcDir)
    
let private copy() =
    let work tempDir fsDir = 
        tempDir
        |> loopFiles
        |> Seq.iter (fun f -> File.Copy(f, fsDir <!!> Path.GetFileName(f)))

    work modelsTempDir modelsDir
    work apiTempDir apiDir 
    work clientTempDir clientDir 
    
/// Generate API model from the swagger definition
let generateModel() =
    brk()
    !> "Generating API models"
    generateSwaggerModel()
    !>> "Cleaning up the generated files"
    cleanup()
    !>> "Formatting the code"
    formatCode()
    !>> "Copying the files to the API directory"
    copy()
    brk()
    !> "Model generation finished"

generateModel()