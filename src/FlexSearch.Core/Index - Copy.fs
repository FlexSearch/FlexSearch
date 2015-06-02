namespace FlexSearch.Core

open FlexLucene.Analysis
open FlexLucene.Analysis.Core
open FlexLucene.Analysis.Miscellaneous
open FlexLucene.Analysis.Util
open FlexLucene.Codecs
open FlexLucene.Codecs.Bloom
open FlexLucene.Codecs.Idversion
open FlexLucene.Codecs.Lucene42
open FlexLucene.Codecs.Perfield
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Sandbox
open FlexLucene.Search
open FlexLucene.Search.Similarities
open FlexLucene.Store
open FlexSearch.Core
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

/// <summary>
/// Represents the current state of the index.
/// </summary>
type IndexState = 
    | Opening = 1
    | Recovering = 2
    | OnlineMaster = 3
    | OnlineFollower = 4
    | Offline = 5
    | Closing = 6
    | Faulted = 7

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
    
    member this.IsOpen(indexName) = 
        match stateDb.TryGetValue(indexName) with
        | (true, state) -> 
            match state.IndexState with
            | IndexState.Online -> Choice1Of2(state)
            | _ -> Choice2Of2(Errors.INDEX_IS_OFFLINE |> GenerateOperationMessage)
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
                do! writer.WriteFile<Index>
                        (Path.Combine
                             (serverSettings.DataFolder, indexName, sprintf "conf%s" Constants.SettingsFileExtension), 
                         indexInfo)
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
    //    type FlexSearch.Api.FieldSimilarity with
    //        member this.GetSimilairity() = 
    //            match this with
    //            | FieldSimilarity.TFIDF -> Choice1Of2(new DefaultSimilarity() :> Similarity)
    //            | FieldSimilarity.BM25 -> Choice1Of2(new BM25Similarity() :> Similarity)
    //            | _ -> 
    //                Choice2Of2(Errors.UNSUPPORTED_SIMILARITY
    //                           |> GenerateOperationMessage
    //                           |> Append("Similarity", this.ToString()))
    //    type FlexSearch.Api.IndexVersion with
    //        
    //        member this.GetLuceneIndexVersion() = 
    //            match this with
    //            | IndexVersion.Lucene_4_9 -> Choice1Of2(FlexLucene.util.Version.LUCENE_4_9)
    //            | IndexVersion.Lucene_4_10 -> Choice1Of2(FlexLucene.util.Version.LUCENE_4_10_0)
    //            | IndexVersion.Lucene_4_10_1 -> Choice1Of2(FlexLucene.util.Version.LUCENE_4_10_1)
    //            | _ -> 
    //                Choice2Of2(Errors.UNSUPPORTED_INDEX_VERSION
    //                           |> GenerateOperationMessage
    //                           |> Append("Version", this.ToString()))
    //        
    //        member this.GetDefaultCodec() = 
    //            match this with
    //            | IndexVersion.Lucene_4_9 -> Choice1Of2(new FlexCodec410() :> Codec)
    //            | IndexVersion.Lucene_4_10 -> Choice1Of2(new FlexCodec410() :> Codec)
    //            | IndexVersion.Lucene_4_10_1 -> Choice1Of2(new FlexCodec410() :> Codec)
    //            | _ -> 
    //                Choice2Of2(Errors.UNSUPPORTED_INDEX_VERSION
    //                           |> GenerateOperationMessage
    //                           |> Append("Version", this.ToString()))
    //        
    //        member this.GetDefaultPostingsFormat() = 
    //            match this with
    //            | IndexVersion.Lucene_4_9 -> Choice1Of2(FieldPostingsFormat.Lucene_4_1)
    //            | IndexVersion.Lucene_4_10 -> Choice1Of2(FieldPostingsFormat.Lucene_4_1)
    //            | IndexVersion.Lucene_4_10_1 -> Choice1Of2(FieldPostingsFormat.Lucene_4_1)
    //            | _ -> 
    //                Choice2Of2(Errors.UNSUPPORTED_INDEX_VERSION
    //                           |> GenerateOperationMessage
    //                           |> Append("Version", this.ToString()))
    //    
    //    let GetSimilarityProvider(settings : FlexIndexSetting) = 
    //        maybe { 
    //            let! defaultSimilarity = settings.IndexConfiguration.DefaultFieldSimilarity.GetSimilairity()
    //            let mappings = new Dictionary<string, Similarity>(StringComparer.OrdinalIgnoreCase)
    //            for field in settings.FieldsLookup do
    //                // Only add if the format is not same as default postings format
    //                if field.Value.Similarity <> settings.IndexConfiguration.DefaultFieldSimilarity then 
    //                    let! similarity = field.Value.Similarity.GetSimilairity()
    //                    mappings.Add(field.Key, similarity)
    //            return! Choice1Of2(new FlexPerFieldSimilarityProvider(mappings, defaultSimilarity))
    //        }
    //    
    //    /// Creates Lucene index writer configuration from flex index setting 
    //    let private GetIndexWriterConfig(flexIndexSetting : FlexIndexSetting) = 
    //        maybe { 
    //            let! indexVersion = flexIndexSetting.IndexConfiguration.IndexVersion.GetLuceneIndexVersion()
    //            let! codec = flexIndexSetting.IndexConfiguration.IndexVersion.GetDefaultCodec()
    //            let iwc = new IndexWriterConfig(indexVersion, flexIndexSetting.IndexAnalyzer)
    //            iwc.setOpenMode (FlexLucene.index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
    //            iwc.setRAMBufferSizeMB (System.Double.Parse(flexIndexSetting.IndexConfiguration.RamBufferSizeMb.ToString())) 
    //            |> ignore
    //            iwc.setCodec (codec) |> ignore
    //            let! similarityProvider = GetSimilarityProvider(flexIndexSetting)
    //            iwc.setSimilarity (similarityProvider) |> ignore
    //            return! Choice1Of2(iwc)
    //        }
    //    
    //    /// Create a Lucene file-system lock over a directory    
    //    let private GetIndexDirectory (directoryPath : string) (directoryType : DirectoryType) = 
    //        // Note: Might move to SingleInstanceLockFactory to provide other services to open
    //        // the index in read-only mode
    //        let lockFactory = new NativeFSLockFactory()
    //        let file = new java.io.File(directoryPath)
    //        try 
    //            match directoryType with
    //            | DirectoryType.FileSystem -> 
    //                Choice1Of2(FSDirectory.``open`` (file, lockFactory) :> FlexLucene.store.Directory)
    //            | DirectoryType.MemoryMapped -> 
    //                Choice1Of2(MMapDirectory.``open`` (file, lockFactory) :> FlexLucene.store.Directory)
    //            | DirectoryType.Ram -> Choice1Of2(new RAMDirectory() :> FlexLucene.store.Directory)
    //            | _ -> 
    //                Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
    //                           |> GenerateOperationMessage
    //                           |> Append("Message", "Unknown directory type."))
    //        with e -> 
    //            Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
    //                       |> GenerateOperationMessage
    //                       |> Append("Message", e.Message))
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
            MurmurHash2.Hash32(byteArray, 0, byteArray.Length) % shardCount

