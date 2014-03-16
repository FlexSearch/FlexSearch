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

/**
 * Available types in Thrift
 *
 *  bool        Boolean, one byte
 *  byte        Signed byte
 *  i16         Signed 16-bit integer
 *  i32         Signed 32-bit integer
 *  i64         Signed 64-bit integer
 *  double      64-bit floating point value
 *  string      String
 *  binary      Blob (byte array)
 *  map<t1,t2>  Map from one type to another
 *  list<t1>    Ordered list of one type
 *  set<t1>     Set of unique elements of one type
 *
 */
 
namespace csharp FlexSearch.Api
namespace java org.FlexSearch.Api

// ----------------------------------------------------------------------------
//	Enums
// ----------------------------------------------------------------------------

// Node role
enum NodeRole {
	Master = 1
    Slave = 2
}


enum FieldSimilarity {
	BM25 = 1
	TDF = 2
}

/*
<div id="FieldPostingsFormat" type="glossary"></div>
## FieldPostingsFormat
Encodes/decodes terms, postings, and proximity data.

**Memory FieldPostingsFormat**
Postings and DocValues formats that are read entirely into memory.

**Bloom FieldPostingsFormat**
A PostingsFormat useful for low doc-frequency fields such as primary keys.

**Pulsing FieldPostingsFormat**
Pulsing Codec: inlines low frequency terms' postings into terms dictionary.
``` java
*/
enum FieldPostingsFormat {
	Direct = 1
	Memory = 2
	Bloom = 3 
	Pulsing = 4 
	Lucene41PostingsFormat = 5
}
//```
//<div></div>

/*
<div id="DirectoryType" type="glossary"></div>
## DirectoryType

**Directory**
A Directory is a flat list of files. Files may be written once, when they are created. Once a file is created it may only be opened for read, or deleted. Random access is permitted both when reading and writing.

**Ram Directory**
A memory-resident Directory implementation. This is not intended to work with huge indexes. Everything beyond several hundred megabytes will waste resources (GC cycles), because it uses an internal buffer size of 1024 bytes, producing millions of byte[1024] arrays. This class is optimized for small memory-resident indexes. It also has bad concurrency on multithreaded environments.
It is recommended to materialize large indexes on disk and use MMapDirectory, which is a high-performance directory implementation working directly on the file system cache of the operating system.

**MemoryMapped Directory**
File-based Directory implementation that uses mmap for reading, and FSDirectory.FSIndexOutput for writing.
NOTE: memory mapping uses up a portion of the virtual memory address space in your process equal to the size of the file being mapped. Before using this class, be sure your have plenty of virtual address space, e.g. by using a 64 bit JRE, or a 32 bit JRE with indexes that are guaranteed to fit within the address space. On 32 bit platforms also consult MMapDirectory(File, LockFactory, int) if you have problems with mmap failing because of fragmented address space. If you get an OutOfMemoryException, it is recommended to reduce the chunk size, until it works.

Due to this bug in Sun's JRE, MMapDirectory's IndexInput.close() is unable to close the underlying OS file handle. Only when GC finally collects the underlying objects, which could be quite some time later, will the file handle be closed.

This will consume additional transient disk usage: on Windows, attempts to delete or overwrite the files will result in an exception; on other platforms, which typically have a "delete on last close" semantics, while such operations will succeed, the bytes are still consuming space on disk. For many applications this limitation is not a problem (e.g. if you have plenty of disk space, and you don't rely on overwriting files on Windows) but it's still an important limitation to be aware of.

**FileSystem Directory**
FileSystem Directory is a straightforward implementation using java.io.RandomAccessFile. However, it has poor concurrent performance (multiple threads will bottleneck) as it synchronizes when multiple threads read from the same file.

``` java
*/
enum DirectoryType {
	FileSystem = 1
	MemoryMapped = 2
	Ram = 3
}
//``` 
//<div></div>

/*
<div id="FieldTermVector" type="glossary"></div>
## FieldTermVector
**DoNotStoreTermVector**
Do not store term vectors.

**StoreTermVector**
Store the term vectors of each document. A term vector is a list of the document's terms and their number of occurrences in that document.

**StoreTermVectorsWithPositions**
Store the term vector + token position information

**StoreTermVectorsWithPositionsandOffsets**
Store the term vector + Token position and offset information

``` java
*/
enum FieldTermVector {
	DoNotStoreTermVector = 1
	StoreTermVector = 2
	StoreTermVectorsWithPositions = 3
	StoreTermVectorsWithPositionsandOffsets = 4
}
//```
//<div></div>

