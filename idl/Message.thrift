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
 
namespace csharp FlexSearch.Api.Message
namespace java org.FlexSearch.Api.Message


// ----------------------------------------------------------------------------
//	Exceptions
// ----------------------------------------------------------------------------

struct OperationMessage {
	1: string DeveloperMessage
	2: string UserMessage
	3: i32 ErrorCode
}

// ----------------------------------------------------------------------------
//	Specialized Exceptions
// ----------------------------------------------------------------------------

const OperationMessage INDEX_NOT_FOUND = 
	{
		"DeveloperMessage" : "The requested index does not exist.", 
		"UserMessage" : "The requested index does not exist.", 
		"ErrorCode": 1000
	}
	
const OperationMessage INDEX_ALREADY_EXISTS = 
	{
		"DeveloperMessage" : "The requested index already exist.", 
		"UserMessage" : "The requested index already exist.", 
		"ErrorCode": 1002
	}
	
const OperationMessage INDEX_SHOULD_BE_OFFLINE = 
	{
		"DeveloperMessage" : "Index should be made offline before attempting to update index settings.", 
		"UserMessage" : "Index should be made offline before attempting the operation.", 
		"ErrorCode": 1003
	}
	
const OperationMessage INDEX_IS_OFFLINE = 
	{
		"DeveloperMessage" : "The index is offline or closing. Please bring the index online to use it.", 
		"UserMessage" : "The index is offline or closing. Please bring the index online to use it.", 
		"ErrorCode": 1004
	}
	
const OperationMessage INDEX_IS_OPENING = 
	{
		"DeveloperMessage" : "The index is in opening state. Please wait some time before making another request.", 
		"UserMessage" : "The index is in opening state. Please wait some time before making another request.", 
		"ErrorCode": 1005
	}
	
const OperationMessage INDEX_REGISTERATION_MISSING = 
	{
		"DeveloperMessage" : "Registeration information associated with the index is missing.", 
		"UserMessage" : "Registeration information associated with the index is missing.",
		"ErrorCode": 1006
	}
	
const OperationMessage INDEXING_DOCUMENT_ID_MISSING = 
	{
		"DeveloperMessage" : "Document id missing.", 
		"UserMessage" : "Document Id is required in order to index an document. Please specify _id and submit the document for indexing.",
		"ErrorCode": 1007
	}	

const OperationMessage ERROR_OPENING_INDEXWRITER = 
	{
		"DeveloperMessage" : "{To be populated by the developer code}", 
		"UserMessage" : "Unable to open index writer.",
		"ErrorCode": 1008
	}	

const OperationMessage ERROR_ADDING_INDEX_STATUS = 
	{
		"DeveloperMessage" : "Unable to set the index status.", 
		"UserMessage" : "Unable to set the index status.",
		"ErrorCode": 1009
	}

const OperationMessage INDEX_IS_ALREADY_ONLINE = 
	{
		"DeveloperMessage" : "The index is already online or opening at the moment.", 
		"UserMessage" : "The index is already online or opening at the moment.", 
		"ErrorCode": 1010
	}
	
const OperationMessage INDEX_IS_ALREADY_OFFLINE = 
	{
		"DeveloperMessage" : "The index is already offline or closing at the moment.", 
		"UserMessage" : "The index is already offline or closing at the moment.", 
		"ErrorCode": 1011
	}

const OperationMessage INDEX_IS_IN_INVALID_STATE = 
	{
		"DeveloperMessage" : "Index is in invalid state.", 
		"UserMessage" : "Index is in invalid state.", 
		"ErrorCode": 1012
	}	
// ----------------------------------------------------------------------------
//	Validation Exceptions
// ----------------------------------------------------------------------------
const OperationMessage PROPERTY_CANNOT_BE_EMPTY = 
	{
		"DeveloperMessage" : "Field:{propertyName} cannot be empty.", 
		"UserMessage" : "Field:{propertyName} cannot be empty.",
		"ErrorCode": 2001
	}
	
