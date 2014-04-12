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

include "Message.thrift"
include "Dto.thrift"
include "Api.thrift"

service FlexSearchService {
	// Index related operations
	Api.Index GetIndex(1:string indexName) throws (1:Message.InvalidOperation ex),
	void UpdateIndex(1:Api.Index index) throws (1:Message.InvalidOperation ex),
	void DeleteIndex(1:string indexName) throws (1:Message.InvalidOperation ex),
	void CreateIndex(1:Api.Index index) throws (1:Message.InvalidOperation ex),

	list<Api.Index> GetAllIndex() throws (1:Message.InvalidOperation ex),
	bool CheckIndexExists(1:string indexName) throws (1:Message.InvalidOperation ex),

	Api.IndexState GetIndexStatus(1:string indexName) throws (1:Message.InvalidOperation ex),
	void SetIndexStatus(1:string indexName, 2:Api.IndexState state) throws (1:Message.InvalidOperation ex),
	
	// Job status related operations
	Api.Job GetJob(1:string jobId) throws (1:Message.InvalidOperation ex),

	// Search operations
	Api.SearchResults Search(1:Api.SearchQuery query) throws (1:Message.InvalidOperation ex),
	list<map<string, string>> FlatSearch(1:Api.SearchQuery query) throws (1:Message.InvalidOperation ex)
}

