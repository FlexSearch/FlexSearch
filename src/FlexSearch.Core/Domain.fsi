namespace FlexSearch.Core
open System.Collections.Generic

// ----------------------------------------------------------------------------
#if def_FieldSimilarity_enum
Similarity defines the components of Lucene scoring. Similarity determines how 
Lucene weights terms, and Lucene interacts with Similarity at both index-time 
and query-time.
#endif

type FieldSimilarity = 

#if opt_BM25 
BM25 Similarity defines the components of Lucene scoring.
#endif
    | BM25 = 1

#if opt_TFIDF
TFIDF Similarity defines the components of Lucene scoring. This is the default 
Lucene similarity.
#endif
    | TFIDF = 2
// ----------------------------------------------------------------------------

#if def_DirectoryType
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.
#endif

type DirectoryType = 

#if opt_FileSystem
#endif
    | FileSystem = 1

#if opt_MemoryMapped
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
#endif
    | MemoryMapped = 2

#if opt_Ram
A memory-resident Directory implementation. This is not intended to work with 
huge indexes. Everything beyond several hundred megabytes will waste resources 
(GC cycles), because it uses an internal buffer size of 1024 bytes, producing 
millions of byte[1024] arrays. This class is optimized for small memory-resident 
indexes. It also has bad concurrency on multi-threaded environments.

It is recommended to materialize large indexes on disk and use MMapDirectory, 
which is a high-performance directory implementation working directly on the 
file system cache of the operating system.
#endif
    | Ram = 3
// ----------------------------------------------------------------------------

#if def_FieldTermVector
These options instruct FlexSearch to maintain full term vectors for each document, 
optionally including the position and offset information for each term occurrence 
in those vectors. These can be used to accelerate highlighting and other ancillary 
functionality, but impose a substantial cost in terms of index size. These can 
only be configured for custom field type.
#endif

type FieldTermVector = 

#if opt_DoNotStoreTermVector
Do not store term vectors.
#endif
    | DoNotStoreTermVector = 1

#if opt_StoreTermVector
Store the term vectors of each document. A term vector is a list of the document's 
terms and their number of occurrences in that document.
#endif
    | StoreTermVector = 2

#if opt_StoreTermVectorsWithPositions
Store the term vector and token position information.
#endif
    | StoreTermVectorsWithPositions = 3

#if opt_StoreTermVectorsWithPositionsandOffsets
Store the term vector, Token position and offset information.
#endif
    | StoreTermVectorsWithPositionsandOffsets = 4
// ----------------------------------------------------------------------------

#if def_FieldIndexOptions_enum
Controls how much information is stored in the postings lists.
#endif

type FieldIndexOptions = 

#if opt_DocsOnly
Only documents are indexed: term frequencies and positions are omitted.
#endif
    | DocsOnly = 1

#if opt_DocsAndFreqs
Only documents are indexed: term frequencies and positions are omitted.
#endif
    | DocsAndFreqs = 2

#if opt_DocsAndFreqsAndPositions
Indexes documents, frequencies and positions
#endif
    | DocsAndFreqsAndPositions = 3

#if opt_DocsAndFreqsAndPositionsAndOffsets
Indexes documents, frequencies, positions and offsets.
#endif
    | DocsAndFreqsAndPositionsAndOffsets = 4
// ----------------------------------------------------------------------------

#if def_IndexVersion_enum
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.
#endif
type IndexVersion = 

#if opt_Lucene_4_x_x
Lucene 4.x.x index format
It is deprecated and is here for legacy support
#endif
    | Lucene_4_x_x = 1

#if opt_Lucene_5_0_0
Lucene 5.0.0 index format
#endif
    | Lucene_5_0_0 = 2
// ----------------------------------------------------------------------------

#if def_JobStatus_enum
Represents the status of job.
#endif

type JobStatus = 

    | Undefined = 0

#if opt_Initializing
Job is currently initializing. This essentially means that the job is added to 
the job queue but has started executing. Depending upon the type of connector a 
job can take a while to move from this status. This also depends upon the 
parallel capability of the connector. Most of the connectors are designed to 
perform one operation at a time. 
#endif    
    | Initializing = 1

