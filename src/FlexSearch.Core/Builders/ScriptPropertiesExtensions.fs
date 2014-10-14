// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

[<AutoOpen>]
module ScriptPropertiesExtensions = 
    open FlexSearch.Api
    open FlexSearch.Core
    open System.Collections.Generic
    open System
    open FlexSearch.Common
    open Microsoft.CSharp
    open System.CodeDom.Compiler
    
    /// <summary>
    /// Template method code for computed field script
    /// </summary>
    let private StringReturnScriptTemplate = """
class Foo {
    static public string Execute(dynamic fields) { [SourceCode] }
}
"""
    
    /// <summary>
    /// Generates a Function which returns a string value
    /// </summary>
    /// <param name="source"></param>
    /// <param name="template"></param>
    let internal GenerateStringReturnScript(source : string) = 
        let sourceCode = StringReturnScriptTemplate.Replace("[SourceCode]", source)
        try 
            let ccp = new CSharpCodeProvider()
            let cp = new CompilerParameters()
            cp.ReferencedAssemblies.Add("Microsoft.CSharp.dll") |> ignore
            cp.ReferencedAssemblies.Add("System.dll") |> ignore
            cp.ReferencedAssemblies.Add("System.Core.dll") |> ignore
            cp.GenerateExecutable <- false
            cp.IncludeDebugInformation <- false
            cp.GenerateInMemory <- true
            let cr = ccp.CompileAssemblyFromSource(cp, sourceCode)
            let foo = cr.CompiledAssembly.GetType("Foo")
            let meth = foo.GetMethod("Execute")
            let compiledScript = 
                Delegate.CreateDelegate(typeof<System.Func<System.Dynamic.DynamicObject, string>>, meth) :?> System.Func<System.Dynamic.DynamicObject, string>
            Choice1Of2(compiledScript)
        with e -> 
            Choice2Of2(Errors.SCRIPT_CANT_BE_COMPILED
                       |> GenerateOperationMessage
                       |> Append("Message", e.Message))
    
    type Script with
        static member Build(scripts : List<Script>, factoryCollection : IFactoryCollection) = 
            maybe { 
                let getScript (script : Script) = 
                    maybe { 
                        match script.ScriptType with
                        | ScriptType.SearchProfileSelector -> let! compiledScript = GenerateStringReturnScript
                                                                                        (script.Source)
                                                              return compiledScript
                        | ScriptType.ComputedField -> let! compiledScript = GenerateStringReturnScript
                                                                                (script.Source)
                                                      return compiledScript
                        | _ -> 
                            return! Choice2Of2(Errors.UNKNOWN_SCRIPT_TYPE
                                               |> GenerateOperationMessage
                                               |> Append("Script Type", script.ScriptType.ToString()))
                    }
                
                let profileSelectorScripts = 
                    new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
                let computedFieldScripts = 
                    new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
                let customScoringScripts = 
                    new Dictionary<string, System.Dynamic.DynamicObject * double -> double>(StringComparer.OrdinalIgnoreCase)
                for script in scripts do
                    let! scriptObject = getScript (script)
                    match script.ScriptType with
                    | ScriptType.ComputedField -> computedFieldScripts.Add(script.ScriptName, scriptObject)
                    | ScriptType.SearchProfileSelector -> profileSelectorScripts.Add(script.ScriptName, scriptObject)
                    | _ -> failwith "not possible"
                let scriptsManager = 
                    { ComputedFieldScripts = computedFieldScripts
                      ProfileSelectorScripts = profileSelectorScripts
                      CustomScoringScripts = customScoringScripts }
                return scriptsManager
            }
