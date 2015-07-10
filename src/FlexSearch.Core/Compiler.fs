// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

/// Scripts can be used to automate various processing in FlexSearch. Script Type signifies
/// the type of operation that the current script can perform. These can vary from scripts
/// used for computing fields dynamically at index time or scripts which can be used to alter
/// FlexSearch's default scoring.    
[<AutoOpen>]
module Scripts = 
    open System
    open System.Collections.Generic
    
    type ComputedDelegate = Func<string, string, IReadOnlyDictionary<string, string>, string [], string>
    
    type PostSearchDeletegate = Func<SearchQuery.Dto, string, float32, Dictionary<string, string>, bool * float32>
    
    type SearchProfileDelegate = Action<SearchQuery.Dto, Dictionary<string, string>>
    
    type T = 
        /// Default signature which is used by computed scripts
        /// Format -> indexName, fieldName, fields, parameters
        | ComputedScript of script : ComputedDelegate
        /// Script which can be used to filter search query results
        /// Format -> searchQuery, id, score, fields -> filtering result * score
        | PostSearchScript of script : PostSearchDeletegate
        /// Script to process search profile data
        | SearchProfileScript of script : SearchProfileDelegate
    
    type ScriptType = 
        | Computed = 1
        | PostSearch = 2
        | SearchProfile = 3
    
    let pattern (scriptType : ScriptType) = 
        match scriptType with
        | ScriptType.Computed -> "computed_*.csx"
        | ScriptType.PostSearch -> "postsearch_*.csx"
        | ScriptType.SearchProfile -> "searchprofile_*.csx"
        | _ -> failwithf "Unknown ScriptType"

[<AutoOpen>]
module Compiler = 
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis
    open System.IO
    open System.Text
    open System.Linq
    open System.Reflection
    open System
    
    let scriptingNamespace = "Flexsearch.Scripting"
    let scriptMethodName = "Execute"
    let usingStatements = """
using System;
using FlexSearch.Core;
using System.Collections.Generic;
"""
    
    let metaDataReference = 
        [| MetadataReference.CreateFromFile(typeof<System.Object>.Assembly.Location) :> MetadataReference
           
           MetadataReference.CreateFromFile(typeof<FlexSearch.Core.SearchQuery.Dto>.Assembly.Location) :> MetadataReference |]
    
    let generateSyntaxTree (scriptName) (text : string) = 
        let syntax = 
            if text.Contains("namespace") then text
            else 
                sprintf "namespace %s { %s public static class %s { public static %s } }" scriptingNamespace 
                    usingStatements scriptName text
        CSharpSyntaxTree.ParseText(syntax)
    
    let generateCompilationUnit (syntaxTree : SyntaxTree) = 
        CSharpCompilation.Create
            (Path.GetRandomFileName(), [| syntaxTree |], metaDataReference, 
             new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
    
    let generateAssembly (scriptName : string) (cu : CSharpCompilation) = 
        use ms = new MemoryStream()
        let result = cu.Emit(ms)
        match result.Success with
        | false -> 
            let errors = 
                Seq.fold (fun (acc : StringBuilder) (elem : Diagnostic) -> acc.AppendLine(elem.GetMessage())) 
                    (new StringBuilder()) (result.Diagnostics.Where(fun x -> x.Severity = DiagnosticSeverity.Error))
            failwith (errors.ToString())
        | true -> 
            ms.Seek(0L, SeekOrigin.Begin) |> ignore
            let assm = Assembly.Load(ms.ToArray())
            let typ = assm.GetType(sprintf "%s.%s" scriptingNamespace scriptName)
            match typ.GetMethod(scriptMethodName) with
            | null -> failwith "The given script does not contain a method named 'Execute'"
            | _ -> typ
    
    let generateDeletegate (scriptType : ScriptType) (typ : Type) = 
        let methodInfo = typ.GetMethod(scriptMethodName)
        match scriptType with
        | ScriptType.Computed -> 
            ComputedScript <| (Delegate.CreateDelegate(typeof<ComputedDelegate>, methodInfo) :?> ComputedDelegate)
        | ScriptType.PostSearch -> 
            PostSearchScript 
            <| (Delegate.CreateDelegate(typeof<PostSearchDeletegate>, methodInfo) :?> PostSearchDeletegate)
        | ScriptType.SearchProfile -> 
            SearchProfileScript 
            <| (Delegate.CreateDelegate(typeof<SearchProfileDelegate>, methodInfo) :?> SearchProfileDelegate)
        | _ -> failwithf "Unknown ScriptType"
    
    /// Compiles a script with the given source code
    let compileScript (sourceCode, scriptName, scriptType : ScriptType) = 
        try 
            sourceCode
            |> generateSyntaxTree scriptName
            |> generateCompilationUnit
            |> generateAssembly scriptName
            |> generateDeletegate scriptType
            |> fun x -> (sprintf "%s%s" scriptName (string scriptType), x)
            |> ok
        with e -> fail <| ScriptCannotBeCompiled(scriptName, exceptionPrinter e)
    
    /// Compiles a script from the file
    let compileFromFile (filePath, scriptType : ScriptType) = 
        let code = File.ReadAllText(filePath)
        let fileName = Path.GetFileNameWithoutExtension(filePath)
        compileScript (code, fileName |> after '_', scriptType)
    
    /// Compiles all the scripts of a given type
    let compileAllScripts (scriptType : ScriptType) = 
        Directory.EnumerateFiles(Constants.ScriptFolder, scriptType |> pattern, SearchOption.TopDirectoryOnly)
        |> Seq.map (fun x -> compileFromFile (x, scriptType))
        |> Seq.filter resultToBool
        |> Seq.map extract
