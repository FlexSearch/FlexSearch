namespace FlexSearch.Core
open System.Collections.Generic

// ----------------------------------------------------------------------------
#if enum_FieldSimilarity
Similarity defines the components of Lucene scoring. Similarity determines how 
Lucene weights terms, and Lucene interacts with Similarity at both index-time 
and query-time.
#endif

type FieldSimilarity = 
#if opt_Undefined
#endif
    | Undefined = 0
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

#if enum_DirectoryType
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.
#endif

type DirectoryType = 
#if opt_Undefined
#endif
    | Undefined = 0
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

#if enum_FieldTermVector
These options instruct FlexSearch to maintain full term vectors for each document, 
optionally including the position and offset information for each term occurrence 
in those vectors. These can be used to accelerate highlighting and other ancillary 
functionality, but impose a substantial cost in terms of index size. These can 
only be configured for custom field type.
#endif

type FieldTermVector = 
#if opt_Undefined
#endif
    | Undefined = 0
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

#if enum_FieldIndexOptions
Controls how much information is stored in the postings lists.
#endif

type FieldIndexOptions = 
#if opt_Undefined
#endif
    | Undefined = 0
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

#if enum_IndexVersion
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.
#endif
type IndexVersion = 
#if opt_Undefined
#endif
    | Undefined = 0
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

#if enum_JobStatus
Represents the status of job.
#endif

type JobStatus = 
#if opt_Undefined
#endif
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

#if enum_ShardStatus
Signifies Shard status
#endif
type ShardStatus = 
    #if opt_Undefined
    #endif
    | Undefined = 0
    #if opt_Opening
    #endif
    | Opening = 1
    #if opt_Recovering
    #endif
    | Recovering = 2
    #if opt_Online
    #endif
    | Online = 3
    #if opt_Offline
    #endif
    | Offline = 4
    #if opt_Closing
    #endif
    | Closing = 5
    #if opt_Faulted
    #endif
    | Faulted = 6

#if enum_IndexStatus
Represents the current state of the index.
#endif
type IndexStatus = 
    #if opt_Undefined
    #endif
    | Undefined = 0
    #if opt_Opening
    #endif
    | Opening = 1
    #if opt_Recovering
    #endif
    | Recovering = 2
    #if opt_Online
    #endif
    | Online = 3
    #if opt_OnlineFollower
    #endif
    | OnlineFollower = 4
    #if opt_Offline
    #endif
    | Offline = 5
    #if opt_Closing
    #endif
    | Closing = 6
    #if opt_Faulted
    #endif
    | Faulted = 7

#if enum_FieldDataType
The field type defines how FlexSearch should interpret data in a field and how 
the field can be queried. There are many field types included with FlexSearch 
by default and custom types can also be defined.
#endif

type FieldDataType = 
#if opt_Undefined
#endif
    | Undefined = 0
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

#if dto_ShardConfiguration
Allows to control various Index Shards related settings.
#endif

[<ToString; Sealed>]
type ShardConfiguration = 
    inherit DtoBase
    new : unit -> ShardConfiguration
    override Validate : unit -> Result<unit>

#if prop_ShardCount
Total number of shards to be present in the given index.
#endif
    member ShardCount : int with get, set
// ----------------------------------------------------------------------------

#if dto_IndexConfiguration
Allows to control various Index related settings.
#endif

[<ToString; Sealed>]
type IndexConfiguration = 
    inherit DtoBase
    new : unit -> IndexConfiguration
    override Validate : unit -> Result<unit>

#if prop_CommitTimeSeconds
The amount of time in seconds that FlexSearch should wait before committing 
changes to the disk. This is only used if no commits have happended in the
set time period otherwise CommitEveryNFlushes takes care of commits
#endif
    member CommitTimeSeconds : int with get, set

#if prop_CommitEveryNFlushes
Determines how often the data be committed to the physical medium. Commits are 
more expensive than flushes so keep the setting as high as possilbe. Making
this setting too high will result in excessive ram usage. 
#endif    
    member CommitEveryNFlushes : int with get, set

