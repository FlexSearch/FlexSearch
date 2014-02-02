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
	ClusterMaster = 1
    ClusterSlave = 2
    Index = 3
    Query = 4
    UnDefined = 5	
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
	Lucene46 = 1
	Lucene47 = 2
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
struct Node {
	1:	required string NodeName
	2:	required string IpAddress
	3:	required i32 Port
	4:	required NodeRole NodeRole
	5:	required i32 Priority
}


struct ShardAllocationDetail {
	1:	required i16 ShardNumber
	2:	required list<string> Nodes
}


struct ShardConfiguration {
	1:	optional i16 ShardCount = 1
    2:	optional i16 Replica = 1
    3:	optional ShardAllocationStrategy AllocationStrategy = 2
	4:	required list<ShardAllocationDetail> AllocationDetails
	5:	optional bool AutoRebalance = false
    6:	optional i32 AutoRebalanceTimeOut = 300
}


struct IndexConfiguration {
	1:	optional i32 CommitTimeSec = 60
	2:	optional DirectoryType DirectoryType = 2
	3:	optional i32 DefaultWriteLockTimeout =  1000

	// Determines the amount of RAM that may be used for buffering added documents and deletions before // they are flushed to the Directory.
	4:	optional i32 RamBufferSizeMb = 100
	5:	optional i32 RefreshTimeMilliSec = 25
	6:	optional IndexVersion IndexVersion = IndexVersion.Lucene46
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


// ----------------------------------------------------------------------------
//	Exceptions
// ----------------------------------------------------------------------------

exception InvalidOperation {
	1: string DeveloperMessage
	2: string UserMessage
	3: i32 ErrorCode
}

// ----------------------------------------------------------------------------
//	Specialized Exceptions
// ----------------------------------------------------------------------------
const InvalidOperation INDEX_NOT_FOUND = {"DeveloperMessage" : "The requested index does not exist.", "UserMessage" : "The requested index does not exist.", "ErrorCode": 1000}
const InvalidOperation INDEX_ALREADY_EXISTS = {"DeveloperMessage" : "The requested index already exist.", "UserMessage" : "The requested index already exist.", "ErrorCode": 1002}
const InvalidOperation INDEX_SHOULD_BE_OFFLINE = {"DeveloperMessage" : "Index should be made offline before attempting to update index settings.", "UserMessage" : "Index should be made offline before attempting the operation.", "ErrorCode": 1003}


// ----------------------------------------------------------------------------
//	Distributed coordination related
// ----------------------------------------------------------------------------
struct VoteResponse {
	1: string VotedFor
	2: bool VoteGranted
}

service FlexSearchService {
	// Consensus related
	VoteResponse RequestVoteForClusterMaster(1: string serverName, 2: i32 metric)
	
	// Identity related and gossip
	list<Index> GetAllIndexSettings() 
	oneway void HeartBeat()
	oneway void DeadNodeNotification(1: string nodeName)
	oneway void JoinNodeNotification(1: string nodeName)
	
	// Index related
	void AddIndex (1: Index index) throws(1: InvalidOperation message)
	void UpdateIndex (1: Index index) throws(1: InvalidOperation message)
	void GetIndex (1: string indexName) throws(1: InvalidOperation message)
	void DeleteIndex (1: string indexName) throws(1: InvalidOperation message)
	void SetIndexState (1: string indexName, 2: bool online) throws(1: InvalidOperation message)
	void UpdateIndexConfiguration (1: string indexName, 2: IndexConfiguration configuration) throws(1: InvalidOperation message)
	void UpdateShardConfiguration (1: string indexName, 2: ShardConfiguration configuration) throws(1: InvalidOperation message)
	
	// This is used by the non master shard to request full file level index synchronzation. 
	// Shard master will return a guid which can be used to check the status of the sync operation
	string RequestFullIndexSync (1: string indexName, 2: i32 shardNumber, 3: string networkPath) throws(1: InvalidOperation message)
	
	// This is used for TLog based synchronization. A paging mechanism is supported to enable smaller packet size over the network
	list<Document> RequestTransactionLog (1: string indexName, 2: i32 shardNumber, 3: i64 startTimeStamp, 4: i64 endTimestamp, 5: i32 count, 6: i32 skip) throws(1: InvalidOperation message)
	
	// Get the total number of transactions that have happened in the given time period. This is useful to get the total change count. This will help in deciding 
	// if the node needs a full recovery or not.
	i32 RequestTransactionLogCount (1: string indexName, 2: i32 shardNumber, 3: i64 startTimeStamp, 4: i64 endTimestamp) throws(1: InvalidOperation message)
	
	// All transaction log records older than the end timestamp will be purged
	string PurgeTLog (1: string indexName, 2: i32 shardNumber, 3: i64 endTimeStamp)
	
	// Job related
	Job GetJobById (1: string JobId) throws(1: InvalidOperation message)
	
	// Document related
	oneway void AddDocument(1: Document document)
	oneway void AddDocumentToReplica(1: Document document)
	oneway void UpdateDocument(1: Document document)
	oneway void UpdateDocumentInReplica(1: Document document)
	oneway void DeleteDocument(1: Document document)
	oneway void DeleteDocumentFromReplica(1: Document document)
	Document GetDocument(1: string indexName, 2: string documentId)
	
	// Logs related
	// Something for node status, cluster status, performance logs etc
}

