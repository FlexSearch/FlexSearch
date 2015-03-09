namespace FlexSearch.Core

open System
open System.Collections.Generic
open System.Linq

/// <summary>
/// Similarity defines the components of Lucene scoring. Similarity 
/// determines how Lucene weights terms, and Lucene interacts with 
/// Similarity at both index-time and query-time.
/// </summary>
type FieldSimilarity = 
    /// <summary>
    /// BM25 Similarity defines the components of Lucene scoring.
    /// </summary>
    | BM25 = 1
    /// <summary>
    /// TFIDF Similarity defines the components of Lucene scoring. 
    /// This is the default Lucene similarity.
    /// </summary>
    | TFIDF = 2

/// <summary>
/// A postings format is responsible for encoding/decoding terms, postings, and proximity data.
/// </summary>
type FieldPostingsFormat = 
    /// <summary>
    /// Wraps Lucene41PostingsFormat format for on-disk storage, but then at read time 
    /// loads and stores all terms and postings directly in RAM as byte[], int[].
    ///
    /// WARNING: This is exceptionally RAM intensive: it makes no effort to compress 
    /// the postings data, storing terms as separate byte[] and postings as 
    /// separate int[], but as a result it gives substantial increase in search performance.
    /// </summary>
    | Direct = 1
    /// <summary>
    /// Stores terms and postings (docs, positions, payloads) in RAM, using an FST.
    /// Note that this codec implements advance as a linear scan! This means if you 
    /// store large fields in here, queries that rely on advance will 
    /// (AND BooleanQuery, PhraseQuery) will be relatively slow!
    /// </summary>
    | Memory = 2
    /// <summary>
    /// A PostingsFormat useful for low doc-frequency fields such as primary keys. 
    /// Bloom filters are maintained in a ".blm" file which offers "fast-fail" for 
    /// reads in segments known to have no record of the key. A choice of delegate 
    /// PostingsFormat is used to record all other Postings data.
    ///
    /// A choice of BloomFilterFactory can be passed to tailor Bloom Filter settings 
    /// on a per-field basis. The default configuration is DefaultBloomFilterFactory 
    /// which allocates a ~8mb bit-set and hashes values using MurmurHash2. 
    /// This should be suitable for most purposes.
    ///
    /// NOTE: This uses Lucene 4.1 postings format as a wrapper.
    /// </summary>
    | Bloom_4_1 = 3
    /// <summary>
    /// This postings format "in-lines" the postings for terms that have low docFreq. 
    /// It wraps another postings format, which is used for writing the non in-lined terms. 
    ///
    /// NOTE: This uses Lucene 4.1 postings format as a wrapper.
    /// </summary>
    | Pulsing_4_1 = 4
    /// <summary>
    /// Lucene 4.1 postings format, which encodes postings in packed integer blocks 
    /// for fast decode.
    /// </summary>
    | Lucene_4_1 = 5

/// <summary>
/// A Directory is a flat list of files. Files may be written once, when they are created. 
/// Once a file is created it may only be opened for read, or deleted. Random access is 
/// permitted both when reading and writing.
/// </summary>
type DirectoryType = 
    /// <summary>
    /// FileSystem Directory is a straightforward implementation using java.io.RandomAccessFile. 
    /// However, it has poor concurrent performance (multiple threads will bottleneck) 
    /// as it synchronizes when multiple threads read from the same file.
    /// </summary>
    | FileSystem = 1
    /// <summary>
    /// File-based Directory implementation that uses memory map for reading, and 
    /// FSDirectory.FSIndexOutput for writing.
    /// 
    /// NOTE: memory mapping uses up a portion of the virtual memory address space 
    /// in your process equal to the size of the file being mapped. Before using this 
    /// class, be sure your have plenty of virtual address space, e.g. by using a 64 
    /// bit JRE, or a 32 bit JRE with indexes that are guaranteed to fit within the 
    /// address space. On 32 bit platforms also consult MMapDirectory(File, LockFactory, 
    /// int) if you have problems with mmap failing because of fragmented address 
    /// space. If you get an OutOfMemoryException, it is recommended to reduce the 
    /// chunk size, until it works.
    /// 
    /// Due to this bug in Sun's JRE, MMapDirectory's IndexInput.close() is unable 
    /// to close the underlying OS file handle. Only when GC finally collects the 
    /// underlying objects, which could be quite some time later, will the file handle
    /// be closed.
    /// 
    /// This will consume additional transient disk usage: on Windows, attempts to 
    /// delete or overwrite the files will result in an exception; on other platforms, 
    /// which typically have a "delete on last close" semantics, while such operations
    /// will succeed, the bytes are still consuming space on disk. For many 
    /// applications this limitation is not a problem (e.g. if you have plenty of 
    /// disk space, and you don't rely on overwriting files on Windows) but it's 
    /// still an important limitation to be aware of.
    /// </summary>
    | MemoryMapped = 2
    /// <summary>
    /// A memory-resident Directory implementation. This is not intended to work with 
    /// huge indexes. Everything beyond several hundred megabytes will waste resources 
    /// (GC cycles), because it uses an internal buffer size of 1024 bytes, producing 
    /// millions of byte[1024] arrays. This class is optimized for small memory-resident 
    /// indexes. It also has bad concurrency on multi-threaded environments.
    /// 
    /// It is recommended to materialize large indexes on disk and use MMapDirectory, 
    /// which is a high-performance directory implementation working directly on the 
    /// file system cache of the operating system.
    /// </summary>
    | Ram = 3

