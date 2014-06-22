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

// ----------------------------------------------------------------------------
// Contains document building and indexing related operations
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<RequireQualifiedAccess>]
module Document = 
    /// <summary>
    ///  Method to map a string based id to a lucene shard 
    /// </summary>
    /// <param name="id">Id of the document</param>
    /// <param name="shardCount">Total available shards</param>
    let MapToShard (id : string) shardCount = 
        if (shardCount = 1) then 0
        else 
            let mutable total = 0
            for i in id do
                total <- total + System.Convert.ToInt32(i)
            total % shardCount
    
// ----------------------------------------------------------------------------
// Contains lucene writer IO and infrastructure related operations
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<RequireQualifiedAccess>]
module IO = 
    // ----------------------------------------------------------------------------     
    // Creates lucene index writer configuration from flex index setting 
    // ---------------------------------------------------------------------------- 
    let private GetIndexWriterConfig (flexIndexSetting : FlexIndexSetting) = 
        try 
            let iwc = new IndexWriterConfig(Constants.LuceneVersion, flexIndexSetting.IndexAnalyzer)
            iwc.setOpenMode (org.apache.lucene.index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
            iwc.setRAMBufferSizeMB (System.Double.Parse(flexIndexSetting.IndexConfiguration.RamBufferSizeMb.ToString())) 
            |> ignore
            Choice1Of2(iwc)
        with e -> 
            let error = OperationMessage.WithDeveloperMessage(MessageConstants.ERROR_OPENING_INDEXWRITER, e.Message)
            Choice2Of2(error)
    
    // ----------------------------------------------------------------------------                  
    // Create a lucene file-system lock over a directory    
    // ---------------------------------------------------------------------------- 
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
    
    // ---------------------------------------------------------------------------- 
    // Creates lucene index writer from flex index setting  
    // ----------------------------------------------------------------------------                    
    let GetIndexWriter(indexSetting : FlexIndexSetting, directoryPath : string) = 
        maybe { 
            let! iwc = GetIndexWriterConfig indexSetting
            let! indexDirectory = GetIndexDirectory directoryPath indexSetting.IndexConfiguration.DirectoryType
            let indexWriter = new IndexWriter(indexDirectory, iwc)
            let trackingIndexWriter = new TrackingIndexWriter(indexWriter)
            return! Choice1Of2(indexWriter, trackingIndexWriter)
        }