const OperationMessage REGEX_NOT_MATCHED = 
	{
		"DeveloperMessage" : "Field:{propertyName} does not match the regex pattern {value}.", 
		"UserMessage" : "Field:{propertyName} does not match the regex pattern {value}.",
		"ErrorCode": 2002
	}	
	
const OperationMessage VALUE_NOT_IN = 
	{
		"DeveloperMessage" : "Field:{propertyName} cannot have the following as valid values: {value}.", 
		"UserMessage" : "Field:{propertyName} cannot have the following as valid values: {value}.", 
		"ErrorCode": 2003
	}	
	
const OperationMessage VALUE_ONLY_IN = 
	{
		"DeveloperMessage" : "Field:{propertyName} can only have the following as valid values: {value}.", 
		"UserMessage" : "Field:{propertyName} can only have the following as valid values: {value}.", 
		"ErrorCode": 2004
	}
	
const OperationMessage GREATER_THAN_EQUAL_TO = 
	{
		"DeveloperMessage" : "Field:{propertyName} should be greater than or equal to: {value}.", 
		"UserMessage" : "Field:{propertyName} should be greater than or equal to: {value}.", 
		"ErrorCode": 2005
	}	

const OperationMessage GREATER_THAN = 
	{
		"DeveloperMessage" : "Field:{propertyName} should be greater than: {value}.", 
		"UserMessage" : "Field:{propertyName} should be greater than: {value}.", 
		"ErrorCode": 2006
	}	
	
const OperationMessage LESS_THAN_EQUAL_TO = 
	{
		"DeveloperMessage" : "Field:{propertyName} should be less than or equal to: {value}.", 
		"UserMessage" : "Field:{propertyName} should be less than or equal to: {value}.", 
		"ErrorCode": 2005
	}	

const OperationMessage LESS_THAN = 
	{
		"DeveloperMessage" : "Field:{propertyName} should be less than: {value}.", 
		"UserMessage" : "Field:{propertyName} should be less than: {value}.", 
		"ErrorCode": 2006
	}
	
const OperationMessage FILTER_CANNOT_BE_INITIALIZED = 
	{
		"DeveloperMessage" : "Filter:{propertyName} cannot be initialized: {value}", 
		"UserMessage" : "Filter:{propertyName} cannot be initialized.", 
		"ErrorCode": 2007
	}
	
const OperationMessage FILTER_NOT_FOUND = 
	{
		"DeveloperMessage" : "Filter:{propertyName} not found.", 
		"UserMessage" : "Filter:{propertyName} not found.", 
		"ErrorCode": 2008
	}	
	
const OperationMessage TOKENIZER_CANNOT_BE_INITIALIZED = 
	{
		"DeveloperMessage" : "Tokenizer:{propertyName} cannot be initialized: {value}", 
		"UserMessage" : "Tokenizer:{propertyName} cannot be initialized.", 
		"ErrorCode": 2009
	}
	
const OperationMessage TOKENIZER_NOT_FOUND = 
	{
		"DeveloperMessage" : "Tokenizer:{propertyName} not found.", 
		"UserMessage" : "Tokenizer:{propertyName} not found.", 
		"ErrorCode": 2010
	}

const OperationMessage ATLEAST_ONE_FILTER_REQUIRED = 
	{
		"DeveloperMessage" : "Atleast one filter should be specified for a custom analyzer.", 
		"UserMessage" : "Atleast one filter should be specified for a custom analyzer.", 
		"ErrorCode": 2011
	}
	
const OperationMessage UNKNOWN_FIELD_TYPE = 
	{
		"DeveloperMessage" : "Unsupported field type specified in the Field Properties.", 
		"UserMessage" : "Unsupported field type specified in the Field Properties.", 
		"ErrorCode": 2012
	}

const OperationMessage SCRIPT_NOT_FOUND = 
	{
		"DeveloperMessage" : "Script{propertyName} not found.", 
		"UserMessage" : "Script{propertyName} not found.", 
		"ErrorCode": 2013
	}