/// <summary>
/// These options instruct FlexSearch to maintain full term vectors for each document, 
/// optionally including the position and offset information for each term occurrence 
/// in those vectors. These can be used to accelerate highlighting and other ancillary 
/// functionality, but impose a substantial cost in terms of index size. These can 
/// only be configured for custom field type.
/// </summary>
type FieldTermVector = 
    /// <summary>
    /// Do not store term vectors.
    /// </summary>
    | DoNotStoreTermVector = 1
    /// <summary>
    /// Store the term vectors of each document. A term vector is a list of the 
    /// document's terms and their number of occurrences in that document.
    /// </summary>
    | StoreTermVector = 2
    /// <summary>
    /// Store the term vector and token position information
    /// </summary>
    | StoreTermVectorsWithPositions = 3
    /// <summary>
    /// Store the term vector, Token position and offset information
    /// </summary>
    | StoreTermVectorsWithPositionsandOffsets = 4

/// <summary>
/// Controls how much information is stored in the postings lists.
/// </summary>
type FieldIndexOptions = 
    /// <summary>
    /// Only documents are indexed: term frequencies and positions are omitted.
    /// </summary>
    | DocsOnly = 1
    /// <summary>
    /// Only documents and term frequencies are indexed: positions are omitted.
    /// </summary>
    | DocsAndFreqs = 2
    /// <summary>
    /// Indexes documents, frequencies and positions
    /// </summary>
    | DocsAndFreqsAndPositions = 3
    /// <summary>
    /// Indexes documents, frequencies, positions and offsets.
    /// </summary>
    | DocsAndFreqsAndPositionsAndOffsets = 4

/// <summary>
/// The field type defines how FlexSearch should interpret data in a field and how the 
/// field can be queried. There are many field types included with FlexSearch by default, 
/// and custom types can also be defined.
/// </summary>
type FieldType = 
    /// <summary>
    /// Integer
    /// </summary>
    | Int = 1
    /// <summary>
    /// Double
    /// </summary>
    | Double = 2
    /// <summary>
    /// Field to store keywords. The entire input will be treated as a single word. This is 
    /// useful for fields like customerid, referenceid etc. These fields only support complete 
    /// text matching while searching and no partial word match is available.
    /// </summary>
    | ExactText = 3
    /// <summary>
    /// General purpose field to store normal textual data
    /// </summary>
    | Text = 4
    /// <summary>
    /// Similar to Text field but supports highlighting of search results
    /// </summary>
    | Highlight = 5
    /// <summary>
    /// Boolean
    /// </summary>
    | Bool = 6
    /// <summary>
    /// Fixed format date field (Supported format: YYYYmmdd)
    /// </summary>
    | Date = 7
    /// <summary>
    /// Fixed format datetime field (Supported format: YYYYMMDDhhmmss)
    /// </summary>
    | DateTime = 8
    /// <summary>
    /// Custom field type which gives more granular control over the field configuration
    /// </summary>
    | Custom = 9
    /// <summary>
    /// Non-indexed field. Only used for retrieving stored text. Searching is not
    /// possible over these fields.
    /// </summary>
    | Stored = 10
    /// <summary>
    /// Long
    /// </summary>
    | Long = 11

/// <summary>
/// Corresponds to Lucene Index version. There will
/// always be a default codec associated with each index version.
/// </summary>
type IndexVersion = 
    /// <summary>
    /// Lucene 4.9 index format
    /// </summary>
    | Lucene_4_9 = 1
    /// <summary>
    /// Lucene 4.10 index format
    /// </summary>
    | Lucene_4_10 = 2
    /// <summary>
    /// Lucene 4.10.1 index format
    /// </summary>
    | Lucene_4_10_1 = 3

