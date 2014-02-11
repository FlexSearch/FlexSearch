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