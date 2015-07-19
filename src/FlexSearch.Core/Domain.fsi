namespace FlexSearch.Core
// ----------------------------------------------------------------------------
                                                                                
# begin_def
# def_FieldSimilarity_enum """
Similarity defines the components of Lucene scoring. Similarity determines how 
Lucene weights terms, and Lucene interacts with Similarity at both index-time 
and query-time.
"""
# opt_BM25 """
BM25 Similarity defines the components of Lucene scoring.
"""
# opt_TFIDF """
TFIDF Similarity defines the components of Lucene scoring. This is the default 
Lucene similarity.
"""
type FieldSimilarity = 
    | BM25 = 1
    | TFIDF = 2
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_DirectoryType_enum """
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.
"""
# opt_MemoryMapped """
File-based Directory implementation that uses memory map for reading, and 
FSDirectory.FSIndexOutput for writing.
 
NOTE: memory mapping uses up a portion of the virtual memory address space 
in your process equal to the size of the file being mapped. Before using this 
class, be sure your have plenty of virtual address space, e.g. by using a 64 
bit JRE, or a 32 bit JRE with indexes that are guaranteed to fit within the 
address space. On 32 bit platforms also consult MMapDirectory(File, LockFactory, 
int) if you have problems with mmap failing because of fragmented address 
space. If you get an OutOfMemoryException, it is recommended to reduce the 
chunk size, until it works.

Due to this bug in Sun's JRE, MMapDirectory's IndexInput.close() is unable 
to close the underlying OS file handle. Only when GC finally collects the 
underlying objects, which could be quite some time later, will the file handle
be closed.