/// <summary>
/// Scripts can be used to automate various processing in FlexSearch. Script Type signifies
/// the type of operation that the current script can perform. These can vary from scripts
/// used for computing fields dynamically at index time or scripts which can be used to alter
/// FlexSearch's default scoring.
/// </summary>
type ScriptType = 
    /// <summary>
    /// Can be used to dynamically select a search profile based upon the given input.
    /// Not available in the current version.
    /// </summary>
    | SearchProfileSelector = 1
    /// <summary>
    /// Can be used to modify the default scoring of the engine.
    /// Not available at the moment.
    /// </summary>
    | CustomScoring = 2
    /// <summary>
    /// Can be used to dynamically compute fields at index time. For example one can write
    /// a script to generate full name automatically from first name and last name. These
    /// get executed at index time only.
    /// </summary>
    | ComputedField = 3

/// <summary>
/// Represents the current state of the index.
/// </summary>
type IndexState = 
    /// <summary>
    /// Index is opening. 
    /// </summary>
    | Opening = 1
    /// <summary>
    /// Index is Online.
    /// </summary>
    | Online = 2
    /// <summary>
    /// Index is off-line.
    /// </summary>
    | Offline = 3
    /// <summary>
    /// Index is closing
    /// </summary>
    | Closing = 4

/// <summary>
/// Represents the status of job.
/// </summary>
type JobStatus = 
    /// <summary>
    /// Job is currently initializing. This essentially means that the job is added to the 
    /// job queue but has started executing. Depending upon the type of connector a job can take
    /// a while to move from this status. This also depends upon the parallel capability of the
    // connector. Most of the connectors are designed to perform one operation at a time. 
    /// </summary>
    | Initializing = 1
    /// <summary>
    /// Job is initialized. All the parameters supplied as a part of the job are correct and job has 
    /// been scheduled successfully.
    /// </summary>
    | Initialized = 2
    /// <summary>
    /// Job is currently being executed by the engine. 
    /// </summary>
    | InProgress = 3
    /// <summary>
    /// Job has finished without errors.
    /// </summary>
    | Completed = 4
    /// <summary>
    /// Job has finished with errors. Check the message property to get error details. Jobs have access to
    /// the engine's logging service so they could potentially write more error information to the logs.
    /// </summary>
    | CompletedWithErrors = 5

/// <summary>
/// Missing Value option is used by FlexSearch in conjunction with Search profile based query. This tells the
/// engine about how to resolve an error if a field which is used in the search profile is not supplied by the 
/// caller. For example, if the profile requires `firstname` and it is not supplied by the caller then the
/// missing value option can be used to configure the engine's behaviour.
/// </summary>
type MissingValueOption = 
    /// <summary>
    /// Throw an error with the information about the missing field.
    /// </summary>
    | ThrowError = 1
    /// <summary>
    /// Use the default value supplied by the user as a part of search profile configuration.
    /// </summary>
    | Default = 2
    /// <summary>
    /// Ignore the missing field related conditions. This is essentially replacing the condition with
    /// a match all condition.
    /// </summary>
    | Ignore = 3

/// <summary>
/// Allows to control various Index Shards related settings.
/// </summary>
[<ToString; Sealed>]
type ShardConfiguration() = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Total number of shards to be present in the given index.
    /// </summary>
    member val ShardCount = 1 with get, set
    
    override this.Validate() = this.ShardCount |> gt ("ShardCount") 1