#if opt_Initialized
Job is initialized. All the parameters supplied as a part of the job are 
correct and job has been scheduled successfully.
#endif
    | Initialized = 2

#if opt_InProgress
Job is currently being executed by the engine.
#endif
    | InProgress = 3

#if opt_Completed
Job has finished without errors.
#endif
    | Completed = 4

#if opt_CompletedWithErrors
Job has finished with errors. Check the message property to get error 
details. Jobs have access to the engine's logging service so they could 
potentially write more error information to the logs.
#endif
    | CompletedWithErrors = 5
// ----------------------------------------------------------------------------

#if def_FieldType_enum
The field type defines how FlexSearch should interpret data in a field and how 
the field can be queried. There are many field types included with FlexSearch 
by default and custom types can also be defined.
#endif

type FieldType = 

#if opt_Int
Integer
#endif
    | Int = 1

#if opt_Double
Double
#endif
    | Double = 2

#if opt_ExactText
Field to store keywords. The entire input will be treated as a single word. 
This is useful for fields like customerid, referenceid etc. These fields only 
support complete text matching while searching and no partial word match is 
available.
#endif
    | ExactText = 3

#if opt_Text
General purpose field to store normal textual data
#endif
    | Text = 4

#if opt_Highlight
Similar to Text field but supports highlighting of search results
#endif
    | Highlight = 5

#if opt_Bool
Boolean
#endif
    | Bool = 6

#if opt_Date
Fixed format date field (Supported format: YYYYmmdd)
#endif
    | Date = 7

#if opt_DateTime
Fixed format datetime field (Supported format: YYYYMMDDhhmmss)
#endif
    | DateTime = 8

#if opt_Custom
Custom field type which gives more granular control over the field configuration
#endif
    | Custom = 9

#if opt_Stored
Non-indexed field. Only used for retrieving stored text. Searching is not
possible over these fields.
#endif
    | Stored = 10

#if opt_Long
Long
#endif
    | Long = 11
// ----------------------------------------------------------------------------

#if def_ShardConfiguration
Allows to control various Index Shards related settings.
#endif

type ShardConfiguration = 
    inherit DtoBase

#if prop_ShardCount
Total number of shards to be present in the given index.
#endif
    val ShardCount : int
// ----------------------------------------------------------------------------

#if def_IndexConfiguration
Allows to control various Index related settings.
#endif

type IndexConfiguration = 
    inherit DtoBase

#if prop_CommitTimeSeconds
The amount of time in seconds that FlexSearch should wait before committing 
changes to the disk. This is only used if no commits have happended in the
set time period otherwise CommitEveryNFlushes takes care of commits
#endif
    val CommitTimeSeconds : int

#if prop_CommitEveryNFlushes
Determines how often the data be committed to the physical medium. Commits are 
more expensive than flushes so keep the setting as high as possilbe. Making
this setting too high will result in excessive ram usage. 
#endif    
    val CommitEveryNFlushes : int

#if prop_CommitOnClose
Determines whether to commit first before closing an index
#endif
    val CommitOnClose: bool    

#if prop_AutoCommit
Determines whether to enable auto commit functionality or not
#endif
    val AutoCommit : bool

#if prop_DirectoryType
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.
#endif
    val DirectoryType : DirectoryType

#if prop_DefaultWriteLockTimeout
The default maximum time to wait for a write lock (in milliseconds).
#endif
    val DefaultWriteLockTimeout : int

#if prop_RamBufferSizeMb
Determines the amount of RAM that may be used for buffering added documents and 
deletions before they are flushed to the Directory.
#endif
    val RamBufferSizeMb : int

#if prop_MaxBufferedDocs
The number of buffered added documents that will trigger a flush if enabled.
#endif
    val MaxBufferedDocs : int

#if prop_RefreshTimeMilliseconds
The amount of time in milliseconds that FlexSearch should wait before reopening 
index reader. This helps in keeping writing and real time aspects of the engine 
separate.
#endif
    val RefreshTimeMilliseconds : int

#if prop_AutoRefresh
Determines whether to enable auto refresh or not
#endif
    val AutoRefresh  : bool

#if prop_IndexVersion
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.
#endif
    val IndexVersion : IndexVersion