// ----------------------------------------------------------------------------
/// Version cache store used across the system. This helps in resolving 
/// conflicts arising out of concurrent threads trying to update a Lucene document.
/// Every document update should go through version cache to ensure the update
/// integrity and optimistic locking.
/// In order to reduce contention there will be one CacheStore per shard. 
/// Initially Lucene's LiveFieldValues seemed like a good alternative but it
/// complicates the design and requires thread management
// ----------------------------------------------------------------------------
[<Sealed>]
type VersionCacheStore(shard : FlexShardWriter, indexSettings : FlexIndexSetting) as self = 
    
    /// Will be used to represent the deleted document version
    static let deletedValue = 0L
    
    /// The reason to use two dictionary instead of one is to avoid calling clear method
    /// on the dictionary as it acquires all locks. Also, there is a small span of time
    /// between before and after refresh when we won't have the values in the index
    [<VolatileFieldAttribute>]
    let mutable current = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    [<VolatileFieldAttribute>]
    let mutable old = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    let PKLookup(id : string, r : IndexReader) = 
        let term = new Term(indexSettings.FieldsLookup.[Constants.IdField].SchemaName, id)
        
        let rec loop counter = 
            let readerContext = r.Leaves().get(counter) :?> LeafReaderContext
            let reader = readerContext.Reader()
            let terms = reader.Terms(indexSettings.FieldsLookup.[Constants.IdField].SchemaName)
            assert (terms <> null)
            let termsEnum = terms.iterator (null)
            match termsEnum.SeekExact(term.Bytes()) with
            | true -> 
                let docsEnums = termsEnum.docs (null, null, 0)
                let nDocs = 
                    reader.getNumericDocValues (indexSettings.FieldsLookup.[Constants.LastModifiedFieldDv].SchemaName)
                nDocs.get (docsEnums.nextDoc())
            | false -> 
                if counter - 1 > 0 then loop (counter - 1)
                else 0L
        if r.Leaves().size() > 0 then loop (r.Leaves().size() - 1)
        else 0L
    
    let AddOrUpdate(id : string, version : int64, comparison : int64) : bool = 
        match current.TryGetValue(id) with
        | true, oldValue -> 
            if comparison = 0L then 
                // It is an unconditional update
                current.TryUpdate(id, version, oldValue)
            else current.TryUpdate(id, version, comparison)
        | _ -> current.TryAdd(id, version)
    
    do shard.SearcherManager.AddListener(self)
    
    /// <summary>
    /// Dispose method which will be called automatically through Fody inter-leaving 
    /// </summary>
    member this.DisposeManaged() = 
        if shard.SearcherManager <> null then shard.SearcherManager.RemoveListener(self)
    
    interface ReferenceManager.RefreshListener with
        member this.afterRefresh (b : bool) : unit = 
            // Now drop all the old values because they are now
            // visible via the searcher that was just opened; if
            // didRefresh is false, it's possible old has some
            // entries in it, which is fine: it means they were
            // actually already included in the previously opened
            // reader.  So we can safely clear old here:
            old <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
        member this.beforeRefresh() : unit = 
            old <- current
            // Start sending all updates after this point to the new
            // dictionary.  While reopen is running, any lookup will first
            // try this new dictionary, then fall back to old, then to the
            // current searcher:
            current <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
    
    interface IVersioningCacheStore with
        member this.AddOrUpdate(id : string, version : int64, comparison : int64) : bool = 
            AddOrUpdate(id, version, comparison)
        member this.Delete(id : string, version : Int64) : bool = AddOrUpdate(id, deletedValue, version)
        member this.GetValue(id : string) : int64 = 
            match current.TryGetValue(id) with
            | true, value -> value
            | _ -> 
                // Search old
                match old.TryGetValue(id) with
                | true, value -> value
                | _ -> 
                    // Go to the searcher to get the latest value
                    let s = shard.SearcherManager.Acquire() :?> IndexSearcher
                    let value = PKLookup(id, s.GetIndexReader())
                    shard.SearcherManager.Release(s)
                    current.TryAdd(id, value) |> ignore
                    value
    
    interface IDisposable with
        member x.Dispose() : unit = ()

[<Sealed>]
type VersioningManger(indexSettings : FlexIndexSetting, shards : FlexShardWriter []) = 
    let versionCaches = shards |> Array.map (fun s -> new VersionCacheStore(s, indexSettings) :> IVersioningCacheStore)
    
    let VersionCheck(document : FlexDocument, shardNumber : int, newVersion : int64) = 
        maybe { 
            match document.TimeStamp with
            | 0L -> 
                // We don't care what the version is let's proceed with normal operation
                // and bypass id check.
                return! Choice1Of2(0L)
            | -1L -> // Ensure that the document does not exists. Perform Id check
                let existingVersion = versionCaches.[shardNumber].GetValue(document.Id)
                if existingVersion <> 0L then 
                    return! Choice2Of2(Errors.INDEXING_DOCUMENT_ID_ALREADY_EXISTS |> GenerateOperationMessage)
                else return! Choice1Of2(0L)
            | 1L -> 
                // Ensure that the document does exist
                let existingVersion = versionCaches.[shardNumber].GetValue(document.Id)
                if existingVersion <> 0L then return! Choice1Of2(existingVersion)
                else return! Choice2Of2(Errors.INDEXING_DOCUMENT_ID_NOT_FOUND |> GenerateOperationMessage)
            | x when x > 1L -> 
                // Perform a version check and ensure that the provided version matches the version of 
                // the document
                let existingVersion = versionCaches.[shardNumber].GetValue(document.Id)
                if existingVersion <> 0L then 
                    if existingVersion <> document.TimeStamp || existingVersion > newVersion then 
                        return! Choice2Of2(Errors.INDEXING_VERSION_CONFLICT |> GenerateOperationMessage)
                    else return! Choice1Of2(existingVersion)
                else return! Choice2Of2(Errors.INDEXING_DOCUMENT_ID_NOT_FOUND |> GenerateOperationMessage)
            | _ -> 
                System.Diagnostics.Debug.Fail("This condition should never get executed.")
                return! Choice1Of2(0L)
        }
    
    /// <summary>
    /// Dispose method which will be called automatically through Fody inter-leaving 
    /// </summary>   
    member this.DisposeManaged() = 
        // Explicitly dispose all caches
        versionCaches |> Array.iter (fun s -> (s :?> IDisposable).Dispose())
    
    interface IVersionManager with
        member x.VersionCheck(document : FlexDocument, newVersion : int64) : Choice<int64, OperationMessage> = 
            failwith "Not implemented yet"
        member x.VersionCheck(document : FlexDocument, shardNumber : int, newVersion : int64) : Choice<int64, OperationMessage> = 
            VersionCheck(document, shardNumber, newVersion)
        member x.AddOrUpdate(id : string, shardNumber : int, version : int64, comparison : int64) = 
            versionCaches.[shardNumber].AddOrUpdate(id, version, comparison)
        member x.Delete(id : string, shardNumber : int, version : int64) = 
            versionCaches.[shardNumber].Delete(id, version)
    
    interface IDisposable with
        member x.Dispose() : unit = ()

