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

open FSharp.Data
open FlexSearch.Utility
open FlexSearch.Api.Types
open FlexSearch.Analysis.Analyzers
open FlexSearch.Core
open FlexSearch.Core.Index
open ServiceStack.FluentValidation

open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.analysis
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.store
open org.apache.lucene.facet.search
open org.apache.lucene.search

open ServiceStack.Logging

open System
open System.IO
open System.Collections.Generic
open System.Diagnostics
open System.Xml
open System.Xml.Linq

// ----------------------------------------------------------------------------
// Top level settings parse function   
// ----------------------------------------------------------------------------   
module SettingsBuilder = 

    let private keyWordAnalyzer = new CaseInsensitiveKeywordAnalyzer()

    // ----------------------------------------------------------------------------
    // Convert Api field objects to domain flex fields 
    // ----------------------------------------------------------------------------  
    let buildFields(fieldsDict : Dictionary<string, IndexFieldProperties>, flexAnalyzers: Dictionary<string, Analyzer>, scripts: Dictionary<string, ScriptProperties>, factoryCollection: IFactoryCollection) =
        
        // ----------------------------------------------------------------------------
        // Utility function to get analyzer by name
        // ----------------------------------------------------------------------------
        let getAnalyzer(analyzerName) =
            // First try finding it in the same configuration file
            match flexAnalyzers.TryGetValue(analyzerName) with
            | (true, analyzer) -> analyzer
            | _ -> 
                match factoryCollection.AnalyzerFactory.GetModuleByName(analyzerName) with
                | Some(analyzer) -> analyzer
                | _ -> failwithf "The configured analyzer does not exist: %s" analyzerName


        let result = new Dictionary<string, FlexField>(StringComparer.OrdinalIgnoreCase)
        for field in fieldsDict do
            let source = 
                if(String.IsNullOrWhiteSpace(field.Value.ScriptName)) then 
                    None
                else
                    match scripts.TryGetValue(field.Value.ScriptName) with
                    | (true, a) -> 
                        Some(factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript(a).Execute)
                    | _ -> None
            
            let (fieldType, requiresAnalyzer) =
                match field.Value.FieldType with
                | FieldType.Int -> (FlexInt, false)
                | FieldType.Double -> (FlexDouble, false)
                | FieldType.Bool -> (FlexBool(keyWordAnalyzer), true)
                | FieldType.Date -> (FlexDate, false)            
                | FieldType.DateTime -> (FlexDateTime, false)
                | FieldType.Stored -> (FlexStored, false)
                | FieldType.ExactText -> (FlexExactText(keyWordAnalyzer), true)
                | FieldType.Text 
                | FieldType.Highlight 
                | FieldType.Custom ->  
                    let fieldAnalyzers : FieldAnalyzers = 
                        {
                            SearchAnalyzer = getAnalyzer(field.Value.SearchAnalyzer) 
                            IndexAnalyzer = getAnalyzer(field.Value.IndexAnalyzer)
                        }
                    
                    match field.Value.FieldType with
                    | FieldType.Text -> (FlexText(fieldAnalyzers), true)
                    | FieldType.Highlight -> (FlexHighlight(fieldAnalyzers), true)
                    | FieldType.Custom ->  
                        let indexingInformation = {Index = field.Value.Index; Tokenize = field.Value.Analyze; FieldTermVector = field.Value.FieldTermVector}
                        (FlexCustom(fieldAnalyzers, indexingInformation), true)
                    | _ -> failwithf ""
                            
                | _ -> failwithf "Unknow field type in settings builder" 


            let fieldDummy = 
                {
                    FieldName = field.Key
                    FieldType = fieldType
                    FieldInformation = None
                    Source = source
                    StoreInformation = FieldStoreInformation.Create(false, field.Value.Store)
                    RequiresAnalyzer = requiresAnalyzer
                    DefaultField = null
                }
            let fieldFinal = { fieldDummy with DefaultField = FlexField.CreateDefaultLuceneField fieldDummy }
            result.Add(field.Key, fieldFinal)


        // The below 3 fields are only added for searching here
        [|("id", FlexExactText(keyWordAnalyzer)); ("type", FlexExactText(keyWordAnalyzer)); ("lastmodified", FlexDateTime)|]
            |> Array.iter(fun x ->
                let (name, fieldType) = x
                let dummy = 
                    {
                        FieldName = name
                        FieldType = fieldType
                        FieldInformation = None
                        Source = None
                        StoreInformation = FieldStoreInformation.Create(false, true)
                        RequiresAnalyzer = true
                        DefaultField = null
                    }
                let field = { dummy with DefaultField = FlexField.CreateDefaultLuceneField dummy }
                result.Add(name, field)
            )
        
        result


    // ----------------------------------------------------------------------------
    // Build analyzer definition for flexindexsettings from api analyzers
    // ----------------------------------------------------------------------------
    let buildAnalyzers(analyzersDict : Dictionary<string, FlexSearch.Api.Types.AnalyzerProperties>, factoryCollection: IFactoryCollection) =
        let result = new Dictionary<string, Analyzer>(StringComparer.OrdinalIgnoreCase)
        for analyzer in analyzersDict do
            let tokenizer =
                match factoryCollection.TokenizerFactory.GetModuleByName(analyzer.Value.TokenizerName) with
                | Some(a)   -> a
                | _         -> failwithf "The specified tokenizer is not valid: %s" analyzer.Value.TokenizerName 

            let filters = new ResizeArray<IFlexFilterFactory>()

            for filter in analyzer.Value.Filters do
                match factoryCollection.FilterFactory.GetModuleByName(filter.FilterName) with
                | Some(a) -> 
                    if filter.Parameters = null then 
                        filter.Parameters <- new Dictionary<string,string>()                        
                    a.Initialize(filter.Parameters) |> ignore
                    filters.Add(a)
                | _  -> failwithf "The specified filter is not valid: %s" filter.FilterName 

            result.Add(analyzer.Key, new FlexSearch.Analysis.CustomAnalyzer(tokenizer, filters.ToArray()) :> org.apache.lucene.analysis.Analyzer)

        result


    // ----------------------------------------------------------------------------
    // Build all the scripts for the index
    // ----------------------------------------------------------------------------
    let getScriptsManager(scripts : Dictionary<string, ScriptProperties>, factoryCollection: IFactoryCollection) =
        let profileSelectorScripts = new Dictionary<string, (IReadOnlyDictionary<string, string> -> string)>(StringComparer.OrdinalIgnoreCase)
        let customScoringScripts = new Dictionary<string, (IReadOnlyDictionary<string, string> * double -> double)>(StringComparer.OrdinalIgnoreCase)
        for script in scripts do
            match script.Value.ScriptType with
            | ScriptType.SearchProfileSelector ->
                profileSelectorScripts.Add(script.Key, factoryCollection.ScriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript(script.Value).Execute)
            | ScriptType.CustomScoring ->
                customScoringScripts.Add(script.Key, factoryCollection.ScriptFactoryCollection.CustomScoringScriptFactory.CompileScript(script.Value).Execute)
            | _ -> ()

        let scriptsManager = {ProfileSelectorScripts = profileSelectorScripts ; CustomScoringScripts = customScoringScripts}
        scriptsManager


    // ----------------------------------------------------------------------------
    // Top level settings builder   
    // ----------------------------------------------------------------------------   
    let public SettingsBuilder(factoryCollection: IFactoryCollection, indexValidator: AbstractValidator<Index>) =  {
        new ISettingsBuilder with
            member x.BuildSetting (index) =
                indexValidator.ValidateAndThrow(index)
                let analyzers = buildAnalyzers(index.Analyzers, factoryCollection)
                let fields =  buildFields(index.Fields, analyzers, index.Scripts, factoryCollection) 
                let fieldsArray: FlexField array = Array.zeroCreate fields.Count
                fields.Values.CopyTo(fieldsArray, 0);

                let scriptsManager = getScriptsManager(index.Scripts, factoryCollection)

                let flexIndexSetting = 
                    {
                        IndexName               = index.IndexName
                        IndexAnalyzer           = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, true)
                        SearchAnalyzer          = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, false)
                        Fields                  = fieldsArray
                        SearchProfiles          = index.SearchProfiles
                        ScriptsManager          = scriptsManager
                        FieldsLookup            = fields
                        IndexConfig             = index.Configuration
                        BaseFolder              = 
                            if index.Configuration.DirectoryType = DirectoryType.Ram then
                                index.IndexName
                            else
                                Constants.DataFolder.Value + "\\" + index.IndexName
                    }

                flexIndexSetting    
        }    