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
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.Services
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Xml
open System.Xml.Linq
open Validator
open org.apache.lucene.analysis
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store


/// <summary>
/// Top level settings builder
/// </summary>
[<Sealed>]
type SettingsBuilder(factoryCollection : IFactoryCollection) = 
    interface ISettingsBuilder with
        member this.BuildSetting(index) = 
            maybe { 
                do! index.Validate(factoryCollection)
                let! analyzers = AnalyzerProperties.Build(this.Analyzers, factoryCollection)
                let! scriptManager = ScriptProperties.Build(this.Scripts, factoryCollection)
                let! fields = FieldProperties.Build(this.Fields, analyzers, this.Scripts, factoryCollection)
                let fieldsArray : FlexField array = Array.zeroCreate fields.Count
                fields.Values.CopyTo(fieldsArray, 0)
                let baseField = if index.IndexConfiguration.DirectoryType = DirectoryType.Ram then index.IndexName  else Constants.DataFolder + "\\" + index.IndexName
                let flexIndexSetting = 
                    { IndexName = index.IndexName
                        IndexAnalyzer = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, true)
                        SearchAnalyzer = FlexField.GetPerFieldAnalyzerWrapper(fieldsArray, false)
                        Fields = fieldsArray
                        SearchProfiles = searchProfiles
                        ScriptsManager = scriptsManager
                        FieldsLookup = fields
                        IndexConfiguration = index.IndexConfiguration
                        ShardConfiguration = index.ShardConfiguration
                        BaseFolder =  baseField}
                return flexIndexSetting
            }