#if prop_CommitOnClose
Determines whether to commit first before closing an index
#endif
    member CommitOnClose: bool with get, set   

#if prop_AutoCommit
Determines whether to enable auto commit functionality or not
#endif
    member AutoCommit : bool with get, set

#if prop_DirectoryType
A Directory is a flat list of files. Files may be written once, when they are 
created. Once a file is created it may only be opened for read, or deleted. 
Random access is permitted both when reading and writing.
#endif
    member DirectoryType : DirectoryType with get, set

#if prop_DefaultWriteLockTimeout
The default maximum time to wait for a write lock (in milliseconds).
#endif
    member DefaultWriteLockTimeout : int with get, set

#if prop_RamBufferSizeMb
Determines the amount of RAM that may be used for buffering added documents and 
deletions before they are flushed to the Directory.
#endif
    member RamBufferSizeMb : int with get, set

#if prop_MaxBufferedDocs
The number of buffered added documents that will trigger a flush if enabled.
#endif
    member MaxBufferedDocs : int with get, set

#if prop_RefreshTimeMilliseconds
The amount of time in milliseconds that FlexSearch should wait before reopening 
index reader. This helps in keeping writing and real time aspects of the engine 
separate.
#endif
    member RefreshTimeMilliseconds : int with get, set

#if prop_AutoRefresh
Determines whether to enable auto refresh or not
#endif
    member AutoRefresh  : bool with get, set

#if prop_IndexVersion
Corresponds to Lucene Index version. There will always be a default codec 
associated with each index version.
#endif
    member IndexVersion : IndexVersion with get, set

#if prop_UseBloomFilterForId
Signifies if bloom filter should be used for encoding Id field.
#endif
    member UseBloomFilterForId : bool with get, set

#if prop_DefaultFieldSimilarity
Similarity defines the components of Lucene scoring. Similarity determines how 
Lucene weights terms and Lucene interacts with Similarity at both index-time 
and query-time.
#endif
    member DefaultFieldSimilarity : FieldSimilarity with get, set

#if prop_AllowReads
Signifies if reads are allowed on the index. Useful for creating write only index.
#endif
    member AllowReads : bool with get, set
    
#if prop_AllowWrites
Signifies if writes are allowed on the index. Useful for creating read only index.
#endif    
    member AllowWrites : bool with get, set
// ----------------------------------------------------------------------------

#if dto_TokenFilter
Filters consume input and produce a stream of tokens. In most cases a filter looks 
at each token in the stream sequentially and decides whether to pass it along, 
replace it or discard it. A filter may also do more complex analysis by looking 
ahead to consider multiple tokens at once, although this is less common. 
#endif

[<ToString; Sealed>]
type TokenFilter = 
    inherit DtoBase
    new : unit -> TokenFilter
    override Validate : unit -> Result<unit>

#if prop_FilterName       
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
    member FilterName : string with get, set
       
#if prop_Parameters 
Parameters required by the filter.
#endif
    member Parameters : Dictionary<string, string> with get, set
// ----------------------------------------------------------------------------

#if dto_Tokenizer
memberTokenizer breaks up a stream of text into tokens, where each token is a sub-sequence
of the characters in the text. An analyzer is aware of the field it is configured 
for, but a tokenizer is not.
#endif

[<ToString; Sealed>]
type Tokenizer = 
    inherit DtoBase
    new : unit -> Tokenizer
    override Validate : unit -> Result<unit>

#if prop_TokenizerName         
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
    member TokenizerName : string with get, set

#if prop_Parameters         
Parameters required by the tokenizer.
#endif
    member Parameters : Dictionary<string, string> with get, set
// ----------------------------------------------------------------------------

#if dto_Analyzer
An analyzer examines the text of fields and generates a token stream.
#endif

[<ToString; Sealed>]
type Analyzer =
    inherit DtoBase
    new : unit -> Analyzer
    override Validate : unit -> Result<unit>