/// <summary>
/// Wrapper around SearcherManager to expose .net IDisposable functionality
/// </summary>
type RealTimeSearcher(searchManger : SearcherManager) = 
    let indexSearcher = searchManger.Acquire() :?> IndexSearcher
    
    /// <summary>
    /// Dispose method which will be called automatically through Fody inter-leaving 
    /// </summary>
    member __.DisposeManaged() = searchManger.Release(indexSearcher)
    
    member __.IndexSearcher = indexSearcher
    
    /// <summary>
    /// IndexReader provides an interface for accessing a point-in-time view of 
    /// an index. Any changes made to the index via IndexWriter 
    /// will not be visible until a new IndexReader is opened. 
    /// </summary>
    member __.IndexReader = indexSearcher.GetIndexReader()
    
    interface IDisposable with
        member __.Dispose() : unit = ()

///// <summary>
///// An IndexWriter creates and maintains an index. This is a wrapper around
///// Lucene IndexWriter to expose the functionality in a controlled and functional 
///// manner.
///// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
///// SearcherManger through an easy to manage abstraction.
///// </summary>
//type FlexIndexWriter(config : IndexWriterConfig, directory : Directory) = 
//    let indexWriter = new IndexWriter(directory, config)
//    let trackingIndexWriter = new TrackingIndexWriter(indexWriter)
//    let searchManager = new SearcherManager(directory, new SearcherFactory())
//    
//    let CommitToDisk() = 
//        if indexWriter.HasUncommittedChanges() then indexWriter.Commit()
//    
//    let CloseIndex() = 
//        try 
//            searchManager.Close()
//            indexWriter.Close()
//        with e as AlreadyClosedException -> ()
//    
//    /// <summary>
//    /// Dispose method which will be called automatically through Fody inter-leaving 
//    /// </summary>
//    member __.DisposeManaged() = 
//        CommitToDisk()
//        CloseIndex()
//    
//    /// <summary>
//    /// Adds a document to this index.
//    /// </summary>
//    /// <param name="document"></param>
//    member __.AddDocument(document : Document) = trackingIndexWriter.AddDocument(document) |> ignore
//    
//    /// <summary>
//    /// Deletes the document with the given id.
//    /// </summary>
//    /// <param name="id"></param>
//    member __.DeleteDocument(id : string) = trackingIndexWriter.DeleteDocuments(new Term(Constants.IdField, id))
//    
//    /// <summary>
//    /// Delete all documents in the index.
//    /// </summary>
//    member __.DeleteAll() = trackingIndexWriter.DeleteAll()
//    
//    /// <summary>
//    /// Updates a document by id by first deleting the document containing term and then 
//    /// adding the new document.
//    /// </summary>
//    /// <param name="id"></param>
//    /// <param name="document"></param>
//    member __.UpdateDocument(id : string, document : Document) = 
//        trackingIndexWriter.UpdateDocument(new Term(Constants.IdField, id), document)
//    
//    /// <summary>
//    /// Returns real time searcher. 
//    /// Note: Use it with 'use' keyword to automatically return the searcher to the pool
//    /// </summary>
//    member __.GetRealTimeSearcher() = new RealTimeSearcher(searchManager)
//    
//    /// <summary>
//    /// Commits all pending changes (added & deleted documents, segment merges, added indexes, etc.) to the index, 
//    /// and syncs all referenced index files, such that a reader will see the changes and the index updates will 
//    /// survive an OS or machine crash or power loss. Note that this does not wait for any running background 
//    /// merges to finish. This may be a costly operation, so you should test the cost in your application and 
//    /// do it only when really necessary.
//    /// </summary>
//    member __.Commit() = CommitToDisk()
//    
//    /// <summary>
//    /// Commits all changes to an index, waits for pending merges to complete, closes all 
//    /// associated files and releases the write lock.
//    /// </summary>
//    member __.Close() = CloseIndex()
//    
//    /// <summary>
//    /// Adds a listener, to be notified when a reference is refreshed/swapped.
//    /// </summary>
//    /// <param name="item"></param>
//    member __.AddRefreshListener(item : ReferenceManager.RefreshListener) = searchManager.AddListener(item)
//    
//    /// <summary>
//    /// Remove a listener added with AddRefreshListener.
//    /// </summary>
//    /// <param name="item"></param>
//    member __.RemoveRefreshListener(item : ReferenceManager.RefreshListener) = searchManager.RemoveListener(item)
//    
//    interface IDisposable with
//        member __.Dispose() : unit = ()
[<AutoOpen>]
[<RequireQualifiedAccess>]
module Index1 = 
    /// <summary>
    /// Index auto commit changes job 
    /// </summary>
    /// <param name="flexIndex"></param>
    let internal CommitJob(flexIndex : FlexIndex) = 
        // Looping over array by index number is usually the fastest
        // iteration method
        for i in 0..flexIndex.Shards.Length - 1 do
            // Lucene 4.4.0 feature to check for uncommitted changes
            if flexIndex.Shards.[i].IndexWriter.hasUncommittedChanges() then flexIndex.Shards.[i].IndexWriter.commit()
    
    /// <summary>
    /// Index auto commit changes job
    /// </summary>
    /// <param name="flexIndex"></param>
    let internal RefreshIndexJob(flexIndex) = 
        // Looping over array by index number is usually the fastest
        // iteration method
        for i in 0..flexIndex.Shards.Length - 1 do
            flexIndex.Shards.[i].SearcherManager.maybeRefresh() |> ignore
    
    /// <summary>
    /// Creates a async timer which can be used to execute a function at specified
    /// period of time. This is used to schedule all recurring indexing tasks
    /// </summary>
    /// <param name="delay">Delay to be applied</param>
    /// <param name="work">Method to perform the work</param>
    /// <param name="flexIndex">Index on which the job is to be scheduled</param>
    let scheduleIndexJob delay (work : FlexIndex -> unit) flexIndex = 
        let rec loop time (cts : CancellationTokenSource) = 
            async { 
                do! Async.Sleep(time)
                if (cts.IsCancellationRequested) then cts.Dispose()
                else 
                    try 
                        work (flexIndex)
                    with e -> cts.Dispose()
                return! loop delay cts
            }
        loop delay flexIndex.Token
    
    /// <summary>
    /// Add index to the registration
    /// </summary>
    /// <param name="state">Index state</param>
    /// <param name="flexIndexSetting">Index setting</param>
    let internal AddIndex(flexIndexSetting : FlexIndexSetting) = 
        maybe { 
            /// Generate shards for the newly added index
            let generateShards flexIndexSetting = 
                try 
                    let shards = 
                        Array.init flexIndexSetting.ShardConfiguration.ShardCount (fun a -> 
                            let path = 
                                Path.Combine([| flexIndexSetting.BaseFolder
                                                "shards"
                                                a.ToString()
                                                "index" |])
                            // Only create directory for non-ram index
                            if flexIndexSetting.IndexConfiguration.DirectoryType <> DirectoryType.Ram then 
                                Directory.CreateDirectory(path) |> ignore
                            let writers = IndexingHelpers.GetIndexWriter(flexIndexSetting, path)
                            match writers with
                            | Choice2Of2(e) -> failwith e.UserMessage
                            | Choice1Of2(indexWriter, trackingIndexWriter) -> 
                                let shard = 
                                    { ShardNumber = a
                                      SearcherManager = new SearcherManager(indexWriter, true, new SearcherFactory())
                                      IndexWriter = indexWriter
                                      TrackingIndexWriter = trackingIndexWriter }
                                shard)
                    Choice1Of2(shards)
                with e -> 
                    Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
                               |> GenerateOperationMessage
                               |> Append("Message", e.Message))
            let! shards = generateShards flexIndexSetting
            let flexIndex = 
                { IndexSetting = flexIndexSetting
                  Shards = shards
                  ThreadLocalStore = new ThreadLocal<ThreadLocalDocument>()
                  VersioningManager = new VersioningManger(flexIndexSetting, shards) :> IVersionManager
                  Token = new System.Threading.CancellationTokenSource() }
            // Add the scheduler for the index
            // Commit Scheduler
            Async.Start
                (ScheduleIndexJob (flexIndexSetting.IndexConfiguration.CommitTimeSeconds * 1000) CommitJob flexIndex)
            // NRT Scheduler
            Async.Start
                (ScheduleIndexJob flexIndexSetting.IndexConfiguration.RefreshTimeMilliseconds RefreshIndexJob flexIndex)
            return! Choice1Of2(flexIndex)
        }
    
    /// <summary>
    /// Close an open index
    /// </summary>
    /// <param name="state"></param>
    /// <param name="flexIndex"></param>
    let internal CloseIndex(flexIndex : FlexIndex) = 
        try 
            flexIndex.Token.Cancel()
            for shard in flexIndex.Shards do
                try 
                    shard.SearcherManager.close()
                    shard.IndexWriter.commit()
                    shard.IndexWriter.close()
                with e -> ()
        with e -> () //logger.Error("Error while closing index:" + flexIndex.IndexSetting.IndexName, e)
    
    /// <summary>
    /// Function to check if the requested index is available. If yes then tries to 
    /// retrieve the document template associated with the index from thread local store.
    /// If there is no template document for the requested index then goes ahead
    /// and creates one. 
    /// </summary>
    /// <param name="state"></param>
    /// <param name="indexName"></param>
    let internal GetDocumentTemplate(flexIndex : FlexIndex) = 
        match flexIndex.ThreadLocalStore.IsValueCreated with
        | true -> Choice1Of2(flexIndex, flexIndex.ThreadLocalStore.Value)
        | _ -> 
            let luceneDocument = new Document()
            let fieldLookup = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase)
            let idField = 
                new StringField(flexIndex.IndexSetting.FieldsLookup.[Constants.IdField].SchemaName, "", Field.Store.YES)
            luceneDocument.add (idField)
            fieldLookup.Add(Constants.IdField, idField)
            let lastModifiedField = 
                new LongField(flexIndex.IndexSetting.FieldsLookup.[Constants.LastModifiedField].SchemaName, 
                              GetCurrentTimeAsLong(), Field.Store.YES)
            luceneDocument.add (lastModifiedField)
            fieldLookup.Add(Constants.LastModifiedField, lastModifiedField)
            let lastModifiedFieldDv = 
                new NumericDocValuesField(flexIndex.IndexSetting.FieldsLookup.[Constants.LastModifiedFieldDv].SchemaName, 
                                          GetCurrentTimeAsLong())
            luceneDocument.add (lastModifiedFieldDv)
            fieldLookup.Add(Constants.LastModifiedFieldDv, lastModifiedFieldDv)
            for field in flexIndex.IndexSetting.Fields do
                // Ignore these 4 fields here.
                if (field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField 
                    || field.FieldName = Constants.LastModifiedFieldDv) then ()
                else 
                    let defaultField = FlexField.CreateDefaultLuceneField field
                    luceneDocument.add (defaultField)
                    fieldLookup.Add(field.FieldName, defaultField)
            let documentTemplate = 
                { Document = luceneDocument
                  FieldsLookup = fieldLookup
                  LastGeneration = 0 }
            flexIndex.ThreadLocalStore.Value <- documentTemplate
            Choice1Of2(flexIndex, documentTemplate)
    
    let inline private GetTargetShard(id : string, count : int) = 
        if (count = 1) then 0
        else IndexingHelpers.MapToShard id count
    
    /// <summary>
    /// Updates the current thread local index document with the incoming data
    /// </summary>
    /// <param name="flexIndex"></param>
    /// <param name="documentTemplate"></param>
    /// <param name="documentId"></param>
    /// <param name="version"></param>
    /// <param name="fields"></param>
    let internal UpdateDocument(flexIndex : FlexIndex, document : FlexDocument) = 
        let UpdateFields(documentTemplate) = 
            // Create a dynamic dictionary which will be used during scripting
            let dynamicFields = new DynamicDictionary(document.Fields)
            for field in flexIndex.IndexSetting.Fields do
                // Ignore these 3 fields here.
                if (field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField 
                    || field.FieldName = Constants.LastModifiedFieldDv) then ()
                else 
                    // If it is computed field then generate and add it otherwise follow standard path
                    match field.Source with
                    | Some(s) -> 
                        try 
                            // Wrong values for the data type will still be handled as update Lucene field will
                            // check the data type
                            let value = s.Invoke(dynamicFields)
                            FlexField.UpdateLuceneField field documentTemplate.FieldsLookup.[field.FieldName] value
                        with e -> 
                            FlexField.UpdateLuceneFieldToDefault field documentTemplate.FieldsLookup.[field.FieldName]
                    | None -> 
                        match document.Fields.TryGetValue(field.FieldName) with
                        | (true, value) -> 
                            FlexField.UpdateLuceneField field documentTemplate.FieldsLookup.[field.FieldName] value
                        | _ -> 
                            FlexField.UpdateLuceneFieldToDefault field documentTemplate.FieldsLookup.[field.FieldName]
        maybe { 
            let documentTemplate = flexIndex.ThreadLocalStore.Value
            let targetShard = GetTargetShard(document.Id, flexIndex.Shards.Length)
            let timeStamp = GetCurrentTimeAsLong()
            let! existingVersion = flexIndex.VersioningManager.VersionCheck(document, targetShard, timeStamp)
            if flexIndex.VersioningManager.AddOrUpdate(document.Id, targetShard, timeStamp, existingVersion) then 
                documentTemplate.FieldsLookup.[Constants.IdField].setStringValue(document.Id)
                documentTemplate.FieldsLookup.[Constants.LastModifiedField].setLongValue(timeStamp)
                documentTemplate.FieldsLookup.[Constants.LastModifiedFieldDv].setLongValue(timeStamp)
                UpdateFields(documentTemplate)
                return! Choice1Of2(targetShard, documentTemplate)
            else return! Choice2Of2(Errors.INDEXING_VERSION_CONFLICT |> GenerateOperationMessage)
        }

