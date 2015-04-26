﻿namespace FlexSearch.Core

open FlexLucene.Analysis
open FlexLucene.Analysis.Core
open FlexLucene.Analysis.Miscellaneous
open FlexLucene.Analysis.Standard
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
open java.util

type FieldsMeta = 
    { IdField : Field.T
      TimeStampField : Field.T
      ModifyIndex : Field.T
      Fields : Field.T []
      Lookup : IReadOnlyDictionary<string, Field.T> }

type AnalyzerWrapper(?defaultAnalyzer0 : Analyzer) = 
    inherit DelegatingAnalyzerWrapper(Analyzer.PER_FIELD_REUSE_STRATEGY)
    let mutable map = conDict<Analyzer>()
    let defaultAnalyzer = defaultArg defaultAnalyzer0 (new StandardAnalyzer() :> Analyzer)
    
    /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    /// indexing rather than the individual field analyzer           
    member __.BuildAnalyzer(fields : Field.T [], isIndexAnalyzer : bool) = 
        let analyzerMap = conDict<Analyzer>()
        analyzerMap.[Constants.IdField] <- CaseInsensitiveKeywordAnalyzer
        analyzerMap.[Constants.LastModifiedField] <- CaseInsensitiveKeywordAnalyzer
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

module IndexSetting = 
    /// General index settings
    type T = 
        { IndexName : string
          IndexAnalyzer : AnalyzerWrapper
          SearchAnalyzer : AnalyzerWrapper
          Fields : Field.T []
          FieldsLookup : IReadOnlyDictionary<string, Field.T>
          SearchProfiles : IReadOnlyDictionary<string, Predicate * SearchQuery.Dto>
          ScriptsManager : ScriptsManager
          IndexConfiguration : IndexConfiguration.Dto
          BaseFolder : string
          ShardConfiguration : ShardConfiguration.Dto }

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
              ScriptsManager = Unchecked.defaultof<_>
              IndexConfiguration = Unchecked.defaultof<_>
              BaseFolder = path
              ShardConfiguration = Unchecked.defaultof<_> }
        { Setting = setting }
    
    let withShardConfiguration (conf) (build) = 
        { build with Setting = { build.Setting with ShardConfiguration = conf } }
    
    let withIndexConfiguration (c : IndexConfiguration.Dto) (build) = 
        let defaultIndexPostingsFormat = 
            c.IndexVersion
            |> IndexVersion.getDefaultPostingsFormat
            |> extract
        
        let idIndexPostingsFormat = 
            c.IndexVersion
            |> IndexVersion.getIdFieldPostingsFormat c.UseBloomFilterForId
            |> extract
        
        c.DefaultIndexPostingsFormat <- defaultIndexPostingsFormat
        c.IdIndexPostingsFormat <- idIndexPostingsFormat
        { build with Setting = { build.Setting with IndexConfiguration = c } }
    
    /// Compile all the scripts and initialize the script manager
    let withScripts (scripts : seq<Script.Dto>) (build) = 
        let profileSelectorScripts = 
            new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
        let computedFieldScripts = 
            new Dictionary<string, System.Func<System.Dynamic.DynamicObject, string>>(StringComparer.OrdinalIgnoreCase)
        let customScoringScripts = 
            new Dictionary<string, System.Dynamic.DynamicObject * double -> double>(StringComparer.OrdinalIgnoreCase)
        for s in scripts do
            if s.ScriptType = ScriptType.Dto.ComputedField then 
                let a = returnOrFail (Script.compileComputedFieldScript (s.Source))
                computedFieldScripts.Add(s.ScriptName, a)
        let sm = 
            { ComputedFieldScripts = computedFieldScripts
              ProfileSelectorScripts = profileSelectorScripts
              CustomScoringScripts = customScoringScripts }
        { build with Setting = { build.Setting with ScriptsManager = sm } }
    
    /// Creates per field analyzer for an index from the index field data. These analyzers are used for searching and
    /// indexing rather than the individual field analyzer           
    let buildAnalyzer (fields : Field.T [], isIndexAnalyzer : bool) = 
        let analyzer = new AnalyzerWrapper()
        analyzer.BuildAnalyzer(fields, isIndexAnalyzer)
        analyzer
    
    let withFields (fields : Field.Dto array, analyzerService) (build) = 
        if isNull (build.Setting.ScriptsManager) then 
            failwithf "Internal Error: Script manager should be initialized before creating IndexFields."
        let ic = build.Setting.IndexConfiguration
        let resultLookup = new Dictionary<string, Field.T>(StringComparer.OrdinalIgnoreCase)
        let result = new ResizeArray<Field.T>()
        // Add system fields
        resultLookup.Add(Constants.IdField, Field.getIdField (ic.IdIndexPostingsFormat))
        resultLookup.Add(Constants.LastModifiedField, Field.getTimeStampField (ic.DefaultIndexPostingsFormat))
        for field in fields do
            let fieldObject = returnOrFail (Field.build (field, ic, analyzerService, build.Setting.ScriptsManager))
            resultLookup.Add(field.FieldName, fieldObject)
            result.Add(fieldObject)
        let fieldArr = result.ToArray()
        { build with Setting = 
                         { build.Setting with FieldsLookup = resultLookup
                                              Fields = fieldArr
                                              SearchAnalyzer = buildAnalyzer (fieldArr, false)
                                              IndexAnalyzer = buildAnalyzer (fieldArr, true) } }
    
    /// Build search profiles from the Index object
    let withSearchProfiles (profiles : SearchQuery.Dto array, parser : IFlexParser) (build) = 
        let result = new Dictionary<string, Predicate * SearchQuery.Dto>(StringComparer.OrdinalIgnoreCase)
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
            |> IndexVersion.getDefaultCodec
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