#if prop_AnalyzerName
Name of the analyzer
#endif
    member AnalyzerName : string with get, set
        
#if prop_Tokenizer
#endif
    member Tokenizer : Tokenizer with get, set
  
#if prop_Filters     
Filters to be used by the analyzer.
#endif
    member Filters : List<TokenFilter> with get, set
// ----------------------------------------------------------------------------

#if dto_Field
A field is a section of a Document. 

Fields can contain different kinds of data. A name field, for example, 
is text (character data). A shoe size field might be a floating point number 
so that it could contain memberues like 6 and 9.5. Obviously, the definition of 
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

[<ToString; Sealed>]
type Field = 
    inherit DtoBase
    new : unit -> Field
    new : fieldName:string -> Field
    new : fieldName:string * fieldType:FieldDataType -> Field

#if prop_FieldName        
Name of the field.
#endif
    member FieldName : string with get, set

#if prop_Analyze        
Signifies if the field should be analyzed using an analyzer. 
#endif
    member Analyze : bool with get, set

#if prop_Index        
Signifies if a field should be indexed. A field can only be 
stored without indexing.
#endif
    member Index : bool with get, set

#if prop_Store        
Signifies if a field should be stored so that it can retrieved
while searching.
#endif
    member Store : bool with get, set

#if prop_AllowSort        
If AllowSort is set to true then we will index the field with docmemberues.
#endif
    member AllowSort : bool with get, set

#if prop_AllowFaceting
If AllowFaceting is set to true then we will index the field with sorted set docvalues
#endif
    member AllowFaceting : bool with get, set

#if prop_IndexAnalyzer        
Analyzer to be used while indexing.
#endif
    member IndexAnalyzer : string with get, set

#if prop_SearchAnalyzer        
Analyzer to be used while searching.
#endif
    member SearchAnalyzer : string with get, set

#if prop_FieldType        
AUTO
#endif
    member FieldType : FieldDataType with get, set

#if prop_Similarity        
AUTO
#endif
    member Similarity : FieldSimilarity with get, set

#if prop_IndexOptions        
AUTO
#endif
    member IndexOptions : FieldIndexOptions with get, set

#if prop_TermVector        
AUTO
#endif
    member TermVector : FieldTermVector with get, set

#if prop_OmitNorms        
If true, omits the norms associated with this field (this disables length 
normalization and index-time boosting for the field, and saves some memory). 
Defaults to true for all primitive (non-analyzed) field types, such as int, 
float, data, bool, and string. Only full-text fields or fields that need an 
index-time boost need norms.
#endif
    member OmitNorms : bool with get, set

#if prop_ScriptName        
Fields can get their content dynamically through scripts. This is the name of 
the script to be used for getting field data at index time.
Script name follows the below convention
ScriptName('param1','param2','param3')
#endif
    member ScriptName : string with get, set
// ----------------------------------------------------------------------------

#if dto_HighlightOption 
Used for configuring the settings for text highlighting in the search results
#endif

[<ToString; Sealed>]
type HighlightOption = 
    inherit DtoBase
    new : unit -> HighlightOption
    new : fields:string [] -> HighlightOption
    override Validate : unit -> Result<unit>

#if prop_FragmentsToReturn        
Total number of fragments to return per document
#endif
    member FragmentsToReturn : int with get, set

#if prop_HighlightedFields      
The fields to be used for text highlighting
#endif
    member HighlightedFields : string[] with get, set

#if prop_PostTag        
Post tag to represent the ending of the highlighted word
#endif
    member PostTag : string with get, set

#if prop_PreTag        
Pre tag to represent the ending of the highlighted word
#endif
    member PreTag : string with get, set
// ----------------------------------------------------------------------------

#if dto_SearchQuery
memberSearch query is used for searching over a FlexSearch index. This provides
membera consistent syntax to execute various types of queries. The syntax is similar
memberto the SQL syntax. This was done on purpose to reduce the learning curve.
#endif