/// <summary>
/// Allows to control various Index related settings.
/// </summary>
[<ToString; Sealed>]
type IndexConfiguration() = 
    inherit ValidatableBase()
    
    /// <summary>
    /// The amount of time in seconds that FlexSearch should wait before committing changes to the disk.
    /// </summary>
    member val CommitTimeSeconds = 60 with get, set
    
    /// <summary>
    /// A Directory is a flat list of files. Files may be written once, when they are created. Once a 
    /// file is created it may only be opened for read, or deleted. Random access is permitted both 
    /// when reading and writing.
    /// </summary>
    member val DirectoryType = DirectoryType.MemoryMapped with get, set
    
    /// <summary>
    /// The default maximum time to wait for a write lock (in milliseconds).
    /// </summary>
    member val DefaultWriteLockTimeout = 1000 with get, set
    
    /// <summary>
    /// Determines the amount of RAM that may be used for buffering added documents and deletions 
    /// before they are flushed to the Directory.
    /// </summary>
    member val RamBufferSizeMb = 100 with get, set
    
    /// <summary>
    /// The amount of time in milliseconds that FlexSearch should wait before reopening index reader. 
    /// This helps in keeping writing and real time aspects of the engine separate.
    /// </summary>
    member val RefreshTimeMilliseconds = 25 with get, set
    
    /// <summary>
    /// Corresponds to Lucene Index version. There will
    /// always be a default codec associated with each index version.
    /// </summary>
    member val IndexVersion = IndexVersion.Lucene_4_10_1 with get, set
    
    /// <summary>
    /// A postings format is responsible for encoding/decoding terms, postings, and proximity data.
    /// </summary>
    member val IdFieldPostingsFormat = FieldPostingsFormat.Bloom_4_1 with get, set
    
    /// <summary>
    /// This will be computed at run time based on the index version
    /// </summary>
    member val DefaultIndexPostingsFormat = Unchecked.defaultof<FieldPostingsFormat> with get, set
    
    /// <summary>
    /// Similarity defines the components of Lucene scoring. Similarity determines how Lucene weights terms,
    /// and Lucene interacts with Similarity at both index-time and query-time.
    /// </summary>
    member val DefaultFieldSimilarity = FieldSimilarity.TFIDF with get, set
    
    override this.Validate() = this.CommitTimeSeconds
                               |> gte "CommitTimeSeconds" 15
                               >>= (fun _ -> this.RamBufferSizeMb |> gte "RamBufferSizeMb" 20)
                               >>= (fun _ -> this.RefreshTimeMilliseconds |> gte "RefreshTimeMilliseconds" 25)

/// <summary>
/// A field is a section of a Document. 
/// <para>
/// Fields can contain different kinds of data. A name field, for example, 
/// is text (character data). A shoe size field might be a floating point number 
/// so that it could contain values like 6 and 9.5. Obviously, the definition of 
/// fields is flexible (you could define a shoe size field as a text field rather
/// than a floating point number, for example), but if you define your fields correctly, 
/// FlexSearch will be able to interpret them correctly and your users will get better 
/// results when they perform a query.
/// </para>
/// <para>
/// You can tell FlexSearch about the kind of data a field contains by specifying its 
/// field type. The field type tells FlexSearch how to interpret the field and how 
/// it can be queried. When you add a document, FlexSearch takes the information in 
/// the document’s fields and adds that information to an index. When you perform a 
/// query, FlexSearch can quickly consult the index and return the matching documents.
/// </para>
/// </summary>
[<ToString; Sealed>]
type Field(fieldName : string, fieldType : FieldType) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Name of the field.
    /// </summary>
    member val FieldName = fieldName with get, set
    
    /// <summary>
    /// Signifies if the field should be analyzed using an analyzer. 
    /// </summary>
    member val Analyze = true with get, set
    
    /// <summary>
    /// Signifies if a field should be indexed. A field can only be 
    /// stored without indexing.
    /// </summary>
    member val Index = true with get, set
    
    /// <summary>
    /// Signifies if a field should be stored so that it can retrieved
    /// while searching.
    /// </summary>
    member val Store = true with get, set
    
    /// <summary>
    /// Analyzer to be used while indexing.
    /// </summary>
    member val IndexAnalyzer = StandardAnalyzer with get, set
    
    /// <summary>
    /// Analyzer to be used while searching.
    /// </summary>
    member val SearchAnalyzer = StandardAnalyzer with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val FieldType = fieldType with get, set
    
    /// <summary>
    ///  AUTO
    /// </summary>
    member val PostingsFormat = FieldPostingsFormat.Lucene_4_1 with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val Similarity = FieldSimilarity.TFIDF with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val IndexOptions = FieldIndexOptions.DocsAndFreqsAndPositions with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val TermVector = FieldTermVector.DoNotStoreTermVector with get, set
    
    /// <summary>
    /// If true, omits the norms associated with this field (this disables length 
    /// normalization and index-time boosting for the field, and saves some memory). 
    /// Defaults to true for all primitive (non-analyzed) field types, such as int, 
    /// float, data, bool, and string. Only full-text fields or fields that need an 
    /// index-time boost need norms.
    /// </summary>
    member val OmitNorms = true with get, set
    
    /// <summary>
    /// Fields can get their content dynamically through scripts. This is the name of 
    /// the script to be used for getting field data at index time.
    /// </summary>
    member val ScriptName = "" with get, set
    
    new(fieldName : string) = Field(fieldName, FieldType.Text)
    new() = Field(Unchecked.defaultof<string>, FieldType.Text)
    override this.Validate() = 
        this.FieldName
        |> propertyNameValidator "FieldName"
        >>= (fun _ -> 
        if (this.FieldType = FieldType.Text || this.FieldType = FieldType.Highlight || this.FieldType = FieldType.Custom) 
           && (String.IsNullOrWhiteSpace(this.SearchAnalyzer) || String.IsNullOrWhiteSpace(this.IndexAnalyzer)) then 
            fail (AnalyzerIsMandatory(this.FieldName))
        else ok())