module DocumentTemplate = 
    /// This is responsible for creating a wrapper around Document which can be cached and re-used.
    /// Note: Make sure that the template is not accessed by multiple threads.
    type T = 
        { Setting : IndexSetting.T
          TemplateFields : array<Field>
          Template : Document }
    
    let inline protectedFields (fieldName) = fieldName = Constants.IdField || fieldName = Constants.LastModifiedField
    
    /// Create a new document template            
    let create (s : IndexSetting.T) = 
        let template = new Document()
        let fields = new ResizeArray<Field>()
        
        let add (field) = 
            template.Add(field)
            fields.Add(field)
        add (Field.getStringField (s.FieldsLookup.[Constants.IdField].SchemaName, "", Field.store))
        add (Field.getLongField (s.FieldsLookup.[Constants.LastModifiedField].SchemaName, int64 0, Field.store))
        add (new NumericDocValuesField(s.FieldsLookup.[Constants.LastModifiedField].SchemaName, int64 0))
        for field in s.Fields do
            // Ignore these 4 fields here.
            if not (protectedFields (field.FieldName)) then add (Field.createDefaultLuceneField (field))
        { Setting = s
          TemplateFields = fields.ToArray()
          Template = template }
    
    /// Update the lucene Document based upon the passed FlexDocument.
    /// Note: Do not update the document from multiple threads.
    let updateTempate (document : Document.Dto) (template : T) = 
        // Update meta fields
        // Id Field
        template.TemplateFields.[0].SetStringValue(document.Id)
        // Timestamp fields
        template.TemplateFields.[1].SetLongValue(document.TimeStamp)
        template.TemplateFields.[2].SetLongValue(document.TimeStamp)
        // Create a dynamic dictionary which will be used during scripting
        let dynamicFields = new DynamicDictionary(document.Fields)
        // Performance of F# iter is very slow here.
        let mutable i = 2
        for field in template.Setting.Fields do
            i <- i + 1
            // Ignore these 3 fields here.
            if not (protectedFields (field.FieldName)) then 
                // If it is computed field then generate and add it otherwise follow standard path
                match field.Source with
                | Some(s) -> 
                    try 
                        // Wrong values for the data type will still be handled as update Lucene field will
                        // check the data type
                        let value = s.Invoke(dynamicFields)
                        value |> Field.updateLuceneField field template.TemplateFields.[i]
                    with _ -> Field.updateLuceneFieldToDefault field template.TemplateFields.[i]
                | None -> 
                    match document.Fields.TryGetValue(field.FieldName) with
                    | (true, value) -> value |> Field.updateLuceneField field template.TemplateFields.[i]
                    | _ -> Field.updateLuceneFieldToDefault field template.TemplateFields.[i]
        template.Template

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