[<RequireQualifiedAccess; AutoOpen>]
module Lucene = 
    //    /// Creates Lucene index writer configuration from flex index setting 
    //    let private GetIndexWriterConfig(flexIndexSetting : FlexIndexSetting) = 
    //        maybe { 
    //            let! indexVersion = flexIndexSetting.IndexConfiguration.IndexVersion.GetLuceneIndexVersion()
    //            let! codec = flexIndexSetting.IndexConfiguration.IndexVersion.GetDefaultCodec()
    //            let iwc = new IndexWriterConfig(indexVersion, flexIndexSetting.IndexAnalyzer)
    //            iwc.setOpenMode (FlexLucene.index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
    //            iwc.setRAMBufferSizeMB (System.Double.Parse(flexIndexSetting.IndexConfiguration.RamBufferSizeMb.ToString())) 
    //            |> ignore
    //            iwc.setCodec (codec) |> ignore
    //            let! similarityProvider = GetSimilarityProvider(flexIndexSetting)
    //            iwc.setSimilarity (similarityProvider) |> ignore
    //            return! Choice1Of2(iwc)
    //        }
    //    
    //    /// Create a Lucene file-system lock over a directory    
    //    let private GetIndexDirectory (directoryPath : string) (directoryType : DirectoryType) = 
    //        // Note: Might move to SingleInstanceLockFactory to provide other services to open
    //        // the index in read-only mode
    //        let lockFactory = new NativeFSLockFactory()
    //        let file = new java.io.File(directoryPath)
    //        try 
    //            match directoryType with
    //            | DirectoryType.FileSystem -> 
    //                Choice1Of2(FSDirectory.``open`` (file, lockFactory) :> FlexLucene.store.Directory)
    //            | DirectoryType.MemoryMapped -> 
    //                Choice1Of2(MMapDirectory.``open`` (file, lockFactory) :> FlexLucene.store.Directory)
    //            | DirectoryType.Ram -> Choice1Of2(new RAMDirectory() :> FlexLucene.store.Directory)
    //            | _ -> 
    //                Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
    //                           |> GenerateOperationMessage
    //                           |> Append("Message", "Unknown directory type."))
    //        with e -> 
    //            Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
    //                       |> GenerateOperationMessage
    //                       |> Append("Message", e.Message))
    //    
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
            return! ok (indexWriter, trackingIndexWriter)
        }

