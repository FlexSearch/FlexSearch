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
open FlexSearch.Common
open FlexSearch.Core
open FlexSearch.Utility
open FlexSearch.Java
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open java.io
open java.lang
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.lucene.codecs
open org.apache.lucene.codecs.bloom
open org.apache.lucene.codecs.idversion
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.codecs.perfield
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.sandbox
open org.apache.lucene.search
open org.apache.lucene.search.similarities
open org.apache.lucene.store

type IndexRegisteration = 
    { IndexState : IndexState
      IndexInfo : Index
      Index : option<FlexIndex> }

[<Sealed>]
type RegisterationManager(writer : IThreadSafeWriter, formatter : IFormatter, serverSettings : ServerSettings) = 
    let stateDb = new ConcurrentDictionary<string, IndexRegisteration>(StringComparer.OrdinalIgnoreCase)
    member this.GetAllIndiceInfo() = stateDb.Values |> Seq.map (fun x -> x.IndexInfo)
    
    member this.GetStatus(indexName) = 
        match stateDb.TryGetValue(indexName) with
        | (true, reg) -> Choice1Of2(reg.IndexState)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    member this.GetIndexInfo(indexName) = 
        match stateDb.TryGetValue(indexName) with
        | (true, reg) -> Choice1Of2(reg.IndexInfo)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    member this.GetIndex(indexName) = 
        match stateDb.TryGetValue(indexName) with
        | (true, reg) -> Choice1Of2(reg.Index)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    member this.GetRegisteration(indexName) = 
        match stateDb.TryGetValue(indexName) with
        | (true, state) -> Choice1Of2(state)
        | _ -> Choice2Of2(Errors.INDEX_REGISTERATION_MISSING |> GenerateOperationMessage)
    
    member this.UpdateStatus(indexName, state) = 
        match stateDb.TryGetValue(indexName) with
        | (true, reg) -> 
            let newReg = { reg with IndexState = state }
            match stateDb.TryUpdate(indexName, newReg, reg) with
            | true -> Choice1Of2()
            | false -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    member this.RemoveRegisteration(indexName) = 
        match stateDb.TryGetValue(indexName) with
        | (true, reg) -> 
            stateDb.TryRemove(indexName) |> ignore
            Choice1Of2()
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    member this.UpdateRegisteration(indexName : string, state : IndexState, indexInfo : Index, index : FlexIndex option) = 
        maybe { 
            assert (indexName <> null)
            // Only write to file for non ram type indices
            if indexInfo.IndexConfiguration.DirectoryType <> DirectoryType.Ram then 
                do! writer.WriteToFile
                        (Path.Combine(serverSettings.DataFolder, indexName, "conf.yml"), 
                         formatter.SerializeToString(indexInfo))
            match stateDb.TryGetValue(indexName) with
            | (true, reg) -> 
                let registeration = 
                    { IndexState = state
                      IndexInfo = indexInfo
                      Index = index }
                stateDb.TryUpdate(indexName, registeration, reg) |> ignore
                return ()
            | _ -> 
                let registeration = 
                    { IndexState = state
                      IndexInfo = indexInfo
                      Index = index }
                stateDb.TryAdd(indexName, registeration) |> ignore
                return ()
        }

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

[<AutoOpen>]
module IndexingHelpers = 
    type FlexSearch.Api.FieldSimilarity with
        member this.GetSimilairity() = 
            match this with
            | FieldSimilarity.TFIDF -> Choice1Of2(new DefaultSimilarity() :> Similarity)
            | FieldSimilarity.BM25 -> Choice1Of2(new BM25Similarity() :> Similarity)
            | _ -> 
                Choice2Of2(Errors.UNSUPPORTED_SIMILARITY
                           |> GenerateOperationMessage
                           |> Append("Similarity", this.ToString()))
    
    type FlexSearch.Api.IndexVersion with
        
        member this.GetLuceneIndexVersion() = 
            match this with
            | IndexVersion.Lucene_4_9 -> Choice1Of2(org.apache.lucene.util.Version.LUCENE_4_9)
            | IndexVersion.Lucene_4_10 -> Choice1Of2(org.apache.lucene.util.Version.LUCENE_4_10_0)
            | IndexVersion.Lucene_4_10_1 -> Choice1Of2(org.apache.lucene.util.Version.LUCENE_4_10_1)
            | _ -> 
                Choice2Of2(Errors.UNSUPPORTED_INDEX_VERSION
                           |> GenerateOperationMessage
                           |> Append("Version", this.ToString()))
        
        member this.GetDefaultCodec() = 
            match this with
            | IndexVersion.Lucene_4_9 -> Choice1Of2(new FlexCodec410() :> Codec)
            | IndexVersion.Lucene_4_10 -> Choice1Of2(new FlexCodec410() :> Codec)
            | IndexVersion.Lucene_4_10_1 -> Choice1Of2(new FlexCodec410() :> Codec)
            | _ -> 
                Choice2Of2(Errors.UNSUPPORTED_INDEX_VERSION
                           |> GenerateOperationMessage
                           |> Append("Version", this.ToString()))
        
        member this.GetDefaultPostingsFormat() = 
            match this with
            | IndexVersion.Lucene_4_9 -> Choice1Of2(FieldPostingsFormat.Lucene_4_1)
            | IndexVersion.Lucene_4_10 -> Choice1Of2(FieldPostingsFormat.Lucene_4_1)
            | IndexVersion.Lucene_4_10_1 -> Choice1Of2(FieldPostingsFormat.Lucene_4_1)
            | _ -> 
                Choice2Of2(Errors.UNSUPPORTED_INDEX_VERSION
                           |> GenerateOperationMessage
                           |> Append("Version", this.ToString()))
    
    let GetSimilarityProvider(settings : FlexIndexSetting) = 
        maybe { 
            let! defaultSimilarity = settings.IndexConfiguration.DefaultFieldSimilarity.GetSimilairity()
            let mappings = new Dictionary<string, Similarity>(StringComparer.OrdinalIgnoreCase)
            for field in settings.FieldsLookup do
                // Only add if the format is not same as default postings format
                if field.Value.Similarity <> settings.IndexConfiguration.DefaultFieldSimilarity then 
                    let! similarity = field.Value.Similarity.GetSimilairity()
                    mappings.Add(field.Key, similarity)
            return! Choice1Of2(new FlexPerFieldSimilarityProvider(mappings, defaultSimilarity))
        }
    
    /// Creates Lucene index writer configuration from flex index setting 
    let private GetIndexWriterConfig(flexIndexSetting : FlexIndexSetting) = 
        maybe { 
            let! indexVersion = flexIndexSetting.IndexConfiguration.IndexVersion.GetLuceneIndexVersion()
            let! codec = flexIndexSetting.IndexConfiguration.IndexVersion.GetDefaultCodec()
            let iwc = new IndexWriterConfig(indexVersion, flexIndexSetting.IndexAnalyzer)
            iwc.setOpenMode (org.apache.lucene.index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
            iwc.setRAMBufferSizeMB (System.Double.Parse(flexIndexSetting.IndexConfiguration.RamBufferSizeMb.ToString())) 
            |> ignore
            iwc.setCodec (codec) |> ignore
            let! similarityProvider = GetSimilarityProvider(flexIndexSetting)
            iwc.setSimilarity (similarityProvider) |> ignore
            return! Choice1Of2(iwc)
        }
    
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
                Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
                           |> GenerateOperationMessage
                           |> Append("Message", "Unknown directory type."))
        with e -> 
            Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
                       |> GenerateOperationMessage
                       |> Append("Message", e.Message))
    
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
