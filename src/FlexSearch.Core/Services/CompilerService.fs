// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open CSScriptLibrary
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.ComponentModel.Composition
open System.ComponentModel.Composition.Hosting
open System.Reflection

[<AutoOpen>]
[<RequireQualifiedAccess>]
/// Exposes high level operations that can performed across the system.
/// Most of the services basically act as a wrapper around the functions 
/// here. Care should be taken to not introduce any mutable state in the
/// module but to only pass mutable state as an instance of NodeState
module CompilerService = 
    /// <summary>
    /// Template method code for computed field script
    /// </summary>
    let private StringReturnScriptTemplate = """
public string Execute(System.Collections.Generic.IReadOnlyDictionary<string,string> fields) { [SourceCode] }
"""
    
    /// <summary>
    /// Generates a Function which returns a string value
    /// </summary>
    /// <param name="source"></param>
    /// <param name="template"></param>
    let GenerateStringReturnScript(source : string) = 
        let sourceCode = StringReturnScriptTemplate.Replace("[SourceCode]", source)
        try 
            let compiledScript = 
                CSScript.LoadDelegate<System.Func<System.Collections.Generic.IReadOnlyDictionary<string, string>, string>>
                    (sourceCode)
            Choice1Of2(compiledScript)
        with e -> Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.SCRIPT_CANT_BE_COMPILED, e.Message))