/// <summary>
/// Filters consume input and produce a stream of tokens. In most cases a filter looks 
/// at each token in the stream sequentially and decides whether to pass it along, 
/// replace it or discard it. A filter may also do more complex analysis by looking 
/// ahead to consider multiple tokens at once, although this is less common. 
/// </summary>
[<ToString; Sealed>]
type TokenFilter(filterName : string) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// The name of the filter. Some pre-defined filters are the following-
    /// + Ascii Folding Filter
    /// + Standard Filter
    /// + Beider Morse Filter
    /// + Double Metaphone Filter
    /// + Caverphone2 Filter
    /// + Metaphone Filter
    /// + Refined Soundex Filter
    /// + Soundex Filter
    /// + Keep Words Filter
    /// + Length Filter
    /// + Lower Case Filter
    /// + Pattern Replace Filter
    /// + Stop Filter
    /// + Synonym Filter
    /// + Reverse String Filter
    /// + Trim Filter
    /// For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
    /// </summary>
    member val FilterName = filterName with get, set
    
    /// <summary>
    /// Parameters required by the filter.
    /// </summary>
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    new() = TokenFilter(Unchecked.defaultof<string>)
    override this.Validate() = this.FilterName |> propertyNameValidator "FilterName"

/// <summary>
/// Tokenizer breaks up a stream of text into tokens, where each token is a sub-sequence
/// of the characters in the text. An analyzer is aware of the field it is configured 
/// for, but a tokenizer is not.
/// </summary>
[<ToString; Sealed>]
type Tokenizer(tokenizerName : string) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// The name of the tokenizer. Some pre-defined tokenizers are the following-
    /// + Standard Tokenizer
    /// + Classic Tokenizer
    /// + Keyword Tokenizer
    /// + Letter Tokenizer
    /// + Lower Case Tokenizer
    /// + UAX29 URL Email Tokenizer
    /// + White Space Tokenizer
    /// For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
    /// </summary>
    member val TokenizerName = tokenizerName with get, set
    
    /// <summary>
    /// Parameters required by the tokenizer.
    /// </summary>
    member val Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    new() = Tokenizer(Unchecked.defaultof<_>)
    override this.Validate() = this.TokenizerName |> propertyNameValidator "TokenizerName"

/// <summary>
/// An analyzer examines the text of fields and generates a token stream.
/// </summary>
[<ToString; Sealed>]
type Analyzer() = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Name of the analyzer
    /// </summary>
    member val AnalyzerName = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val Tokenizer = Unchecked.defaultof<Tokenizer> with get, set
    
    /// <summary>
    /// Filters to be used by the analyzer.
    /// </summary>
    member val Filters = new List<TokenFilter>() with get, set
    
    override this.Validate() = this.AnalyzerName
                               |> propertyNameValidator "AnalyzerName"
                               >>= this.Tokenizer.Validate
                               >>= fun _ -> seqValidator (this.Filters.Cast<ValidatableBase>())

/// <summary>
/// Script is used to add scripting capability to the index. These can be used to generate dynamic
/// field values based upon other indexed values or to modify scores of the returned results.
/// Any valid C# expression can be used as a script.
/// </summary>
[<ToString; Sealed>]
type Script(scriptName : string, source : string, scriptType : ScriptType) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Name of the script.
    /// </summary>
    member val ScriptName = scriptName with get, set
    
    /// <summary>
    /// Source code of the script. 
    /// </summary>
    member val Source = source with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val ScriptType = scriptType with get, set
    
    new() = Script(Unchecked.defaultof<string>, Unchecked.defaultof<string>, ScriptType.ComputedField)
    override this.Validate() = this.ScriptName
                               |> propertyNameValidator "ScriptName"
                               >>= fun _ -> this.Source |> notEmpty "Source"

