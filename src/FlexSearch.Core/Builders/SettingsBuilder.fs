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
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Api.Validation
open FlexSearch.Common
open FlexSearch.Core
open System.Collections.Generic
open org.apache.lucene.analysis

/// <summary>
/// Top level settings builder
/// </summary>
[<Sealed>]
type SettingsBuilder(factoryCollection : IFactoryCollection, serverSettings : ServerSettings) = 
    interface ISettingsBuilder with
        member this.BuildSetting(index : FlexSearch.Api.Index) = 
            maybe { 
                do! (index :> IValidator).MaybeValidator()
                let! defaultPostingsFormat = index.IndexConfiguration.IndexVersion.GetDefaultPostingsFormat()
                index.IndexConfiguration.DefaultIndexPostingsFormat <- defaultPostingsFormat
                let! analyzers = Analyzer.Build(index.Analyzers, factoryCollection)
                let! scriptManager = ScriptProperties.Build(index.Scripts, factoryCollection)
                let! fields = Field.Build
                                  (index.Fields, index.IndexConfiguration, analyzers, index.Scripts, factoryCollection)
                let fieldsArray : FlexField array = Array.zeroCreate fields.Count
                fields.Values.CopyTo(fieldsArray, 0)
                let baseFolder = serverSettings.DataFolder + "\\" + index.IndexName
                let indexAnalyzer = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, true)
                let searchAnalyzer = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, false)
                let! searchProfiles = FlexSearch.Api.SearchQuery.Build
                                          (index.SearchProfiles, fields, 
                                           FlexSearch.Api.SearchQuery.QueryTypes(factoryCollection), 
                                           new Parsers.FlexParser())
                let flexIndexSetting = 
                    { IndexName = index.IndexName
                      IndexAnalyzer = indexAnalyzer
                      SearchAnalyzer = searchAnalyzer
                      Fields = fieldsArray
                      SearchProfiles = searchProfiles
                      ScriptsManager = scriptManager
                      FieldsLookup = fields
                      IndexConfiguration = index.IndexConfiguration
                      ShardConfiguration = index.ShardConfiguration
                      BaseFolder = baseFolder }
                return flexIndexSetting
            }
