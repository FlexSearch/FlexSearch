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

// ----------------------------------------------------------------------------
//	Enums
// ----------------------------------------------------------------------------

// Node role
enum NodeRole {
	ClusterMaster = 1,
    ClusterSlave = 2,
    Index = 3,
    Query = 4,
    UnDefined = 5	
}


enum FieldSimilarity {
	BM25 = 1,
	TDF = 2
}


enum FieldPostingsFormat {
	Direct = 1,
	Memory = 2, // Postings and DocValues formats that are read entirely into memory.
	Bloom = 3, // A PostingsFormat useful for low doc-frequency fields such as primary keys.
	Pulsing = 4, // Pulsing Codec: inlines low frequency terms' postings into terms dictionary.	
	Lucene41PostingsFormat = 5
}


enum DirectoryType {
	FileSystem = 1,
	MemoryMapped = 2,
	Ram = 3
}


enum FieldTermVector {
	DoNotStoreTermVector = 1,
	StoreTermVector = 2,
	StoreTermVectorsWithPositions = 3,
	StoreTermVectorsWithPositionsandOffsets = 4
}


enum FieldIndexOptions {
	// Only documents are indexed: term frequencies and positions are omitted. 
	// Phrase and other positional queries on the field will throw an exception, 
	// and scoring will behave as if any term in the document appears only once.
	DocsOnly = 1,

	// Only documents and term frequencies are indexed: positions are omitted. This enables normal scoring, 
	// except Phrase and other positional queries will throw an exception.
	DocsAndFreqs = 2,

	// Indexes documents, frequencies and positions. This is a typical default for full-text 
	// search: full scoring is enabled and positional queries are supported.
	DocsAndFreqsAndPositions = 3,

	/// Indexes documents, frequencies, positions and offsets. Character offsets are encoded alongside the positions.
	DocsAndFreqsAndPositionsAndOffsets = 4
}


enum FieldType {
	Int = 1,
	Double = 2,
	ExactText = 3,
	Text = 4,
	Highlight = 5,
	Bool = 6,
	Date = 7,
	DateTime = 8,
	Custom = 9,
	Stored = 10,
	Long = 11
}


enum ShardAllocationStrategy {
	Automatic = 1,
	Manual = 2
}


enum IndexVersion {
	Lucene46 = 1,
	Lucene47 = 2
}


enum ScriptType {
	SearchProfileSelector = 1,
    CustomScoring = 2,
    ComputedField = 3
}


// ----------------------------------------------------------------------------
//	Structs
// ----------------------------------------------------------------------------
struct Node {
	1:	required string NodeName;
	2:	required string IpAddress;
	3:	required NodeRole NodeRole;
}


struct ShardAllocationDetail {
	1:	required i16 ShardNumber;
	2:	required list<string> Nodes;
}


struct ShardConfiguration {
	1:	optional i16 ShardCount = 1;
    2:	optional i16 Replica = 1;
    3:	optional ShardAllocationStrategy AllocationStrategy = 2;
	4:	required list<ShardAllocationDetail> AllocationDetails;
	5:	optional bool AutoRebalance = false;
    6:	optional i32 AutoRebalanceTimeOut = 300;
}


struct IndexConfiguration {
	1:	optional i32 CommitTimeSec = 60;
	2:	optional DirectoryType DirectoryType = 2;
	3:	optional i32 DefaultWriteLockTimeout =  1000;

	// Determines the amount of RAM that may be used for buffering added documents and deletions before // they are flushed to the Directory.
	4:	optional i32 RamBufferSizeMb = 100;
	5:	optional i32 RefreshTimeMilliSec = 25;
	6:	optional IndexVersion IndexVersion = IndexVersion.Lucene46;
	7:	required ShardConfiguration Configuration;
}


struct IndexFieldProperties {
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


// ----------------------------------------------------------------------------
//	Analyzer related
// ----------------------------------------------------------------------------
struct TokenFilter {
	1:	required string FilterName;
    2:	optional map<string, string> Parameters;
}


struct Tokenizer {
	1:	required string TokenizerName;
    2:	optional map<string, string> Parameters;
}


struct AnalyzerProperties {
	1:	required Tokenizer Tokenizer;
	2:	required list<TokenFilter> Filters;
}


struct ScriptProperties {
	1:	required string Source;
	2:	required ScriptType ScriptType;
}