/*
<div id="FieldIndexOptions" type="glossary"></div>
## FieldIndexOptions
**DocsOnly**
Only documents are indexed: term frequencies and positions are omitted. Phrase and other positional queries on the field will throw an exception, and scoring will behave as if any term in the document appears only once.

**DocsAndFreqs**
Only documents and term frequencies are indexed: positions are omitted. This enables normal scoring, except Phrase and other positional queries will throw an exception.

**DocsAndFreqsAndPositions**
Indexes documents, frequencies and positions. This is a typical default for full-text search: full scoring is enabled and positional queries are supported.

**DocsAndFreqsAndPositionsAndOffsets**
Indexes documents, frequencies, positions and offsets. Character offsets are encoded alongside the positions.

``` java
*/
enum FieldIndexOptions {
	DocsOnly = 1
	DocsAndFreqs = 2
	DocsAndFreqsAndPositions = 3
	DocsAndFreqsAndPositionsAndOffsets = 4
}
//```
//<div></div>

/*
<div id="FieldType" type="glossary"></div>
## FieldType
For detailed information refer to [Index Fields](|filename|indexfield.md).
``` java
*/
enum FieldType {
	Int = 1
	Double = 2
	ExactText = 3
	Text = 4
	Highlight = 5
	Bool = 6
	Date = 7
	DateTime = 8
	Custom = 9
	Stored = 10
	Long = 11
}
//```
//<div></div>

enum ShardAllocationStrategy {
	Automatic = 1
	Manual = 2
}

/*
<div id="IndexVersion" type="glossary"></div>
## IndexVersion
Version of the Lucene index used behind the scene. 
``` java
*/
enum IndexVersion {
	Lucene47 = 1
}
//```
//<div></div>

/*
<div id="ScriptType" type="glossary"></div>
## ScriptType

``` java
*/
enum ScriptType {
	SearchProfileSelector = 1
    CustomScoring = 2
    ComputedField = 3
}
//```
//<div></div>

/*
<div id="IndexState" type="glossary"></div>
## IndexState

``` java
*/
enum IndexState {
    Opening = 1
    Online = 2
    Offline = 3
    Closing = 4
}
//```
//<div></div>

/*
<div id="JobStatus" type="glossary"></div>
## JobStatus

``` java
*/
enum JobStatus {
	Initializing = 1
	Initialized = 2
	InProgress = 3
	Completed = 4
	CompletedWithErrors = 5
}
//```
//<div></div>

// ----------------------------------------------------------------------------
//	Structs
// ----------------------------------------------------------------------------

/*
<div id="ShardConfiguration" type="glossary"></div>
## ShardConfiguration

``` java
*/
struct ShardConfiguration {
	1:	optional i32 ShardCount = 1
}
//```
//<div></div>

/*
<div id="IndexConfiguration" type="glossary"></div>
## IndexConfiguration
**CommitTimeSec**

**DirectoryType**
Refer to directory type.

**DefaultWriteLockTimeout**

**RamBufferSizeMb**
Determines the amount of RAM that may be used for buffering added documents and deletions before they are flushed to the Directory.

**RefreshTimeMilliSec**

**IndexVersion**
Refer to IndexVersion.

``` java
*/
struct IndexConfiguration {
	1:	optional i32 CommitTimeSec = 60
	2:	optional DirectoryType DirectoryType = 2
	3:	optional i32 DefaultWriteLockTimeout =  1000
	4:	optional i32 RamBufferSizeMb = 100
	5:	optional i32 RefreshTimeMilliSec = 25
	6:	optional IndexVersion IndexVersion = IndexVersion.Lucene47
}
//```
//<div></div>

/*
<div id="FieldProperties" type="glossary"></div>
## FieldType
Refer to Index Fields.
``` java
*/
struct FieldProperties {
	1:	optional bool Analyze = true
	2:	optional bool Index = true
	3:	optional bool Store = true
	4:	optional string IndexAnalyzer = "standardanalyzer"
	5:	optional string SearchAnalyzer = "standardanalyzer"
	6:	optional FieldType FieldType = 4
	7:	optional FieldPostingsFormat PostingsFormat = 5
	8:	optional FieldIndexOptions IndexOptions = 3
	9:	optional FieldTermVector TermVector = 3
	10:	optional bool OmitNorms = true
	11:	optional string ScriptName = ""
}
//```
//<div></div>

struct Job {
	1:	required string JobId
	2:	optional i32 TotalItems
	3:	optional i32 ProcessedItems
	4:	optional i32 FailedItems
	5:	required JobStatus Status = 1
	6:	optional string Message
}


// ----------------------------------------------------------------------------
//	Analyzer related
// ----------------------------------------------------------------------------
/*
<div id="TokenFilter" type="glossary"></div>
## TokenFilter
Refer to 'FlexSearch Analysis'
``` java
*/
struct TokenFilter {
	1:	required string FilterName
    2:	optional map<string, string> Parameters
}
//```
//<div></div>