const OperationMessage ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE = 
	{
		"DeveloperMessage" : "FieldType:{propertyName} does not support custom analyzer.", 
		"UserMessage" : "FieldType:{propertyName} does not support custom analyzer.", 
		"ErrorCode": 2014
	}

const OperationMessage UNKNOWN_SCRIPT_TYPE = 
	{
		"DeveloperMessage" : "ScriptType:{propertyName} is not supported.", 
		"UserMessage" : "ScriptType:{propertyName} is not supported.", 
		"ErrorCode": 2015
	}
	
const OperationMessage ANALYZER_NOT_FOUND = 
	{
		"DeveloperMessage" : "Analyzer:{propertyName} not found.", 
		"UserMessage" : "Analyzer:{propertyName} not found.", 
		"ErrorCode": 2016
	}
	
// ----------------------------------------------------------------------------
//	Compilation Exceptions
// ----------------------------------------------------------------------------	
const OperationMessage SCRIPT_CANT_BE_COMPILED = 
	{
		"DeveloperMessage" : "Script:{propertyName} cannot be compiled. {value}", 
		"UserMessage" : "Script:{propertyName} cannot be compiled.", 
		"ErrorCode": 3000
	}
	

// ----------------------------------------------------------------------------
//	Mef Related
// ----------------------------------------------------------------------------	
const OperationMessage MODULE_NOT_FOUND = 
	{
		"DeveloperMessage" : "Module:{propertyName} can not be found. Please make sure all the compiled dependecies are accessible by the server.", 
		"UserMessage" : "Module:{propertyName} can not be found. Please make sure all the compiled dependecies are accessible by the server.", 
		"ErrorCode": 4000
	}
	
	
// ----------------------------------------------------------------------------
//	Search Related
// ----------------------------------------------------------------------------	
const OperationMessage INVALID_QUERY_TYPE = 
	{
		"DeveloperMessage" : "QueryType:{propertyName} can not be found. Please make sure all the compiled dependecies are accessible by the server.", 
		"UserMessage" : "QueryType:{propertyName} can not be found. Please make sure all the compiled dependecies are accessible by the server.", 
		"ErrorCode": 5000
	}
	
const OperationMessage INVALID_FIELD_NAME = 
	{
		"DeveloperMessage" : "FieldName:{propertyName} can not be found.", 
		"UserMessage" : "FieldName:{propertyName} can not be found.", 
		"ErrorCode": 5001
	}
	
const OperationMessage MISSING_FIELD_VALUE = 
	{
		"DeveloperMessage" : "Search value canot be empty.", 
		"UserMessage" : "Search value canot be empty. No value provided for the field.", 
		"ErrorCode": 5002
	}
	
const OperationMessage MISSING_FIELD_VALUE_1 = 
	{
		"DeveloperMessage" : "FieldName:{propertyName} No value provided for the field.", 
		"UserMessage" : "FieldName:{propertyName} No value provided for the field.", 
		"ErrorCode": 5003
	}
	
const OperationMessage UNKNOWN_MISSING_VALUE_OPTION = 
	{
		"DeveloperMessage" : "MissingValueOption:{propertyName} is not supported.", 
		"UserMessage" : "MissingValueOption:{propertyName} is not supported.", 
		"ErrorCode": 5004
	}
	
const OperationMessage QUERYSTRING_PARSING_ERROR = 
	{
		"DeveloperMessage" : "Unable to parse the passed query string. {value}", 
		"UserMessage" : "Unable to parse the passed query string.", 
		"ErrorCode": 5005
	}
	
const OperationMessage DATA_CANNOT_BE_PARSED = 
	{
		"DeveloperMessage" : "Field:{propertyName} The passed data cannot be parsed. Check if the passed data is in the correct format required by the query operator", 
		"UserMessage" : "Field:{propertyName} The passed data cannot be parsed. Check if the passed data is in the correct format required by the query operator", 
		"ErrorCode": 5006
	}
	
