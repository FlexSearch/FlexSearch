// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexLucene.Analysis
open FlexLucene.Analysis.Standard
open FlexLucene.Codecs.Bloom
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Search
open FlexLucene.Search.Similarities
open FlexSearch.Core
open FlexSearch.Core.DictionaryHelpers
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Threading
open java.util
open System.Diagnostics
open System.Threading.Tasks

/// Builder related to creating Index Settings
[<AutoOpenAttribute>]
module IndexSettingBuilder = 
    open IndexSetting
    
    /// Builder object which will be passed around to build
    /// index setting
    type BuilderObject = 
        { Setting : IndexSetting.T }
    
    let withIndexName (indexName, path) = 
        Directory.CreateDirectory(path) |> ignore
        let setting = 
            { IndexName = indexName
              IndexAnalyzer = Unchecked.defaultof<_>
              SearchAnalyzer = Unchecked.defaultof<_>
              Fields = Unchecked.defaultof<_>
              //FieldsLookup = Unchecked.defaultof<_>
              SearchProfiles = Unchecked.defaultof<_>
              IndexConfiguration = Unchecked.defaultof<_>
              BaseFolder = path
              ShardConfiguration = Unchecked.defaultof<_> }
        { Setting = setting }
    
    let withShardConfiguration (conf) (build) = 
        { build with Setting = { build.Setting with ShardConfiguration = conf } }
    let withIndexConfiguration (conf) (build) = 
        { build with Setting = { build.Setting with IndexConfiguration = conf } }
    
    /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    /// indexing rather than the individual field analyzer
    let buildAnalyzer (fields : FieldCollection, isIndexAnalyzer : bool) = 
        let analyzer = new AnalyzerWrapper()
        analyzer.BuildAnalyzer(fields, isIndexAnalyzer)
        analyzer
    
    let withFields (fields : Field[], analyzerService, scriptService) (build) = 
        let ic = build.Setting.IndexConfiguration
        let resultLookup = new Dictionary<string, FieldSchema>(StringComparer.OrdinalIgnoreCase)
        let result = new FieldCollection()
        // Add system fields
        FieldSchema.getMetaFields () |> Seq.iter (fun x -> result.Add(x))
        for field in fields do
            let fieldObject = returnOrFail (FieldSchema.build (field, ic, analyzerService, scriptService))
            resultLookup.Add(field.FieldName, fieldObject)
            result.Add(fieldObject)
        // Perf: Intern all the field names in the string pool. This is done as the field names will be
        // used millions of times during execution.
        result |> Seq.iter (fun x -> 
                      String.Intern x.FieldName |> ignore
                      String.Intern x.SchemaName |> ignore)
        { build with Setting = 
                         { build.Setting with Fields = result
                                              SearchAnalyzer = buildAnalyzer (result, false)
                                              IndexAnalyzer = buildAnalyzer (result, true) } }
    
    /// Build search profiles from the Index object
    let withSearchProfiles (profiles : SearchQuery[], parser : IFlexParser) (build) = 
        let result = new Dictionary<string, Predicate * SearchQuery>(StringComparer.OrdinalIgnoreCase)
        for profile in profiles do
            let predicate = returnOrFail <| parser.Parse profile.QueryString
            result.Add(profile.QueryName, (predicate, profile))
        { build with Setting = { build.Setting with SearchProfiles = result } }
    
    /// Build the final index setting object
    let build (build) = 
        assert (notNull build.Setting.SearchProfiles)
        assert (notNull build.Setting.Fields)
        assert (notNull build.Setting.IndexConfiguration)
        assert (notNull build.Setting.ShardConfiguration)
        assert (notNull build.Setting.IndexAnalyzer)
        assert (notNull build.Setting.SearchAnalyzer)
        build.Setting

/// Builders related to creating Lucene IndexWriterConfig
module IndexWriterConfigBuilder = 
    /// Returns an instance of per field similarity provider 
    let getSimilarityProvider (s : IndexSetting.T) = 
        let defaultSimilarity = 
            Similarity.TFIDF
            |> FieldSimilarity.getLuceneT
            |> extract
        
        let mappings = new Dictionary<string, LuceneSimilarity>(StringComparer.OrdinalIgnoreCase)
        for field in s.Fields do
            // Only add if the format is not same as default postings format
            if field.Similarity <> Similarity.TFIDF then 
                let similarity = 
                    field.Similarity
                    |> FieldSimilarity.getLuceneT
                    |> extract
                mappings.Add(field.FieldName, similarity)
        new FieldSimilarity.Provider(mappings, defaultSimilarity)
    
    /// Build Index writer settings with the given index settings
    let buildWithSettings (s : IndexSetting.T) = 
        let iwc = new IndexWriterConfig(s.IndexAnalyzer)
        
        let codec = 
            s.IndexConfiguration.IndexVersion
            |> Codec.getCodec s.IndexConfiguration.UseBloomFilterForId
            |> extract
        
        let similarityProvider = s |> getSimilarityProvider
        iwc.SetCommitOnClose(s.IndexConfiguration.CommitOnClose) |> ignore
        iwc.SetOpenMode(IndexWriterConfigOpenMode.CREATE_OR_APPEND) |> ignore
        iwc.SetRAMBufferSizeMB(double s.IndexConfiguration.RamBufferSizeMb) |> ignore
        iwc.SetMaxBufferedDocs(s.IndexConfiguration.MaxBufferedDocs) |> ignore
        iwc.SetCodec(codec) |> ignore
        iwc.SetSimilarity(similarityProvider) |> ignore
        iwc
    
    /// Used for updating real time Index writer settings
    let updateWithSettings (s : IndexSetting.T) (iwc : LiveIndexWriterConfig) = 
        iwc.SetRAMBufferSizeMB(double s.IndexConfiguration.RamBufferSizeMb)
