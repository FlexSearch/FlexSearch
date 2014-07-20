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
    open Validator
    open FlexSearch.Api.Message
    open CSScriptLibrary
    
    /// <summary>
    /// Template method code for computed field script
    /// </summary>
    let private StringReturnScriptTemplate = """
public string Execute(dynamic fields) { [SourceCode] }
"""
    
    // The below settings are to prevent locking in case of multi-threaded scenario
    CSScript.GlobalSettings.InMemoryAsssembly <- true
    CSScript.GlobalSettings.OptimisticConcurrencyModel <- false
    CSScript.CacheEnabled <- true
    CSScript.GlobalSettings.TargetFramework <- Constants.DotNetFrameWork
    
    /// <summary>
    /// Generates a Function which returns a string value
    /// </summary>
    /// <param name="source"></param>
    /// <param name="template"></param>
    let internal GenerateStringReturnScript(source : string) = 
        let sourceCode = StringReturnScriptTemplate.Replace("[SourceCode]", source)
        try 
            let compiledScript = 
                CSScript.LoadDelegate<System.Func<System.Dynamic.DynamicObject, string>>
                    (sourceCode, null, false, [| "Microsoft.CSharp" |])
            Choice1Of2(compiledScript)
        with e -> Choice2Of2(MessageConstants.SCRIPT_CANT_BE_COMPILED |> Append("Message", e.Message))
    
    type ScriptProperties with
        
        /// <summary>
        /// Validate Script properties
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(factoryCollection : IFactoryCollection) = 
            maybe { do! Validate "ScriptSource" this.Source |> NotNullAndEmpty }
        
        static member Build(scripts : Dictionary<string, ScriptProperties>, factoryCollection : IFactoryCollection) = 
            maybe { 
                let getScript (script : KeyValuePair<string, ScriptProperties>) = 
                    maybe { 
                        match script.Value.ScriptType with
                        | ScriptType.SearchProfileSelector -> let! compiledScript = GenerateStringReturnScript
                                                                                        (script.Value.Source)
                                                              return compiledScript
                        | ScriptType.ComputedField -> let! compiledScript = GenerateStringReturnScript
                                                                                (script.Value.Source)
                                                      return compiledScript
                        | _ -> 
                            return! Choice2Of2
                                        (MessageConstants.UNKNOWN_SCRIPT_TYPE 
                                         |> Append("Script Type", script.Value.ScriptType.ToString()))
                    }
                
                let profileSelectorScripts = 
                    new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
                let computedFieldScripts = 
                    new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
                let customScoringScripts = 
                    new Dictionary<string, System.Dynamic.DynamicObject * double -> double>(StringComparer.OrdinalIgnoreCase)
                for script in scripts do
                    let! scriptObject = getScript (script)
                    match script.Value.ScriptType with
                    | ScriptType.ComputedField -> computedFieldScripts.Add(script.Key, scriptObject)
                    | ScriptType.SearchProfileSelector -> profileSelectorScripts.Add(script.Key, scriptObject)
                    | _ -> failwith "not possible"
                let scriptsManager = 
                    { ComputedFieldScripts = computedFieldScripts
                      ProfileSelectorScripts = profileSelectorScripts
                      CustomScoringScripts = customScoringScripts }
                return scriptsManager
            }
