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
namespace FlexSearch.Core

open FlexSearch
open FlexSearch.Api
open FlexSearch.Api.Exception
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Linq

// ----------------------------------------------------------------------------
// Contains all validators used for domain validation 
// ----------------------------------------------------------------------------
module Validator = 
    // General validation exception thrown by all validators
    exception ValidationException of PropertyName : string * ErrorMessage : string * ErrorCode : string
    
    type ValidationResult<'T> = 
        | Success of 'T
        | Error of PropertyName : string * ErrorMessage : string * ErrorCode : string
    
    // Validation helper wrapper function
    let validate propName (v : 'a) = (propName, v)
    
    // ----------------------------------------------------------------------------
    // General validation helpers
    // ----------------------------------------------------------------------------
    let notNullAndEmpty (propName : string, value : string) = 
        if System.String.IsNullOrWhiteSpace(value) <> true then Choice1Of2()
        else Choice2Of2(InvalidOperation.WithPropertyName(ExceptionConstants.PROPERTY_CANNOT_BE_EMPTY, propName))
    
    let regexMatch (pattern : string) (propName : string, value : string) = 
        let m = System.Text.RegularExpressions.Regex.Match(value, pattern)
        if m.Success then Choice1Of2()
        else Choice2Of2(InvalidOperation.WithPropertyName(ExceptionConstants.REGEX_NOT_MATCHED, propName, pattern))
    
    let notIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) <> true then Choice1Of2()
        else 
            Choice2Of2
                (InvalidOperation.WithPropertyName
                     (ExceptionConstants.VALUE_NOT_IN, propName, (String.Join(",", values))))
    
    let onlyIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) = true then Choice1Of2()
        else 
            Choice2Of2
                (InvalidOperation.WithPropertyName
                     (ExceptionConstants.VALUE_ONLY_IN, propName, (String.Join(",", values))))
    
    let greaterThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value >= range then Choice1Of2()
        else 
            Choice2Of2
                (InvalidOperation.WithPropertyName(ExceptionConstants.GREATER_THAN_EQUAL_TO, propName, range.ToString()))
    
    let greaterThan (range : int) (propName : string, value : int) = 
        if value > range then Choice1Of2(propName, value)
        else Choice2Of2(InvalidOperation.WithPropertyName(ExceptionConstants.GREATER_THAN, propName, range.ToString()))
    
    let lessThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value <= range then Choice1Of2()
        else 
            Choice2Of2
                (InvalidOperation.WithPropertyName(ExceptionConstants.LESS_THAN_EQUAL_TO, propName, range.ToString()))
    
    let lessThan (range : int) (propName : string, value : int) = 
        if value < range then Choice1Of2()
        else Choice2Of2(InvalidOperation.WithPropertyName(ExceptionConstants.LESS_THAN, propName, range.ToString()))
    
    // ----------------------------------------------------------------------------
    // FlexSearch related validation helpers
    // ----------------------------------------------------------------------------
    let mustGenerateFilterInstance (factoryCollection : Interface.IFactoryCollection) 
        (propName : string, value : TokenFilter) = 
        match factoryCollection.FilterFactory.GetModuleByName(value.FilterName) with
        | Some(instance) -> 
            try 
                instance.Initialize(value.Parameters, factoryCollection.ResourceLoader)
                Choice1Of2()
            with e -> 
                Choice2Of2
                    (InvalidOperation.WithPropertyName
                         (ExceptionConstants.FILTER_CANNOT_BE_INITIALIZED, propName, e.Message))
        | _ -> Choice2Of2(InvalidOperation.WithPropertyName(ExceptionConstants.FILTER_NOT_FOUND, propName))
    
    let mustGenerateTokenizerInstance (factoryCollection : Interface.IFactoryCollection) 
        (propName : string, value : Tokenizer) = 
        match factoryCollection.TokenizerFactory.GetModuleByName(value.TokenizerName) with
        | Some(instance) -> 
            try 
                instance.Initialize(value.Parameters, factoryCollection.ResourceLoader)
                Choice1Of2()
            with e -> 
                Choice2Of2
                    (InvalidOperation.WithPropertyName
                         (ExceptionConstants.TOKENIZER_CANNOT_BE_INITIALIZED, propName, e.Message))
        | _ -> Choice2Of2(InvalidOperation.WithPropertyName(ExceptionConstants.TOKENIZER_NOT_FOUND, propName))
    
    // ----------------------------------------------------------------------------
    // FlexSearch validation constructs
    // ----------------------------------------------------------------------------
    // Validator to validate the properties name in FlexSearch
    let propertyNameValidator (propName : string, value : string) = 
        maybe { 
            do! (propName, value) |> notNullAndEmpty
            do! (propName, value) |> regexMatch "^[a-z0-9]*$"
            do! (propName, value) |> notIn [| "id"; "lastmodified"; "type" |]
        }
    
    /// Filter validator which checks both the input parameters and naming convention
    let FilterValidator(factoryCollection : Interface.IFactoryCollection, value : TokenFilter) = 
        maybe { 
            do! ("FilterName", value.FilterName) |> propertyNameValidator
            do! ("FilterName", value) |> mustGenerateFilterInstance factoryCollection
        }
    
    let TokenizerValidator(factoryCollection : Interface.IFactoryCollection, value : Tokenizer) = 
        maybe { 
            do! validate "TokenizerName" value.TokenizerName |> propertyNameValidator
            do! validate "Tokenizer" value |> mustGenerateTokenizerInstance factoryCollection
        }

    let AnalyzerValidator (factoryCollection : Interface.IFactoryCollection) (propName: string, value: AnalyzerProperties) = maybe {
        do! TokenizerValidator(factoryCollection, value.Tokenizer)
        if value.Filters.Count = 0 then 
            return! Choice2Of2(ExceptionConstants.ATLEAST_ONE_FILTER_REQUIRED)
        else
            value.Filters.ToArray() |> Array.iter(fun x -> do! FilterValidator(factoryCollection, x))
        }

