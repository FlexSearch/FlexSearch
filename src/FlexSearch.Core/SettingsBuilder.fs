// ----------------------------------------------------------------------------
// Flexsearch settings (Settings.fs)
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
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Xml
open System.Xml.Linq
open org.apache.lucene.analysis
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

// ----------------------------------------------------------------------------
// Top level settings parse function   
// ----------------------------------------------------------------------------   
module SettingsBuilder = 
    let private keyWordAnalyzer = new CaseInsensitiveKeywordAnalyzer()
    
    /// Convert Api field objects to domain flex fields  
    let private buildFields (fieldsDict : Dictionary<string, FieldProperties>, 
                             flexAnalyzers : Dictionary<string, Analyzer>, 
                             scripts : Dictionary<string, ScriptProperties>, factoryCollection : IFactoryCollection) = 
        maybe { 
            /// Utility function to get analyzer by name  
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
                        match factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript(a) with
                        | Choice1Of2(x) -> Choice1Of2(Some(x.Execute))
                        | Choice2Of2(e) -> Choice2Of2(e)
                    | _ -> 
                        Choice2Of2
                            (OperationMessage.WithPropertyName(MessageConstants.SCRIPT_NOT_FOUND, field.ScriptName))
            
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
                        let fieldAnalyzers : FieldAnalyzers = 
                            { SearchAnalyzer = searchAnalyzer
                              IndexAnalyzer = indexAnalyzer }
                        match field.FieldType with
                        | FieldType.Text -> return! Choice1Of2(FlexText(fieldAnalyzers), true)
                        | FieldType.Highlight -> return! Choice1Of2(FlexHighlight(fieldAnalyzers), true)
                        | FieldType.Custom -> 
                            let indexingInformation = 
                                { Index = field.Index
                                  Tokenize = field.Analyze
                                  FieldTermVector = field.TermVector }
                            return! Choice1Of2(FlexCustom(fieldAnalyzers, indexingInformation), true)
                        | _ -> 
                            return! Choice2Of2
                                        (OperationMessage.WithPropertyName
                                             (MessageConstants.ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE, 
                                              field.FieldType.ToString()))
                    | _ -> 
                        return! Choice2Of2
                                    (OperationMessage.WithPropertyName
                                         (MessageConstants.UNKNOWN_FIELD_TYPE, field.FieldType.ToString()))
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
                          StoreInformation = FieldStoreInformation.Create(false, field.Value.Store)
                          RequiresAnalyzer = requiresAnalyzer
                          DefaultField = null }
                    
                    let fieldFinal = { fieldDummy with DefaultField = FlexField.CreateDefaultLuceneField fieldDummy }
                    return fieldFinal
                }
            
            let result = new Dictionary<string, FlexField>(StringComparer.OrdinalIgnoreCase)
            let! fields = mapExitOnFailure (Seq.toList (fieldsDict)) getField
            fields |> Seq.iter (fun x -> result.Add(x.FieldName, x))
            [| (// The below 3 fields are only added for searching here
                Constants.IdField, FlexExactText(keyWordAnalyzer))
               (Constants.TypeField, FlexExactText(keyWordAnalyzer))
               (Constants.LastModifiedField, FlexDateTime) |]
            |> Array.iter (fun x -> 
                   let (name, fieldType) = x
                   
                   let dummy = 
                       { FieldName = name
                         FieldType = fieldType
                         FieldInformation = None
                         Source = None
                         StoreInformation = FieldStoreInformation.Create(false, true)
                         RequiresAnalyzer = true
                         DefaultField = null }
                   
                   let field = { dummy with DefaultField = FlexField.CreateDefaultLuceneField dummy }
                   result.Add(name, field))
            return result
        }
    
    // ----------------------------------------------------------------------------
    // Build analyzer definition for flexindexsettings from api analyzers
    // ----------------------------------------------------------------------------
    let private buildAnalyzers (analyzersDict : Dictionary<string, FlexSearch.Api.AnalyzerProperties>, 
                                factoryCollection : IFactoryCollection) = 
        maybe { 
            let getFilter (filterProperties : FlexSearch.Api.TokenFilter) = 
                maybe { 
                    let! tokenizerFactory = factoryCollection.FilterFactory.GetModuleByName(filterProperties.FilterName)
                    tokenizerFactory.Initialize(filterProperties.Parameters, factoryCollection.ResourceLoader) |> ignore
                    return tokenizerFactory
                }
            
            let getAnalyzer (analyzer : KeyValuePair<string, AnalyzerProperties>) = 
                maybe { 
                    let! tokenizerFactory = factoryCollection.TokenizerFactory.GetModuleByName
                                                (analyzer.Value.Tokenizer.TokenizerName)
                    tokenizerFactory.Initialize(analyzer.Value.Tokenizer.Parameters, factoryCollection.ResourceLoader) 
                    |> ignore
                    let! filters = mapExitOnFailure (Seq.toList (analyzer.Value.Filters)) getFilter
                    return (analyzer.Key, 
                            
                            new CustomAnalyzer(tokenizerFactory, filters.ToArray()) :> org.apache.lucene.analysis.Analyzer)
                }
            
            let result = new Dictionary<string, Analyzer>(StringComparer.OrdinalIgnoreCase)
            let! analyzers = mapExitOnFailure (Seq.toList (analyzersDict)) getAnalyzer
            analyzers |> Seq.iter (fun x -> 
                             let (key, value) = x
                             result.Add(key, value))
            return result
        }
    
    /// Build all the scripts for the index
    let private getScriptsManager (scripts : Dictionary<string, ScriptProperties>, 
                                   factoryCollection : IFactoryCollection) = 
        maybe { 
            let getScript (script : KeyValuePair<string, ScriptProperties>) = 
                maybe { 
                    match script.Value.ScriptType with
                    | ScriptType.SearchProfileSelector -> 
                        let! compiledScript = factoryCollection.ScriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript
                                                  (script.Value)
                        return (script.Key, script.Value.ScriptType, compiledScript.Execute)
                    | ScriptType.ComputedField -> let! compiledScript = factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript
                                                                            (script.Value)
                                                  return (script.Key, script.Value.ScriptType, compiledScript.Execute)
                    | _ -> 
                        return! Choice2Of2
                                    (OperationMessage.WithPropertyName
                                         (MessageConstants.UNKNOWN_SCRIPT_TYPE, script.Value.ScriptType.ToString()))
                }
            
            let profileSelectorScripts = 
                new Dictionary<string, IReadOnlyDictionary<string, string> -> string>(StringComparer.OrdinalIgnoreCase)
            let computedFieldScripts = 
                new Dictionary<string, IReadOnlyDictionary<string, string> -> string>(StringComparer.OrdinalIgnoreCase)
            let customScoringScripts = 
                new Dictionary<string, IReadOnlyDictionary<string, string> * double -> double>(StringComparer.OrdinalIgnoreCase)
            let! scripts = mapExitOnFailure (Seq.toList (scripts)) getScript
            scripts |> Seq.iter (fun x -> 
                           let (key, scriptType, value) = x
                           match scriptType with
                           | ScriptType.ComputedField -> computedFieldScripts.Add(key, value)
                           | ScriptType.SearchProfileSelector -> profileSelectorScripts.Add(key, value)
                           | _ -> failwith "not possible")
            let scriptsManager = 
                { ComputedFieldScripts = computedFieldScripts
                  ProfileSelectorScripts = profileSelectorScripts
                  CustomScoringScripts = customScoringScripts }
            return scriptsManager
        }
    
    // ----------------------------------------------------------------------------
    // Top level settings builder   
    // ----------------------------------------------------------------------------   
    let public SettingsBuilder (factoryCollection : IFactoryCollection) (indexValidator : IIndexValidator) = 
        { new ISettingsBuilder with
              member x.BuildSetting(index) = 
                  maybe { 
                      do! indexValidator.Validate(index)
                      let! analyzers = buildAnalyzers (index.Analyzers, factoryCollection)
                      let! fields = buildFields (index.Fields, analyzers, index.Scripts, factoryCollection)
                      let fieldsArray : FlexField array = Array.zeroCreate fields.Count
                      fields.Values.CopyTo(fieldsArray, 0)
                      let! scriptsManager = getScriptsManager (index.Scripts, factoryCollection)
                      let flexIndexSetting = 
                          { IndexName = index.IndexName
                            IndexAnalyzer = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, true)
                            SearchAnalyzer = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, false)
                            Fields = fieldsArray
                            SearchProfiles = index.SearchProfiles
                            ScriptsManager = scriptsManager
                            FieldsLookup = fields
                            IndexConfiguration = index.IndexConfiguration
                            ShardConfiguration = index.ShardConfiguration
                            BaseFolder = 
                                if index.IndexConfiguration.DirectoryType = DirectoryType.Ram then index.IndexName
                                else Constants.DataFolder.Value + "\\" + index.IndexName }
                      return flexIndexSetting
                  } }
