﻿// ----------------------------------------------------------------------------
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

open FlexSearch.Api
open FlexSearch.Core
open System.Collections.Generic
open System
open Validator
open FlexSearch.Api.Message
open org.apache.lucene.analysis

[<AutoOpen>]
module FieldPropertiesExtensions = 
    let private keyWordAnalyzer = new CaseInsensitiveKeywordAnalyzer()
    
    /// <summary>
    /// FieldType to be used for ID fields
    /// </summary>
    let GetIdField(configuration: IndexConfiguration) = 
        let indexInformation = 
            { Index = true
              Tokenize = false
              FieldTermVector = FieldTermVector.DoNotStoreTermVector
              FieldIndexOptions = FieldIndexOptions.DocsOnly }
        
        let idField = 
            { FieldName = Constants.IdField
              FieldType = FlexCustom(keyWordAnalyzer, keyWordAnalyzer, indexInformation)
              FieldInformation = None
              Source = None
              PostingsFormat = configuration.IdFieldPostingsFormat
              Similarity = FieldSimilarity.TFIDF
              StoreInformation = FieldStoreInformation.Create(false, true)
              RequiresAnalyzer = true
              DefaultField = null }
        
        idField
    
    /// <summary>
    /// FieldType to be used for ID fields
    /// </summary>
    let GetTimeStampField(configuration: IndexConfiguration) = 
        let idField = 
            { FieldName = Constants.LastModifiedField
              FieldType = FlexDateTime
              FieldInformation = None
              Source = None
              PostingsFormat = configuration.DefaultIndexPostingsFormat
              Similarity = configuration.DefaultFieldSimilarity
              StoreInformation = FieldStoreInformation.Create(false, true)
              RequiresAnalyzer = false
              DefaultField = null }
        idField
    
    type FieldProperties with
        
        /// <summary>
        /// Validates index field properties
        /// </summary>
        /// <param name="factoryCollection"></param>
        /// <param name="analyzers"></param>
        /// <param name="scripts"></param>
        /// <param name="propName"></param>
        member this.Validate(factoryCollection : IFactoryCollection, analyzers : Dictionary<string, AnalyzerProperties>, 
                             scripts : Dictionary<string, ScriptProperties>, propName : string) = 
            maybe { 
                if String.IsNullOrWhiteSpace(this.ScriptName) <> true then 
                    do! this.ScriptName.ValidatePropertyValue("ScriptName")
                    if scripts.ContainsKey(this.ScriptName) <> true then 
                        return! Choice2Of2(MessageConstants.SCRIPT_NOT_FOUND |> Append("Script Name", this.ScriptName))
                match this.FieldType with
                | FieldType.Custom | FieldType.Highlight | FieldType.Text -> 
                    if String.IsNullOrWhiteSpace(this.SearchAnalyzer) <> true then 
                        if analyzers.ContainsKey(this.SearchAnalyzer) <> true then 
                            if factoryCollection.AnalyzerFactory.ModuleExists(this.SearchAnalyzer) <> true then 
                                return! Choice2Of2
                                            (MessageConstants.ANALYZER_NOT_FOUND 
                                             |> Append("Analyzer Name", this.SearchAnalyzer))
                    if String.IsNullOrWhiteSpace(this.IndexAnalyzer) <> true then 
                        if analyzers.ContainsKey(this.IndexAnalyzer) <> true then 
                            if factoryCollection.AnalyzerFactory.ModuleExists(this.IndexAnalyzer) <> true then 
                                return! Choice2Of2
                                            (MessageConstants.ANALYZER_NOT_FOUND 
                                             |> Append("Analyzer Name", this.SearchAnalyzer))
                | _ -> return! Choice1Of2()
            }
        
        /// <summary>
        /// Build method to generate FlexField from Index Properties
        /// </summary>
        /// <param name="flexAnalyzers"></param>
        /// <param name="scripts"></param>
        /// <param name="factoryCollection"></param>
        /// <param name="propName"></param>
        member this.Build(flexAnalyzers : Dictionary<string, Analyzer>, scripts : Dictionary<string, ScriptProperties>, 
                          factoryCollection : IFactoryCollection, propName : string) = 
            /// Helper to get an analyzer from dictionary and if not found then
            /// tries to resolve from the dictionary
            let getAnalyzer (analyzerName) = 
                // First try finding it in the same configuration file
                match flexAnalyzers.TryGetValue(analyzerName) with
                | (true, analyzer) -> Choice1Of2(analyzer)
                | _ -> factoryCollection.AnalyzerFactory.GetModuleByName(analyzerName)
            
            let getSource (field : FieldProperties) = 
                if (String.IsNullOrWhiteSpace(field.ScriptName)) then Choice1Of2(None)
                else 
                    match scripts.TryGetValue(field.ScriptName) with
                    | (true, a) -> 
                        match GenerateStringReturnScript(a.Source) with
                        | Choice1Of2(x) -> Choice1Of2(Some(x))
                        | Choice2Of2(e) -> Choice2Of2(e)
                    | _ -> Choice2Of2(MessageConstants.SCRIPT_NOT_FOUND |> Append("Script Name", field.ScriptName))
            
            let getFieldType (field : FieldProperties) = 
                maybe { 
                    match field.FieldType with
                    | FieldType.Int -> return! Choice1Of2(FlexInt, false)
                    | FieldType.Double -> return! Choice1Of2(FlexDouble, false)
                    | FieldType.Bool -> return! Choice1Of2(FlexBool(keyWordAnalyzer), true)
                    | FieldType.Date -> return! Choice1Of2(FlexDate, false)
                    | FieldType.DateTime -> return! Choice1Of2(FlexDateTime, false)
                    | FieldType.Stored -> return! Choice1Of2(FlexStored, false)
                    | FieldType.ExactText -> return! Choice1Of2(FlexExactText(keyWordAnalyzer), true)
                    | FieldType.Text | FieldType.Highlight | FieldType.Custom -> 
                        let! searchAnalyzer = getAnalyzer (field.SearchAnalyzer)
                        let! indexAnalyzer = getAnalyzer (field.IndexAnalyzer)
                        match field.FieldType with
                        | FieldType.Text -> return! Choice1Of2(FlexText(searchAnalyzer, indexAnalyzer), true)
                        | FieldType.Highlight -> return! Choice1Of2(FlexHighlight(searchAnalyzer, indexAnalyzer), true)
                        | FieldType.Custom -> 
                            let indexingInformation = 
                                { Index = field.Index
                                  Tokenize = field.Analyze
                                  FieldTermVector = field.TermVector
                                  FieldIndexOptions = field.IndexOptions }
                            return! Choice1Of2(FlexCustom(searchAnalyzer, indexAnalyzer, indexingInformation), true)
                        | _ -> 
                            return! Choice2Of2
                                        (MessageConstants.ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE 
                                         |> Append("Field Type", field.FieldType.ToString()))
                    | _ -> 
                        return! Choice2Of2
                                    (MessageConstants.UNKNOWN_FIELD_TYPE 
                                     |> Append("Field Type", field.FieldType.ToString()))
                }
            
            let getField (field : KeyValuePair<string, FieldProperties>) = 
                maybe { 
                    let! source = getSource (field.Value)
                    let! (fieldType, requiresAnalyzer) = getFieldType (field.Value)
                    let fieldDummy = 
                        { FieldName = field.Key
                          FieldType = fieldType
                          FieldInformation = None
                          Source = source
                          PostingsFormat = field.Value.PostingsFormat
                          Similarity = field.Value.Similarity
                          StoreInformation = FieldStoreInformation.Create(false, field.Value.Store)
                          RequiresAnalyzer = requiresAnalyzer
                          DefaultField = null }
                    
                    let fieldFinal = { fieldDummy with DefaultField = FlexField.CreateDefaultLuceneField fieldDummy }
                    return fieldFinal
                }
            
            getField (new KeyValuePair<string, FieldProperties>(propName, this))
        
        /// <summary>
        /// Generate FlexField dictionary from Fields dictionary
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="flexAnalyzers"></param>
        /// <param name="scripts"></param>
        /// <param name="factoryCollection"></param>
        static member Build(fields : Dictionary<string, FieldProperties>, indexConfiguration: IndexConfiguration, flexAnalyzers : Dictionary<string, Analyzer>, 
                            scripts : Dictionary<string, ScriptProperties>, factoryCollection : IFactoryCollection) = 
            maybe { 
                let result = new Dictionary<string, FlexField>(StringComparer.OrdinalIgnoreCase)
                // Add system fields
                result.Add(Constants.IdField, GetIdField(indexConfiguration))
                result.Add(Constants.LastModifiedField, GetTimeStampField(indexConfiguration))
                for field in fields do
                    let! fieldObject = field.Value.Build(flexAnalyzers, scripts, factoryCollection, field.Key)
                    result.Add(field.Key, fieldObject)
                return result
            }
