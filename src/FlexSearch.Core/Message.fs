﻿// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open Microsoft.FSharp.Reflection
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Reflection

/// Extrenal representation of messages which will be send to
/// the end user. 
[<CLIMutableAttribute>]
type OperationMessage = 
    { Properties : array<string * string>
      Message : string
      ErrorCode : string }

/// Interface to expose internal message format
type IMessage = 
    abstract OperationMessage : unit -> OperationMessage

/// General purpose exception to be used. It is better to use
/// DU based error than this exception.
exception ValidationException of IMessage

//    abstract UserMessage : unit -> string
//    abstract LogMessage : unit -> string
module MessageHelpers = 
    let private cases = new ConcurrentDictionary<System.Type, string * PropertyInfo []>()
    
    /// Get the label associated with a DU pattern and also returns the field info. 
    /// This uses a cache to avoid reflection.
    let getCaseInfo (x : 'a) = 
        match cases.TryGetValue(typeof<'a>) with
        | true, case -> case
        | _ -> 
            let union = (FSharpValue.GetUnionFields(x, typeof<'a>) |> fst)
            assert (notNull union.Name)
            let case = (union.Name, union.GetFields())
            cases.TryAdd(typeof<'a>, case) |> ignore
            case
    
    /// Converts a case to operation message
    let caseToMsg (x : 'a) msg = 
        let (caseName, props) = getCaseInfo (x)
        { // TODO: Find a more efficient way to generate properties
          Message = msg
          Properties = props |> Array.map (fun p -> 
            let value = 
                try
                    let v = p.GetValue(x)
                    if isNull v then
                        String.Empty
                    else
                        v.ToString()
                with _ -> String.Empty
            (p.Name, value))
          ErrorCode = caseName }
    
    /// Converts an operation message to string
    let msgToString (om : OperationMessage) = sprintf "%A" om
    
    /// Converts any object to a pretty printed string
    let toFormattedString (x : 'a) = sprintf "%A" x
            
open MessageHelpers

type ValidationError = 
    | GreaterThan of fieldName : string * lowerLimit : string * value : string
    | LessThan of fieldName : string * upperLimit : string * value : string
    | GreaterThanEqual of fieldName : string * lowerLimit : string * value : string
    | LessThanEqual of fieldName : string * lowerLimit : string * value : string
    | NotBlank of fieldName : string
    | RegexMatch of fieldName : string * regexExpr : string
    | KeyNotFound of key : string
    override this.ToString() = sprintf "%A" this
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | GreaterThan(fn, ll, v) -> sprintf "Field '%s' must be greater than %s, but found %s" fn ll v
            | LessThan(fn, ul, v) -> sprintf "Field '%s' must be less than %s, but found %s" fn ul v
            | GreaterThanEqual(fn, ll, v) -> 
                sprintf "Field '%s' must be greater than or equal to %s, but found %s" fn ll v
            | LessThanEqual(fn, ul, v) -> sprintf "Field '%s' must be less than or equal to %s, but found %s" fn ul v
            | NotBlank(fn) -> sprintf "Field '%s' must not be blank" fn
            | RegexMatch(fn, re) -> sprintf "Field '%s' must match Regex expression: %s" fn re
            | KeyNotFound(key) -> sprintf "Key not found: %s" key
            |> caseToMsg this

type AnalysisMessage = 
    | TokenizerNotFound of analyzerName : string * tokenizerName : string
    | UnableToInitializeTokenizer of analyzerName : string * tokenizerName : string * message : string * ``exception`` : string
    | FilterNotFound of analyzerName : string * filterName : string
    | UnableToInitializeFilter of analyzerName : string * filterName : string * message : string * ``exception`` : string
    | AnalyzerBuilder of analyzerName : string * message : string * ``exception`` : string
    | AnalyzerNotFound of analyzerName : string
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | TokenizerNotFound(an, tn) -> sprintf "Tokenizer with the name %s does not exist. Analyzer Name: %s" tn an
            | UnableToInitializeTokenizer(an, tn, m, exp) -> 
                sprintf "Tokenizer with the name %s cannot be initialized. Analyzer Name: %s. Error: %s. Exception: %s" 
                    tn an m exp
            | FilterNotFound(an, fn) -> sprintf "Filter with the name %s does not exist. Analyzer Name: %s" fn an
            | UnableToInitializeFilter(an, fn, m, exp) -> 
                sprintf "Filter with the name %s cannot be initialized. Analyzer Name: %s. Error: %s. Exception: %s" fn 
                    an m exp
            | AnalyzerBuilder(an, m, e) -> 
                sprintf "The analyzer '%s' threw an exception while building: %s; \n%s" an m e
            | AnalyzerNotFound(a) -> sprintf "The analyzer '%s' was not found" a
            |> caseToMsg this

type BuilderError = 
    | InvalidPropertyName of fieldName : string * value : string
    | AnalyzerIsMandatory of fieldName : string
    | DuplicateFieldValue of groupName : string * fieldName : string
    | ScriptNotFound of scriptName : string * fieldName : string
    | ResourceNotFound of resourceName : string * resourceType : string
    | UnSupportedSimilarity of similarityName : string
    | UnSupportedIndexVersion of indexVersion : string
    | UnsupportedDirectoryType of directoryType : string
    | UnSupportedFieldType of fieldName : string * fieldType : string
    | ScriptCannotBeCompiled of error : string
    | AnalyzerNotSupportedForFieldType of fieldName : string * analyzerName : string
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | InvalidPropertyName(fn, v) -> sprintf "Property name is invalid. Expected '%s' but found '%s'" fn v
            | AnalyzerIsMandatory(fn) -> sprintf "Analyzer is mandatory for field '%s'" fn
            | DuplicateFieldValue(gn, fn) -> sprintf "A duplicate entry (%s) has been found in the group '%s'" fn gn
            | ScriptNotFound(sn, fn) -> sprintf "The script '%s' was not found against the field '%s'" sn fn
            | ResourceNotFound(rn, rt) -> sprintf "The resource '%s' of type %s was not found" rn rt
            | UnSupportedSimilarity(s) -> sprintf "Unsupported similarity: %s" s
            | UnSupportedIndexVersion(i) -> sprintf "Unsupported index version: %s" i
            | UnsupportedDirectoryType(d) -> sprintf "Unsupported directory type: %s" d
            | UnSupportedFieldType(fn, ft) -> sprintf "Unsupported field type '%s' for field '%s'" fn ft
            | ScriptCannotBeCompiled(e) -> sprintf "Script cannot be compiled: \n%s" e
            | AnalyzerNotSupportedForFieldType(f, a) -> sprintf "Analyzer '%s' not supported for field '%s'" f a
            |> caseToMsg this

type SearchMessage = 
    | QueryNotFound of queryName : string
    | InvalidFieldName of fieldName : string
    | StoredFieldCannotBeSearched of fieldName : string
    | MissingFieldValue of fieldName : string
    | UnknownMissingVauleOption of fieldName : string
    | DataCannotBeParsed of fieldName : string * expectedDataType : string
    | ExpectingNumericData of fieldName : string
    | QueryOperatorFieldTypeNotSupported of fieldName : string
    | QueryStringParsingError of error : string
    | UnknownSearchProfile of indexName : string * profileName : string
    | PurelyNegativeQueryNotSupported
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | QueryNotFound(q) -> sprintf "Query not found: %s" q
            | InvalidFieldName(f) -> sprintf "Invalid field name: %s" f
            | StoredFieldCannotBeSearched(f) -> sprintf "Stored field cannot be searched: %s" f
            | MissingFieldValue(f) -> sprintf "Missing field value: %s" f
            | UnknownMissingVauleOption(f) -> sprintf "Unknown missing field value option: %s" f
            | DataCannotBeParsed(f, e) -> sprintf "Data cannot be parsed for field '%s'. Expected data type %s." f e
            | ExpectingNumericData(f) -> sprintf "Expecting numeric data: %s" f
            | QueryOperatorFieldTypeNotSupported(f) -> 
                sprintf "Query operator field type not supported for field '%s'" f
            | QueryStringParsingError(e) -> sprintf "Query string parsing error: \n%s" e
            | UnknownSearchProfile(i, p) -> sprintf "Unknown search profile '%s' for index '%s'" p i
            | PurelyNegativeQueryNotSupported -> "Purely negative queries (not top query) not supported"
            |> caseToMsg this

type IndexingMessage = 
    | IndexAlreadyExists of indexName : string
    | IndexShouldBeOnline of indexName : string
    | IndexIsAlreadyOnline of indexName : string
    | IndexIsAlreadyOffline of indexName : string
    | IndexInOpenState of indexName : string
    | IndexInInvalidState of indexName : string
    | ErrorOpeningIndexWriter of indexPath : string * exp : string * data : ResizeArray<KeyValuePair<string, string>>
    | IndexNotFound of indexName : string
    | DocumentIdAlreadyExists of indexName : string * id : string
    | DocumentIdNotFound of indexName : string * id : string
    | IndexingVersionConflict of indexName : string * id : string * existingVersion : string
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | IndexAlreadyExists(i) -> sprintf "Index '%s' already exists" i
            | IndexShouldBeOnline(i) -> sprintf "Index '%s' should be online" i
            | IndexIsAlreadyOnline(i) -> sprintf "Index '%s' is already online" i
            | IndexIsAlreadyOffline(i) -> sprintf "Index '%s' is already offline" i
            | IndexInOpenState(i) -> sprintf "Index '%s' is in an open state" i
            | IndexInInvalidState(i) -> sprintf "Index '%s' is in an invalid state" i
            | ErrorOpeningIndexWriter(ip, _, _) -> sprintf "Error opening index writer at path '%s'." ip
            | IndexNotFound(i) -> sprintf "Index '%s' was not found" i
            | DocumentIdAlreadyExists(idx, id) -> sprintf "Document ID '%s' already exists for index '%s'" id idx
            | DocumentIdNotFound(idx, id) -> sprintf "Document ID '%s' not found on index '%s'" id idx
            | IndexingVersionConflict(idx, id, v) -> 
                sprintf "Indexing version conflict for index '%s': given ID is %s, but the exising version is %s" idx id 
                    v
            |> caseToMsg this

type ModuleInitMessage = 
    | ModuleNotFound of moduleName : string * moduleType : string
    | ModuleInitializationError of moduleName : string * moduleType : string * error : string
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | ModuleNotFound(mn, mt) -> sprintf "Module '%s' of type '%s' was not found" mn mt
            | ModuleInitializationError(mn, mt, e) -> 
                sprintf "An error occurred while initializing the module '%s' of type '%s': \n%s" mn mt e
            |> caseToMsg this

type GeneralMessage = 
    | UnableToUpdateMemory
    | UnableToParseConfig of path : string * error : string
    | JobNotFound of jobId : string
    // Generic error to be used by plugins
    | GenericError of userMessage : string * data : ResizeArray<KeyValuePair<string, string>>
    | NotImplemented
    | HeaderRowIsEmpty
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | UnableToUpdateMemory -> "Unable to update memory"
            | UnableToParseConfig(p, e) -> sprintf "Unable to parse configuration file from address '%s': \n%s" p e
            | JobNotFound(j) -> sprintf "Job ID '%s' not found" j
            | NotImplemented -> "Feature not implemented"
            | HeaderRowIsEmpty -> ""
            | GenericError(u, d) -> sprintf "Generic error in plugin: %s \nData: %A" u d
            |> caseToMsg this

type HttpServerMessage = 
    | HttpUnableToParse of error : string
    | HttpUnsupportedContentType
    | HttpNoBodyDefined
    | HttpNotSupported
    | HttpUriIdNotSupplied
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | HttpUnableToParse(e) -> sprintf "Unable to deserialize the HTTP body: \n%s" e
            | HttpUnsupportedContentType -> "Unsupported content type for the HTTP message"
            | HttpNoBodyDefined -> "No body defined for the HTTP message"
            | HttpNotSupported -> "The requested URI endpoint is not supported."
            | HttpUriIdNotSupplied -> "The URI enpoint expects an id which is not supplied"
            |> caseToMsg this

type FileMessage = 
    | FileNotFound of filePath : string
    | FileReadError of filePath : string * error : string
    | FileWriteError of filePath : string * error : string
    | PathDoesNotExist of path : string
    | StoreUpdateError
    interface IMessage with
        member this.OperationMessage() = 
            match this with
            | FileNotFound(p) -> sprintf "File not found at address: %s" p
            | FileReadError(f, e) -> sprintf "There was an error reading the file at path '%s': \n%s" f e
            | FileWriteError(f, e) -> sprintf "There was an error writing to the file at path '%s': \n%s" f e
            | PathDoesNotExist(p) -> sprintf "Path does not exist: %s" p
            | StoreUpdateError -> ""
            |> caseToMsg this