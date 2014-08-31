namespace FlexSearch.Api

open System.Collections.Generic
open System

type OperationMessage = 
    { DeveloperMessage : string
      UserMessage : string
      ErrorCode : int }
    static member GetErrorCode(input : string) = 
        let first = input.IndexOf(''')
        if first = -1 then -1
        else 
            let second = input.IndexOf(''', first + 1)
            if second = -1 then -1
            else 
                match Int32.TryParse(input.Substring(first + 1, second - (first + 1)).Trim()) with
                | true, res -> res
                | _ -> -1

[<AutoOpen>]
module OperationMessageExtensions = 
    type String with
        member this.GetErrorCode() = 
            let first = this.IndexOf(''')
            if first = -1 then -1
            else 
                let second = this.IndexOf(''', first + 1)
                if second = -1 then -1
                else 
                    match Int32.TryParse(this.Substring(first + 1, second - (first + 1)).Trim()) with
                    | true, res -> res
                    | _ -> -1
    
    /// <summary>
    /// Append the given key value pair to the developer message
    /// The developer message has a format of key1='value1',key2='value2'
    /// This is specifically done to enable easy error message parsing in the user interface
    /// </summary>
    let Append (key, value) (message : OperationMessage) = 
        { message with UserMessage = sprintf "%s; %s = '%s'" message.UserMessage key value }
    
    let GenerateOperationMessage(input : string) = 
        // Check if the error is using right format
        let userMessage = 
            if input.Contains("Message") <> true then sprintf "Message = '%s'" input
            else input
        { DeveloperMessage = ""
          UserMessage = userMessage
          ErrorCode = OperationMessage.GetErrorCode(input) }
    
    let AppendKv (key, value) (message : string) = sprintf "%s; %s = %s" message key value

