#load "Helpers.fsx"
#load "SwaggerInjector.fsx"

open Helpers
open SwaggerInjector
open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Text
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
        javaExec <| sprintf """-jar %s generate -i %s -l csharp -t %s -o obj -c %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-full.json") (specDir <!!> "cs-template") (scriptDir <!!> "codegen-config.json")
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

        let tag name = sprintf @"[\s]*<%s>[^<]+<\/%s>" name name

        let deleteProperty fileStr propName  =
            Regex.Replace(fileStr, 
                          sprintf @"\/\/\/[\s]*<summary>[^<]+<\/[^<]*%s[\s]*{[\s]*get;[^/]+" propName, 
                          "")

        let deleteMethod fileStr methName  =
            Regex.Replace(fileStr, 
                          sprintf @"\/\/\/%s[^<]*%s[^<]*%s\(\)[\s]*{[^/]+" (tag "summary") (tag "returns") methName, 
                          "")

        let perFileCleanup (fileName : string) (contents : string) =
            if contents.Contains("public OperationMessage Error { get; set; }") then
                contents.Replace("////,IResponseError", ",IResponseError").Replace(" = new OperationMessage();", "")
            else
                contents.Replace(" = new OperationMessage();", "")

        let cleanupFile(f : string) =
            let mutable file = File.ReadAllLines(f)
            let fileSb = new StringBuilder()
            file 
            |> Array.map (codeCorrection f)
            |> Array.filter (String.IsNullOrWhiteSpace >> not)
            |> Array.iter (fileSb.AppendLine >> ignore)

            fileSb.ToString()
            |> perFileCleanup f
            |> fun fileContents -> File.WriteAllText(f, fileContents)
        
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
        javaExec <| sprintf """-jar %s generate -i %s -l typescript-angular -o obj -t %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-full.json") (specDir <!!> "ts-template")
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
        !> "TypeScript Model generation finished"

module JavaScript =
    let tempJsDir = scriptDir <!!> @"obj\src"
    let targetJsDir = rootDir <!!> @"deploy\clients\js"

    let generateJavaScriptModel() =
        !>> "Cleaning Models directory"    
        [tempJsDir; targetJsDir] |> Seq.iter (ensureDir >> ignore)
        [tempJsDir; targetJsDir] |> Seq.iter emptyDir
        javaExec <| sprintf """-jar %s generate -i %s -l javascript -o obj -t %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-full.json") (specDir <!!> "js-template")
        let generatedFiles = Directory.GetFiles(tempJsDir, "*.js", SearchOption.AllDirectories).Count()
        if generatedFiles > 0 then
            brk()
            !> (sprintf "%i files generated." generatedFiles)
            brk()
        else
            brk()
            !> "Swagger code generation failed." 
            brk()
    
    let copy() =
        // Copy the files
        copyFiles tempJsDir targetJsDir
        // Copy the directories
        loopDir tempJsDir
        |> Seq.iter (fun src -> copyDir src targetJsDir)
    
    let generateJavaScript() =
        brk()
        !> "Generating TypeScript API models"
        generateJavaScriptModel()
        !>> "Copying the files to the API directory"
        copy()
        brk()
        !> "JavaScript Model generation finished"

module Html =
    let targetHtmlDir = rootDir <!!> @"documentation"

    let generateHtmlModel() =
        !>> "Cleaning Models directory"    
        targetHtmlDir |> (ensureDir >> ignore)
        targetHtmlDir |> emptyDir
        javaExec <| sprintf """-jar %s generate -i %s -l html -t %s -o %s""" (toolsDir <!!> "swagger-codegen-cli.jar") (specDir <!!> "swagger-full.json") (specDir <!!> "html-template") targetHtmlDir
        let generatedFiles = File.Exists(targetHtmlDir <!!> "index.html")
        if generatedFiles then
            // rename file to api.html
            File.Copy(targetHtmlDir <!!> "index.html", targetHtmlDir <!!> "api.html", true)
            brk()
            !> (sprintf "HTML files generated.")
            brk()
        else
            brk()
            !> "Swagger code generation failed." 
            brk()
    
    let generateHtml() =
        brk()
        !> "Generating HTML API models"
        generateHtmlModel()
        brk()
        !> "HTML Model generation finished"

/// Generate API model from the swagger definition
let generateModel() =
    // First generate the full swagger file according to the glossary
    injectSwagger()
    CSharp.generateCSharp()
    TypeScript.generateTypeScript()
    JavaScript.generateJavaScript()
    Html.generateHtml()

// Initialize the git submodule containing the CodeFormatter if it's not there
if File.Exists(toolsDir <!!> "CodeFormatter/CodeFormatter.exe") |> not then
    !> "Downloading the CodeFormatter application..."
    exec("git", "submodule init")
    exec("git", "submodule update")

generateModel()