[<RequireQualifiedAccess; AutoOpen>]
module Queries = 
    /// <summary>
    ///  Returns TermQuery for the id field
    /// </summary>
    /// <param name="value"></param>
    let GetIdTermQuery(value : string) = new Term(Constants.IdField, value)

open Shielded

type ClusterState = 
    { Analyzers : LazyFactory<FlexLucene.Analysis.Analyzer, Analyzer.T>
      Indices : ConcurrentDictionary<string, IndexRegisteration>
      ServerSettings : ServerSettings
      IsClusterMaster : Shielded<bool> }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccessAttribute>]
module ClusterState = 
    let getAllIndiceInfo (cs) = cs.Indices.Values |> Seq.map (fun x -> x.IndexInfo)
    
    let getStatus (indexName) (cs) = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, reg) -> Choice1Of2(reg.IndexState)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    let getIndexInfo (indexName) cs = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, reg) -> Choice1Of2(reg.IndexInfo)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    let getIndex (indexName) cs = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, reg) -> Choice1Of2(reg.Index)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    let getRegisteration (indexName) cs = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, state) -> Choice1Of2(state)
        | _ -> Choice2Of2(Errors.INDEX_REGISTERATION_MISSING |> GenerateOperationMessage)
    
    let isOpen (indexName) cs = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, state) -> 
            match state.IndexState with
            | IndexState.Online -> Choice1Of2(state)
            | _ -> Choice2Of2(Errors.INDEX_IS_OFFLINE |> GenerateOperationMessage)
        | _ -> Choice2Of2(Errors.INDEX_REGISTERATION_MISSING |> GenerateOperationMessage)
    
    let updateStatus (indexName, state) cs = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, reg) -> 
            let newReg = { reg with IndexState = state }
            match cs.Indices.TryUpdate(indexName, newReg, reg) with
            | true -> Choice1Of2()
            | false -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    let removeRegisteration (indexName) cs = 
        match cs.Indices.TryGetValue(indexName) with
        | (true, reg) -> 
            cs.Indices.TryRemove(indexName) |> ignore
            Choice1Of2()
        | _ -> Choice2Of2(Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)
    
    let UpdateRegisteration (indexName : string, state : IndexState, indexInfo : Index, index : FlexIndex option) cs = 
        maybe { 
            assert (indexName <> null)
            // Only write to file for non ram type indices
            if indexInfo.IndexConfiguration.DirectoryType <> DirectoryType.Ram then 
                do! writer.WriteFile<Index>
                        (Path.Combine
                             (serverSettings.DataFolder, indexName, sprintf "conf%s" Constants.SettingsFileExtension), 
                         indexInfo)
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
/// General index settings
/// </summary>
type IndexSetting = 
    { IndexName : string
      IndexAnalyzer : PerFieldAnalyzerWrapper
      SearchAnalyzer : PerFieldAnalyzerWrapper
      Fields : Field.T []
      FieldsLookup : IDictionary<string, Field.T>
      SearchProfiles : IDictionary<string, Predicate * SearchQuery.T>
      ScriptsManager : ScriptsManager
      IndexConfiguration : IndexConfiguration.T
      BaseFolder : string
      ShardConfiguration : ShardConfiguration.T }

/// <summary>
/// Shard writer to write data to physical shard
/// </summary>
type FlexShardWriter = 
    { ShardNumber : int
      SearcherManager : SearcherManager
      IndexWriter : IndexWriter
      TrackingIndexWriter : TrackingIndexWriter }

///// <summary>
///// Represents a dummy Lucene document. There will be one per index stored in a dictionary
///// </summary>
//type ThreadLocalDocument = 
//    { Document : Document
//      FieldsLookup : Dictionary<string, Field>
//      LastGeneration : int }
/// <summary>
/// Represents an index in Flex terms which may consist of a number of
/// valid Lucene indices.
/// </summary>
[<RequireQualifiedAccessAttribute>]
module Index = 
    type T = 
        { IndexSetting : IndexSetting
          Shards : FlexShardWriter []
          VersioningManager : IVersionManager
          Token : CancellationTokenSource }
    
    [<RequireQualifiedAccessAttribute>]
    module Builder = 
        /// Populate postings format and other computed bits of Index Configuration
        let buildIndexConfiguration (c : IndexConfiguration.T) = 
            let defaultIndexPostingsFormat = c.IndexVersion
                                            |> IndexVersion.getDefaultPostingsFormat
                                            |> extract
            let idIndexPostingsFormat = c.IndexVersion
                                       |> IndexVersion.getIdFieldPostingsFormat (not(c.DoNotUseBloomFilterForId))
                                       |> extract
            
            {c with DefaultIndexPostingsFormat = defaultIndexPostingsFormat; IdIndexPostingsFormat = idIndexPostingsFormat}
        
        /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
        /// indexing rather than the individual field analyzer           
        let buildPerFieldAnalyzerWrapper (fields : Field.T [], isIndexAnalyzer : bool) = 
            let analyzerMap = new java.util.HashMap()
            analyzerMap.put (Constants.IdField, CaseInsensitiveKeywordAnalyzer) |> ignore
            analyzerMap.put (Constants.TypeField, CaseInsensitiveKeywordAnalyzer) |> ignore
            analyzerMap.put (Constants.LastModifiedField, CaseInsensitiveKeywordAnalyzer) |> ignore
            fields 
            |> Array.iter 
                   (fun x -> 
                   if isIndexAnalyzer then 
                       match x.FieldType with
                       | FieldType.Custom(a, b, c) -> analyzerMap.put (x.SchemaName, b) |> ignore
                       | FieldType.Highlight(a, b) -> analyzerMap.put (x.SchemaName, b) |> ignore
                       | FieldType.Text(a, b) -> analyzerMap.put (x.SchemaName, b) |> ignore
                       | FieldType.ExactText(a) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.Bool(a) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.Date | FieldType.DateTime | FieldType.Int | FieldType.Double | FieldType.Stored | FieldType.Long -> 
                           ()
                   else 
                       match x.FieldType with
                       | FieldType.Custom(a, b, c) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.Highlight(a, _) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.Text(a, _) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.ExactText(a) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.Bool(a) -> analyzerMap.put (x.SchemaName, a) |> ignore
                       | FieldType.Date | FieldType.DateTime | FieldType.Int | FieldType.Double | FieldType.Stored | FieldType.Long -> 
                           ())
            new PerFieldAnalyzerWrapper(new FlexLucene.Analysis.Standard.StandardAnalyzer(), analyzerMap)
        
        /// Compile all the scripts and initialize the script manager
        let buildScriptManager (scripts : seq<Script.T>) = 
            let profileSelectorScripts = 
                new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
            let computedFieldScripts = 
                new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
            let customScoringScripts = 
                new Dictionary<string, System.Dynamic.DynamicObject * double -> double>(StringComparer.OrdinalIgnoreCase)
            for s in scripts do
                if s.ScriptType = ScriptType.ComputedField then 
                    let a = returnOrFail (Script.compileComputedFieldScript (s.Source))
                    computedFieldScripts.Add(s.ScriptName, a)
            { ComputedFieldScripts = computedFieldScripts
              ProfileSelectorScripts = profileSelectorScripts
              CustomScoringScripts = customScoringScripts }
        
        /// Build FlexField from fields
        let buildFields (fields : List<Field.Dto>, indexConfiguration : IndexConfiguration.T, 
                         analyzerService : LazyFactory<FlexLucene.Analysis.Analyzer, Analyzer>, 
                         scriptsManager : ScriptsManager) = 
            let result = new Dictionary<string, Field.T>(StringComparer.OrdinalIgnoreCase)
            // Add system fields
            result.Add(Constants.IdField, Field.getIdField (indexConfiguration.IdIndexPostingsFormat))
            result.Add
                (Constants.LastModifiedField, Field.getTimeStampField (indexConfiguration.DefaultIndexPostingsFormat))
            result.Add
                (Constants.LastModifiedFieldDv, 
                 Field.getTimeStampDvField (indexConfiguration.DefaultIndexPostingsFormat))
            for field in fields do
                let fieldObject = 
                    returnOrFail (Field.build (field, indexConfiguration, analyzerService, scriptsManager))
                result.Add(field.FieldName, fieldObject)
            result
        
        /// Build search profiles from the Index object
        let buildSearchProfiles (profiles : List<SearchQuery.T>, parser : IFlexParser) = 
            maybe { 
                let result = new Dictionary<string, Predicate * SearchQuery.T>(StringComparer.OrdinalIgnoreCase)
                for profile in profiles do
                    assert (String.IsNullOrWhiteSpace(profile.QueryName) <> true)
                    let! predicate = parser.Parse(profile.QueryString)
                    result.Add(profile.QueryName, (predicate, profile))
                return result
            }
        
        /// Build FlexIndexSetting from an index dto
        let build (index : Index, serverSettings : ServerSettings, 
                   analyzerService : LazyFactory<FlexLucene.Analysis.Analyzer, Analyzer>) = 
            maybe { 
                do! index.Validate()
                let indexConf = buildIndexConfiguration (index.IndexConfiguration)
                let scriptManager = buildScriptManager (index.Scripts)
                let fields = buildFields (index.Fields, index.IndexConfiguration, analyzerService, scriptManager)
                let fieldsArray : Field.T array = Array.zeroCreate fields.Count
                fields.Values.CopyTo(fieldsArray, 0)
                let baseFolder = Path.Combine(serverSettings.DataFolder, index.IndexName)
                let indexAnalyzer = buildPerFieldAnalyzerWrapper (fieldsArray, true)
                let searchAnalyzer = buildPerFieldAnalyzerWrapper (fieldsArray, false)
                let! searchProfiles = buildSearchProfiles (index.SearchProfiles, new Parsers.FlexParser())
                let flexIndexSetting = 
                    { IndexName = index.IndexName
                      IndexAnalyzer = indexAnalyzer
                      SearchAnalyzer = searchAnalyzer
                      Fields = fieldsArray
                      SearchProfiles = searchProfiles
                      ScriptsManager = scriptManager
                      FieldsLookup = fields
                      IndexConfiguration = indexConf
                      ShardConfiguration = index.ShardConfiguration
                      BaseFolder = baseFolder }
                return flexIndexSetting
            }
    
    /// Open an index on the node
    let openIndexOnNode (flexIndexSetting : IndexSetting) (state : ClusterState) = ()
    
    /// Load a given index to node. This is not same as opening an index
    let loadIndexToNode (index : Index) (state : ClusterState) = 
        let getSimilarityProvider (s : IndexSetting) = 
            maybe { 
                let! defaultSimilarity = FieldSimilarity.build (s.IndexConfiguration.DefaultFieldSimilarity)
                let mappings = new Dictionary<string, Similarity>(StringComparer.OrdinalIgnoreCase)
                for field in s.FieldsLookup do
                    // Only add if the format is not same as default postings format
                    if field.Value.Similarity <> s.IndexConfiguration.DefaultFieldSimilarity then 
                        let! similarity = FieldSimilarity.build (field.Value.Similarity)
                        mappings.Add(field.Key, similarity)
                return new FlexPerFieldSimilarityProvider(mappings, defaultSimilarity)
            }
        
        let getIndexWriterConfig (s : IndexSetting) = 
            maybe { 
                let! indexVersion = s.IndexConfiguration.IndexVersion |> IndexVersion.build
                let! codec = s.IndexConfiguration.IndexVersion |> IndexVersion.getDefaultCodec
                let iwc = new IndexWriterConfig(s.IndexAnalyzer)
                iwc.SetOpenMode(FlexLucene.Index.IndexWriterConfig.OpenMode.CREATE_OR_APPEND) |> ignore
                iwc.SetRAMBufferSizeMB(System.Double.Parse(s.IndexConfiguration.RamBufferSizeMb.ToString())) |> ignore
                iwc.SetCodec(codec) |> ignore
                let! similarityProvider = getSimilarityProvider (s)
                iwc.SetSimilarity(similarityProvider) |> ignore
                return iwc
            }
        
        let getIndexWriter (path, s : IndexSetting) = 
            maybe { 
                let ic = s.IndexConfiguration
                if ic.DirectoryType <> DirectoryType.Ram then Directory.CreateDirectory(path) |> ignore
                let! iwc = getIndexWriterConfig (s)
                let! indexDirectory = DirectoryType.getIndexDirectory (ic.DirectoryType, path)
                let indexWriter = new IndexWriter(indexDirectory, iwc)
                return indexWriter
            }
        
        let getTrackingWriter (indexWriter) = new TrackingIndexWriter(indexWriter)
        maybe { 
            /// Generate shards for the newly added index
            let generateShards flexIndexSetting = 
                try 
                    let shards = 
                        Array.init flexIndexSetting.ShardConfiguration.ShardCount (fun a -> 
                            let path = 
                                Path.Combine([| flexIndexSetting.BaseFolder
                                                "shards"
                                                a.ToString()
                                                "index" |])
                            // Only create directory for non-ram index
                            if flexIndexSetting.IndexConfiguration.DirectoryType <> DirectoryType.Ram then 
                                Directory.CreateDirectory(path) |> ignore
                            let writers = IndexingHelpers.GetIndexWriter(flexIndexSetting, path)
                            match writers with
                            | Choice2Of2(e) -> failwith e.UserMessage
                            | Choice1Of2(indexWriter, trackingIndexWriter) -> 
                                let shard = 
                                    { ShardNumber = a
                                      SearcherManager = new SearcherManager(indexWriter, true, new SearcherFactory())
                                      IndexWriter = indexWriter
                                      TrackingIndexWriter = trackingIndexWriter }
                                shard)
                    Choice1Of2(shards)
                with e -> 
                    Choice2Of2(Errors.ERROR_OPENING_INDEXWRITER
                               |> GenerateOperationMessage
                               |> Append("Message", e.Message))
            let! shards = generateShards flexIndexSetting
            let flexIndex = 
                { IndexSetting = flexIndexSetting
                  Shards = shards
                  ThreadLocalStore = new ThreadLocal<ThreadLocalDocument>()
                  VersioningManager = new VersioningManger(flexIndexSetting, shards) :> IVersionManager
                  Token = new System.Threading.CancellationTokenSource() }
            // Add the scheduler for the index
            // Commit Scheduler
            Async.Start
                (ScheduleIndexJob (flexIndexSetting.IndexConfiguration.CommitTimeSeconds * 1000) CommitJob flexIndex)
            // NRT Scheduler
            Async.Start
                (ScheduleIndexJob flexIndexSetting.IndexConfiguration.RefreshTimeMilliseconds RefreshIndexJob flexIndex)
            return! Choice1Of2(flexIndex)
        }
    
    /// Add given index information to node. This is not same as opening an index
    let addIndexToNode (flexIndexSetting : IndexSetting) (state : ClusterState) = ()
    
    /// Add a new index to the cluster. New index can only be added by the cluster master.
    let addIndexToCluster (flexIndexSetting : IndexSetting) (state : ClusterState) = 
        if state.IsClusterMaster.Value then addIndexToNode flexIndexSetting state
        else 
            //TODO: Contact cluster master and add the new index to the cluster 
            failwithf "Not supported"

/// <summary>
/// This is responsible for creating a wrapper around FlexDocument which can be cached and re-used.
/// Note: Make sure that the template is not accessed by multiple threads.
/// </summary>
type DocumentTemplate(indexSettings : IndexSetting) = 
    let documentTemplate = new Document()
    let fieldsLookup = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase)
    
    do 
        let idField = new StringField(indexSettings.FieldsLookup.[Constants.IdField].SchemaName, "", Field.Store.YES)
        documentTemplate.Add(idField)
        fieldsLookup.Add(Constants.IdField, idField)
        let lastModifiedField = 
            new LongField(indexSettings.FieldsLookup.[Constants.LastModifiedField].SchemaName, GetCurrentTimeAsLong(), 
                          Field.Store.YES)
        documentTemplate.Add(lastModifiedField)
        fieldsLookup.Add(Constants.LastModifiedField, lastModifiedField)
        let lastModifiedFieldDv = 
            new NumericDocValuesField(indexSettings.FieldsLookup.[Constants.LastModifiedFieldDv].SchemaName, 
                                      GetCurrentTimeAsLong())
        documentTemplate.Add(lastModifiedFieldDv)
        fieldsLookup.Add(Constants.LastModifiedFieldDv, lastModifiedFieldDv)
        for field in indexSettings.Fields do
            // Ignore these 4 fields here.
            if (field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField 
                || field.FieldName = Constants.LastModifiedFieldDv) then ()
            else 
                let defaultField = FlexField.CreateDefaultLuceneField field
                documentTemplate.add (defaultField)
                fieldsLookup.Add(field.FieldName, defaultField)
    
    /// <summary>
    /// Update the lucene Document based upon the passed FlexDocument.
    /// Note: Do not update the document from multiple threads.
    /// </summary>
    /// <param name="document"></param>
    member __.UpdateTempate(document : FlexDocument) = 
        // Create a dynamic dictionary which will be used during scripting
        let dynamicFields = new DynamicDictionary(document.Fields)
        for field in indexSettings.Fields do
            // Ignore these 3 fields here.
            if (field.FieldName = Constants.IdField || field.FieldName = Constants.LastModifiedField 
                || field.FieldName = Constants.LastModifiedFieldDv) then ()
            else 
                // If it is computed field then generate and add it otherwise follow standard path
                match field.Source with
                | Some(s) -> 
                    try 
                        // Wrong values for the data type will still be handled as update Lucene field will
                        // check the data type
                        let value = s.Invoke(dynamicFields)
                        FlexField.UpdateLuceneField field fieldsLookup.[field.FieldName] value
                    with e -> FlexField.UpdateLuceneFieldToDefault field fieldsLookup.[field.FieldName]
                | None -> 
                    match document.Fields.TryGetValue(field.FieldName) with
                    | (true, value) -> FlexField.UpdateLuceneField field fieldsLookup.[field.FieldName] value
                    | _ -> FlexField.UpdateLuceneFieldToDefault field fieldsLookup.[field.FieldName]
        documentTemplate

