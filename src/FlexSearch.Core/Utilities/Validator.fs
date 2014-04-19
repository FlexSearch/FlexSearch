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
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Linq

// ----------------------------------------------------------------------------
// Contains all validators used for domain validation 
// ----------------------------------------------------------------------------
module Validator = 
    // Validation helper wrapper function
    let validate propName (v : 'a) = (propName, v)
    
    // ----------------------------------------------------------------------------
    // General validation helpers
    // ----------------------------------------------------------------------------
    let notNullAndEmpty (propName : string, value : string) = 
        if System.String.IsNullOrWhiteSpace(value) <> true then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.PROPERTY_CANNOT_BE_EMPTY, propName))
    
    let regexMatch (pattern : string) (propName : string, value : string) = 
        let m = System.Text.RegularExpressions.Regex.Match(value, pattern)
        if m.Success then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.REGEX_NOT_MATCHED, propName, pattern))
    
    let notIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) <> true then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.VALUE_NOT_IN, propName, (String.Join(",", values))))
    
    let onlyIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) = true then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.VALUE_ONLY_IN, propName, (String.Join(",", values))))
    
    let greaterThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value >= range then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.GREATER_THAN_EQUAL_TO, propName, range.ToString()))
    
    let greaterThan (range : int) (propName : string, value : int) = 
        if value > range then Choice1Of2(propName, value)
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.GREATER_THAN, propName, range.ToString()))
    
    let lessThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value <= range then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.LESS_THAN_EQUAL_TO, propName, range.ToString()))
    
    let lessThan (range : int) (propName : string, value : int) = 
        if value < range then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.LESS_THAN, propName, range.ToString()))
    
    // ----------------------------------------------------------------------------
    // FlexSearch related validation helpers
    // ----------------------------------------------------------------------------
    let mustGenerateFilterInstance (factoryCollection : IFactoryCollection) 
        (propName : string, value : TokenFilter) = 
        match factoryCollection.FilterFactory.GetModuleByName(value.FilterName) with
        | Choice1Of2(instance) -> 
            try 
                instance.Initialize(value.Parameters, factoryCollection.ResourceLoader)
                Choice1Of2()
            with e -> 
                Choice2Of2
                    (OperationMessage.WithPropertyName
                         (MessageConstants.FILTER_CANNOT_BE_INITIALIZED, propName, e.Message))
        | _ -> Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.FILTER_NOT_FOUND, propName))
    
    let mustGenerateTokenizerInstance (factoryCollection : IFactoryCollection) 
        (propName : string, value : Tokenizer) = 
        match factoryCollection.TokenizerFactory.GetModuleByName(value.TokenizerName) with
        | Choice1Of2(instance) -> 
            try 
                instance.Initialize(value.Parameters, factoryCollection.ResourceLoader)
                Choice1Of2()
            with e -> 
                Choice2Of2
                    (OperationMessage.WithPropertyName
                         (MessageConstants.TOKENIZER_CANNOT_BE_INITIALIZED, propName, e.Message))
        | _ -> Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.TOKENIZER_NOT_FOUND, propName))
    
    // ----------------------------------------------------------------------------
    // FlexSearch validation constructs
    // ----------------------------------------------------------------------------
    // Validator to validate the properties name in FlexSearch
    let propertyNameValidator (propName : string, value : string) = 
        maybe { 
            do! (propName, value) |> notNullAndEmpty
            do! (propName, value) |> regexMatch "^[a-z0-9_]*$"
            do! (propName, value) |> notIn [| Constants.IdField; Constants.LastModifiedField; Constants.TypeField |]
        }
    
    /// Filter validator which checks both the input parameters and naming convention
    let FilterValidator (factoryCollection : IFactoryCollection) (value : TokenFilter) = 
        maybe { 
            do! ("FilterName", value.FilterName) |> propertyNameValidator
            do! ("FilterName", value) |> mustGenerateFilterInstance factoryCollection
        }
    
    let TokenizerValidator(factoryCollection : IFactoryCollection, value : Tokenizer) = 
        maybe { 
            do! validate "TokenizerName" value.TokenizerName |> propertyNameValidator
            do! validate "Tokenizer" value |> mustGenerateTokenizerInstance factoryCollection
        }
    
    let AnalyzerValidator (factoryCollection : IFactoryCollection) 
        (propName : string, value : AnalyzerProperties) = 
        maybe { 
            do! TokenizerValidator(factoryCollection, value.Tokenizer)
            if value.Filters.Count = 0 then return! Choice2Of2(MessageConstants.ATLEAST_ONE_FILTER_REQUIRED)
            else do! iterExitOnFailure (List.ofSeq (value.Filters)) (FilterValidator factoryCollection)
        }
    
    let IndexConfigurationValidator(propName : string, value : IndexConfiguration) = 
        maybe { 
            do! validate "CommitTimeSec" value.CommitTimeSec |> greaterThanOrEqualTo 60
            do! validate "RefreshTimeMilliSec" value.RefreshTimeMilliSec |> greaterThanOrEqualTo 25
            do! validate "RamBufferSizeMb" value.RamBufferSizeMb |> greaterThanOrEqualTo 100
        }
    
    let ScriptValidator (factoryCollection : IFactoryCollection) (propName : string, value : ScriptProperties) = 
        maybe { 
            do! validate "ScriptSource" value.Source |> notNullAndEmpty
            match value.ScriptType with
            | ScriptType.SearchProfileSelector -> let! script = factoryCollection.ScriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript
                                                                    (value)
                                                  return! Choice1Of2()
            | ScriptType.ComputedField -> let! script = factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript
                                                            (value)
                                          return! Choice1Of2()
            | _ -> 
                return! Choice2Of2
                            (OperationMessage.WithPropertyName
                                 (MessageConstants.UNKNOWN_SCRIPT_TYPE, value.ScriptType.ToString()))
        }
    
    let IndexFieldValidator (factoryCollection : IFactoryCollection) 
        (analyzers : Dictionary<string, AnalyzerProperties>) (scripts : Dictionary<string, ScriptProperties>) 
        (propName : string, value : FieldProperties) = 
        maybe { 
            if String.IsNullOrWhiteSpace(value.ScriptName) <> true then 
                do! validate "ScriptName" value.ScriptName |> propertyNameValidator
                if scripts.ContainsKey(value.ScriptName) <> true then 
                    return! Choice2Of2
                                (OperationMessage.WithPropertyName(MessageConstants.SCRIPT_NOT_FOUND, value.ScriptName))
            match value.FieldType with
            | FieldType.Custom | FieldType.Highlight | FieldType.Text -> 
                if String.IsNullOrWhiteSpace(value.SearchAnalyzer) <> true then 
                    if analyzers.ContainsKey(value.SearchAnalyzer) <> true then 
                        if factoryCollection.AnalyzerFactory.ModuleExists(value.SearchAnalyzer) <> true then 
                            return! Choice2Of2
                                        (OperationMessage.WithPropertyName
                                             (MessageConstants.ANALYZER_NOT_FOUND, value.SearchAnalyzer))
                if String.IsNullOrWhiteSpace(value.IndexAnalyzer) <> true then 
                    if analyzers.ContainsKey(value.IndexAnalyzer) <> true then 
                        if factoryCollection.AnalyzerFactory.ModuleExists(value.IndexAnalyzer) <> true then 
                            return! Choice2Of2
                                        (OperationMessage.WithPropertyName
                                             (MessageConstants.ANALYZER_NOT_FOUND, value.IndexAnalyzer))
            | _ -> return! Choice1Of2()
        }
    
    //    let SearchConditionValidator(factoryCollection : Interface.IFactoryCollection, fields: Dictionary<string, IndexFieldProperties>, value: SearchCondition) =
    //        if fields.ContainsKey(value.FieldName) <> true then
    //            raise (ValidationException("SeachCondition", "The specified 'FieldName' does not exist: " + value.FieldName + ".", "3000"))
    //        if value.Boost <> 0 then
    //            validate "Boost" value.Boost |> greaterThanOrEqualTo 1 |> ignore
    //        if factoryCollection.SearchQueryFactory.ModuleExists(value.Operator) <> true then
    //            raise (ValidationException("SeachCondition", "The specified 'Operator' does not exist: " + value.Operator + ".", "3000"))
    //
    //
    //    let SearchFilterValidator(factoryCollection : Interface.IFactoryCollection, fields: Dictionary<string, FieldProperties>, value: SearchFilter) =
    //        ()
    //
    //
    //    let SearchProfileValidator (fields : Dictionary<string, FieldProperties>) (propName: string, value: SearchQuery) =
    //        ()
    type IndexValidator(factoryCollection : IFactoryCollection) = 
        interface IIndexValidator with
            member this.Validate(value : Index) = 
                maybe { 
                    do! validate "IndexName" value.IndexName |> propertyNameValidator
                    do! validate "Configuration" value.IndexConfiguration |> IndexConfigurationValidator
                    do! iterExitOnFailure (Seq.toList (value.Analyzers)) (fun x -> 
                            maybe { 
                                do! validate "AnalyzerName" x.Key |> propertyNameValidator
                                do! validate "AnalyzerProperties" x.Value |> AnalyzerValidator factoryCollection
                            })
                    do! iterExitOnFailure (Seq.toList (value.Scripts)) (fun x -> 
                            maybe { 
                                do! validate "ScriptName" x.Key |> propertyNameValidator
                                do! validate "ScriptProperties" x.Value |> ScriptValidator factoryCollection
                            })
                    do! iterExitOnFailure (Seq.toList (value.Fields)) (fun x -> 
                            maybe { 
                                do! validate "FieldName" x.Key |> propertyNameValidator
                                do! validate "FieldProperties" x.Value 
                                    |> IndexFieldValidator factoryCollection value.Analyzers value.Scripts
                            })
                    return! Choice1Of2()
                }
//                value.SearchProfiles.ToArray() |> Array.iter(fun x ->
//                    validate "SearchProfileName" x.Key |> propertyNameValidator |> ignore
//                    validate "SearchProfileProperties" x.Value 
//                        |> SearchProfileValidator value.Fields |> ignore
//                )