#if prop_UseBloomFilterForId
Signifies if bloom filter should be used for encoding Id field.
#endif
    val UseBloomFilterForId : bool

#if prop_DefaultFieldSimilarity
Similarity defines the components of Lucene scoring. Similarity determines how 
Lucene weights terms and Lucene interacts with Similarity at both index-time 
and query-time.
#endif
    val DefaultFieldSimilarity : FieldSimilarity
// ----------------------------------------------------------------------------

#if def_TokenFilter
Filters consume input and produce a stream of tokens. In most cases a filter looks 
at each token in the stream sequentially and decides whether to pass it along, 
replace it or discard it. A filter may also do more complex analysis by looking 
ahead to consider multiple tokens at once, although this is less common. 
#endif
type TokenFilter = 
    inherit DtoBase

#if FilterName       
The name of the filter. Some pre-defined filters are the following-
+ Ascii Folding Filter
+ Standard Filter
+ Beider Morse Filter
+ Double Metaphone Filter
+ Caverphone2 Filter
+ Metaphone Filter
+ Refined Soundex Filter
+ Soundex Filter
For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
#endif
    val FilterName : string
       
#if Parameters 
Parameters required by the filter.
#endif
    val Parameters : Dictionary<string, string>
// ----------------------------------------------------------------------------

#if def_Tokenizer
valTokenizer breaks up a stream of text into tokens, where each token is a sub-sequence
of the characters in the text. An analyzer is aware of the field it is configured 
for, but a tokenizer is not.
#endif

type Tokenizer = 
    inherit DtoBase

#if FilterName         
The name of the tokenizer. Some pre-defined tokenizers are the following-
+ Standard Tokenizer
+ Classic Tokenizer
+ Keyword Tokenizer
+ Letter Tokenizer
+ Lower Case Tokenizer
+ UAX29 URL Email Tokenizer
+ White Space Tokenizer
For more details refer to http://flexsearch.net/docs/concepts/understanding-analyzers-tokenizers-and-filters/
#endif
    val TokenizerName : string

#if Parameters         
Parameters required by the tokenizer.
#endif
    val Parameters : Dictionary<string, string>
// ----------------------------------------------------------------------------

#if def_Analyzer
An analyzer examines the text of fields and generates a token stream.
#endif

type Analyzer =
    inherit DtoBase

#if AnalyzerName
Name of the analyzer
#endif
    val AnalyzerName : string
        
#if AnalyzerName
#endif
    val Tokenizer : Tokenizer
  
#if Filters     
Filters to be used by the analyzer.
#endif
    val Filters : List<TokenFilter>
// ----------------------------------------------------------------------------

#if def_Field
A field is a section of a Document. 

Fields can contain different kinds of data. A name field, for example, 
is text (character data). A shoe size field might be a floating point number 
so that it could contain values like 6 and 9.5. Obviously, the definition of 
fields is flexible (you could define a shoe size field as a text field rather
than a floating point number, for example), but if you define your fields correctly, 
FlexSearch will be able to interpret them correctly and your users will get better 
results when they perform a query.

You can tell FlexSearch about the kind of data a field contains by specifying its 
field type. The field type tells FlexSearch how to interpret the field and how 
it can be queried. When you add a document, FlexSearch takes the information in 
the document’s fields and adds that information to an index. When you perform a 
query, FlexSearch can quickly consult the index and return the matching documents.
#endif

type Field = 
    inherit DtoBase

#if FieldName        
Name of the field.
#endif
    val FieldName : string

#if Analyze        
Signifies if the field should be analyzed using an analyzer. 
#endif
    val Analyze : bool

#if Index        
Signifies if a field should be indexed. A field can only be 
stored without indexing.
#endif
    val Index : bool

#if Store        
Signifies if a field should be stored so that it can retrieved
while searching.
#endif
    val Store : bool

#if AllowSort        
If AllowSort is set to true then we will index the field with docvalues.
#endif
    val AllowSort : bool

#if IndexAnalyzer        
Analyzer to be used while indexing.
#endif
    val IndexAnalyzer : string

#if SearchAnalyzer        
Analyzer to be used while searching.
#endif
    val SearchAnalyzer : string

#if FieldType        
AUTO
#endif
    val FieldType : FieldType