[<ToString; Sealed>]
type SearchQuery = 
    inherit DtoBase
    new : unit -> SearchQuery
    new : index:string * query:string -> SearchQuery
    override Validate : unit -> Result<unit>

#if prop_QueryName        
Unique name of the query. This is only required if you are setting up a 
search profile.
#endif
    member QueryName : string with get, set

#if prop_Columns        
Columns to be returned as part of results.
+ *  - return all columns
+ [] - return no columns
+ ["columnName"] -  return specific column
#endif
    member Columns : string[] with get, set

#if prop_Count        
Count of results to be returned
#endif
    member Count : int with get, set

#if prop_Highlights        
AUTO
#endif
    member Highlights : HighlightOption with get, set
 
#if prop_IndexName       
Name of the index
#endif
    member IndexName : string with get, set

#if prop_OrderBy        
Can be used to order the results by score or specific field.
#endif
    member OrderBy : string with get, set

#if prop_OrderByDirection        
Can be used to determine the sort order.
#endif
    member OrderByDirection : string with get, set

#if prop_CutOff        
Can be used to remove results lower than a certain threshold.
This works in conjunction with the score of the top record as
all the other records are filtered using the score set by the
top scoring record.
#endif
    member CutOff: double with get, set

#if prop_DistinctBy       
Can be used to return records with distinct memberues for 
the given field. Works in a manner similar to Sql distinct by clause.
#endif
    member DistinctBy : string with get, set

#if prop_Skip        
Used to enable paging and skip certain pre-fetched results.
#endif
    member Skip : int with get, set

#if prop_QueryString        
Query string to be used for searching
#endif
    member QueryString : string with get, set

#if prop_ReturnFlatResult        
If true will return collapsed search results which are in tabular form.
Flat results enable easy binding to a grid but grouping results is tougher
with Flat result.
#endif
    member ReturnFlatResult : bool with get, set

#if prop_ReturnScore        
If true then scores are returned as a part of search result.
#endif
    member ReturnScore : bool with get, set

#if prop_SearchProfile        
Profile Name to be used for profile based searching.
#endif
    member SearchProfile : string with get, set

#if prop_SearchProfileScript        
Script which can be used to select a search profile. This can help in
dynamic selection of search profile based on the incoming data.
#endif
    member SearchProfileScript : string with get, set

#if prop_OverrideProfileOptions        
Can be used to override the configuration saved in the search profile
with the one which is passed as the Search Query
#endif
    member OverrideProfileOptions : bool with get, set

#if prop_ReturnEmptyStringForNull        
Returns an empty string for null memberues saved in the index rather than
the null constant
#endif
    member ReturnEmptyStringForNull : bool with get, set
// ----------------------------------------------------------------------------

#if dto_FacetGroup
Holds the configuration for a facet
#endif
[<ToString; Sealed>]
type FacetGroup =
    inherit DtoBase
    new : unit -> FacetGroup
    new : index : string -> FacetGroup

    #if prop_IndexName
    #endif
    member IndexName : string with get, set

    #if prop_FieldName
    The field to group against
    #endif
    member FieldName : string with get, set

    #if prop_Count
    Count of results to return
    #endif
    member Count : int with get, set

    #if prop_FieldValue
    The value of the field grouping against. Used for Drill Down Queries.
    TODO
    #endif
    member FieldValue : string with get, set

    override Validate : unit -> Result<unit>

#if dto_FacetQuery
FacetQuery is used for submitting faceted search requests.
#endif
[<ToString; Sealed>]
type FacetQuery =
    inherit DtoBase
    new : unit -> FacetQuery
    new : index : string -> FacetQuery
    
    #if prop_IndexName
    #endif
    member IndexName : string with get, set

    #if prop_Query
    Used to filter the results on which faceting will occur
    #endif
    member Query : string with get, set
    
    #if prop_Count
    Used to count how many results to return
    #endif
    member Count : int with get, set

    #if prop_GroupBy
    Configures how the facets will apply. You can group by multiple fields,
    thus the array of configurations. Order matters. 
    #endif
    member GroupBy : FacetGroup [] with get, set
    
    override Validate : unit -> Result<unit> 

