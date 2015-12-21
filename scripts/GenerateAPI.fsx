#load "Helpers.fsx"
module GenerateAPI = 
    open Helpers
    open System
    open System.Diagnostics
    open System.IO
    open System.Linq
    open System.Text.RegularExpressions

    let private generateSwaggerModel() = 
        !>> "Cleaning Models directory"
        emptyDir modelsTempDir
        emptyDir modelsDir
        javaExec <| sprintf """-jar %s generate -i %s -l csharp -t %s -o obj -Dmodels""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-partial.json") (specDir <!!> "template")
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
            line

        let cleanupFile(f) =
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
        modelsTempDir
        |> loopFiles
        |> Seq.iter (fun f -> File.Copy(f, modelsDir <!!> Path.GetFileName(f)))
    
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
        brk()