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
    let private notNullAndEmpty (propName : string, value : string) = 
        if System.String.IsNullOrWhiteSpace(value) <> true then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.PROPERTY_CANNOT_BE_EMPTY, propName))
    
    let private regexMatch (pattern : string) (propName : string, value : string) = 
        let m = System.Text.RegularExpressions.Regex.Match(value, pattern)
        if m.Success then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.REGEX_NOT_MATCHED, propName, pattern))
    
    let private notIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) <> true then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.VALUE_NOT_IN, propName, (String.Join(",", values))))
    
    let private onlyIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) = true then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.VALUE_ONLY_IN, propName, (String.Join(",", values))))
    
    let private greaterThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value >= range then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.GREATER_THAN_EQUAL_TO, propName, range.ToString()))
    
    let private greaterThan (range : int) (propName : string, value : int) = 
        if value > range then Choice1Of2(propName, value)
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.GREATER_THAN, propName, range.ToString()))
    
    let private lessThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value <= range then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.LESS_THAN_EQUAL_TO, propName, range.ToString()))
    
    let private lessThan (range : int) (propName : string, value : int) = 
        if value < range then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.LESS_THAN, propName, range.ToString()))
    
    // ----------------------------------------------------------------------------
    // FlexSearch related validation helpers
    // ----------------------------------------------------------------------------
    let private mustGenerateFilterInstance (factoryCollection : IFactoryCollection) 
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
    
    let private mustGenerateTokenizerInstance (factoryCollection : IFactoryCollection) 
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
    type System.String with
        /// <summary>
        /// Validate a given property value
        /// </summary>
        /// <param name="propertyName"></param>
        member this.ValidatePropertyValue(propertyName : string) = 
            maybe { 
                do! (propertyName, this) |> notNullAndEmpty
                do! (propertyName, this) |> regexMatch "^[a-z0-9_]*$"
                do! (propertyName, this) 
                    |> notIn [| Constants.IdField; Constants.LastModifiedField; Constants.TypeField |]
            }
    
    type Api.TokenFilter with
        /// <summary>
        /// Filter validator which checks both the input parameters and naming convention
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(factoryCollection : IFactoryCollection) = 
            maybe { 
                do! this.FilterName.ValidatePropertyValue("FilterName")
                do! ("FilterName", this) |> mustGenerateFilterInstance factoryCollection
            }
    
    type Api.Tokenizer with
        /// <summary>
        /// Tokenizer validator which checks both the input parameters and naming convention
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(factoryCollection : IFactoryCollection) = 
            maybe { 
                do! this.TokenizerName.ValidatePropertyValue("FilterName")
                do! ("FilterName", this) |> mustGenerateTokenizerInstance factoryCollection
            }
    
    type Api.AnalyzerProperties with
        /// <summary>
        /// Tokenizer validator which checks both the input parameters and naming convention
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(analyzerName : string, factoryCollection : IFactoryCollection) = 
            let rec loop (list : TokenFilter list) = 
                match list with
                | head :: tail -> 
                    match (head.Validate(factoryCollection)) with
                    | Choice1Of2(_) -> loop tail
                    | Choice2Of2(e) -> Choice2Of2(e)
                | [] -> Choice1Of2()
            maybe { 
                do! this.Tokenizer.Validate(factoryCollection)
                if this.Filters.Count = 0 then return! Choice2Of2(MessageConstants.ATLEAST_ONE_FILTER_REQUIRED)
                do! loop (List.ofSeq (this.Filters))
            }
    
    type Api.IndexConfiguration with
        /// <summary>
        /// Validator to validate index configuration
        /// </summary>
        member this.Validate() = 
            maybe { 
                do! ("CommitTimeSec", this.CommitTimeSec) |> greaterThanOrEqualTo 60
                do! ("RefreshTimeMilliSec", this.RefreshTimeMilliSec) |> greaterThanOrEqualTo 25
                do! ("RamBufferSizeMb", this.RamBufferSizeMb) |> greaterThanOrEqualTo 100
            }
    
    type Api.ScriptProperties with
        /// <summary>
        /// Validate Script properties
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(factoryCollection : IFactoryCollection) = 
            maybe { do! validate "ScriptSource" this.Source |> notNullAndEmpty }
    
    type Api.FieldProperties with
        /// <summary>
        /// Validates index field properties
        /// </summary>
        /// <param name="factoryCollection"></param>
        /// <param name="analyzers"></param>
        /// <param name="scripts"></param>
        /// <param name="propName"></param>
        member this.Validate (factoryCollection : IFactoryCollection) 
               (analyzers : Dictionary<string, AnalyzerProperties>) (scripts : Dictionary<string, ScriptProperties>) 
               (propName : string) = 
            maybe { 
                if String.IsNullOrWhiteSpace(this.ScriptName) <> true then 
                    do! this.ScriptName.ValidatePropertyValue("ScriptName")
                if scripts.ContainsKey(this.ScriptName) <> true then 
                    return! Choice2Of2
                                (OperationMessage.WithPropertyName(MessageConstants.SCRIPT_NOT_FOUND, this.ScriptName))
                match this.FieldType with
                | FieldType.Custom | FieldType.Highlight | FieldType.Text -> 
                    if String.IsNullOrWhiteSpace(this.SearchAnalyzer) <> true then 
                        if analyzers.ContainsKey(this.SearchAnalyzer) <> true then 
                            if factoryCollection.AnalyzerFactory.ModuleExists(this.SearchAnalyzer) <> true then 
                                return! Choice2Of2
                                            (OperationMessage.WithPropertyName
                                                 (MessageConstants.ANALYZER_NOT_FOUND, this.SearchAnalyzer))
                    if String.IsNullOrWhiteSpace(this.IndexAnalyzer) <> true then 
                        if analyzers.ContainsKey(this.IndexAnalyzer) <> true then 
                            if factoryCollection.AnalyzerFactory.ModuleExists(this.IndexAnalyzer) <> true then 
                                return! Choice2Of2
                                            (OperationMessage.WithPropertyName
                                                 (MessageConstants.ANALYZER_NOT_FOUND, this.IndexAnalyzer))
                | _ -> return! Choice1Of2()
            }
    
    type Api.Index with
        /// <summary>
        /// Validate Index properties
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(factoryCollection : IFactoryCollection) = 
            maybe { 
                do! this.IndexName.ValidatePropertyValue("IndexName")
                do! this.IndexConfiguration.Validate()
                do! iterExitOnFailure (Seq.toList (this.Analyzers)) (fun x -> 
                        maybe { 
                            do! x.Key.ValidatePropertyValue("AnalyzerName")
                            do! x.Value.Validate(x.Key, factoryCollection)
                        })
                do! iterExitOnFailure (Seq.toList (this.Scripts)) (fun x -> 
                        maybe { 
                            do! x.Key.ValidatePropertyValue("ScriptName")
                            do! x.Value.Validate(factoryCollection)
                        })
                do! iterExitOnFailure (Seq.toList (this.Fields)) (fun x -> 
                        maybe { 
                            do! x.Key.ValidatePropertyValue("FieldName")
                            do! x.Value.Validate (factoryCollection) (this.Analyzers) (this.Scripts) (x.Key)
                        })
                return! Choice1Of2()
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
//                value.SearchProfiles.ToArray() |> Array.iter(fun x ->
//                    validate "SearchProfileName" x.Key |> propertyNameValidator |> ignore
//                    validate "SearchProfileProperties" x.Value 
//                        |> SearchProfileValidator value.Fields |> ignore
//                )
