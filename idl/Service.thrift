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
 
namespace csharp FlexSearch.Api.Service
namespace java org.FlexSearch.Api.Service

include "Exception.thrift"
include "Dto.thrift"
include "Api.thrift"

service FlexSearchService {
	
	// Index related
	void AddIndex (1: Api.Index index) throws(1: Exception.InvalidOperation message)
	void UpdateIndex (1: Api.Index index) throws(1: Exception.InvalidOperation message)
	void GetIndex (1: string indexName) throws(1: Exception.InvalidOperation message)
	void DeleteIndex (1: string indexName) throws(1: Exception.InvalidOperation message)
	void SetIndexState (1: string indexName, 2: bool online) throws(1: Exception.InvalidOperation message)
	void UpdateIndexConfiguration (1: string indexName, 2: Api.IndexConfiguration configuration) throws(1: Exception.InvalidOperation message)
	void UpdateShardConfiguration (1: string indexName, 2: Api.ShardConfiguration configuration) throws(1: Exception.InvalidOperation message)
		
	// Job related
	Api.Job GetJobById (1: string JobId) throws(1: Exception.InvalidOperation message)
	
	// Document related
	oneway void AddDocument(1: Api.Document document)
	oneway void AddDocumentToReplica(1: Api.Document document)
	oneway void UpdateDocument(1: Api.Document document)
	oneway void UpdateDocumentInReplica(1: Api.Document document)
	oneway void DeleteDocument(1: Api.Document document)
	oneway void DeleteDocumentFromReplica(1: Api.Document document)
	Api.Document GetDocument(1: string indexName, 2: string documentId)
	
	/**
	// Logs related
	// Something for node status, cluster status, performance logs etc
	
	// This is used by the non master shard to request full file level index synchronzation. 
	// Shard master will return a guid which can be used to check the status of the sync operation
	string RequestFullIndexSync (1: string indexName, 2: i32 shardNumber, 3: string networkPath) throws(1: Exception.InvalidOperation message)
	
	// This is used for TLog based synchronization. A paging mechanism is supported to enable smaller packet size over the network
	list<Document> RequestTransactionLog (1: string indexName, 2: i32 shardNumber, 3: i64 startTimeStamp, 4: i64 endTimestamp, 5: i32 count, 6: i32 skip) throws(1: Exception.InvalidOperation message)
	
	// Get the total number of transactions that have happened in the given time period. This is useful to get the total change count. This will help in deciding 
	// if the node needs a full recovery or not.
	i32 RequestTransactionLogCount (1: string indexName, 2: i32 shardNumber, 3: i64 startTimeStamp, 4: i64 endTimestamp) throws(1: Exception.InvalidOperation message)
	
	// All transaction log records older than the end timestamp will be purged
	string PurgeTLog (1: string indexName, 2: i32 shardNumber, 3: i64 endTimeStamp)
	**/
}

