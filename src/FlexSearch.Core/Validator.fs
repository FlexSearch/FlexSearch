// ----------------------------------------------------------------------------
// Validators (Validator.fs)
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open FluentValidation
open FluentValidation.Results
open FlexSearch.Api.Types
open FlexSearch.Core
open System.Linq
open System.Collections.Generic
open System
open FlexSearch

// ----------------------------------------------------------------------------
// Contains all validators used for domain validation 
// ----------------------------------------------------------------------------
module Validator =

    // General validation exception thrown by all validators
    exception ValidationException of PropertyName : string * ErrorMessage : string * ErrorCode : string 
    
    // Validation helper wrapper function
    let validate propName (v: 'a) = (propName, v)

    // ----------------------------------------------------------------------------
    // General validation helpers
    // ----------------------------------------------------------------------------
    let notNullAndEmpty (propName: string, value: string) =
        if System.String.IsNullOrWhiteSpace(value) <> true then
            (propName, value)
        else
            raise (ValidationException(propName, "The given property cannot be empty.", "3000"))


    let regexMatch (pattern : string) (propName: string, value: string) =
        let m = System.Text.RegularExpressions.Regex.Match(value, pattern)
        if m.Success then (propName, value)
        else raise (ValidationException(propName, "The given property does not match the regex pattern: " + pattern + "." , "3000"))


    let notIn (values: string[]) (propName: string, value: string) =
        if values.Contains(value) <> true then
            (propName, value)
        else
            raise (ValidationException(propName, "The given property cannot have the following as valid values: " + String.Join("," , values) + ".", "3000"))


    let onlyIn (values: string[]) (propName: string, value: string) =
        if values.Contains(value) then
            (propName, value)
        else
            raise (ValidationException(propName, "The given property can only have the following as valid values: " + String.Join("," , values) + ".", "3000"))


    let greaterThanOrEqualTo (range: int) (propName: string, value: int) =
        if value >= range then (propName, value)
        else
            raise (ValidationException(propName, "The given property should be greater than or equal to: " + range.ToString() + ".", "3000"))


    let greaterThan (range: int) (propName: string, value: int) =
        if value > range then (propName, value)
        else
            raise (ValidationException(propName, "The given property should be greater than: " + range.ToString() + ".", "3000"))


    let lessThanOrEqualTo (range: int) (propName: string, value: int) =
        if value <= range then (propName, value)
        else
            raise (ValidationException(propName, "The given property should be less than or equal to: " + range.ToString() + ".", "3000"))


    let lessThan (range: int) (propName: string, value: int) =
        if value < range then (propName, value)
        else
            raise (ValidationException(propName, "The given property should be less than: " + range.ToString() + ".", "3000"))


    // ----------------------------------------------------------------------------
    // FlexSearch related validation helpers
    // ----------------------------------------------------------------------------
    let mustGenerateFilterInstance (factoryCollection : Interface.IFactoryCollection) (propName: string, value: Filter) =
        match factoryCollection.FilterFactory.GetModuleByName(value.FilterName) with
        | Some(instance) ->    
            try
                instance.Initialize(value.Parameters, factoryCollection.ResourceLoader)
                (propName, value)
             with
            | e -> 
                raise (ValidationException(propName, "Filter cannot be initialized : " + e.Message + ".", "3000"))
        | _ ->  raise (ValidationException(propName, "The requested filter does not exist: " + value.FilterName + ".", "3000"))


    let mustGenerateTokenizerInstance (factoryCollection : Interface.IFactoryCollection) (propName: string, value: Tokenizer) =
        match factoryCollection.TokenizerFactory.GetModuleByName(value.TokenizerName) with
        | Some(instance) ->    
            try
                instance.Initialize(value.Parameters, factoryCollection.ResourceLoader)
                (propName, value)
             with
            | e -> 
                raise (ValidationException(propName, "Tokenizer cannot be initialized : " + e.Message + ".", "3000"))
        | _ ->  raise (ValidationException(propName, "The requested tokenizer does not exist: " + value.TokenizerName + ".", "3000"))


    // ----------------------------------------------------------------------------
    // FlexSearch validation constructs
    // ----------------------------------------------------------------------------
    
    // Validator to validate the properties name in FlexSearch
    let propertyNameValidator (propName: string, value: string) =
        validate propName value
        |> notNullAndEmpty
        |> regexMatch "^[a-z0-9]*$"
        |> notIn [|"id"; "lastmodified"; "type"|]
        |> ignore


    // Filter validator which checks both the input parameters and naming convention
    let FilterValidator (factoryCollection : Interface.IFactoryCollection, value: Filter) =
        validate "FilterName" value.FilterName |> propertyNameValidator |> ignore
        validate "Filter" value |> mustGenerateFilterInstance factoryCollection |> ignore


    let TokenizerValidator (factoryCollection : Interface.IFactoryCollection, value: Tokenizer) =
        validate "TokenizerName" value.TokenizerName |> propertyNameValidator |> ignore
        validate "Tokenizer" value |> mustGenerateTokenizerInstance factoryCollection |> ignore


    let AnalyzerValidator (factoryCollection : Interface.IFactoryCollection, value: AnalyzerProperties) =
        TokenizerValidator(factoryCollection, value.Tokenizer)
        if value.Filters.Count = 0 then raise (ValidationException("Filters", "Atleast one filter should be specified for a custom analyzer." , "3000"))
        value.Filters.ToArray() |> Array.iter(fun x -> FilterValidator(factoryCollection, x) |> ignore)


    let IndexConfigurationValidator(propName: string, value: IndexConfiguration) =
        validate "CommitTimeSec" value.CommitTimeSec |> greaterThanOrEqualTo 60 |> ignore
        validate "RefreshTimeMilliSec" value.RefreshTimeMilliSec |> greaterThanOrEqualTo 25 |> ignore
        validate "Shards" value.Shards |> greaterThanOrEqualTo 1 |> ignore
        validate "RamBufferSizeMb" value.RamBufferSizeMb |> greaterThanOrEqualTo 100 |> ignore


    let ScriptValidator(factoryCollection : Interface.IFactoryCollection, value: ScriptProperties) =
        validate "ScriptSource" value.ScriptSource |> notNullAndEmpty |> ignore
        try
            match value.ScriptType with
            | ScriptType.SearchProfileSelector ->
                factoryCollection.ScriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript(value) |> ignore
            | ScriptType.CustomScoring ->
                factoryCollection.ScriptFactoryCollection.CustomScoringScriptFactory.CompileScript(value) |> ignore
                
            | ScriptType.ComputedField ->
                factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript(value) |> ignore
            | _ -> raise (ValidationException("Script", "The requested script type does not exist: " + value.ScriptType.ToString() + ".", "3000"))
        with
        | e -> raise (ValidationException("Script", "Script cannot be compiled : " + e.Message + ".", "3000"))


    let IndexFieldValidator(factoryCollection : Interface.IFactoryCollection, analyzers: Dictionary<string, AnalyzerProperties>, scripts : Dictionary<string, ScriptProperties>, value: IndexFieldProperties) =
        validate "ScriptName" value.ScriptName |> notNullAndEmpty 
        //match value.ScriptName