[<AutoOpen>]
module Errors = 
    let INDEX_NOT_FOUND = "ErrorCode = '1000'; Message='Index not found.'"
    let INDEX_ALREADY_EXISTS = "ErrorCode = '1002'; Message='Index already exists.'"
    let INDEX_SHOULD_BE_OFFLINE = 
        "ErrorCode = '1003'; Message='Index should be made off-line before attempting the operation.'"
    let INDEX_IS_OFFLINE = 
        "ErrorCode = '1004'; Message = 'Index is off-line or closing. Please bring the index on-line to use it.'"
    let INDEX_IS_OPENING = 
        "ErrorCode = '1005'; Message = 'Index is in opening state. Please wait some time before making another request.'"
    let INDEX_REGISTERATION_MISSING = 
        "ErrorCode = '1006'; Message = 'Registration information associated with the index is missing.'"
    let INDEXING_DOCUMENT_ID_MISSING = 
        "ErrorCode = '1007'; Message = 'Document Id is required in order to index an document. Please specify _id and submit the document for indexing.'"
    let ERROR_OPENING_INDEXWRITER = "ErrorCode = '1008'; Message = 'Unable to open index writer.'"
    let ERROR_ADDING_INDEX_STATUS = "ErrorCode = '1009'; Message = 'Unable to set the index status.'"
    let INDEX_IS_ALREADY_ONLINE = "ErrorCode = '1009'; Message = 'Index is already on-line or opening at the moment.'"
    let INDEX_IS_ALREADY_OFFLINE = "ErrorCode = '1011'; Message = 'Index is already off-line or closing at the moment.'"
    let INDEX_IS_IN_INVALID_STATE = "ErrorCode = '1012'; Message = 'Index is in invalid state.'"
    // ----------------------------------------------------------------------------
    //	Validation Exceptions
    // ----------------------------------------------------------------------------
    let PROPERTY_CANNOT_BE_EMPTY = "ErrorCode = '2001'; Message = 'Field cannot be empty.'"
    let REGEX_NOT_MATCHED = "ErrorCode = '2002'; Message = 'Field does not match the regex pattern.'"
    let VALUE_NOT_IN = "ErrorCode = '2003';"
    let VALUE_ONLY_IN = "ErrorCode = '2004';"
    let GREATER_THAN_EQUAL_TO = "ErrorCode = '2005';"
    let GREATER_THAN = "ErrorCode = '2006';"
    let LESS_THAN_EQUAL_TO = "ErrorCode = '2005';"
    let LESS_THAN = "ErrorCode = '2006';"
    let FILTER_CANNOT_BE_INITIALIZED = "ErrorCode = '2007'; Message = 'Filter cannot be initialized.'"
    let FILTER_NOT_FOUND = "ErrorCode = '2008'; Message = 'Filter not found.'"
    let TOKENIZER_CANNOT_BE_INITIALIZED = "ErrorCode = '2009'; Message = 'Tokenizer cannot be initialized.'"
    let TOKENIZER_NOT_FOUND = "ErrorCode = '2010'; Message = 'Tokenizer not found.'"
    let ATLEAST_ONE_FILTER_REQUIRED = 
        "ErrorCode = '2011'; Message = 'At least one filter should be specified for a custom analyzer.'"
    let UNKNOWN_FIELD_TYPE = "ErrorCode = '2012'; Message = 'Unsupported field type specified in the Field Properties.'"
    let SCRIPT_NOT_FOUND = "ErrorCode = '2013'; Message = 'Script not found.'"
    let ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE = 
        "ErrorCode = '2014'; Message = 'FieldType does not support custom analyzer.'"
    let UNKNOWN_SCRIPT_TYPE = "ErrorCode = '2015'; Message = 'ScriptType not supported.'"
    let ANALYZER_NOT_FOUND = "ErrorCode = '2016'; Message = 'Analyzer not found.'"
    // ----------------------------------------------------------------------------
    //	Compilation Exceptions
    // ----------------------------------------------------------------------------	
    let SCRIPT_CANT_BE_COMPILED = "ErrorCode = '3000'; Message = 'Script cannot be compiled.'"
    // ----------------------------------------------------------------------------
    //	MEF Related
    // ----------------------------------------------------------------------------	
    let MODULE_NOT_FOUND = 
        "ErrorCode = '4000'; Message = 'Module can not be found. Please make sure all compiled dependencies are accessible by the server.'"
    // ----------------------------------------------------------------------------
    //	Search Related
    // ----------------------------------------------------------------------------	
    let INVALID_QUERY_TYPE = 
        "ErrorCode = '5000'; Message = 'QueryType can not be found. Please make sure all compiled dependencies are accessible by the server.'"
    let INVALID_FIELD_NAME = "ErrorCode = '5001'; Message = 'FieldName can not be found.'"
    let MISSING_FIELD_VALUE = 
        "ErrorCode = '5002'; Message = 'Search value cannot be empty. No value provided for the field.'"
    let MISSING_FIELD_VALUE_1 = "ErrorCode = '5003'; Message = 'FieldName No value provided for the field.'"
    let UNKNOWN_MISSING_VALUE_OPTION = "ErrorCode = '5004'; Message = 'MissingValueOption not supported.'"
    let QUERYSTRING_PARSING_ERROR = "ErrorCode = '5005'; Message = 'Unable to parse the passed query string.'"
    let DATA_CANNOT_BE_PARSED = 
        "ErrorCode = '5006'; Message = 'Passed data cannot be parsed. Check if the passed data is in correct format required by the query operator.'"
    let QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED = 
        "ErrorCode = '5007'; Message = 'Field Query operator does not support the passed field type.'"
    let STORED_FIELDS_CANNOT_BE_SEARCHED = "ErrorCode = '5008'; Message = 'Stored only field cannot be searched.'"
    let SEARCH_PROFILE_NOT_FOUND = "ErrorCode = '5009'; Message = 'Search profile not found.'"
    let NEGATIVE_QUERY_NOT_SUPPORTED = 
        "ErrorCode = '5010'; Message = 'Purely negative queries (top not query) are not supported.'"
    // ----------------------------------------------------------------------------
    //	Http Server
    // ----------------------------------------------------------------------------	
    let HTTP_UNABLE_TO_PARSE = "ErrorCode = '6000'; Message = 'Server is unable to parse the request body.'"
    let HTTP_UNSUPPORTED_CONTENT_TYPE = "ErrorCode = '6001'; Message = 'Unsupported content-type.'"
    let HTTP_NO_BODY_DEFINED = "ErrorCode = '6002'; Message = 'Expecting body. But no body defined.'"
    let HTTP_NOT_SUPPORTED = "ErrorCode = '6003'; Message = 'Request URI endpoint is not supported.'"
    let HTTP_URI_ID_NOT_SUPPLIED = 
        "ErrorCode = '6004'; Message = 'Request URI expects an id to be supplied as a part of URI.'"
    // ----------------------------------------------------------------------------
    //	Persistence store related
    // ----------------------------------------------------------------------------	
    let KEY_NOT_FOUND = "ErrorCode = '7001'; Message = 'Key not found in persistence store.'"
    // ----------------------------------------------------------------------------
    //	Connectors related
    // ----------------------------------------------------------------------------	
    let IMPORTER_NOT_FOUND = "ErrorCode = '8001'; Message = 'Importer not found.'"
    let IMPORTER_DOES_NOT_SUPPORT_BULK_INDEXING = 
        "ErrorCode = '8002'; Message = 'Importer does not support bulk indexing.'"
    let IMPORTER_DOES_NOT_SUPPORT_INCREMENTAL_INDEXING = 
        "ErrorCode = '8003'; Message = 'Importer does not support incremental indexing.'"
    let JOBID_IS_NOT_FOUND = "ErrorCode = '8004'; Message = 'Job id not found.'"