/// <summary>
/// Represents the current state of the index.
/// </summary>
type IndexState = 
    | Opening = 1
    | Recovering = 2
    | Online = 3
    | OnlineFollower = 4
    | Offline = 5
    | Closing = 6
    | Faulted = 7

module TransactionLog = 
    //open ProtoBuf
    open MsgPack
    open MsgPack.Serialization
    open System.Text
    
    type Operation = 
        | Create = 1
        | Update = 2
        | Delete = 3
    
    /// Represents a single Transaction log record entry.
    [<CLIMutableAttribute>]
    type T = 
        { TransactionId : int64
          Operation : Operation
          [<NullGuard.AllowNullAttribute>]
          Document : Document.Dto
          /// This will be used for delete operation as we
          /// don't require a document
          [<NullGuard.AllowNullAttribute>]
          Id : string
          /// This will be used for delete operation
          [<NullGuard.AllowNullAttribute>]
          Query : string }
        
        static member Create(tranxId, operation, document, ?id : string, ?query : string) = 
            let id = defaultArg id String.Empty
            let query = defaultArg query String.Empty
            { TransactionId = tranxId
              Operation = operation
              Document = document
              Id = id
              Query = query }
        
        static member Create(txId, id) = 
            { TransactionId = txId
              Operation = Operation.Delete
              Document = Document.Dto.Default
              Id = id
              Query = String.Empty }
    
    let msgPackSerializer = SerializationContext.Default.GetSerializer<T>()
    let serializer (stream, entry : T) = msgPackSerializer.Pack(stream, entry)
    let deSerializer (stream) = msgPackSerializer.Unpack(stream)
    
    type TxWriter(path : string, gen : int64) = 
        let mutable currentGen = gen
        let mutable fileStream = Unchecked.defaultof<_>
        let populateFS() = 
            fileStream <- new FileStream(path +/ gen.ToString(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
        
        let processor = 
            MailboxProcessor.Start(fun inbox -> 
                let rec loop() = 
                    async { 
                        let! (data : byte [], gen) = inbox.Receive()
                        if gen <> currentGen then 
                            fileStream.Close()
                            currentGen <- gen
                            populateFS()
                        // TODO: Test writing async
                        fileStream.Write(data, 0, data.Length)
                        fileStream.Flush()
                        //fileStream.Write(newline, 0, newline.Length)
                        return! loop()
                    }
                populateFS()
                loop())
        
        /// Reads exitisting Tx Log and returns all the entries
        member __.ReadLog(gen : int64) = 
            if File.Exists(path +/ gen.ToString()) then 
                try 
                    seq { 
                        use fileStream = 
                            new FileStream(path +/ gen.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                        while fileStream.Position <> fileStream.Length do
                            yield msgPackSerializer.Unpack(fileStream)
                    }
                with _ -> Seq.empty
            else Seq.empty
        
        /// Append a new entry to TxLog        
        member __.Append(data, gen) = processor.Post(data, gen)
        
        interface IDisposable with
            member __.Dispose() : unit = 
                if not (isNull fileStream) then fileStream.Close()

module ShardWriter = 
    /// Signifies Shard status
    type Status = 
        | Opening
        | Recovering
        | Online
        | Offline
        | Closing
        | Faulted
    
    /// Returns the user commit data to be stored with the index
    let getCommitData (gen : int64) (modifyIndex : int64) = 
        hashMap()
        |> putC (Constants.generationLabel, gen)
        |> putC (Constants.modifyIndex, modifyIndex)
    
    type FileWriter(directory, config) = 
        inherit IndexWriter(directory, config)
        let mutable state = Unchecked.defaultof<T>
        member __.SetState(s : T) = state <- s
        /// A hook for extending classes to execute operations after pending 
        /// added and deleted documents have been flushed to the Directory but 
        /// before the change is committed (new segments_N file written).
        override this.doAfterFlush() = 
            /// State can be null when the index writer is opened for the
            /// very first time
            if not (isNull state) then state.IncrementFlushCount() |> ignore
    
    /// An IndexWriter creates and maintains an index. This is a wrapper around
    /// Lucene IndexWriter to expose the functionality in a controlled and functional 
    /// manner.
    /// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
    /// SearcherManger through an easy to manage abstraction.
    and T = 
        { IndexWriter : FileWriter
          TrackingIndexWriter : TrackingIndexWriter
          SearcherManager : SearcherManager
          TxWriter : TransactionLog.TxWriter
          CommitDuration : int64
          /// Shows the status of the current Shard
          mutable Status : Status
          /// Represents the generation of commit
          Generation : int64
          /// Represents the last commit time. This is used by the
          /// timebased commit to check if auto-commit should take
          /// place or not.
          LastCommitTime : int64
          /// Represents the total outstanding flushes that have occured
          /// since the last commit
          mutable OutstandingFlushes : int32
          /// Represents the current modify index. This is used by for
          /// recovery and shard sync from transaction logs.
          ModifyIndex : int64
          Settings : IndexConfiguration.Dto
          /// Transaction log path to be used
          TxLogPath : string
          ShardNo : int }
        member this.GetNextIndex() = Interlocked.Increment(ref this.ModifyIndex)
        member this.GetNextGen() = Interlocked.Increment(ref this.Generation)
        member this.IncrementFlushCount() = Interlocked.Increment(ref this.OutstandingFlushes)
        member this.ResetFlushCount() = this.OutstandingFlushes <- new Int32()
    
    /// Get the highest modified index value from the shard   
    let getMaxModifyIndex (r : IndexReader) = 
        let mutable max = 0L
        for i = 0 to r.Leaves().size() - 1 do
            let ctx = r.Leaves().get(i) :?> LeafReaderContext
            let reader = ctx.Reader()
            let nDocs = reader.getNumericDocValues ("modifyindex")
            let liveDocs = reader.getLiveDocs()
            for j = 0 to reader.maxDoc() do
                if (liveDocs <> null || liveDocs.get (j)) then max <- Math.Max(max, nDocs.get (j))
        max
    
    /// Commits all pending changes (added & deleted documents, segment merges, added indexes, etc.) to the index, 
    /// and syncs all referenced index files, such that a reader will see the changes and the index updates will 
    /// survive an OS or machine crash or power loss. Note that this does not wait for any running background 
    /// merges to finish. This may be a costly operation, so you should test the cost in your application and 
    /// do it only when really necessary.
    let commit (sw : T) = 
        if sw.IndexWriter.HasUncommittedChanges() 
           && (DateTime.Now.Ticks - sw.LastCommitTime > TimeSpan.TicksPerSecond * sw.CommitDuration 
               || sw.OutstandingFlushes >= sw.Settings.CommitEveryNFlushes) then 
            Interlocked.CompareExchange(ref sw.LastCommitTime, DateTime.Now.Ticks, sw.LastCommitTime) |> ignore
            sw.ResetFlushCount()
            let generation = sw.Generation
            // Increment the generation before committing so that the 
            // newly added items go to the next log file
            sw.GetNextGen() |> ignore
            // Set the new commit data
            getCommitData generation sw.ModifyIndex
            |> sw.IndexWriter.SetCommitData
            |> sw.IndexWriter.Commit
    
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
    let deleteDocument (id : string) (idFieldName : string) (sw : T) = 
        sw.TrackingIndexWriter.DeleteDocuments(id.Term(idFieldName)) |> ignore
    
    /// Delete all documents in the index.
    let deleteAll (sw : T) = sw.TrackingIndexWriter.DeleteAll() |> ignore
    
    /// Updates a document by id by first deleting the document containing term and then 
    /// adding the new document.
    let updateDocument (id : string, idFieldName : string, document : Document) (sw : T) = 
        sw.TrackingIndexWriter.UpdateDocument(id.Term(idFieldName), document) |> ignore
    
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
    
    /// Returns the total number of docs present in the index
    let getDocumentCount (sw : T) = sw.IndexWriter.NumDocs()
    
    /// Create a new shard
    let create (shardNumber : int, settings : IndexConfiguration.Dto, config : IndexWriterConfig, basePath : string, 
                directory : FlexLucene.Store.Directory) = 
        let iw = new FileWriter(directory, config)
        let commitData = iw.GetCommitData()
        
        let generation = 
            let gen = pLong 1L (commitData.getOrDefault (Constants.generationLabel, "1") :?> string)
            // It is a newly created index. 
            if gen = 1L then 
                // Add a dummy commit so that seacher Manager could be initialized
                getCommitData 1L 1L
                |> iw.SetCommitData
                |> iw.Commit
            // Increment the generation as it is used to write the TxLog
            gen + 1L
        
        let trackingWriter = new TrackingIndexWriter(iw)
        let searcherManager = new SearcherManager(iw, true, new SearcherFactory())
        let modifyIndex = pLong 1L (commitData.getOrDefault (Constants.modifyIndex, "1") :?> string)
        let logPath = basePath +/ "shards" +/ shardNumber.ToString() +/ "txlogs"
        Directory.CreateDirectory(logPath) |> ignore
        let state = 
            { IndexWriter = iw
              TrackingIndexWriter = trackingWriter
              SearcherManager = searcherManager
              Generation = generation
              CommitDuration = int64 settings.CommitTimeSeconds
              LastCommitTime = 0L
              OutstandingFlushes = 0
              Status = Opening
              ModifyIndex = modifyIndex
              TxWriter = new TransactionLog.TxWriter(logPath, generation)
              Settings = settings
              TxLogPath = logPath
              ShardNo = shardNumber }
        iw.SetState(state)
        state

/// Version cache store used across the system. This helps in resolving 
/// conflicts arising out of concurrent threads trying to update a Lucene document.
/// Every document update should go through version cache to ensure the update
/// integrity and optimistic locking.
/// In order to reduce contention there will be one CacheStore per shard. 
/// Initially Lucene's LiveFieldValues seemed like a good alternative but it
/// complicates the design and requires thread management
module VersionCache = 
    /// Cache store represented using two concurrent dictionaries
    /// The reason to use two dictionary instead of one is to avoid calling clear method
    /// on the dictionary as it acquires all locks. Also, there is a small span of time
    /// between before and after refresh when we won't have the values in the index
    type T = 
        { mutable Current : ConcurrentDictionary<string, int64>
          mutable Old : ConcurrentDictionary<string, int64>
          IdFieldName : string
          LastModifiedFieldName : string
          ShardWriter : ShardWriter.T }
        
        interface ReferenceManager.RefreshListener with
            member this.afterRefresh (_ : bool) : unit = 
                // Now drop all the old values because they are now
                // visible via the searcher that was just opened; if
                // didRefresh is false, it's possible old has some
                // entries in it, which is fine: it means they were
                // actually already included in the previously opened
                // reader.  So we can safely clear old here:
                this.Old <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
            member this.beforeRefresh() : unit = 
                this.Old <- this.Current
                // Start sending all updates after this point to the new
                // dictionary.  While reopen is running, any lookup will first
                // try this new dictionary, then fall back to old, then to the
                // current searcher:
                this.Current <- new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
        
        interface IDisposable with
            member this.Dispose() = 
                // Remove the listener when disposing the object
                if not (isNull this.ShardWriter.SearcherManager) then 
                    this.ShardWriter |> ShardWriter.removeRefreshListener (this)
    
    let create (settings : IndexSetting.T, shardWriter : ShardWriter.T) = 
        let store = 
            { Current = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
              Old = new ConcurrentDictionary<string, int64>(StringComparer.OrdinalIgnoreCase)
              IdFieldName = settings.FieldsLookup.[Constants.IdField].SchemaName
              LastModifiedFieldName = settings.FieldsLookup.[Constants.LastModifiedField].SchemaName
              ShardWriter = shardWriter }
        shardWriter |> ShardWriter.addRefreshListener (store)
        store
    
    /// Will be used to represent the deleted document version
    [<LiteralAttribute>]
    let deletedValue = 0L
    
    /// An optimized key based lookup to get the version value using Lucene's DocValues
    let primaryKeyLookup (id : string, r : IndexReader) (cache : T) = 
        let term = new Term(cache.IdFieldName, id)
        
        let rec loop counter = 
            let readerContext = r.Leaves().get(counter) :?> LeafReaderContext
            let reader = readerContext.Reader()
            let terms = reader.Terms(cache.IdFieldName)
            assert (terms <> null)
            let termsEnum = terms.iterator (null)
            match termsEnum.SeekExact(term.Bytes()) with
            | true -> 
                let docsEnums = termsEnum.Docs(null, null, 0)
                let nDocs = reader.getNumericDocValues (cache.LastModifiedFieldName)
                nDocs.get (docsEnums.nextDoc())
            | false -> 
                if counter - 1 > 0 then loop (counter - 1)
                else 0L
        if r.Leaves().size() > 0 then loop (r.Leaves().size() - 1)
        else 0L
    
    /// Add or update a key in the cache store
    let addOrUpdate (id : string, version : int64, comparison : int64) (cache : T) = 
        match cache.Current.TryGetValue(id) with
        | true, oldValue -> 
            if comparison = 0L then 
                // It is an unconditional update
                cache.Current.TryUpdate(id, version, oldValue)
            else cache.Current.TryUpdate(id, version, comparison)
        | _ -> cache.Current.TryAdd(id, version)
    
    let delete (id : string, version : Int64) (cache : T) = addOrUpdate (id, deletedValue, version) cache
    
    let getValue (id : string) (cache : T) = 
        match cache.Current.TryGetValue(id) with
        | true, value -> value
        | _ -> 
            // Search old
            match cache.Old.TryGetValue(id) with
            | true, value -> value
            | _ -> 
                // Go to the searcher to get the latest value
                use s = ShardWriter.getRealTimeSearcher (cache.ShardWriter)
                let value = cache |> primaryKeyLookup (id, s.IndexReader)
                cache.Current.TryAdd(id, value) |> ignore
                value
    
    /// Check and returns the current version number of the document
    let versionCheck (doc : Document.Dto, newVersion) (cache : T) = 
        match doc.TimeStamp with
        | 0L -> 
            // We don't care what the version is let's proceed with normal operation
            // and bypass id check.
            ok <| 0L
        | -1L -> // Ensure that the document does not exists. Perform Id check
            let existingVersion = cache |> getValue (doc.Id)
            if existingVersion <> 0L then fail <| DocumentIdAlreadyExists(doc.IndexName, doc.Id)
            else ok <| existingVersion
        | 1L -> 
            // Ensure that the document exists
            let existingVersion = cache |> getValue (doc.Id)
            if existingVersion <> 0L then ok <| existingVersion
            else fail <| DocumentIdNotFound(doc.IndexName, doc.Id)
        | x when x > 1L -> 
            // Perform a version check and ensure that the provided version matches the version of 
            // the document
            let existingVersion = cache |> getValue (doc.Id)
            if existingVersion <> 0L then 
                if existingVersion <> doc.TimeStamp || existingVersion > newVersion then 
                    fail <| IndexingVersionConflict(doc.IndexName, doc.Id, existingVersion.ToString())
                else ok <| existingVersion
            else fail <| DocumentIdNotFound(doc.IndexName, doc.Id)
        | _ -> 
            // This condition should never get executed unless the user has passed a negative version number
            // smaller than -1. In this case we will ignore version number.
            ok <| 0L

module IndexWriter = 
    ///  Method to map a string based id to a Lucene shard 
    /// Uses MurmurHash2 algorithm
    let inline mapToShard shardCount (id : string) = 
        if (shardCount = 1) then 0
        else 
            let byteArray = System.Text.Encoding.UTF8.GetBytes(id)
            MurmurHash2.Hash32(byteArray, 0, byteArray.Length) % shardCount
    
    /// An IndexWriter creates and maintains an index. This is a wrapper around
    /// Lucene IndexWriter to expose the functionality in a controlled and functional 
    /// manner.
    /// Note: This encapsulates the functionality of IndexWriter, TrackingIndexWriter and
    /// SearcherManger through an easy to manage abstraction.    
    type T = 
        { Template : ThreadLocal<DocumentTemplate.T>
          Caches : VersionCache.T array
          ShardWriters : ShardWriter.T array
          State : IndexState
          Settings : IndexSetting.T }
        member this.GetSchemaName(fieldName) = this.Settings.FieldsLookup.[fieldName].SchemaName
    
    /// Create index settings from the Index Dto
    let createIndexSetting (index : Index.Dto, analyzerService) = 
        try 
            withIndexName (index.IndexName, Constants.DataFolder +/ index.IndexName)
            |> withShardConfiguration (index.ShardConfiguration)
            |> withIndexConfiguration (index.IndexConfiguration)
            |> withScripts (index.Scripts)
            |> withFields (index.Fields, analyzerService)
            |> withSearchProfiles (index.SearchProfiles, new FlexParser())
            |> build
            |> ok
        with :? ValidationException as e -> fail <| e.Data0
    
    /// Close the index    
    let close (writer : T) = writer.ShardWriters |> Array.iter (fun s -> ShardWriter.close (s))
    
    let memoryManager = new Microsoft.IO.RecyclableMemoryStreamManager()
    
    /// Add or update a document
    let addOrUpdateDocument (document : Document.Dto, create : bool, addToTxLog : bool) (s : T) = 
        maybe { 
            let shardNo = document.Id |> mapToShard s.ShardWriters.Length
            let newVersion = GetCurrentTimeAsLong()
            let! existingVersion = s.Caches.[shardNo] |> VersionCache.versionCheck (document, newVersion)
            document.TimeStamp <- newVersion
            do! s.Caches.[shardNo]
                |> VersionCache.addOrUpdate (document.Id, newVersion, existingVersion)
                |> boolToResult UnableToUpdateMemory
            let doc = s.Template.Value |> DocumentTemplate.updateTempate document
            let txId = s.ShardWriters.[shardNo].GetNextIndex()
            if addToTxLog then 
                let opCode = 
                    if create then TransactionLog.Operation.Create
                    else TransactionLog.Operation.Update
                
                let txEntry = TransactionLog.T.Create(txId, opCode, document)
                use stream = memoryManager.GetStream()
                TransactionLog.serializer (stream, txEntry)
                s.ShardWriters.[shardNo].TxWriter.Append(stream.ToArray(), s.ShardWriters.[shardNo].Generation)
            s.ShardWriters.[shardNo] 
            |> if create then ShardWriter.addDocument doc
               else ShardWriter.updateDocument (document.Id, (s.GetSchemaName(Constants.IdField)), doc)
        }
    
    /// Add a document to the index
    let addDocument (document : Document.Dto) (s : T) = s |> addOrUpdateDocument (document, true, true)
    
    /// Add a document to the index
    let updateDocument (document : Document.Dto) (s : T) = s |> addOrUpdateDocument (document, false, true)
    
    /// Delete all documents in the index
    let deleteAllDocuments (s : T) = s.ShardWriters |> Array.Parallel.iter (fun s -> ShardWriter.deleteAll (s))
    
    /// Delete a document from index
    let deleteDocument (id : string) (s : T) = 
        maybe { 
            let shardNo = id |> mapToShard s.ShardWriters.Length
            do! s.Caches.[shardNo]
                |> VersionCache.delete (id, VersionCache.deletedValue)
                |> boolToResult UnableToUpdateMemory
            let txId = s.ShardWriters.[shardNo].GetNextIndex()
            let txEntry = TransactionLog.T.Create(txId, id)
            use stream = memoryManager.GetStream()
            TransactionLog.serializer (stream, txEntry)
            s.ShardWriters.[shardNo] |> ShardWriter.deleteDocument id (s.GetSchemaName(Constants.IdField))
        }
    
    /// Refresh the index    
    let refresh (s : T) = s.ShardWriters |> Array.iter (fun shard -> shard |> ShardWriter.referesh)
    
    /// Commit unsaved data to the index
    let commit (s : T) = s.ShardWriters |> Array.iter (fun shard -> shard |> ShardWriter.commit)
    
    let getRealTimeSearchers (s : T) = 
        Array.init s.ShardWriters.Length (fun x -> ShardWriter.getRealTimeSearcher <| s.ShardWriters.[x])
    
    let getRealTimeSearcher (shardNo : int) (s : T) = 
        assert (s.ShardWriters.Length <= shardNo)
        ShardWriter.getRealTimeSearcher <| s.ShardWriters.[shardNo]
    
    /// Returns the total number of docs present in the index
    let getDocumentCount (s : T) = 
        s.ShardWriters |> Array.fold (fun count shard -> ShardWriter.getDocumentCount (shard) + count) 0
    
    /// Replay all the uncommitted transactions from the logs
    let replayTransactionLogs (indexWriter : T) = 
        let replayShardTransaction (shardWriter : ShardWriter.T) = 
            shardWriter.Status <- ShardWriter.Status.Recovering
            // Read logs for the generation 1 higher than the last committed generation as
            // these represents the records which are not committed
            for entry in shardWriter.TxWriter.ReadLog(shardWriter.Generation) do
                match entry.Operation with
                | TransactionLog.Operation.Create | TransactionLog.Operation.Update -> 
                    let doc = indexWriter.Template.Value |> DocumentTemplate.updateTempate entry.Document
                    shardWriter 
                    |> ShardWriter.updateDocument (entry.Id, indexWriter.GetSchemaName(Constants.IdField), doc)
                | TransactionLog.Operation.Delete -> 
                    shardWriter |> ShardWriter.deleteDocument entry.Id (indexWriter.GetSchemaName(Constants.IdField))
                | _ -> ()
            // Just refresh the index so that the changes are picked up
            // in subsequent searches. We can also commit here but it will
            // introduce blank commits in case there are no logs to replay.
            shardWriter |> ShardWriter.referesh
            shardWriter.Status <- ShardWriter.Status.Online
        indexWriter.ShardWriters |> Array.Parallel.iter (fun shard -> replayShardTransaction (shard))
    
    /// Create a new index instance
    let create (settings : IndexSetting.T) = 
        let template = new ThreadLocal<DocumentTemplate.T>((fun _ -> DocumentTemplate.create (settings)), true)
        
        // Create a shard for the index
        let createShard (n) = 
            let path = settings.BaseFolder +/ "shards" +/ n.ToString() +/ "index"
            let indexWriterConfig = IndexWriterConfigBuilder.buildWithSettings (settings)
            let dir = DirectoryType.getIndexDirectory (settings.IndexConfiguration.DirectoryType, path) |> extract
            ShardWriter.create (n, settings.IndexConfiguration, indexWriterConfig, settings.BaseFolder, dir)
        
        let shardWriters = Array.init settings.ShardConfiguration.ShardCount createShard
        let caches = shardWriters |> Array.map (fun x -> VersionCache.create (settings, x))
        
        let indexWriter = 
            { Template = template
              ShardWriters = shardWriters
              Caches = caches
              State = IndexState.Online
              Settings = settings }
        indexWriter |> replayTransactionLogs
        indexWriter
