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
    /// <summary>
    /// FieldType to be used for ID fields
    /// </summary>
    let IdFieldType = 
        lazy (let fieldType = new FieldType()
              fieldType.setIndexed (true)
              fieldType.setOmitNorms (true)
              fieldType.setIndexOptions (FieldInfo.IndexOptions.DOCS_ONLY)
              fieldType.setTokenized (false)
              fieldType.freeze()
              fieldType)
    
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
            Choice1Of2(iwc)
        with e -> 
            let error = OperationMessage.WithDeveloperMessage(MessageConstants.ERROR_OPENING_INDEXWRITER, e.Message)
            Choice2Of2(error)
    
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
                let error = 
                    OperationMessage.WithDeveloperMessage
                        (MessageConstants.ERROR_OPENING_INDEXWRITER, "Unknown directory type.")
                Choice2Of2(error)
        with e -> 
            let error = OperationMessage.WithDeveloperMessage(MessageConstants.ERROR_OPENING_INDEXWRITER, e.Message)
            Choice2Of2(error)
    
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
