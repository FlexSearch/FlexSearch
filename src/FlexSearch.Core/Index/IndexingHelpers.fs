// ----------------------------------------------------------------------------
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
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store
open org.apache.lucene.codecs.bloom
open org.apache.lucene.index
open org.apache.lucene.sandbox
open org.apache.lucene.codecs.idversion
open org.apache.lucene.document
open org.apache.lucene.analysis
open org.apache.lucene.codecs.perfield
open org.apache.lucene.codecs
open System.Collections.Generic
open org.apache.lucene.search.similarities

/// <summary>
/// Default postings format for FlexSearch
/// </summary>
[<Sealed>]
type FlexPerFieldPostingFormats(mappings : IReadOnlyDictionary<string, PostingsFormat>, defaultFormat : PostingsFormat) = 
    inherit PerFieldPostingsFormat()
    override this.getPostingsFormatForField (fieldName) = 
        match mappings.TryGetValue(fieldName) with
        | true, format -> format
        | _ -> defaultFormat

/// <summary>
/// Default postings format for FlexSearch
/// </summary>
[<Sealed>]
type FlexPerFieldSimilarityProvider(mappings : IReadOnlyDictionary<string, Similarity>, defaultFormat : Similarity) = 
    inherit PerFieldSimilarityWrapper()
    override this.get (fieldName) = 
        match mappings.TryGetValue(fieldName) with
        | true, format -> format
        | _ -> defaultFormat

/// <summary>
/// Default codec for FlexSearch
/// </summary>
[<Sealed>]
type FlexCodec(postingsFormat : FlexPerFieldPostingFormats, delegatingCodec : Codec) = 
    inherit FilterCodec("FlexCodec", delegatingCodec)
    override this.postingsFormat() = postingsFormat :> PostingsFormat