#if dto_FacetQuery
GroupItem is used to hold information about an item within a facet result
Example: There are 3 'Fish' within this category of the facet
#endif
[<ToString; Sealed>]
type GroupItem =
    inherit DtoBase
    new : unit -> GroupItem
    new : name : string * count : int -> GroupItem

    #if prop_Name
    Holds the name of the category set
    #endif
    member Name : string with get, set
    #if prop_Count
    Holds the number of such items within this set
    #endif
    member Count : int with get, set

    override Validate : unit -> Result<unit>

#if dto_FacetSearchResult
/// FacetSearchResult is used to capture the result of a facet query
#endif
[<ToString; Sealed>]
type Group =
    inherit DtoBase
    new : unit -> Group

    #if prop_GroupedBy
    Used to hold the value by which the results were grouped
    #endif
    member GroupedBy : string with get, set

    #if prop_GroupSize
    #endif
    member GroupSize : int with get, set

    #if prop_GroupItems
    #endif
    member GroupItems : GroupItem [] with get, set

    override Validate : unit -> Result<unit>

#if dto_Document
A document represents the basic unit of information which can be added or 
retrieved from the index. A document consists of several fields. A field represents 
the actual data to be indexed. In database analogy an index can be considered as 
a table while a document is a row of that table. Like a table a FlexSearch document 
requires a fix schema and all fields should have a field type.
#endif

[<ToString; Sealed>]
type Document = 
    inherit DtoBase
    new : unit -> Document
    new : indexName:string * id:string -> Document
    override Validate : unit -> Result<unit>
    #if prop_Default
    #endif
    static member Default : Document

#if prop_Fields    
Fields to be added to the document for indexing.
#endif
    member Fields : Dictionary<string, string> with get, set

#if prop_Id        
Unique Id of the document
#endif
    member Id : string with get, set

#if prop_TimeStamp  
Timestamp of the last modification of the document. This field is interpreted 
differently during a create and update operation. It also dictates whether and 
unique Id check is to be performed or not. 

Version number semantics
+ 0 - Don't care about the version and proceed with the operation normally.
+ -1 - Ensure that the document does not exist (Performs unique Id check).
+ 1 - Ensure that the document does exist. This is not relevant for create operation.
> 1 - Ensure that the version matches exactly. This is not relevant for create operation.
#endif
    member TimeStamp : int64 with get, set

#if prop_IndexName
Name of the index
#endif
    member IndexName : string with get, set

#if prop_Highlights        
Any matched text highlighted snippets. Note: Only used for results
#endif
    member Highlights : string [] with get, set

#if prop_Score        
Score of the returned document. Note: Only used for results
#endif
    member Score : double with get, set
// ----------------------------------------------------------------------------

#if dto_Index
FlexSearch index is a logical index built on top of Lucene’s index in a manner 
to support features like schema and sharding. So in this sense a FlexSearch 
index consists of multiple Lucene’s index. Also, each FlexSearch shard is a memberid 
Lucene index.

In case of a database analogy an index represents a table in a database where 
one has to define a schema upfront before performing any kind of operation on 
the table. There are various properties that can be defined at the index creation 
time. Only IndexName is a mandatory property, though one should always define 
Fields in an index to make any use of it.

By default a newly created index stays off-line. This is by design to force the 
user to enable an index before using it.
#endif

[<ToString; Sealed>]
type Index =
    inherit DtoBase
    new : unit -> Index
    override Validate : unit -> Result<unit>

#if prop_Fields
Fields to be used in index.
#endif
    member Fields : Field [] with get, set

#if prop_IndexConfiguration
#endif
    member IndexConfiguration : IndexConfiguration with get, set

#if prop_IndexName
Name of the index
#endif
    member IndexName : string with get, set

#if prop_Active
Signifies if the index is on-line or not? An index has to be on-line in order to 
enable searching over it.
#endif
    member Active : bool with get, set

