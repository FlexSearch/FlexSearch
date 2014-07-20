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
enum NodeRole {
	Master = 1
	Slave = 2
}

enum FieldSimilarity {
	BM25 = 1
	TFIDF = 2
}

enum FieldPostingsFormat {
	Direct = 1
	Memory = 2
	Bloom_4_1 = 3 
	Pulsing_4_1 = 4 
	Lucene_4_1 = 5
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
	DocsOnly = 1
	DocsAndFreqs = 2
	DocsAndFreqsAndPositions = 3
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
	Lucene_4_9 = 1
}

enum Codec {
		Lucene_4_9 = 1
}

enum ScriptType {
	SearchProfileSelector = 1
	CustomScoring = 2
	ComputedField = 3
}

enum IndexState {
	Opening = 1
	Online = 2
	Offline = 3
	Closing = 4
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
	1:	optional i32 ShardCount = 1
}


struct IndexConfiguration {
	1:	optional i32 CommitTimeSec = 60
	2:	optional DirectoryType DirectoryType = 2
	3:	optional i32 DefaultWriteLockTimeout =  1000
	4:	optional i32 RamBufferSizeMb = 100
	5:	optional i32 RefreshTimeMilliSec = 25
	6:	optional IndexVersion IndexVersion = IndexVersion.Lucene_4_9
	7:	optional FieldPostingsFormat IdFieldPostingsFormat = FieldPostingsFormat.Bloom_4_1
	8:	optional FieldPostingsFormat DefaultIndexPostingsFormat = FieldPostingsFormat.Lucene_4_1
	9:	optional Codec DefaultCodec = Codec.Lucene_4_9
	10:	optional bool EnableVersioning = false
	11: optional FieldSimilarity DefaultFieldSimilarity = FieldSimilarity.TFIDF
}

struct FieldProperties {
	1:	optional bool Analyze = true
	2:	optional bool Index = true
	3:	optional bool Store = true
	4:	optional string IndexAnalyzer = "standardanalyzer"
	5:	optional string SearchAnalyzer = "standardanalyzer"
	6:	optional FieldType FieldType = 4
	7:	optional FieldPostingsFormat PostingsFormat = 5
	8:	optional FieldSimilarity Similarity = 2
	9:	optional FieldIndexOptions IndexOptions = 3
	10:	optional FieldTermVector TermVector = 3
	11:	optional bool OmitNorms = true
	12:	optional string ScriptName = ""
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
	1:	required MissingValueOption MissingValueOption
	2:	optional string DefaultValue
}

struct HighlightOption {
	1:	optional i32 FragmentsToReturn = 2
	2:	required list<string> HighlightedFields
	3:	optional string PostTag = "</B>"
	4:	optional string PreTag = "</B>"
}

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
	8:	optional string Logger = "Gibraltar"
}

// ----------------------------------------------------------------------------
//	Index & Document related
// ----------------------------------------------------------------------------
struct Document {
	1:	optional map<string, string> Fields = {}
	2:	optional list<string> Highlights = {}
	3:	required string Id
	4:	optional i64 LastModified
	5:	required string Index
	6:	optional double Score = 0.0
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

struct SearchResults {
	1:	optional list<Document> Documents = {}
	2:	optional i32 RecordsReturned
	3:	optional i32 TotalAvailable
}

struct FilterList {
	1:	required list<string> Words = {}
}

struct MapList {
	1:	required map<string, list<string>> Words = {}
}

struct IndexStatusResponse {
	1:	required IndexState Status
}

struct ImportRequest {
	1:	optional string Id
	2:	optional map<string,string> Parameters = {}
	3:	optional bool ForceCreate = false
	4:	optional string JobId
}

struct ImportResponse {
	1:	optional string JobId
	2:	optional string Message
}