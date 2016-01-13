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
        javaExec <| sprintf """-jar %s generate -i %s -l csharp -t %s -o obj -c %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-partial.json") (specDir <!!> "cs-template") (toolsDir <!!> @"..\codegen-config.json")
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
    let targetTsDir = rootDir <!!> @"srcjs\src\common\client"
    
    let generateTypeScriptModel() =
        !>> "Cleaning Models directory"    
        [tempTsDir; targetTsDir] |> Seq.iter (ensureDir >> ignore)
        [tempTsDir; targetTsDir] |> Seq.iter emptyDir
        javaExec <| sprintf """-jar %s generate -i %s -l typescript-angular -o obj -t %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-partial.json") (specDir <!!> "ts-template")
        let generatedFiles = Directory.GetFiles(tempTsDir).Count()
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
    
    let private cleanup() = 
        /// Perform specific code cleanups
        let codeCorrection (fileName : string) (l : string) =
            
            let capitalizeAfter (after : string) (input : string) =
                let chars = input.ToCharArray()
                chars.[after.Length] <- Char.ToUpper(chars.[after.Length])
                new string(chars)

            // Capitalize type references
            let mutable line = 
                let idx = l.IndexOf("@capitalize@") // 12 chars
                if idx > 0 then 
                    let str1 = l.Substring(0, idx)
                    let str2 = l.Substring(idx + 12)
                    let mutable out = str2

                    let reserved = 
                        let x = ["any"; "number"; "boolean"; "string"]
                        x |> List.append (x |> List.map (fun s -> "Array<" + s))

                    if reserved |> Seq.exists (fun x -> str2.StartsWith(x)) then str1 + str2
                    else
                        if str2.StartsWith("Array<") then str2 |> capitalizeAfter "Array<"
                        else str2 |> capitalizeAfter ""
                        |> fun s2 -> str1 + s2
                else l
            
            line

        let cleanupFile(f : string) =
            let mutable file = File.ReadAllLines(f)
            let lines = 
                file 
                |> Array.map (codeCorrection f)
                |> Array.filter (String.IsNullOrWhiteSpace >> not)

            File.WriteAllLines(f, lines)
        
        tempTsDir
        |> loopFiles
        |> Seq.iter cleanupFile

    let generateTypeScript() =
        brk()
        !> "Generating TypeScript API models"
        generateTypeScriptModel()
        !>> "Cleaning up the generated files"
        cleanup()
        !>> "Copying the files to the API directory"
        copy()
        brk()
        !> "CSharp Model generation finished"

/// Generate API model from the swagger definition
let generateModel() =
    CSharp.generateCSharp()
    TypeScript.generateTypeScript()

generateModel()