#if prop_SearchProfiles
Search Profiles
#endif
    member SearchProfiles : SearchQuery [] with get, set

#if prop_ShardConfiguration
#endif
    member ShardConfiguration : ShardConfiguration with get, set
// ----------------------------------------------------------------------------

#if dto_SearchResults
memberRepresents the result returned by FlexSearch for a given search query.
#endif

[<ToString; Sealed>]
type SearchResults =
    inherit DtoBase
    new : unit -> SearchResults
#if prop_Documents
memberDocuments which are returned as a part of search response.
#endif
    member Documents : System.Collections.Generic.List<Document> with get, set

#if prop_RecordsReturned
memberTotal number of records returned.
#endif
    member RecordsReturned : int with get, set

#if prop_TotalAvailable
memberTotal number of records available on the server. This could be 
membergreater than the returned results depending upon the requested 
memberdocument count.
#endif
    member TotalAvailable : int with get, set
// ----------------------------------------------------------------------------

#if dto_Job
memberUsed by long running processes. All long running FlexSearch operations create
memberan instance of Job and return the Id to the caller. This Id can be used by the
membercaller to check the status of the job.
///
memberNOTE: Job information is not persistent
#endif

[<ToString; Sealed>]
type Job =
    inherit DtoBase
    new : unit -> Job

#if prop_FailedItems
memberItems which have failed processing.
#endif
    member FailedItems : int with get, set

#if prop_JobId
memberUnique Id of the Job
#endif
    member JobId : string with get, set

#if prop_Message
memberAny message that is associated with the job.
#endif
    member Message : string with get, set
    
#if prop_ProcessedItems
memberItems already processed.
#endif
    member ProcessedItems : int with get, set

#if prop_Status
memberOverall status of the job.
#endif
    member Status : JobStatus with get, set
    
#if prop_TotalItems
memberTotal items to be processed as a part of the current job.
#endif
    member TotalItems : int with get, set
// ----------------------------------------------------------------------------

#if dto_AnalysisRequest
memberRequest to analyze a text against an analyzer. The reason to force
memberthis parameter to request body is to avoid escaping of restricted characters
memberin the uri.
memberThis is helpful during analyzer testing.
#endif
[<ToString; Sealed>]
type AnalysisRequest =
    inherit DtoBase
    new : unit -> AnalysisRequest

    #if prop_Text
    #endif
    member Text : string with get, set
// ----------------------------------------------------------------------------

#if dto_CreateResponse

#endif
[<ToString; Sealed>]
type CreateResponse =
    inherit DtoBase
    new : unit -> CreateResponse
    new : id:string -> CreateResponse
    #if prop_Id
    #endif
    member Id : string with get, set
// ----------------------------------------------------------------------------

#if dto_IndexExistsResponse
#endif
[<ToString; Sealed>]
type IndexExistsResponse =
    inherit DtoBase
    new : unit -> IndexExistsResponse
    #if prop_Exists
    #endif
    member Exists : bool with get, set
// ----------------------------------------------------------------------------

#if dto_MemoryDetailsResponse
#endif
[<ToString; Sealed>]
type MemoryDetailsResponse =
    inherit DtoBase
    new : unit -> MemoryDetailsResponse
    override Validate : unit -> Result<unit>
    #if prop_TotalMemory
    #endif
    member TotalMemory : uint64 with get, set
    #if prop_Usage
    #endif
    member Usage : float with get, set
    #if prop_UsedMemory
    #endif
    member UsedMemory : int64 with get, set

#if dto_NoBody
#endif
type NoBody =
    inherit DtoBase
    new : unit -> NoBody

#if dto_SearchProfileTestDto
#endif
type SearchProfileTestDto =
    inherit DtoBase
    new : unit -> SearchProfileTestDto
    override Validate : unit -> Result<unit>
    #if prop_SearchProfile
    #endif
    member SearchProfile : string
    #if prop_SearchQuery
    #endif
    member SearchQuery : SearchQuery
    member SearchProfile : string with set
    member SearchQuery : SearchQuery with set
