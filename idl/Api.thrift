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


enum FieldPostingsFormat {
	Direct = 1
	Memory = 2 // Postings and DocValues formats that are read entirely into memory.
	Bloom = 3 // A PostingsFormat useful for low doc-frequency fields such as primary keys.
	Pulsing = 4 // Pulsing Codec: inlines low frequency terms' postings into terms dictionary.	
	Lucene41PostingsFormat = 5
}


enum DirectoryType {
	FileSystem = 1
	MemoryMapped = 2
	Ram = 3
}


enum FieldTermVector {
	DoNotStoreTermVector = 1
	StoreTermVector = 2
	StoreTermVectorsWithPositions = 3
	StoreTermVectorsWithPositionsandOffsets = 4
}


enum FieldIndexOptions {
	// Only documents are indexed: term frequencies and positions are omitted. 
	// Phrase and other positional queries on the field will throw an exception, 
	// and scoring will behave as if any term in the document appears only once.
	DocsOnly = 1

	// Only documents and term frequencies are indexed: positions are omitted. This enables normal scoring, 
	// except Phrase and other positional queries will throw an exception.
	DocsAndFreqs = 2

	// Indexes documents, frequencies and positions. This is a typical default for full-text 
	// search: full scoring is enabled and positional queries are supported.
	DocsAndFreqsAndPositions = 3

	/// Indexes documents, frequencies, positions and offsets. Character offsets are encoded alongside the positions.
	DocsAndFreqsAndPositionsAndOffsets = 4
}


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


enum ShardAllocationStrategy {
	Automatic = 1
	Manual = 2
}


enum IndexVersion {
	Lucene47 = 1
}


enum ScriptType {
	SearchProfileSelector = 1
    CustomScoring = 2
    ComputedField = 3
}

enum JobStatus {
	Initializing = 1
	Initialized = 2
	InProgress = 3
	Completed = 4
	CompletedWithErrors = 5
}

// ----------------------------------------------------------------------------
//	Structs
// ----------------------------------------------------------------------------
struct ShardConfiguration {
	1:	optional i16 Count = 1
}


struct IndexConfiguration {
	1:	optional i32 CommitTimeSec = 60
	2:	optional DirectoryType DirectoryType = 2
	3:	optional i32 DefaultWriteLockTimeout =  1000

	// Determines the amount of RAM that may be used for buffering added documents and deletions before 
	// they are flushed to the Directory.
	4:	optional i32 RamBufferSizeMb = 100
	5:	optional i32 RefreshTimeMilliSec = 25
	6:	optional IndexVersion IndexVersion = IndexVersion.Lucene47
}


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
struct TokenFilter {
	1:	required string FilterName
    2:	optional map<string, string> Parameters
}


struct Tokenizer {
	1:	required string TokenizerName
    2:	optional map<string, string> Parameters
}


struct AnalyzerProperties {
	1:	required Tokenizer Tokenizer
	2:	required list<TokenFilter> Filters
}


// ----------------------------------------------------------------------------
//	Scripting related
// ----------------------------------------------------------------------------
struct ScriptProperties {
	1:	required string Source
	2:	required ScriptType ScriptType
}


// ----------------------------------------------------------------------------
//	Search related
// ----------------------------------------------------------------------------
enum MissingValueOption {
	ThrowError = 1
	Default = 2
	Ignore = 3
}


struct MissingValue {
	1:	required string FieldName
	2:	required MissingValueOption MissingValueOption
	3:	optional string DefaultValue
}


struct HighlightOption {
	1:	optional i16 FragmentsToReturn = 2
	2:	required list<string> HighlightedFields
	3:	optional string PostTag = "</B>"
	4:	optional string PreTag = "</B>"
}


struct SearchQuery {
	1:	optional list<string> Columns = {}
	2:	optional i32 Count = 10
	3:	optional HighlightOption Highlights = {}
	4:	required string IndexName
	5:	optional string OrderBy = "score"
	6:	optional i32 Skip = 0
	7:	required string QueryString
	8:	optional list<MissingValue> MissingValueCofiguration = {}
	9:	optional MissingValueOption GlobalMissingValue = 1
}


// ----------------------------------------------------------------------------
//	Server Settings
// ----------------------------------------------------------------------------
struct ServerSettings {
	1:	optional i32 HttpPort = 9800
	2:	optional i32 ThriftPort = 9900
	3:	optional string DataFolder = "./data"
	4:	optional string PluginFolder = "./plugins"
	5:	optional string ConfFolder = "./conf"
	6:	optional string NodeName = "FlexNode"
	7:	optional NodeRole NodeRole = 1
}


// ----------------------------------------------------------------------------
//	Index & Document related
// ----------------------------------------------------------------------------
struct Document {
	1:	optional map<string, string> Fields = {}
	2:	optional list<string> Highlights = {}
	3:	required string Id
	4:	optional i64 LastModified
	5:	required i32 Version = 1
	7:	required string Index
	8:	optional double Score = 0.0
}


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