/// <summary>
/// Used for configuring the settings for text highlighting in the search results
/// </summary>
[<ToString; Sealed>]
type HighlightOption(fields : List<string>) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Total number of fragments to return per document
    /// </summary>
    member val FragmentsToReturn = 2 with get, set
    
    /// <summary>
    /// The fields to be used for text highlighting
    /// </summary>
    member val HighlightedFields = fields with get, set
    
    /// <summary>
    /// Post tag to represent the ending of the highlighted word
    /// </summary>
    member val PostTag = "</B>" with get, set
    
    /// <summary>
    /// Pre tag to represent the ending of the highlighted word
    /// </summary>
    member val PreTag = "<B>" with get, set
    
    new() = HighlightOption(Unchecked.defaultof<List<string>>)
    override this.Validate() = ok()

/// <summary>
/// Search query is used for searching over a FlexSearch index. This provides
/// a consistent syntax to execute various types of queries. The syntax is similar
/// to the SQL syntax. This was done on purpose to reduce the learning curve.
/// </summary>
[<ToString; Sealed>]
type SearchQuery(index : string, query : string) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Unique name of the query. This is only required if you are setting up a 
    /// search profile.
    /// </summary>
    member val QueryName = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Columns to be returned as part of results.
    /// + *  - return all columns
    /// + [] - return no columns
    /// + ["columnName"] -  return specific column
    /// </summary>
    member val Columns = new List<string>() with get, set
    
    /// <summary>
    /// Count of results to be returned
    /// </summary>
    member val Count = 10 with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val Highlights = Unchecked.defaultof<HighlightOption> with get, set
    
    /// <summary>
    /// Name of the index
    /// </summary>
    member val IndexName = index with get, set
    
    /// <summary>
    /// Can be used to order the results by score or specific field.
    /// </summary>
    member val OrderBy = "score" with get, set
    
    /// <summary>
    /// Used to enable paging and skip certain pre-fetched results.
    /// </summary>
    member val Skip = 0 with get, set
    
    /// <summary>
    /// Query string to be used for searching
    /// </summary>
    member val QueryString = query with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val MissingValueConfiguration = new Dictionary<string, MissingValueOption>(StringComparer.OrdinalIgnoreCase) with get, set
    
    /// <summary>
    /// Universal configuration for the missing field values. Only applicable
    /// for search profiles.
    /// </summary>
    member val GlobalMissingValue = MissingValueOption.Default with get, set
    
    /// <summary>
    /// If true will return collapsed search results which are in tabular form.
    /// Flat results enable easy binding to a grid but grouping results is tougher
    /// with Flat result.
    /// </summary>
    member val ReturnFlatResult = false with get, set
    
    /// <summary>
    /// If true then scores are returned as a part of search result.
    /// </summary>
    member val ReturnScore = true with get, set
    
    /// <summary>
    /// Profile Name to be used for profile based searching.
    /// </summary>
    member val SearchProfile = Unchecked.defaultof<string> with get, set
    
    /// <summary>
    /// Script which can be used to select a search profile. This can help in
    /// dynamic selection of search profile based on the incoming data.
    /// </summary>
    member val SearchProfileSelector = Unchecked.defaultof<string> with get, set
    
    new() = SearchQuery(Unchecked.defaultof<_>, Unchecked.defaultof<_>)
    override this.Validate() = this.IndexName |> propertyNameValidator "IndexName"

/// <summary>
/// Represents the search result document returned by FlexSearch.
/// </summary>
[<ToString; Sealed>]
type ResultDocument(indexName : string, id : string) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Fields which are part of the returned document
    /// </summary>
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    /// <summary>
    /// Any matched text highlighted snippets.
    /// </summary>
    member val Highlights = new List<string>() with get, set
    
    /// <summary>
    /// Score of the returned document.
    /// </summary>
    member val Score = 0.0 with get, set
    
    /// <summary>
    /// Unique Id.
    /// </summary>
    member val Id = id with get, set
    
    /// <summary>
    /// Timestamp of the last modification of the document
    /// </summary>
    member val TimeStamp = Unchecked.defaultof<Int64> with get, set
    
    /// <summary>
    /// Name of the index
    /// </summary>
    member val IndexName = indexName with get, set
    
    new() = ResultDocument(Unchecked.defaultof<string>, Unchecked.defaultof<string>)
    override this.Validate() = this.IndexName
                               |> notEmpty "IndexName"
                               >>= fun _ -> this.Id |> notEmpty "Id"

