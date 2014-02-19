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
 
namespace csharp FlexSearch.Api.Exception
namespace java org.FlexSearch.Api.Exception


// ----------------------------------------------------------------------------
//	Exceptions
// ----------------------------------------------------------------------------

struct InvalidOperation {
	1: string DeveloperMessage
	2: string UserMessage
	3: i32 ErrorCode
}

// ----------------------------------------------------------------------------
//	Specialized Exceptions
// ----------------------------------------------------------------------------

const InvalidOperation INDEX_NOT_FOUND = 
	{
		"DeveloperMessage" : "The requested index does not exist.", 
		"UserMessage" : "The requested index does not exist.", 
		"ErrorCode": 1000
	}
	
const InvalidOperation INDEX_ALREADY_EXISTS = 
	{
		"DeveloperMessage" : "The requested index already exist.", 
		"UserMessage" : "The requested index already exist.", 
		"ErrorCode": 1002
	}
	
const InvalidOperation INDEX_SHOULD_BE_OFFLINE = 
	{
		"DeveloperMessage" : "Index should be made offline before attempting to update index settings.", 
		"UserMessage" : "Index should be made offline before attempting the operation.", 
		"ErrorCode": 1003
	}
	
const InvalidOperation INDEX_IS_OFFLINE = 
	{
		"DeveloperMessage" : "The index is offline or closing. Please bring the index online to use it.", 
		"UserMessage" : "The index is offline or closing. Please bring the index online to use it.", 
		"ErrorCode": 1004
	}
	
const InvalidOperation INDEX_IS_OPENING = 
	{
		"DeveloperMessage" : "The index is in opening state. Please wait some time before making another request.", 
		"UserMessage" : "The index is in opening state. Please wait some time before making another request.", 
		"ErrorCode": 1005
	}
	
const InvalidOperation INDEX_REGISTERATION_MISSING = 
	{
		"DeveloperMessage" : "Registeration information associated with the index is missing.", 
		"UserMessage" : "Registeration information associated with the index is missing.",
		"ErrorCode": 1006
	}
	
const InvalidOperation INDEXING_DOCUMENT_ID_MISSING = 
	{
		"DeveloperMessage" : "Document id missing.", 
		"UserMessage" : "Document Id is required in order to index an document. Please specify _documentid and submit the document for indexing.",
		"ErrorCode": 1007
	}	