/// <summary>
/// Wrapper around SearcherManager to expose .net IDisposable functionality
/// </summary>
type RealTimeSearcher(searchManger : SearcherManager) = 
    let indexSearcher = searchManger.Acquire() :?> IndexSearcher
    
    /// <summary>
    /// Dispose method which will be called automatically through Fody inter-leaving 
    /// </summary>
    member __.DisposeManaged() = searchManger.Release(indexSearcher)
    
    member __.IndexSearcher = indexSearcher
    
    /// <summary>
    /// IndexReader provides an interface for accessing a point-in-time view of 
    /// an index. Any changes made to the index via IndexWriter 
    /// will not be visible until a new IndexReader is opened. 
    /// </summary>
    member __.IndexReader = indexSearcher.GetIndexReader()
    
    interface IDisposable with
        member __.Dispose() : unit = ()

module ShardWriter = 
    /// An IndexWriter creates and maintains an index. This is a wrapper around
    /// Lucene IndexWriter to expose the functionality in a controlled and functional 
    /// manner.
    /// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
    /// SearcherManger through an easy to manage abstraction.
    type T = 
        { IndexWriter : IndexWriter
          TrackingIndexWriter : TrackingIndexWriter
          SearcherManager : SearcherManager
          ShardNo : int }
    
    let create (shardNumber : int, config : IndexWriterConfig, directory : FlexLucene.Store.Directory) = 
        let iw = new IndexWriter(directory, config)
        { IndexWriter = iw
          TrackingIndexWriter = new TrackingIndexWriter(iw)
          SearcherManager = new SearcherManager(directory, new SearcherFactory())
          ShardNo = shardNumber }
    
    /// Commits all pending changes (added & deleted documents, segment merges, added indexes, etc.) to the index, 
    /// and syncs all referenced index files, such that a reader will see the changes and the index updates will 
    /// survive an OS or machine crash or power loss. Note that this does not wait for any running background 
    /// merges to finish. This may be a costly operation, so you should test the cost in your application and 
    /// do it only when really necessary.
    let commit (sw : T) = 
        if sw.IndexWriter.HasUncommittedChanges() then sw.IndexWriter.Commit()
    
    /// Commits all changes to an index, waits for pending merges to complete, closes all 
    /// associated files and releases the write lock.
    let close (sw : T) = 
        try 
            sw.SearcherManager.Close()
            sw.IndexWriter.Close()
        with e as AlreadyClosedException -> ()
    
    /// Adds a document to this index.
    let addDocument (document : Document) (sw : T) = sw.TrackingIndexWriter.AddDocument(document) |> ignore
    
    /// Deletes the document with the given id.
    let deleteDocument (id : string) (sw : T) = sw.TrackingIndexWriter.DeleteDocuments(GetIdTermQuery(id)) |> ignore
    
    /// Delete all documents in the index.
    let deleteAll () (sw : T) = sw.TrackingIndexWriter.DeleteAll()
    
    /// Updates a document by id by first deleting the document containing term and then 
    /// adding the new document.
    let updateDocument (id : string, document : Document) (sw : T) = 
        sw.TrackingIndexWriter.UpdateDocument(GetIdTermQuery(id), document) |> ignore
    
    /// Returns real time searcher. 
    /// Note: Use it with 'use' keyword to automatically return the searcher to the pool
    let getRealTimeSearcher (sw : T) = new RealTimeSearcher(sw.SearcherManager)
    
    /// You must call this periodically, if you want that GetRealTimeSearcher() will return refreshed instances.
    let referesh (sw : T) = sw.SearcherManager.MaybeRefresh() |> ignore
    
    /// Adds a listener, to be notified when a reference is refreshed/swapped.
    let addRefreshListener (item : ReferenceManager.RefreshListener) (sw : T) = sw.SearcherManager.AddListener(item)
    
    /// Remove a listener added with AddRefreshListener.
    let removeRefreshListener (item : ReferenceManager.RefreshListener) (sw : T) = 
        sw.SearcherManager.RemoveListener(item)