/// <summary>
/// A document represents the basic unit of information which can be added or retrieved from the index. 
/// A document consists of several fields. A field represents the actual data to be indexed. In database 
/// analogy an index can be considered as a table while a document is a row of that table. Like a table a 
/// FlexSearch document requires a fix schema and all fields should have a field type.
/// </summary>
[<ToString; Sealed>]
type FlexDocument(indexName : string, id : string) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Fields to be added to the document for indexing.
    /// </summary>
    /// <remarks>
    /// Field names should be unique.
    /// </remarks>
    member val Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) with get, set
    
    /// <summary>
    /// Unique Id of the document
    /// </summary>
    member val Id = id with get, set
    
    /// <summary>
    /// Timestamp of the last modification of the document. This field is interpreted differently
    /// during a create and update operation. It also dictates whether and unique Id check is to be performed
    ///  or not. 
    /// Version number semantics
    /// + 0 - Don't care about the version and proceed with the operation normally.
    /// + -1 - Ensure that the document does not exist (Performs unique Id check).
    /// + 1 - Ensure that the document does exist. This is not relevant for create operation.
    /// > 1 - Ensure that the version matches exactly. This is not relevant for create operation.
    /// </summary>
    member val TimeStamp = Unchecked.defaultof<Int64> with get, set
    
    /// <summary>
    /// Name of the index
    /// </summary>
    member val IndexName = indexName with get, set
    
    new() = FlexDocument(Unchecked.defaultof<string>, Unchecked.defaultof<string>)
    override this.Validate() = this.IndexName
                               |> notEmpty "IndexName"
                               >>= fun _ -> this.Id |> notEmpty "Id"

/// <summary>
/// FlexSearch index is a logical index built on top of Lucene’s index in a manner 
/// to support features like schema and sharding. So in this sense a FlexSearch 
/// index consists of multiple Lucene’s index. Also, each FlexSearch shard is a valid 
/// Lucene index.
///
/// In case of a database analogy an index represents a table in a database where 
/// one has to define a schema upfront before performing any kind of operation on 
/// the table. There are various properties that can be defined at the index creation 
/// time. Only IndexName is a mandatory property, though one should always define 
/// Fields in an index to make any use of it.
///
/// By default a newly created index stays off-line. This is by design to force the 
/// user to enable an index before using it.
/// </summary>
[<ToString; Sealed>]
type Index(indexName : string) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Name of the index
    /// </summary>
    member val IndexName = indexName with get, set
    
    /// <summary>
    /// Fields to be used in index.
    /// </summary>
    member val Fields = new List<Field>() with get, set
    
    /// <summary>
    /// Scripts to be used in index.
    /// </summary>
    member val Scripts = new List<Script>() with get, set
    
    /// <summary>
    /// Search Profiles
    /// </summary>
    member val SearchProfiles = new List<SearchQuery>() with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val ShardConfiguration = new ShardConfiguration() with get, set
    
    /// <summary>
    /// AUTO
    /// </summary>
    member val IndexConfiguration = new IndexConfiguration() with get, set
    
    /// <summary>
    /// Signifies if the index is on-line or not? An index has to be 
    /// on-line in order to enable searching over it.
    /// </summary>
    member val Online = false with get, set
    
    new() = Index(Unchecked.defaultof<string>)
    member this.AddField(fieldName : string) = this.Fields.Add(new Field(fieldName))
    member this.AddField(fieldName : string, fieldType : FieldType) = this.Fields.Add(new Field(fieldName, fieldType))
    override this.Validate() = 
        let checkDuplicateFieldName() = 
            let duplicateFieldNames = 
                query { 
                    for field in this.Fields do
                        groupBy field.FieldName into g
                        where (g.Count() > 1)
                        select g.Key
                }
            if duplicateFieldNames.Count() <> 0 then fail (DuplicateFieldValue("Fields", "FieldName"))
            else ok()
        
        let checkDuplicateScriptNames() = 
            let duplicateScriptNames = 
                query { 
                    for script in this.Scripts do
                        groupBy script.ScriptName into g
                        where (g.Count() > 1)
                        select g.Key
                }
            if duplicateScriptNames.Count() <> 0 then fail (DuplicateFieldValue("Scripts", "ScriptName"))
            else ok()
        
        let checkDuplicateQueries() = 
            let duplicateSearchQueries = 
                query { 
                    for script in this.SearchProfiles do
                        groupBy script.QueryName into g
                        where (g.Count() > 1)
                        select g.Key
                }
            if duplicateSearchQueries.Count() <> 0 then fail (DuplicateFieldValue("SearchProfiles", "QueryName"))
            else ok()
        
        // Check if the script specified against a fields exists
        let checkScriptExists() = 
            let result = 
                this.Fields
                |> Seq.map (fun field -> 
                       if String.IsNullOrWhiteSpace(field.ScriptName) = false then 
                           if this.Scripts.FirstOrDefault(fun x -> x.ScriptName = field.ScriptName) = Unchecked.defaultof<Script> then 
                               fail (ScriptNotFound(field.ScriptName, field.FieldName))
                           else ok()
                       else ok())
                |> Seq.filter (fun x -> failed x)
                |> Seq.toArray
            if result.Count() = 0 then ok()
            else result.[0]
        
        let validateSearchQuery() = 
            // Check if any query name is missing in search profiles. Cannot do this through annotation as the
            // Query Name is not mandatory for normal Search Queries
            let missingQueryNames = this.SearchProfiles |> Seq.filter (fun x -> String.IsNullOrWhiteSpace(x.QueryName))
            if missingQueryNames.Count() <> 0 then fail (NotEmpty("QueryName"))
            else ok()
        
        this.IndexName
        |> propertyNameValidator "IndexName"
        >>= fun _ -> seqValidator (this.Fields.Cast<ValidatableBase>())
        >>= fun _ -> seqValidator (this.Scripts.Cast<ValidatableBase>())
        >>= fun _ -> seqValidator (this.SearchProfiles.Cast<ValidatableBase>())
        >>= checkDuplicateFieldName
        >>= checkDuplicateScriptNames
        >>= validateSearchQuery
        >>= checkDuplicateQueries
        >>= checkScriptExists