#if FieldSimilarity        
AUTO
#endif
    val Similarity : FieldSimilarity

#if FieldIndexOptions        
AUTO
#endif
    val IndexOptions : FieldIndexOptions

#if FieldTermVector        
AUTO
#endif
    val TermVector : FieldTermVector

#if OmitNorms        
If true, omits the norms associated with this field (this disables length 
normalization and index-time boosting for the field, and saves some memory). 
Defaults to true for all primitive (non-analyzed) field types, such as int, 
float, data, bool, and string. Only full-text fields or fields that need an 
index-time boost need norms.
#endif
    val OmitNorms : bool

#if ScriptName        
Fields can get their content dynamically through scripts. This is the name of 
the script to be used for getting field data at index time.
Script name follows the below convention
ScriptName('param1','param2','param3')
#endif
    val ScriptName : string
// ----------------------------------------------------------------------------

#if def_HighlightOption 
Used for configuring the settings for text highlighting in the search results
#endif

type HighlightOption = 
    inherit DtoBase

#if FragmentsToReturn        
Total number of fragments to return per document
#endif
    val FragmentsToReturn : int

#if HighlightedFields      
The fields to be used for text highlighting
#endif
    val HighlightedFields : string[]

#if PostTag        
Post tag to represent the ending of the highlighted word
#endif
    val PostTag : string

#if PreTag        
Pre tag to represent the ending of the highlighted word
#endif
    val PreTag : string
// ----------------------------------------------------------------------------

#if def_SearchQuery
valSearch query is used for searching over a FlexSearch index. This provides
vala consistent syntax to execute various types of queries. The syntax is similar
valto the SQL syntax. This was done on purpose to reduce the learning curve.
#endif

type SearchQuery = 
    inherit DtoBase

#if QueryName        
Unique name of the query. This is only required if you are setting up a 
search profile.
#endif
    val QueryName : string

#if Columns        
Columns to be returned as part of results.
+ *  - return all columns
+ [] - return no columns
+ ["columnName"] -  return specific column
#endif
    val Columns : string[]

#if Count        
Count of results to be returned
#endif
    val Count : int

#if Highlights        
AUTO
#endif
    val Highlights : HighlightOption
 
#if IndexName       
Name of the index
#endif
    val IndexName : string

#if OrderBy        
Can be used to order the results by score or specific field.
#endif
    val OrderBy : string

#if OrderByDirection        
Can be used to determine the sort order.
#endif
    val OrderByDirection : string

#if CutOff        
Can be used to remove results lower than a certain threshold.
This works in conjunction with the score of the top record as
all the other records are filtered using the score set by the
top scoring record.
#endif
    val CutOff: double

#if DistinctBy       
Can be used to return records with distinct values for 
the given field. Works in a manner similar to Sql distinct by clause.
#endif
    val DistinctBy : string

#if Skip        
Used to enable paging and skip certain pre-fetched results.
#endif
    val Skip : int

#if QueryString        
Query string to be used for searching
#endif
    val QueryString : string

#if ReturnFlatResult        
If true will return collapsed search results which are in tabular form.
Flat results enable easy binding to a grid but grouping results is tougher
with Flat result.
#endif
    val ReturnFlatResult : bool

#if ReturnScore        
If true then scores are returned as a part of search result.
#endif
    val ReturnScore : bool

#if SearchProfile        
Profile Name to be used for profile based searching.
#endif
    val SearchProfile : string

#if SearchProfileScript        
Script which can be used to select a search profile. This can help in
dynamic selection of search profile based on the incoming data.
#endif
    val SearchProfileScript : string

#if OverrideProfileOptions        
Can be used to override the configuration saved in the search profile
with the one which is passed as the Search Query
#endif
    val OverrideProfileOptions : bool

#if ReturnEmptyStringForNull        
Returns an empty string for null values saved in the index rather than
the null constant
#endif
    val ReturnEmptyStringForNull : bool
// ----------------------------------------------------------------------------

#if def_Document
A document represents the basic unit of information which can be added or 
retrieved from the index. A document consists of several fields. A field represents 
the actual data to be indexed. In database analogy an index can be considered as 
a table while a document is a row of that table. Like a table a FlexSearch document 
requires a fix schema and all fields should have a field type.
#endif