module IndexWriter =
    type IndexCommand = 
        | Create of document : FlexDocument

    /// An IndexWriter creates and maintains an index. This is a wrapper around
    /// Lucene IndexWriter to expose the functionality in a controlled and functional 
    /// manner.
    /// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
    /// SearcherManger through an easy to manage abstraction.    
    type T =
        {
            Template : ThreadLocal<DocumentTemplate>
            ShardWriters : ShardWriter.T array
            Settings : IndexSetting        
        }
    
    let create (settings : IndexSetting) =    
        let template = new ThreadLocal<DocumentTemplate>(fun _ -> new DocumentTemplate(settings))
        le indexWriterConfig = IndexConfiguration.getIndexWriterConfiguration()
        let shardWriter = Array.create settings.ShardConfiguration.ShardCount (fun n -> ShardWriter.create(n, ))
type FlexIndexWriter(settings : IndexSetting, mapper : string -> int) = 
    //    let jobDistributor = new ActionBlock<IndexCommand>()
    //    let mainProcessor = new ActionBlock<IndexCommand>()   
    //    let buffer = new BufferBlock<IndexCommand>()
    let threadLocalStore = new ThreadLocal<DocumentTemplate>()
    let shardWriters : FlexShardWriter [] = Array.zeroCreate 5
    let indexAnalyzer = new PerFieldAnalyzerWrapper(new FlexLucene.Analysis.Standard.StandardAnalyzer())
    
    let GetDocumentTemplate() = 
        match threadLocalStore.IsValueCreated with
        | true -> threadLocalStore.Value
        | false -> new DocumentTemplate(settings)
    
    member __.AddDocument(document : FlexSearch.Core.FlexDocument) = 
        shardWriters.[mapper (document.Id)].AddDocument(GetDocumentTemplate().UpdateTempate(document))
    member __.UpdateDocument(document : FlexSearch.Core.FlexDocument) = 
        shardWriters.[mapper (document.Id)].UpdateDocument(document.Id, GetDocumentTemplate().UpdateTempate(document))
    member __.DeleteDocument(id : string) = shardWriters.[mapper (id)].DeleteDocument(id)
    member __.GetRealTimeSearchers() = shardWriters |> Array.map (fun shard -> shard.GetRealTimeSearcher())
    member __.GetRealTimeSearcherForId(id : string) = shardWriters.[mapper (id)].GetRealTimeSearcher()
    member __.Commit() = shardWriters |> Array.iter (fun shard -> shard.Commit())
    member __.Close() = shardWriters |> Array.iter (fun shard -> shard.Close())
    member __.Refresh() = shardWriters |> Array.iter (fun shard -> shard.Referesh())