This will consume additional transient disk usage: on Windows, attempts to 
delete or overwrite the files will result in an exception; on other platforms, 
which typically have a "delete on last close" semantics, while such operations
will succeed, the bytes are still consuming space on disk. For many 
applications this limitation is not a problem (e.g. if you have plenty of 
disk space, and you don't rely on overwriting files on Windows) but it's 
still an important limitation to be aware of.
"""
# opt_Ram """
A memory-resident Directory implementation. This is not intended to work with 
huge indexes. Everything beyond several hundred megabytes will waste resources 
(GC cycles), because it uses an internal buffer size of 1024 bytes, producing 
millions of byte[1024] arrays. This class is optimized for small memory-resident 
indexes. It also has bad concurrency on multi-threaded environments.

It is recommended to materialize large indexes on disk and use MMapDirectory, 
which is a high-performance directory implementation working directly on the 
file system cache of the operating system.
"""
type DirectoryType = 
    | FileSystem = 1
    | MemoryMapped = 2
    | Ram = 3
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_FieldTermVector_enum """
These options instruct FlexSearch to maintain full term vectors for each document, 
optionally including the position and offset information for each term occurrence 
in those vectors. These can be used to accelerate highlighting and other ancillary 
functionality, but impose a substantial cost in terms of index size. These can 
only be configured for custom field type.
"""
# opt_DoNotStoreTermVector """
Do not store term vectors.
"""
# opt_StoreTermVector """
Store the term vectors of each document. A term vector is a list of the document's 
terms and their number of occurrences in that document.
"""
# opt_StoreTermVectorsWithPositions """
Store the term vector and token position information
"""
# opt_StoreTermVectorsWithPositionsandOffsets """
Store the term vector, Token position and offset information
"""

type FieldTermVector = 
    | DoNotStoreTermVector = 1
    | StoreTermVector = 2
    | StoreTermVectorsWithPositions = 3
    | StoreTermVectorsWithPositionsandOffsets = 4

# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_FieldIndexOptions_enum """ 
Controls how much information is stored in the postings lists.
"""
# opt_DocsOnly """
Only documents are indexed: term frequencies and positions are omitted.
"""
# opt_DocsAndFreqs """
Only documents are indexed: term frequencies and positions are omitted.
"""
# opt_DocsAndFreqsAndPositions """
Indexes documents, frequencies and positions
"""
# opt_DocsAndFreqsAndPositionsAndOffsets """
Indexes documents, frequencies, positions and offsets.
"""

type FieldIndexOptions = 
    | DocsOnly = 1
    | DocsAndFreqs = 2
    | DocsAndFreqsAndPositions = 3
    | DocsAndFreqsAndPositionsAndOffsets = 4
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_IndexVersion_enum """ 
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.
"""
# opt_Lucene_4_x_x """
Lucene 4.x.x index format
It is deprecated and is here for legacy support
"""
# opt_Lucene_5_0_0 """
Lucene 5.0.0 index format
"""
type IndexVersion = 
    | Lucene_4_x_x = 1
    | Lucene_5_0_0 = 2
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_JobStatus_enum """
Represents the status of job.
"""
# opt_Initializing """
Job is currently initializing. This essentially means that the job is added to 
the job queue but has started executing. Depending upon the type of connector a 
job can take a while to move from this status. This also depends upon the 
parallel capability of the connector. Most of the connectors are designed to 
perform one operation at a time. 
"""
# opt_Initialized """
opt_Job is initialized. All the parameters supplied as a part of the job are 
correct and job has been scheduled successfully.
"""
# opt_InProgress """
opt_Job is currently being executed by the engine.
"""
# opt_Completed """
opt_Job has finished without errors.
"""
# CompletedWithErrors """
opt_Job has finished with errors. Check the message property to get error 
details. Jobs have access to the engine's logging service so they could 
potentially write more error information to the logs.
"""

type JobStatus = 
    | Undefined = 0
    | Initializing = 1
    | Initialized = 2
    | InProgress = 3
    | Completed = 4
    | CompletedWithErrors = 5
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_FieldType_enum """
The field type defines how FlexSearch should interpret data in a field and how 
the field can be queried. There are many field types included with FlexSearch 
by default and custom types can also be defined.
"""

# opt_Int "Integer"
# opt_Double "Double"
# opt_ExactText """
Field to store keywords. The entire input will be treated as a single word. 
This is useful for fields like customerid, referenceid etc. These fields only 
support complete text matching while searching and no partial word match is 
available.
"""
# opt_Text """
General purpose field to store normal textual data
"""
# opt_Highlight """
Similar to Text field but supports highlighting of search results
"""
# opt_Bool "Boolean"
# opt_Date """
Fixed format date field (Supported format: YYYYmmdd)
"""
# opt_DateTime """
Fixed format datetime field (Supported format: YYYYMMDDhhmmss)
"""
# opt_Custom """
Custom field type which gives more granular control over the field configuration
"""
# opt_Stored """
Non-indexed field. Only used for retrieving stored text. Searching is not
possible over these fields.
"""
# opt_Long "Long"

type FieldType = 
    | Int = 1
    | Double = 2
    | ExactText = 3
    | Text = 4
    | Highlight = 5
    | Bool = 6
    | Date = 7
    | DateTime = 8
    | Custom = 9
    | Stored = 10
    | Long = 11
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_ShardConfiguration """
Allows to control various Index Shards related settings.
"""
# prop_ShardCount_int_1 """
Total number of shards to be present in the given index.
"""

type ShardConfiguration = 
    inherit DtoBase
    val ShardCount : int
# end_def
// ----------------------------------------------------------------------------

# begin_def
# def_IndexConfiguration """
Allows to control various Index related settings.
"""
# prop_CommitTimeSeconds_int_60 """
The amount of time in seconds that FlexSearch should wait before committing 
changes to the disk. This is only used if no commits have happended in the
set time period otherwise CommitEveryNFlushes takes care of commits
"""
# prop_CommitEveryNFlushes_int_3 """
Determines how often the data be committed to the physical medium. Commits are 
more expensive than flushes so keep the setting as high as possilbe. Making
this setting too high will result in excessive ram usage.  
"""
# prop_CommitOnClose_bool_true """
Determines whether to commit first before closing an index
"""
# prop_AutoCommit_bool_true """
Determines whether to enable auto commit functionality or not
"""
# prop_DirectoryType_enum_MemoryMapped """
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.
"""
# prop_DefaultWriteLockTimeout_int_1000 """
The default maximum time to wait for a write lock (in milliseconds).
"""
# prop_RamBufferSizeMb_int_100 """
Determines the amount of RAM that may be used for buffering added documents and 
deletions before they are flushed to the Directory.
"""
# prop_MaxBufferedDocs_int_minus1 """
The number of buffered added documents that will trigger a flush if enabled.
"""
# prop_RefreshTimeMilliseconds_int_500 """
The amount of time in milliseconds that FlexSearch should wait before reopening 
index reader. This helps in keeping writing and real time aspects of the engine 
separate.
"""
# prop_AutoRefresh_bool_true """
Determines whether to enable auto refresh or not
"""
# prop_IndexVersion_IndexVersion_Lucene_5_0_0 """
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.
"""
# prop_UseBloomFilterForId_bool_true """
Signifies if bloom filter should be used for encoding Id field.
"""
# prop_DefaultFieldSimilarity_FieldSimilarity_TFIDF """
Similarity defines the components of Lucene scoring. Similarity determines how 
Lucene weights terms and Lucene interacts with Similarity at both index-time 
and query-time.
"""
type IndexConfiguration = 
    inherit DtoBase
    val CommitTimeSeconds : int
    val CommitEveryNFlushes : int
    val CommitOnClose: bool    
    val AutoCommit : bool
    val DirectoryType : DirectoryType
    val DefaultWriteLockTimeout : int
    val RamBufferSizeMb : int
    val MaxBufferedDocs : int
    val RefreshTimeMilliseconds : int
    val AutoRefresh  : bool
    val IndexVersion : IndexVersion
    val UseBloomFilterForId : bool
    val DefaultFieldSimilarity : FieldSimilarity
    