type Document = 
    inherit DtoBase
 
 #if def_Document       
Fields to be added to the document for indexing.
#endif
    val Fields : Dictionary<string, string>

#if Id        
Unique Id of the document
#endif
    val Id : string

#if TimeStamp  
Timestamp of the last modification of the document. This field is interpreted 
differently during a create and update operation. It also dictates whether and 
unique Id check is to be performed or not. 

Version number semantics
+ 0 - Don't care about the version and proceed with the operation normally.
+ -1 - Ensure that the document does not exist (Performs unique Id check).
+ 1 - Ensure that the document does exist. This is not relevant for create operation.
> 1 - Ensure that the version matches exactly. This is not relevant for create operation.
#endif
    val TimeStamp : int64 

#if IndexName
Name of the index
#endif
    val IndexName : string

#if Highlights        
Any matched text highlighted snippets. Note: Only used for results
#endif
    val Highlights : string []

#if Score        
Score of the returned document. Note: Only used for results
#endif
    val Score : double
// ----------------------------------------------------------------------------

#if dto_Index
FlexSearch index is a logical index built on top of Lucene’s index in a manner 
to support features like schema and sharding. So in this sense a FlexSearch 
index consists of multiple Lucene’s index. Also, each FlexSearch shard is a valid 
Lucene index.

In case of a database analogy an index represents a table in a database where 
one has to define a schema upfront before performing any kind of operation on 
the table. There are various properties that can be defined at the index creation 
time. Only IndexName is a mandatory property, though one should always define 
Fields in an index to make any use of it.

By default a newly created index stays off-line. This is by design to force the 
user to enable an index before using it.
#endif

type Index =
    inherit DtoBase

#if Fields
Fields to be used in index.
#endif
    val Fields : Field []

#if IndexConfiguration
#endif
    val IndexConfiguration : IndexConfiguration

#if IndexName
Name of the index
#endif
    val IndexName : string

#if Online
Signifies if the index is on-line or not? An index has to be on-line in order to 
enable searching over it.
#endif
    val Online : bool

#if SearchProfiles
Search Profiles
#endif
    val SearchProfiles : SearchQuery []

#if ShardConfiguration
#endif
    val ShardConfiguration : ShardConfiguration
// ----------------------------------------------------------------------------

#if dto_SearchResults
valRepresents the result returned by FlexSearch for a given search query.
#endif

type SearchResults =
#if Documents
valDocuments which are returned as a part of search response.
#endif
    val Documents : System.Collections.Generic.List<Document>

#if RecordsReturned
valTotal number of records returned.
#endif
    val RecordsReturned : int

#if TotalAvailable
valTotal number of records available on the server. This could be 
valgreater than the returned results depending upon the requested 
valdocument count.
#endif
    val TotalAvailable : int
// ----------------------------------------------------------------------------

#if dto_Job
valUsed by long running processes. All long running FlexSearch operations create
valan instance of Job and return the Id to the caller. This Id can be used by the
valcaller to check the status of the job.
///
valNOTE: Job information is not persistent
#endif

type Job =
    inherit DtoBase

#if FailedItems
valItems which have failed processing.
#endif
    val FailedItems : int

#if JobId
valUnique Id of the Job
#endif
    val JobId : string

#if Message
valAny message that is associated with the job.
#endif
    val Message : string
    
#if ProcessedItems
valItems already processed.
#endif
    val ProcessedItems : int

#if Status
valOverall status of the job.
#endif
    val Status : JobStatus
    
#if TotalItems
valTotal items to be processed as a part of the current job.
#endif
    val TotalItems : int
// ----------------------------------------------------------------------------

#if dto_AnalysisRequest
valRequest to analyze a text against an analyzer. The reason to force
valthis parameter to request body is to avoid escaping of restricted characters
valin the uri.
valThis is helpful during analyzer testing.
#endif
type AnalysisRequest =
    inherit DtoBase
    val Text : string
// ----------------------------------------------------------------------------

#if dto_CreateResponse

#endif
type CreateResponse =
    val Id : string
// ----------------------------------------------------------------------------

#if dto_IndexExistsResponse
#endif
type IndexExistsResponse =
    val Exists : bool
// ----------------------------------------------------------------------------