/// <summary>
/// Represents the result returned by FlexSearch for a given search query.
/// </summary>
[<ToString; Sealed>]
type SearchResults() = 
    
    /// <summary>
    /// DOcuments which are returned as a part of search response.
    /// </summary>
    member val Documents = new List<ResultDocument>() with get, set
    
    /// <summary>
    /// Total number of records returned.
    /// </summary>
    member val RecordsReturned = 0 with get, set
    
    /// <summary>
    /// Total number of records available on the server. This could be 
    /// greater than the returned results depending upon the requested 
    /// document count. 
    /// </summary>
    member val TotalAvailable = 0 with get, set

/// <summary>
/// Represents a list of words which can be used for filtering by an analyzer.
/// These can contain stop words or keep words etc.
/// </summary>
[<ToString; Sealed>]
type FilterList(words : List<string>) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// List of words
    /// </summary>
    member val Words = words with get, set
    
    new() = FilterList(Unchecked.defaultof<List<string>>)
    override __.Validate() = ok()

/// <summary>
/// Represents a list of words which can be used for synonym matching by an analyzer.
/// </summary>
[<ToString; Sealed>]
type MapList(words : Dictionary<string, List<string>>) = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Words to be used for synonym matching.
    /// </summary>
    member val Words = words with get, set
    
    new() = MapList(new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase))
    override __.Validate() = ok()

/// <summary>
/// Used by long running processes. All long running FlexSearch operations create
/// an instance of Job and return the Id to the caller. This Id can be used by the
/// caller to check the status of the job.
///
/// NOTE: Job information is not persistent
/// </summary>
[<ToString; Sealed>]
type Job() = 
    inherit ValidatableBase()
    
    /// <summary>
    /// Unique Id of the Job
    /// </summary>
    member val JobId = "" with get, set
    
    /// <summary>
    /// Total items to be processed as a part of the current job.
    /// </summary>
    member val TotalItems = 0 with get, set
    
    /// <summary>
    /// Items already processed.
    /// </summary>
    member val ProcessedItems = 0 with get, set
    
    /// <summary>
    /// Items which have failed processing.
    /// </summary>
    member val FailedItems = 0 with get, set
    
    /// <summary>
    /// Overall status of the job.
    /// </summary>
    member val Status = JobStatus.Initializing with get, set
    
    /// <summary>
    /// Any message that is associated with the job.
    /// </summary>
    member val Message = "" with get, set
    
    override __.Validate() = ok()

/// <summary>
/// Request to analyze a text against an analyzer. The reason to force
/// this parameter to request body is to avoid escaping of restricted characters
/// in the uri.
/// This is helpful during analyzer testing.
/// </summary>
[<ToString; Sealed>]
type AnalysisRequest() = 
    inherit ValidatableBase()
    member val Text = Unchecked.defaultof<string> with get, set
    override this.Validate() = this.Text |> notEmpty "Text"
