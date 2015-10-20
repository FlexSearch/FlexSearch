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
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Threading
open java.util
open System.Diagnostics
open System.Threading.Tasks

/// The types of events which can be raised on the event aggregrator
type EventType = 
    | ShardStatusChange of indexName : string * shardNo : int * shardStatus : ShardStatus
    | IndexStatusChange of indexName : string * indexStatus : IndexStatus
    | RegisterForShutdownCallback of service : IRequireNotificationForShutdown

/// A multi-purpose event aggregrator pipeline for raising and subscribing to server
/// event in a decoupled manner
type EventAggregrator() = 
    let event = new Event<EventType>()
    member __.Event() = event.Publish
    member __.Push(e : EventType) = event.Trigger(e)

type FieldsMeta = 
    { IdField : Field.T
      TimeStampField : Field.T
      ModifyIndex : Field.T
      Fields : Field.T []
      Lookup : IReadOnlyDictionary<string, Field.T> }

type AnalyzerWrapper(?defaultAnalyzer0 : LuceneAnalyzer) = 
    inherit DelegatingAnalyzerWrapper(Analyzer.PER_FIELD_REUSE_STRATEGY)
    let mutable map = conDict<LuceneAnalyzer>()
    let defaultAnalyzer = defaultArg defaultAnalyzer0 (new StandardAnalyzer() :> LuceneAnalyzer)
    
    /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    /// indexing rather than the individual field analyzer
    member __.BuildAnalyzer(fields : Field.T [], isIndexAnalyzer : bool) = 
        let analyzerMap = conDict<LuceneAnalyzer>()
        analyzerMap.[MetaFields.IdField] <- CaseInsensitiveKeywordAnalyzer
        analyzerMap.[MetaFields.LastModifiedField] <- CaseInsensitiveKeywordAnalyzer
        fields 
        |> Array.iter 
               (fun x -> 
               if isIndexAnalyzer then 
                   match x.FieldType with
                   | FieldType.Custom(a, b, c) -> analyzerMap |> add (x.SchemaName, b)
                   | FieldType.Highlight(a, b) -> analyzerMap |> add (x.SchemaName, b)
                   | FieldType.Text(a, b) -> analyzerMap |> add (x.SchemaName, b)
                   | FieldType.ExactText(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.Bool(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.Date | FieldType.DateTime | FieldType.Int | FieldType.Double | FieldType.Stored | FieldType.Long -> 
                       ()
               else 
                   match x.FieldType with
                   | FieldType.Custom(a, b, c) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.Highlight(a, _) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.Text(a, _) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.ExactText(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.Bool(a) -> analyzerMap |> add (x.SchemaName, a)
                   | FieldType.Date | FieldType.DateTime | FieldType.Int | FieldType.Double | FieldType.Stored | FieldType.Long -> 
                       ())
        map <- analyzerMap
    
    override this.getWrappedAnalyzer (fieldName) = 
        match map.TryGetValue(fieldName) with
        | true, analyzer -> analyzer
        | _ -> defaultAnalyzer

module Codec = 
    open FlexLucene.Codecs.Lucene53
    open FlexLucene.Codecs.Lucene50
    open FlexLucene.Codecs.Lucene410
    open FlexLucene.Codecs.Lucene41
    open FlexLucene.Codecs
    
    /// Get the default codec associated with the index version
    let getCodec (enableBloomFilter : bool) (version : IndexVersion) = 
        let getPostingsFormat (fieldName : string, enableBloomFilter, defaultFormat) = 
            if fieldName.Equals(MetaFields.IdField) && enableBloomFilter then 
                new BloomFilteringPostingsFormat(defaultFormat) :> PostingsFormat
            else defaultFormat
        match version with
        | IndexVersion.Lucene_5_0_0 -> 
            let postingsFormat = new Lucene50PostingsFormat()
            { new Lucene53Codec() with
                  member this.getPostingsFormatForField (fieldName) = 
                      getPostingsFormat (fieldName, enableBloomFilter, postingsFormat) } :> Codec
            |> ok
        | IndexVersion.Lucene_4_x_x -> 
            let postingsFormat = new Lucene41PostingsFormat()
            { new Lucene410Codec() with
                  member this.getPostingsFormatForField (fieldName) = 
                      getPostingsFormat (fieldName, enableBloomFilter, postingsFormat) } :> Codec
            |> ok
        | unknown -> fail (UnSupportedIndexVersion(unknown.ToString()))

module IndexSetting = 
    /// General index settings
    type T = 
        { IndexName : string
          IndexAnalyzer : AnalyzerWrapper
          SearchAnalyzer : AnalyzerWrapper
          Fields : Field.T []
          FieldsLookup : IReadOnlyDictionary<string, Field.T>
          SearchProfiles : IReadOnlyDictionary<string, Predicate * SearchQuery>
          IndexConfiguration : IndexConfiguration
          BaseFolder : string
          ShardConfiguration : ShardConfiguration }

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
              FieldsLookup = Unchecked.defaultof<_>
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
    let buildAnalyzer (fields : Field.T [], isIndexAnalyzer : bool) = 
        let analyzer = new AnalyzerWrapper()
        analyzer.BuildAnalyzer(fields, isIndexAnalyzer)
        analyzer
    
    let withFields (fields : Field array, analyzerService, scriptService) (build) = 
        let ic = build.Setting.IndexConfiguration
        let resultLookup = new Dictionary<string, Field.T>(StringComparer.OrdinalIgnoreCase)
        let result = new ResizeArray<Field.T>()
        // Add system fields
        resultLookup.Add(MetaFields.IdField, Field.getIdField (ic.UseBloomFilterForId))
        resultLookup.Add(MetaFields.LastModifiedField, Field.getTimeStampField())
        resultLookup.Add(MetaFields.ModifyIndex, Field.getModifyIndexField())
        for field in fields do
            let fieldObject = returnOrFail (Field.build (field, ic, analyzerService, scriptService))
            resultLookup.Add(field.FieldName, fieldObject)
            result.Add(fieldObject)
        let fieldArr = result.ToArray()
        // Perf: Intern all the field names in the string pool. This is done as the field names will be
        // used millions of times during execution.
        fieldArr |> Array.iter (fun x -> 
                        String.Intern x.FieldName |> ignore
                        String.Intern x.SchemaName |> ignore)
        { build with Setting = 
                         { build.Setting with FieldsLookup = resultLookup
                                              Fields = fieldArr
                                              SearchAnalyzer = buildAnalyzer (fieldArr, false)
                                              IndexAnalyzer = buildAnalyzer (fieldArr, true) } }
    
    /// Build search profiles from the Index object
    let withSearchProfiles (profiles : SearchQuery array, parser : IFlexParser) (build) = 
        let result = new Dictionary<string, Predicate * SearchQuery>(StringComparer.OrdinalIgnoreCase)
        for profile in profiles do
            let predicate = returnOrFail <| parser.Parse profile.QueryString
            result.Add(profile.QueryName, (predicate, profile))
        { build with Setting = { build.Setting with SearchProfiles = result } }
    
    /// Build the final index setting object
    let build (build) = 
        assert (notNull build.Setting.SearchProfiles)
        assert (notNull build.Setting.Fields)
        assert (notNull build.Setting.FieldsLookup)
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
            s.IndexConfiguration.DefaultFieldSimilarity
            |> FieldSimilarity.getLuceneT
            |> extract
        
        let mappings = new Dictionary<string, Similarity>(StringComparer.OrdinalIgnoreCase)
        for field in s.FieldsLookup do
            // Only add if the format is not same as default postings format
            if field.Value.Similarity <> s.IndexConfiguration.DefaultFieldSimilarity then 
                let similarity = 
                    field.Value.Similarity
                    |> FieldSimilarity.getLuceneT
                    |> extract
                mappings.Add(field.Key, similarity)
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
        iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
        iwc.SetRAMBufferSizeMB(double s.IndexConfiguration.RamBufferSizeMb) |> ignore
        iwc.SetMaxBufferedDocs(s.IndexConfiguration.MaxBufferedDocs) |> ignore
        iwc.SetCodec(codec) |> ignore
        iwc.SetSimilarity(similarityProvider) |> ignore
        iwc
    
    /// Used for updating real time Index writer settings
    let updateWithSettings (s : IndexSetting.T) (iwc : LiveIndexWriterConfig) = 
        iwc.SetRAMBufferSizeMB(double s.IndexConfiguration.RamBufferSizeMb)

/// Wrapper around SearcherManager to expose .net IDisposable functionality
type RealTimeSearcher(searchManger : SearcherManager) = 
    let indexSearcher = searchManger.Acquire() :?> IndexSearcher
    
    /// Dispose method which will be called automatically through Fody inter-leaving 
    member __.DisposeManaged() = searchManger.Release(indexSearcher)
    
    member __.IndexSearcher = indexSearcher
    
    /// IndexReader provides an interface for accessing a point-in-time view of 
    /// an index. Any changes made to the index via IndexWriter 
    /// will not be visible until a new IndexReader is opened. 
    member __.IndexReader = indexSearcher.GetIndexReader()
    
    interface IDisposable with
        member __.Dispose() : unit = ()