#load "Helpers.fsx"

open Helpers
open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Text.RegularExpressions

let work tempDir fsDir = 
    tempDir
    |> loopFiles
    |> Seq.iter (fun f -> File.Copy(f, fsDir <!!> Path.GetFileName(f)))

module CSharp =
    let private generateSwaggerModel() = 
        !>> "Cleaning Models, Api & Client directories"
        [modelsTempDir; modelsDir; apiDir; clientDir] |> Seq.iter (ensureDir >> ignore)
        [modelsTempDir; modelsDir; apiDir; clientDir] |> Seq.iter emptyDir
        javaExec <| sprintf """-jar %s generate -i %s -l csharp -t %s -o obj -c %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-partial.json") (specDir <!!> "template") (toolsDir <!!> @"..\codegen-config.json")
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
        let codeCorrection (fileName : string) (l : string) =
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
            // Change Dictionary<string,string> to List<KeyValuePair<string, string>>
            if fileName.Contains("OperationMessage.cs") then
                line <- line.Replace("Dictionary<string, string>", "List<KeyValuePair<string, string>>")
        
            line

        let cleanupFile(f : string) =
            let mutable file = File.ReadAllLines(f)
            let lines = 
                file 
                |> Array.map (codeCorrection f)
                |> Array.filter (String.IsNullOrWhiteSpace >> not)

            File.WriteAllLines(f, lines)
        
        modelsTempDir
        |> loopFiles
        |> Seq.iter cleanupFile
    
    let private formatCode() =
        exec(codeFormatterExe, sprintf "%s/FlexSearch.Api/FlexSearch.Api.csproj /nocopyright" srcDir)
    
    let private copy() =
        work modelsTempDir modelsDir
        work apiTempDir apiDir 
        work clientTempDir clientDir 
    
    let generateCSharp() =
        brk()
        !> "Generating CSharp API models"
        generateSwaggerModel()
        !>> "Cleaning up the generated files"
        cleanup()
        !>> "Formatting the code"
        formatCode()
        !>> "Copying the files to the API directory"
        copy()
        brk()
        !> "CSharp Model generation finished"
            
module TypeScript =
    let tempTsDir = scriptDir <!!> @"obj\API\Client"
    let targetTsDir = rootDir <!!> @"srcjs\src\app\client"
    
    let generateTypeScriptModel() =
        !>> "Cleaning Models directory"    
        [tempTsDir; targetTsDir] |> Seq.iter (ensureDir >> ignore)
        [tempTsDir; targetTsDir] |> Seq.iter emptyDir
        javaExec <| sprintf """-jar %s generate -i %s -l typescript-angular -o obj -c %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-partial.json") (toolsDir <!!> @"..\codegen-config.json")
        let generatedFiles = Directory.GetFiles(modelsTempDir).Count()
        if generatedFiles > 0 then
            brk()
            !> (sprintf "%i files generated." generatedFiles)
            brk()
        else
            brk()
            !> "Swagger code generation failed." 
            brk()
    
    let copy() =
        work tempTsDir targetTsDir
    
    let generateTypeScript() =
        brk()
        !> "Generating TypeScript API models"
        generateTypeScriptModel()
        !>> "Copying the files to the API directory"
        copy()
        brk()
        !> "CSharp Model generation finished"

/// Generate API model from the swagger definition
let generateModel() =
    CSharp.generateCSharp()
    TypeScript.generateTypeScript()

generateModel()