/*
<div id="Tokenizer" type="glossary"></div>
## Tokenizer
Refer to 'FlexSearch Analysis'
``` java
*/
struct Tokenizer {
	1:	required string TokenizerName
    2:	optional map<string, string> Parameters
}
//```
//<div></div>

/*
<div id="AnalyzerProperties" type="glossary"></div>
## AnalyzerProperties
Refer to 'FlexSearch Analysis'
``` java
*/
struct AnalyzerProperties {
	1:	required Tokenizer Tokenizer
	2:	required list<TokenFilter> Filters
}
//```
//<div></div>

// ----------------------------------------------------------------------------
//	Scripting related
// ----------------------------------------------------------------------------
/*
<div id="ScriptProperties" type="glossary"></div>
## ScriptProperties

``` java
*/
struct ScriptProperties {
	1:	required string Source
	2:	required ScriptType ScriptType
}
//```
//<div></div>

// ----------------------------------------------------------------------------
//	Search related
// ----------------------------------------------------------------------------
/*
<div id="MissingValueOption" type="glossary"></div>
## MissingValueOption
Refer to 'Search profile basics'.
``` java
*/
enum MissingValueOption {
	ThrowError = 1
	Default = 2
	Ignore = 3
}
//```
//<div></div>

struct MissingValue {
	1:	required MissingValueOption MissingValueOption
	2:	optional string DefaultValue
}

/*
<div id="HighlightOption" type="glossary"></div>
## HighlightOption
Refer to 'Search basics'.
``` java
*/
struct HighlightOption {
	1:	optional i32 FragmentsToReturn = 2
	2:	required list<string> HighlightedFields
	3:	optional string PostTag = "</B>"
	4:	optional string PreTag = "</B>"
}
//```
//<div></div>

/*
<div id="SearchQuery" type="glossary"></div>
## SearchQuery
Refer to 'Search basics'.
``` java
*/
struct SearchQuery {
	1:	optional list<string> Columns = {}
	2:	optional i32 Count = 10
	3:	optional HighlightOption Highlights
	4:	required string IndexName
	5:	optional string OrderBy = "score"
	6:	optional i32 Skip = 0
	7:	required string QueryString
	8:	optional map<string, MissingValueOption> MissingValueConfiguration = {}
	9:	optional MissingValueOption GlobalMissingValue = 1
	10:	optional bool ReturnFlatResult = false
	11:	optional bool ReturnScore = true
	12: optional string SearchProfile
	13: optional string SearchProfileSelector
}
//```
//<div></div>

// ----------------------------------------------------------------------------
//	Server Settings
// ----------------------------------------------------------------------------
/*
<div id="ServerSettings" type="glossary"></div>
## ServerSettings

``` java
*/
struct ServerSettings {
	1:	optional i32 HttpPort = 9800
	2:	optional i32 ThriftPort = 9900
	3:	optional string DataFolder = "./data"
	4:	optional string PluginFolder = "./plugins"
	5:	optional string ConfFolder = "./conf"
	6:	optional string NodeName = "FlexNode"
	7:	optional NodeRole NodeRole = 1
}
//```
//<div></div>

// ----------------------------------------------------------------------------
//	Index & Document related
// ----------------------------------------------------------------------------
/*
<div id="Document" type="glossary"></div>
## Document
Refer to 'Indexing basics'.
``` java
*/
struct Document {
	1:	optional map<string, string> Fields = {}
	2:	optional list<string> Highlights = {}
	3:	required string Id
	4:	optional i64 LastModified
	7:	required string Index
	8:	optional double Score = 0.0
}
//```
//<div></div>

/*
<div id="Index" type="glossary"></div>
## Index

``` java
*/
struct Index {
	1:	optional map<string, AnalyzerProperties> Analyzers = {}
	2:	required IndexConfiguration IndexConfiguration = {}
	3:	required map<string, FieldProperties> Fields = {}
	4:	required string IndexName
	5:	required bool Online = false
	6:	optional map<string, ScriptProperties> Scripts = {}
	7:	optional map<string, SearchQuery> SearchProfiles = {}
	8:	required ShardConfiguration ShardConfiguration = {}
}
//```
//<div></div>

/*
<div id="SearchResults" type="glossary"></div>
## SearchResults

``` java
*/
struct SearchResults {
	1:	optional list<Document> Documents = {}
	2:	optional i32 RecordsReturned
	3:	optional i32 TotalAvailable
}
//```
//<div></div>

struct IndexStatusResponse {
	1:	required IndexState Status
}