[<AutoOpen>]
module IndexingHelpers = 
    type FlexSearch.Api.FieldPostingsFormat with
        member this.GetPostingsFormat() =
            match this with
            | FieldPostingsFormat.Bloom_4_1 -> new org.apache.lucene.codecs.bloom.BloomFilteringPostingsFormat(new org.apache.lucene.codecs.lucene41.Lucene41PostingsFormat()) :> PostingsFormat
            | FieldPostingsFormat.Direct -> new org.apache.lucene.codecs.memory.DirectPostingsFormat() :> PostingsFormat
            | FieldPostingsFormat.Lucene_4_1 -> new org.apache.lucene.codecs.lucene41.Lucene41PostingsFormat() :> PostingsFormat
            | FieldPostingsFormat.Memory -> new org.apache.lucene.codecs.memory.MemoryPostingsFormat() :> PostingsFormat
            | FieldPostingsFormat.Pulsing_4_1 -> new org.apache.lucene.codecs.pulsing.Pulsing41PostingsFormat() :> PostingsFormat
            | _ -> failwithf "Unknown postings format"

    type FlexSearch.Api.Codec with
        member this.GetCodec() =
            match this with
            | Codec.Lucene_4_9 -> new lucene49.Lucene49Codec()
            | _ -> failwithf "Unknown codec"

    type FlexSearch.Api.FieldSimilarity with
        member this.GetSimilairity() =
            match this with
            | FieldSimilarity.TFIDF -> new DefaultSimilarity() :> Similarity
            | FieldSimilarity.BM25 -> new BM25Similarity() :> Similarity
            | _ -> failwithf "Unknown similarity"

    let GetPostingsFormat(settings: FlexIndexSetting) =
        let defaultFormat = settings.IndexConfiguration.DefaultIndexPostingsFormat.GetPostingsFormat()
        let mappings = new Dictionary<string, PostingsFormat>(StringComparer.OrdinalIgnoreCase)
        for field in settings.FieldsLookup do
            // Only add if the format is not same as default postings format
            if  field.Value.PostingsFormat <> settings.IndexConfiguration.DefaultIndexPostingsFormat then
                mappings.Add(field.Key, field.Value.PostingsFormat.GetPostingsFormat())
        new FlexPerFieldPostingFormats(mappings, defaultFormat)
    
    let GetSimilarityProvider(settings: FlexIndexSetting) =
        let defaultSimilarity = settings.IndexConfiguration.DefaultFieldSimilarity.GetSimilairity()
        let mappings = new Dictionary<string, Similarity>(StringComparer.OrdinalIgnoreCase)
        for field in settings.FieldsLookup do
            // Only add if the format is not same as default postings format
            if  field.Value.Similarity <> settings.IndexConfiguration.DefaultFieldSimilarity then
                mappings.Add(field.Key, field.Value.Similarity.GetSimilairity())
        new FlexPerFieldSimilarityProvider(mappings, defaultSimilarity)

    /// Creates Lucene index writer configuration from flex index setting 
    let private GetIndexWriterConfig(flexIndexSetting : FlexIndexSetting) = 
        try 
            let version = 
                org.apache.lucene.util.Version.parseLeniently 
                    (flexIndexSetting.IndexConfiguration.IndexVersion.ToString())
            let iwc = new IndexWriterConfig(version, flexIndexSetting.IndexAnalyzer)
            iwc.setOpenMode (org.apache.lucene.index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
            iwc.setRAMBufferSizeMB (System.Double.Parse(flexIndexSetting.IndexConfiguration.RamBufferSizeMb.ToString())) 
            |> ignore

            let postingsFormat = GetPostingsFormat(flexIndexSetting)
            iwc.setCodec(new FlexCodec(postingsFormat, flexIndexSetting.IndexConfiguration.DefaultCodec.GetCodec())) |> ignore
            let similarityProvider = GetSimilarityProvider(flexIndexSetting)
            iwc.setSimilarity(similarityProvider) |> ignore
            Choice1Of2(iwc)
        with e -> 
            Choice2Of2(MessageConstants.ERROR_OPENING_INDEXWRITER |> Append("Message", e.Message))
    
    /// Create a Lucene file-system lock over a directory    
    let private GetIndexDirectory (directoryPath : string) (directoryType : DirectoryType) = 
        // Note: Might move to SingleInstanceLockFactory to provide other services to open
        // the index in read-only mode
        let lockFactory = new NativeFSLockFactory()
        let file = new java.io.File(directoryPath)
        try 
            match directoryType with
            | DirectoryType.FileSystem -> 
                Choice1Of2(FSDirectory.``open`` (file, lockFactory) :> org.apache.lucene.store.Directory)
            | DirectoryType.MemoryMapped -> 
                Choice1Of2(MMapDirectory.``open`` (file, lockFactory) :> org.apache.lucene.store.Directory)
            | DirectoryType.Ram -> Choice1Of2(new RAMDirectory() :> org.apache.lucene.store.Directory)
            | _ -> 
                Choice2Of2(MessageConstants.ERROR_OPENING_INDEXWRITER |> Append("Message", "Unknown directory type."))
        with e -> 
            Choice2Of2(MessageConstants.ERROR_OPENING_INDEXWRITER |> Append("Message", e.Message))
    
    /// <summary>
    /// Creates index writer from flex index setting  
    /// </summary>
    /// <param name="indexSetting"></param>
    /// <param name="directoryPath"></param>
    let GetIndexWriter(indexSetting : FlexIndexSetting, directoryPath : string) = 
        maybe { 
            let! iwc = GetIndexWriterConfig indexSetting
            let! indexDirectory = GetIndexDirectory directoryPath indexSetting.IndexConfiguration.DirectoryType
            let indexWriter = new IndexWriter(indexDirectory, iwc)
            let trackingIndexWriter = new TrackingIndexWriter(indexWriter)
            return! Choice1Of2(indexWriter, trackingIndexWriter)
        }
    
    /// <summary>
    ///  Method to map a string based id to a Lucene shard 
    /// Uses MurmurHash2 algorithm
    /// </summary>
    /// <param name="id">Id of the document</param>
    /// <param name="shardCount">Total available shards</param>
    let MapToShard (id : string) shardCount = 
        if (shardCount = 1) then 0
        else 
            let byteArray = System.Text.Encoding.UTF8.GetBytes(id)
            MurmurHash2.hash32 (byteArray, 0, byteArray.Length) % shardCount
