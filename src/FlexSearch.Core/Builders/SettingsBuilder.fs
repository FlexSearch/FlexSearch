﻿// ----------------------------------------------------------------------------
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
open FlexSearch.Core

/// <summary>
/// Top level settings builder
/// </summary>
[<Sealed>]
type SettingsBuilder(factoryCollection : IFactoryCollection) = 
    interface ISettingsBuilder with
        member this.BuildSetting(index) = 
            maybe { 
                do! index.Validate(factoryCollection)
                let! analyzers = AnalyzerProperties.Build(index.Analyzers, factoryCollection)
                let! scriptManager = ScriptProperties.Build(index.Scripts, factoryCollection)
                let! fields = FieldProperties.Build(index.Fields, index.IndexConfiguration, analyzers, index.Scripts, factoryCollection)
                let fieldsArray : FlexField array = Array.zeroCreate fields.Count
                fields.Values.CopyTo(fieldsArray, 0)
                let baseFolder = 
                    if index.IndexConfiguration.DirectoryType = DirectoryType.Ram then index.IndexName
                    else Constants.DataFolder + "\\" + index.IndexName
                
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