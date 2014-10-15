namespace FlexSearch.Api

open System
open System.Collections.Generic

type OperationMessage = 
    { DeveloperMessage : string
      UserMessage : string
      ErrorCode : string }
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
            assert (this.Contains(":"))
            this.Substring(0, this.IndexOf(":"))
    
    /// <summary>
    /// Append the given key value pair to the developer message
    /// The developer message has a format of key1='value1',key2='value2'
    /// This is specifically done to enable easy error message parsing in the user interface
    /// </summary>
    let Append (key, value) (message : OperationMessage) = 
        { message with DeveloperMessage = sprintf "%s; %s = '%s'" message.DeveloperMessage key value }
    
    let GenerateOperationMessage(input : string) = 
        assert (input.Contains(":"))
        { DeveloperMessage = ""
          UserMessage = input.Substring(input.IndexOf(":") + 1)
          ErrorCode = input.Substring(0, input.IndexOf(":")) }
    
    let AppendKv (key, value) (message : string) = sprintf "%s; %s = %s" message key value

[<AutoOpen>]
module Errors = 
    let INDEX_NOT_FOUND = "INDEX_NOT_FOUND:Index not found."
    let INDEX_ALREADY_EXISTS = "INDEX_ALREADY_EXISTS:Index already exists."
    let INDEX_SHOULD_BE_OFFLINE = 
        "INDEX_SHOULD_BE_OFFLINE:Index should be made off-line before attempting the operation."
    let INDEX_IS_OFFLINE = "INDEX_IS_OFFLINE:Index is off-line or closing. Please bring the index on-line to use it."
    let INDEX_IS_OPENING = 
        "INDEX_IS_OPENING:Index is in opening state. Please wait some time before making another request."
    let INDEX_REGISTERATION_MISSING = 
        "INDEX_REGISTERATION_MISSING:Registration information associated with the index is missing."
    let ERROR_OPENING_INDEXWRITER = "ERROR_OPENING_INDEXWRITER:Unable to open index writer."
    let UNSUPPORTED_SIMILARITY = "UNSUPPORTED_SIMILARITY:The specified similarity is not supported."
    let UNSUPPORTED_INDEX_VERSION = "UNSUPPORTED_INDEX_VERSION:The specified index version is not supported."
    let ERROR_ADDING_INDEX_STATUS = "ERROR_ADDING_INDEX_STATUS:Unable to set the index status."
    let INDEX_IS_ALREADY_ONLINE = "INDEX_IS_ALREADY_ONLIN:Index is already on-line or opening at the moment."
    let INDEX_IS_ALREADY_OFFLINE = "INDEX_IS_ALREADY_OFFLINE:Index is already off-line or closing at the moment."
    let INDEX_IS_IN_INVALID_STATE = "INDEX_IS_IN_INVALID_STATE:Index is in invalid state."
    let INDEXING_DOCUMENT_ID_MISSING = 
        "INDEXING_DOCUMENT_ID_MISSING:Document Id is required in order to index an document. Please specify _id and submit the document for indexing."
    let INDEXING_DOCUMENT_ID_ALREADY_EXISTS = 
        "INDEXING_DOCUMENT_ID_ALREADY_EXISTS:Index already contains a document with the same id."
    let INDEXING_DOCUMENT_ID_NOT_FOUND = 
        "INDEXING_DOCUMENT_ID_NOT_FOUND:Index does not contain a Document with the requested id."
    let INDEXING_VERSION_CONFLICT = 
        "INDEXING_VERSION_CONFLICT:Document version should exactly match for update operation to be successful."
    let INDEXING_VERSION_CONFLICT_CREATE = 
        "INDEXING_VERSION_CONFLICT_CREATE:Document version should not be greater than 0 for a create operation."
    // ----------------------------------------------------------------------------
    // File operations
    // ----------------------------------------------------------------------------
    let FILE_NOT_FOUND = "FILE_NOT_FOUND:The requested file does not exist at the provided location."
    let FILE_READ_ERROR = "FILE_READ_ERROR:An error occurred while reading the file."
    let FILE_WRITE_ERROR = "FILE_WRITE_ERROR:An error occurred while writing the file."

    // ----------------------------------------------------------------------------
    // Validation Exceptions
    // ----------------------------------------------------------------------------
    let PROPERTY_CANNOT_BE_EMPTY = "PROPERTY_CANNOT_BE_EMPTY:Field cannot be empty."
    let REGEX_NOT_MATCHED = "REGEX_NOT_MATCHED:Field does not match the regex pattern."
    let VALUE_NOT_IN = "VALUE_NOT_IN:"
    let VALUE_ONLY_IN = "VALUE_ONLY_IN:"
    let GREATER_THAN_EQUAL_TO = "GREATER_THAN_EQUAL_TO:"
    let GREATER_THAN = "GREATER_THAN:"
    let LESS_THAN_EQUAL_TO = "LESS_THAN_EQUAL_TO:"
    let LESS_THAN = "LESS_THAN:"
    let FILTER_CANNOT_BE_INITIALIZED = "FILTER_CANNOT_BE_INITIALIZED:Filter cannot be initialized."
    let FILTER_NOT_FOUND = "FILTER_NOT_FOUND:Filter not found."
    let TOKENIZER_CANNOT_BE_INITIALIZED = "TOKENIZER_CANNOT_BE_INITIALIZED:Tokenizer cannot be initialized."
    let TOKENIZER_NOT_FOUND = "TOKENIZER_NOT_FOUND:Tokenizer not found."
    let ATLEAST_ONE_FILTER_REQUIRED = 
        "ATLEAST_ONE_FILTER_REQUIRED:At least one filter should be specified for a custom analyzer."
    let UNKNOWN_FIELD_TYPE = "UNKNOWN_FIELD_TYPE:Unsupported field type specified in the Field Properties."
    let SCRIPT_NOT_FOUND = "SCRIPT_NOT_FOUND:Script not found."
    let ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE = 
        "ANALYZERS_NOT_SUPPORTED_FOR_FIELD_TYPE:FieldType does not support custom analyzer."
    let UNKNOWN_SCRIPT_TYPE = "UNKNOWN_SCRIPT_TYPE:ScriptType not supported."
    let ANALYZER_NOT_FOUND = "ANALYZER_NOT_FOUND:Analyzer not found."
    let DUPLICATE_FIELD_VALUE = "DUPLICATE_FIELD_VALUE:List contains a duplicate value."
    // ----------------------------------------------------------------------------
    // Compilation Exceptions
    // ---------------------------------------------------------------------------- 
    let SCRIPT_CANT_BE_COMPILED = "SCRIPT_CANT_BE_COMPILED:Script cannot be compiled."
    // ----------------------------------------------------------------------------
    // MEF Related
    // ---------------------------------------------------------------------------- 
    let MODULE_NOT_FOUND = 
        "MODULE_NOT_FOUND:Module can not be found. Please make sure all compiled dependencies are accessible by the server."
    // ----------------------------------------------------------------------------
    // Search Related
    // ---------------------------------------------------------------------------- 
    let INVALID_QUERY_TYPE = 
        "INVALID_QUERY_TYPE:QueryType can not be found. Please make sure all compiled dependencies are accessible by the server."
    let INVALID_FIELD_NAME = "INVALID_FIELD_NAME:FieldName can not be found."
    let MISSING_FIELD_VALUE = "MISSING_FIELD_VALUE:Search value cannot be empty. No value provided for the field."
    let MISSING_FIELD_VALUE_1 = "MISSING_FIELD_VALUE_1:FieldName No value provided for the field."
    let UNKNOWN_MISSING_VALUE_OPTION = "UNKNOWN_MISSING_VALUE_OPTION:MissingValueOption not supported."
    let QUERYSTRING_PARSING_ERROR = "QUERYSTRING_PARSING_ERROR:Unable to parse the passed query string."
    let DATA_CANNOT_BE_PARSED = 
        "DATA_CANNOT_BE_PARSED:Passed data cannot be parsed. Check if the passed data is in correct format required by the query operator."
    let QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED = 
        "QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED:Field Query operator does not support the passed field type."
    let STORED_FIELDS_CANNOT_BE_SEARCHED = "STORED_FIELDS_CANNOT_BE_SEARCHED:Stored only field cannot be searched."
    let SEARCH_PROFILE_NOT_FOUND = "SEARCH_PROFILE_NOT_FOUND:Search profile not found."
    let NEGATIVE_QUERY_NOT_SUPPORTED = 
        "NEGATIVE_QUERY_NOT_SUPPORTED:Purely negative queries (top not query) are not supported."
    // ----------------------------------------------------------------------------
    // Http Server
    // ---------------------------------------------------------------------------- 
    let HTTP_UNABLE_TO_PARSE = "HTTP_UNABLE_TO_PARSE:Server is unable to parse the request body."
    let HTTP_UNSUPPORTED_CONTENT_TYPE = "HTTP_UNSUPPORTED_CONTENT_TYPE:Unsupported content-type."
    let HTTP_NO_BODY_DEFINED = "HTTP_NO_BODY_DEFINED:Expecting body. But no body defined."
    let HTTP_NOT_SUPPORTED = "HTTP_NOT_SUPPORTED:Request URI endpoint is not supported."
    let HTTP_URI_ID_NOT_SUPPLIED = "HTTP_URI_ID_NOT_SUPPLIED:Request URI expects an id to be supplied as a part of URI."
    // ----------------------------------------------------------------------------
    // Persistence store related
    // ---------------------------------------------------------------------------- 
    let KEY_NOT_FOUND = "KEY_NOT_FOUND:Key not found in persistence store."
    // ----------------------------------------------------------------------------
    // Connectors related
    // ---------------------------------------------------------------------------- 
    let IMPORTER_NOT_FOUND = "IMPORTER_NOT_FOUND:Importer not found."
    let IMPORTER_DOES_NOT_SUPPORT_BULK_INDEXING = 
        "IMPORTER_DOES_NOT_SUPPORT_BULK_INDEXING:Importer does not support bulk indexing."
    let IMPORTER_DOES_NOT_SUPPORT_INCREMENTAL_INDEXING = 
        "IMPORTER_DOES_NOT_SUPPORT_INCREMENTAL_INDEXING:Importer does not support incremental indexing."
    let JOBID_IS_NOT_FOUND = "JOBID_IS_NOT_FOUND:Job id not found."
    // ----------------------------------------------------------------------------
    // Configuration related
    // ---------------------------------------------------------------------------- 
    let UNABLE_TO_PARSE_CONFIG = "UNABLE_TO_PARSE_CONFIG:Unable to parse the given configuration file."