//    let IndexConfigurationValidator(propName: string, value: IndexConfiguration) =
//        validate "CommitTimeSec" value.CommitTimeSec |> greaterThanOrEqualTo 60 |> ignore
//        validate "RefreshTimeMilliSec" value.RefreshTimeMilliSec |> greaterThanOrEqualTo 25 |> ignore
//        //validate "Shards" value.ShardConfiguration.ShardCount |> greaterThanOrEqualTo 1 |> ignore
//        validate "RamBufferSizeMb" value.RamBufferSizeMb |> greaterThanOrEqualTo 100 |> ignore
//
//
//    let ScriptValidator(factoryCollection : Interface.IFactoryCollection)  (propName: string, value: ScriptProperties) =
//        validate "ScriptSource" value.Source |> notNullAndEmpty |> ignore
//        try
//            match value.ScriptType with
//            | ScriptType.SearchProfileSelector ->
//                factoryCollection.ScriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript(value) |> ignore
//            | ScriptType.CustomScoring ->
//                factoryCollection.ScriptFactoryCollection.CustomScoringScriptFactory.CompileScript(value) |> ignore
//                
//            | ScriptType.ComputedField ->
//                factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript(value) |> ignore
//            | _ -> raise (ValidationException("Script", "The requested script type does not exist: " + value.ScriptType.ToString() + ".", "3000"))
//        with
//        | e -> raise (ValidationException("Script", "Script cannot be compiled : " + e.Message + ".", "3000"))
//
//
////    let IndexFieldValidator(factoryCollection : Interface.IFactoryCollection) (analyzers: Dictionary<string, AnalyzerProperties>) (scripts : Dictionary<string, ScriptProperties>) (propName: string, value: IndexFieldProperties) =
////        if String.IsNullOrWhiteSpace(value.Source) <> true then
////            validate "ScriptName" value.Source |> propertyNameValidator |> ignore
////            if scripts.ContainsKey(value.Source) <> true then
////                raise (ValidationException("IndexField", "The specified script does not exist: " + value.Source + ".", "3000"))
//        
////        match value.FieldType with
////        | FieldType.Custom
////        | FieldType.Highlight
////        | FieldType.Text ->
////            if String.IsNullOrWhiteSpace(value.SearchAnalyzer) <> true then
////                if analyzers.ContainsKey(value.SearchAnalyzer) <> true then
////                    if factoryCollection.AnalyzerFactory.ModuleExists(value.SearchAnalyzer) <> true then
////                        raise (ValidationException("IndexField", "The specified 'SearchAnalyzer' does not exist: " + value.SearchAnalyzer + ".", "3000"))
////
////            if String.IsNullOrWhiteSpace(value.IndexAnalyzer) <> true then
////                if analyzers.ContainsKey(value.IndexAnalyzer) <> true then
////                    if factoryCollection.AnalyzerFactory.ModuleExists(value.IndexAnalyzer) <> true then
////                        raise (ValidationException("IndexField", "The specified 'IndexAnalyzer' does not exist: " + value.SearchAnalyzer + ".", "3000"))
////        | _ -> ()
//
//    
////    let SearchConditionValidator(factoryCollection : Interface.IFactoryCollection, fields: Dictionary<string, IndexFieldProperties>, value: SearchCondition) =
////        if fields.ContainsKey(value.FieldName) <> true then
////            raise (ValidationException("SeachCondition", "The specified 'FieldName' does not exist: " + value.FieldName + ".", "3000"))
////        if value.Boost <> 0 then
////            validate "Boost" value.Boost |> greaterThanOrEqualTo 1 |> ignore
////        if factoryCollection.SearchQueryFactory.ModuleExists(value.Operator) <> true then
////            raise (ValidationException("SeachCondition", "The specified 'Operator' does not exist: " + value.Operator + ".", "3000"))
//
//
////    let SearchFilterValidator(factoryCollection : Interface.IFactoryCollection, fields: Dictionary<string, FieldProperties>, value: SearchFilter) =
////        ()
//
//
//    let SearchProfileValidator (fields : Dictionary<string, FieldProperties>) (propName: string, value: SearchQuery) =
//        ()
//
//
//    type IndexValidator(factoryCollection : Interface.IFactoryCollection) =
//        interface IIndexValidator with
//            member this.Validate(value: Index) = 
//                validate "IndexName" value.IndexName |> propertyNameValidator |> ignore
//                //validate "Configuration" value.Configuration |> IndexConfigurationValidator |> ignore
//        
//                value.Analyzers.ToArray() |> Array.iter(fun x ->
//                    validate "AnalyzerName" x.Key |> propertyNameValidator |> ignore
//                    validate "AnalyzerProperties" x.Value |> AnalyzerValidator factoryCollection |> ignore
//                )
//
//                value.Scripts.ToArray() |> Array.iter(fun x ->
//                    validate "ScriptName" x.Key |> propertyNameValidator |> ignore
//                    validate "ScriptProperties" x.Value |> ScriptValidator factoryCollection |> ignore
//                )
//
//                value.Fields.ToArray() |> Array.iter(fun x ->
//                    validate "FieldName" x.Key |> propertyNameValidator |> ignore
////                    validate "FieldProperties" x.Value 
////                        |> IndexFieldValidator factoryCollection value.Analyzers value.Scripts |> ignore
//                )
//
//                value.SearchProfiles.ToArray() |> Array.iter(fun x ->
//                    validate "SearchProfileName" x.Key |> propertyNameValidator |> ignore
//                    validate "SearchProfileProperties" x.Value 
//                        |> SearchProfileValidator value.Fields |> ignore
//                )
