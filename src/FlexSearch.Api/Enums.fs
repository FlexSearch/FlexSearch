namespace FlexSearch.Api

open System.ComponentModel

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
