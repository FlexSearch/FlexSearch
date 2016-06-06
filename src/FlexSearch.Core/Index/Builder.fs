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
open FlexLucene.Store
open FlexLucene.Codecs
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

[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FieldSimilarity = 
    open FlexLucene.Search.Similarities
    
    /// Converts the enum similarity to Lucene Similarity
    let getLuceneT = 
        function 
        | Similarity.TFIDF -> ok (new ClassicSimilarity() :> Similarity)
        | Similarity.BM25 -> ok (new BM25Similarity() :> Similarity)
        | unknown -> fail (UnSupportedSimilarity(unknown.ToString()))
    
    /// Default similarity provider used by FlexSearch
    [<SealedAttribute>]
    type Provider(mappings : IReadOnlyDictionary<string, Similarity>, defaultFormat : Similarity) = 
        inherit PerFieldSimilarityWrapper()
        override __.Get(fieldName) = 
            match mappings.TryGetValue(fieldName) with
            | true, format -> format
            | _ -> defaultFormat

[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DirectoryType = 
    /// Create a index directory from the given directory type    
    let getIndexDirectory (directoryType : DirectoryType, path : string) = 
        // Note: Might move to SingleInstanceLockFactory to provide other services to open
        // the index in read-only mode
        let lockFactory = NativeFSLockFactory.GetDefault()
        let file = (new java.io.File(path)).toPath()
        try 
            match directoryType with
            | DirectoryType.FileSystem -> ok (FSDirectory.Open(file, lockFactory) :> FlexLucene.Store.Directory)
            | DirectoryType.MemoryMapped -> ok (MMapDirectory.Open(file, lockFactory) :> FlexLucene.Store.Directory)
            | DirectoryType.Ram -> ok (new RAMDirectory() :> FlexLucene.Store.Directory)
            | unknown -> fail (UnsupportedDirectoryType(unknown.ToString()))
        with e -> fail (ErrorOpeningIndexWriter(path, exceptionPrinter (e), new ResizeArray<_>()))

[<RequireQualifiedAccessAttribute; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module IndexConfiguration = 
    let inline getIndexWriterConfiguration (codec : Codec) (similarity : LuceneSimilarity) 
               (indexAnalyzer : LuceneAnalyzer) (configuration : IndexConfiguration) = 
        let iwc = new IndexWriterConfig(indexAnalyzer)
        iwc.SetOpenMode(IndexWriterConfigOpenMode.CREATE_OR_APPEND) |> ignore
        iwc.SetRAMBufferSizeMB(float configuration.RamBufferSizeMb) |> ignore
        iwc.SetCodec(codec).SetSimilarity(similarity) |> ignore
        iwc

/// Builder related to creating Index Settings
[<AutoOpenAttribute>]
module IndexSettingBuilder = 
    /// Builder object which will be passed around to build
    /// index setting
    type BuilderObject = 
        { Setting : IndexSetting }
    
    let withIndexName (indexName, path) = 
        Directory.CreateDirectory(path) |> ignore
        let setting = 
            { IndexName = indexName
              IndexAnalyzer = Unchecked.defaultof<_>
              SearchAnalyzer = Unchecked.defaultof<_>
              Fields = Unchecked.defaultof<_>
              Scripts = Unchecked.defaultof<_>
              PredefinedQueries = Unchecked.defaultof<_>
              IndexConfiguration = Unchecked.defaultof<_>
              BaseFolder = path
              SettingsFolder = Constants.ConfFolder +/ "indices" +/ indexName
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
    
    let withFields (fields : Field [], analyzerService : GetAnalyzer) (build) = 
        let ic = build.Setting.IndexConfiguration
        let resultLookup = new Dictionary<string, FieldSchema>(StringComparer.OrdinalIgnoreCase)
        let result = new FieldCollection()
        // Add system fields
        FieldSchema.getMetaSchemaFields |> Seq.iter (fun x -> result.Add(x))
        for field in fields do
            let fieldObject = returnOrFail <| FieldSchema.build field analyzerService
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
    let withPredefinedQueries (profiles : SearchQuery [], parser : IFlexParser) (build) = 
        let result = new Dictionary<string, Predicate * SearchQuery>(StringComparer.OrdinalIgnoreCase)
        for profile in profiles do
            let predicate = returnOrFail <| parser.Parse profile.QueryString
            result.Add(profile.QueryName, (predicate, profile))
        { build with Setting = { build.Setting with PredefinedQueries = result } }
    
    /// Build the script present in the configuration directory
    let withScripts (build : BuilderObject) = 
        let scripts =
            if File.Exists(build.Setting.SettingsFolder +/ "script.fsx") then 
                returnOrFail <| FSharpCompiler.compile (build.Setting.SettingsFolder +/ "script.fsx")
            else Scripts.Default
        { build with Setting = { build.Setting with Scripts = scripts } }

    /// Build the final index setting object
    let build (build) = 
        assert (notNull build.Setting.PredefinedQueries)
        assert (notNull build.Setting.Fields)
        assert (notNull build.Setting.IndexConfiguration)
        assert (notNull build.Setting.ShardConfiguration)
        assert (notNull build.Setting.IndexAnalyzer)
        assert (notNull build.Setting.SearchAnalyzer)
        build.Setting

/// Builders related to creating Lucene IndexWriterConfig
module IndexWriterConfigBuilder = 
    /// Returns an instance of per field similarity provider 
    let getSimilarityProvider (s : IndexSetting) = 
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
    let buildWithSettings (s : IndexSetting) = 
        let iwc = new IndexWriterConfig(s.IndexAnalyzer)
        
        let codec = 
            s.IndexConfiguration.IndexVersion
            |> Codec.getCodec
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
    let updateWithSettings (s : IndexSetting) (iwc : LiveIndexWriterConfig) = 
        iwc.SetRAMBufferSizeMB(double s.IndexConfiguration.RamBufferSizeMb)
