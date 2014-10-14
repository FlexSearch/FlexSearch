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

open FlexSearch.Api
open FlexSearch.Core
open System.Collections.Generic
open System
open org.apache.lucene.analysis
open FlexSearch.Common

[<AutoOpen>]
module FieldPropertiesExtensions = 
    let private keyWordAnalyzer = new CaseInsensitiveKeywordAnalyzer()
    
    /// <summary>
    /// FieldType to be used for ID fields
    /// </summary>
    let GetIdField(configuration : IndexConfiguration) = 
        let indexInformation = 
            { Index = true
              Tokenize = false
              FieldTermVector = FieldTermVector.DoNotStoreTermVector
              FieldIndexOptions = FieldIndexOptions.DocsOnly }
        
        let idField = 
            { FieldName = Constants.IdField
              SchemaName = 
                  sprintf "%s<%s>" Constants.IdField 
                      (configuration.IdFieldPostingsFormat.ToString().ToLowerInvariant())
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
    let GetTimeStampField(configuration : IndexConfiguration) = 
        let idField = 
            { FieldName = Constants.LastModifiedField
              SchemaName = 
                  sprintf "%s<%s>" Constants.LastModifiedField 
                      (configuration.DefaultIndexPostingsFormat.ToString().ToLowerInvariant())
              FieldType = FlexDateTime
              FieldInformation = None
              Source = None
              PostingsFormat = configuration.DefaultIndexPostingsFormat
              Similarity = configuration.DefaultFieldSimilarity
              StoreInformation = FieldStoreInformation.Create(false, true)
              RequiresAnalyzer = false
              DefaultField = null }
        idField

    /// <summary>
    /// FieldType to be used for ID fields
    /// </summary>
    let GetTimeStampDvField(configuration : IndexConfiguration) = 
        let idField = 
            { FieldName = Constants.LastModifiedFieldDv
              SchemaName = 
                  sprintf "%s<%s>" Constants.LastModifiedFieldDv
                      (configuration.DefaultIndexPostingsFormat.ToString().ToLowerInvariant())
              FieldType = FlexDateTime
              FieldInformation = None
              Source = None
              PostingsFormat = configuration.DefaultIndexPostingsFormat
              Similarity = configuration.DefaultFieldSimilarity
              StoreInformation = FieldStoreInformation.Create(false, true)
              RequiresAnalyzer = false
              DefaultField = null }
        idField    
    type Field with
        
        /// <summary>
        /// Build method to generate FlexField from Index Properties
        /// </summary>
        /// <param name="flexAnalyzers"></param>
        /// <param name="scripts"></param>
        /// <param name="factoryCollection"></param>
        /// <param name="propName"></param>
        member this.Build(flexAnalyzers : Dictionary<string, Analyzer>, scripts : List<Script>, 
                          factoryCollection : IFactoryCollection, propName : string) = 
            /// Helper to get an analyzer from dictionary and if not found then
            /// tries to resolve from the dictionary
            let getAnalyzer (analyzerName) = 
                // First try finding it in the same configuration file
                match flexAnalyzers.TryGetValue(analyzerName) with
                | (true, analyzer) -> Choice1Of2(analyzer)
                | _ -> 
                    match factoryCollection.AnalyzerFactory.GetModuleByName(analyzerName) with
                    | Choice1Of2(analyzer) -> Choice1Of2(analyzer)
                    | Choice2Of2(error) -> 
                        Choice2Of2(error
                                   |> Append("Reason", Errors.ANALYZER_NOT_FOUND)
                                   |> Append("AnalyzerName", analyzerName))
            
            let getSource (field : Field) = 
                if (String.IsNullOrWhiteSpace(field.ScriptName)) then Choice1Of2(None)
                else 
                    match scripts.Find(fun x -> x.ScriptName = field.ScriptName) with
                    | script -> 
                        match GenerateStringReturnScript(script.Source) with
                        | Choice1Of2(x) -> Choice1Of2(Some(x))
                        | Choice2Of2(e) -> Choice2Of2(e)
                    | _ -> 
                        Choice2Of2(Errors.SCRIPT_NOT_FOUND
                                   |> GenerateOperationMessage
                                   |> Append("ScriptName", field.ScriptName))
            
            let getFieldType (field : Field) = 
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
                            return! Choice2Of2(Errors.ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE
                                               |> GenerateOperationMessage
                                               |> Append("FieldType", field.FieldType.ToString()))
                    | _ -> 
                        return! Choice2Of2(Errors.UNKNOWN_FIELD_TYPE
                                           |> GenerateOperationMessage
                                           |> Append("FieldType", field.FieldType.ToString()))
                }
            
            let getField (field : KeyValuePair<string, Field>) = 
                maybe { 
                    let! source = getSource (field.Value)
                    let! (fieldType, requiresAnalyzer) = getFieldType (field.Value)
                    let fieldDummy = 
                        { FieldName = field.Key
                          SchemaName = 
                              sprintf "%s<%s>" field.Key (field.Value.PostingsFormat.ToString().ToLowerInvariant())
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
            
            getField (new KeyValuePair<string, Field>(propName, this))
        
        /// <summary>
        /// Generate FlexField dictionary from Fields dictionary
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="flexAnalyzers"></param>
        /// <param name="scripts"></param>
        /// <param name="factoryCollection"></param>
        static member Build(fields : List<Field>, indexConfiguration : IndexConfiguration, 
                            flexAnalyzers : Dictionary<string, Analyzer>, scripts : List<Script>, 
                            factoryCollection : IFactoryCollection) = 
            maybe { 
                let result = new Dictionary<string, FlexField>(StringComparer.OrdinalIgnoreCase)
                // Add system fields
                result.Add(Constants.IdField, GetIdField(indexConfiguration))
                result.Add(Constants.LastModifiedField, GetTimeStampField(indexConfiguration))
                result.Add(Constants.LastModifiedFieldDv, GetTimeStampDvField(indexConfiguration))
                for field in fields do
                    let! fieldObject = field.Build(flexAnalyzers, scripts, factoryCollection, field.FieldName)
                    result.Add(field.FieldName, fieldObject)
                return result
            }