const OperationMessage QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED = 
	{
		"DeveloperMessage" : "Field:{propertyName} Query operator does not support the passed field type.", 
		"UserMessage" : "Field:{propertyName} Query operator does not support the passed field type.", 
		"ErrorCode": 5007
	}	
	
const OperationMessage STORED_FIELDS_CANNOT_BE_SEARCHED = 
	{
		"DeveloperMessage" : "Field:{propertyName} is of type stored and cannot be searched.", 
		"UserMessage" : "Field:{propertyName} is of type stored and cannot be searched.", 
		"ErrorCode": 5008
	}		
	
const OperationMessage SEARCH_PROFILE_NOT_FOUND = 
	{
		"DeveloperMessage" : "The requested search profile does not exist.", 
		"UserMessage" : "The requested search profile does not exist.", 
		"ErrorCode": 5009
	}

const OperationMessage NEGATIVE_QUERY_NOT_SUPPORTED = 
	{
		"DeveloperMessage" : "Purely negative queries (top not query) are not supported.", 
		"UserMessage" : "Purely negative queries (top not query) are not supported.", 
		"ErrorCode": 5010
	}
	
// ----------------------------------------------------------------------------
//	Http Server
// ----------------------------------------------------------------------------	
const OperationMessage HTTP_UNABLE_TO_PARSE = 
	{
		"DeveloperMessage" : "The server is unable to parse the request body. {value}", 
		"UserMessage" : "The server is unable to parse the request body.", 
		"ErrorCode": 6000
	}
	
const OperationMessage HTTP_UNSUPPORTED_CONTENT_TYPE = 
	{
		"DeveloperMessage" : "Unsupported content-type.", 
		"UserMessage" : "Unsupported content-type.", 
		"ErrorCode": 6001
	}
	
const OperationMessage HTTP_NO_BODY_DEFINED = 
	{
		"DeveloperMessage" : "No body defined.", 
		"UserMessage" : "Expecting body. But no body defined.", 
		"ErrorCode": 6002
	}
	
const OperationMessage HTTP_NOT_SUPPORTED = 
	{
		"DeveloperMessage" : "The request Uri endpoint is not supported.", 
		"UserMessage" : "The request Uri endpoint is not supported.", 
		"ErrorCode": 6003
	}
	
const OperationMessage HTTP_URI_ID_NOT_SUPPLIED = 
	{
		"DeveloperMessage" : "The request URI expects an id to be supplied as a part of URI.", 
		"UserMessage" : "The request URI expects an id to be supplied as a part of URI.", 
		"ErrorCode": 6004
	}	

// ----------------------------------------------------------------------------
//	Persistence store related
// ----------------------------------------------------------------------------	
const OperationMessage KEY_NOT_FOUND = 
	{
		"DeveloperMessage" : "The requested key is not present in the persistences store.", 
		"UserMessage" : "The requested key is not present in the persistences store.", 
		"ErrorCode": 7001
	}	

// ----------------------------------------------------------------------------
//	Connectors related
// ----------------------------------------------------------------------------	
const OperationMessage IMPORTER_NOT_FOUND = 
	{
		"DeveloperMessage" : "The requested importer does not exist.", 
		"UserMessage" : "The requested importer does not exist.", 
		"ErrorCode": 8001
	}

const OperationMessage IMPORTER_DOES_NOT_SUPPORT_BULK_INDEXING = 
	{
		"DeveloperMessage" : "The requested importer does not support bulk indexing.", 
		"UserMessage" : "The requested importer does not support bulk indexing", 
		"ErrorCode": 8002
	}

const OperationMessage IMPORTER_DOES_NOT_SUPPORT_INCREMENTAL_INDEXING = 
	{
		"DeveloperMessage" : "The requested importer does not support incremental indexing.", 
		"UserMessage" : "The requested importer does not support incremental indexing", 
		"ErrorCode": 8003
	}