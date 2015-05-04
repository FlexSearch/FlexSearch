// ----------------------------------------------------------------------------
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

open EventSourceProxy.NuGet
open FlexLucene.Analysis.Custom
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Linq
open System.Threading

[<AutoOpen>]
[<RequireQualifiedAccess>]
/// Contains all the flex constants which do not change per instance
module Constants = 
    [<Literal>]
    let generationLabel = "generation"
    
    [<Literal>]
    let modifyIndex = "modifyIndex"
    
    [<Literal>]
    let IdField = "_id"
    
    [<Literal>]
    let LastModifiedField = "_lastmodified"
    
    //[<Literal>]
    //let LastModifiedFieldDv = "_lastmodifieddv"
    [<Literal>]
    let ModifyIndex = "_modifyindex"
    
    [<Literal>]
    let VersionField = "_version"
    
    [<Literal>]
    let DocumentField = "_document"
    
    [<Literal>]
    let Score = "_score"
    
    [<Literal>]
    let DotNetFrameWork = "4.5.1"
    
    [<Literal>]
    let StandardAnalyzer = "standard"
    
    // Default value to be used for string data type
    [<Literal>]
    let StringDefaultValue = "null"
    
    /// Default value to be used for flex date data type
    let DateDefaultValue = Int64.Parse("00010101")
    
    /// Default value to be used for date time data type
    let DateTimeDefaultValue = Int64.Parse("00010101000000")
    
    // Flex root folder path
    let private rootFolder = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
    let createDir (directoryPath) = Directory.CreateDirectory(directoryPath).FullName
    
    /// Flex index folder
    let DataFolder = rootFolder +/ "Data" |> createDir
    
    /// Flex index folder
    let ConfFolder = rootFolder +/ "Conf" |> createDir
    
    /// Flex plug-in folder
    let PluginFolder = rootFolder +/ "Plugins" |> createDir
    
    /// Flex logs folder
    let LogsFolder = rootFolder +/ "Logs" |> createDir
    
    /// Flex web files folder
    let WebFolder = rootFolder +/ "Web" |> createDir
    
    /// Resources folder to be used for saving analyzer resource files
    let ResourcesFolder = ConfFolder +/ "Resources" |> createDir
    
    /// Extension to be used by settings file
    let SettingsFileExtension = ".yml"
    
    let CaseInsensitiveKeywordAnalyzer = 
        CustomAnalyzer.Builder().withTokenizer("keyword").addTokenFilter("lowercase").build() :> FlexLucene.Analysis.Analyzer

[<AutoOpen>]
module DateTimeHelpers = 
    open System.Globalization
    
    /// Internal format used to represent dates 
    let DateTimeFormat = "yyyyMMddHHmmssfff"
    
    /// Represents all the date time format supported by FlexSearch
    let SupportedDateFormat = [| "yyyyMMdd"; "yyyyMMddHHmm"; "yyyyMMddHHmmss" |]
    
    /// Coverts a date to FlexSearch date format
    let inline dateToFlexFormat (dt : DateTime) = int64 <| dt.ToString(DateTimeFormat)
    
    /// Returns current date time in Flex compatible format
    let inline GetCurrentTimeAsLong() = int64 <| dateToFlexFormat (DateTime.Now)
    
    /// Parses a given date according to supported date styles
    let inline parseDate (dt : string) = 
        DateTime.ParseExact(dt, SupportedDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None)
    
    /// Parses a given date according to supported date styles
    let inline tryParseDate (dt : string) = 
        DateTime.TryParseExact(dt, SupportedDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None)
    
    /// Parses a given date and returns it in FlexSearch format wrapped in an option type.    
    let inline parseDateFlexFormat (dt : string) = 
        match tryParseDate dt with
        | true, date -> 
            date
            |> dateToFlexFormat
            |> Some
        | _ -> None

type Error = 
    // Generic Validation Errors
    | GreaterThan of fieldName : string * lowerLimit : string * value : string
    | LessThan of fieldName : string * upperLimit : string * value : string
    | GreaterThanEqual of fieldName : string * lowerLimit : string * value : string
    | LessThanEqual of fieldName : string * lowerLimit : string * value : string
    | NotBlank of fieldName : string
    | RegexMatch of fieldName : string * regexExpr : string
    | KeyNotFound of key : string
    // Analysis related 
    | TokenizerNotFound of analyzerName : string * tokenizerName : string
    | UnableToInitializeTokenizer of analyzerName : string * tokenizerName : string * message : string * exp : string
    | FilterNotFound of analyzerName : string * filterName : string
    | UnableToInitializeFilter of analyzerName : string * filterName : string * message : string * exp : string
    | AnalyzerBuilder of analyzerName : string * message : string * exp : string
    | AnalyzerNotFound of analyzerName : string
    // Domain related
    | InvalidPropertyName of fieldName : string * value : string
    | AnalyzerIsMandatory of fieldName : string
    | DuplicateFieldValue of groupName : string * fieldName : string
    | ScriptNotFound of scriptName : string * fieldName : string
    // Builder related errors
    | ResourceNotFound of resourceName : string * resourceType : string
    | UnSupportedSimilarity of similarityName : string
    | UnSupportedIndexVersion of indexVersion : string
    | UnsupportedDirectoryType of directoryType : string
    | UnSupportedFieldType of fieldName : string * fieldType : string
    | ScriptCannotBeCompiled of error : string
    | AnalyzerNotSupportedForFieldType of fieldName : string * analyzerName : string
    // Search Realted
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
    // Indexing related errors
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
    // Modules related
    | ModuleNotFound of moduleName : string * moduleType : string
    | ModuleInitializationError of moduleName : string * moduleType : string * error : string
    // Concurrent Dictionary
    | UnableToUpdateMemory
    // Http server related
    | HttpUnableToParse of error : string
    | HttpUnsupportedContentType
    | HttpNoBodyDefined
    | HttpNotSupported
    | HttpUriIdNotSupplied
    // Configuration related
    | UnableToParseConfig of path : string * error : string
    // File related error
    | FileNotFound of filePath : string
    | FileReadError of filePath : string * error : string
    | FileWriteError of filePath : string * error : string
    | PathDoesNotExist of path : string
    | StoreUpdateError
    // Generic error to be used by plugins
    | GenericError of userMessage : string * data : ResizeArray<KeyValuePair<string, string>>
    // CSV file header does not exist or could not be generated
    | HeaderRowIsEmpty
    | JobNotFound of jobId : string
    | NotImplemented

exception ValidationException of Error

[<CLIMutableAttribute>]
type OperationMessage = 
    { DeveloperMessage : string
      UserMessage : string
      ErrorCode : string }

[<AutoOpen>]
module Errors = 
    let inline toMessage (error : Error) = 
        let msg err usrMsg devMsg = 
            { UserMessage = usrMsg
              DeveloperMessage = devMsg
              ErrorCode = err }
        match error with
        // Generic Validation Errors
        | GreaterThan(fn, ll, v) -> 
            msg "GREATER_THAN" "Greater than" <| sprintf "Field '%s' must be greater than %s, but found %s" fn ll v
        | LessThan(fn, ul, v) -> 
            msg "LESS_THAN" "Less than" <| sprintf "Field '%s' must be less than %s, but found %s" fn ul v
        | GreaterThanEqual(fn, ll, v) -> 
            msg "GREATER_OR_EQUAL" "Greater than or equal" 
            <| sprintf "Field '%s' must be greater than or equal to %s, but found %s" fn ll v
        | LessThanEqual(fn, ul, v) -> 
            msg "LESS_OR_EQUAL" "Less than or equal" 
            <| sprintf "Field '%s' must be less than or equal to %s, but found %s" fn ul v
        | NotBlank(fn) -> msg "NOT_BLANK" "Not blank" <| sprintf "Field '%s' must not be blank" fn
        | RegexMatch(fn, re) -> 
            msg "REGEX_MATCH" "Regex match" <| sprintf "Field '%s' must match Regex expression: %s" fn re
        | KeyNotFound(key) -> msg "KEY_NOT_FOUND" "Key not found" <| sprintf "Key not found: %s" key
        // Domain related
        | InvalidPropertyName(fn, v) -> 
            msg "INVALID_PROPERTY_NAME" "Invalid property name" 
            <| sprintf "Property name is invalid. Expected '%s' but found '%s'" fn v
        | AnalyzerIsMandatory(fn) -> 
            msg "ANALYZER_IS_MANDATORY" "Analyzer is mandatory" <| sprintf "Analyzer is mandatory for field '%s'" fn
        | DuplicateFieldValue(gn, fn) -> 
            msg "DUPLICATE_FIELD_VALUE" "Duplicate field value" 
            <| sprintf "A duplicate entry (%s) has been found in the group '%s'" fn gn
        | ScriptNotFound(sn, fn) -> 
            msg "SCRIPT_NOT_FOUND" "Script not found" 
            <| sprintf "The script '%s' was not found against the field '%s'" sn fn
        // Analysis related 
        | TokenizerNotFound(an, tn) -> 
            msg "TOKENIZER_NOT_FOUND" "Tokenizer not found" 
            <| sprintf "Tokenizer with the name %s does not exist. Analyzer Name: %s" tn an
        | UnableToInitializeTokenizer(an, tn, m, exp) -> 
            msg "UNABLE_TO_INITIALIZE_TOKENIZER" "Unable to initialize tokenizer" 
            <| sprintf "Tokenizer with the name %s cannot be initialized. Analyzer Name: %s. Error: %s. Exception: %s" 
                   tn an m exp
        | FilterNotFound(an, fn) -> 
            msg "FILTER_NOT_FOUND" "Filter not found" 
            <| sprintf "Filter with the name %s does not exist. Analyzer Name: %s" fn an
        | UnableToInitializeFilter(an, fn, m, exp) -> 
            msg "UNABLE_TO_INITIALIZE_FILTER" "Unable to initialize filter" 
            <| sprintf "Filter with the name %s cannot be initialized. Analyzer Name: %s. Error: %s. Exception: %s" fn 
                   an m exp
        // Builder related errors
        | AnalyzerBuilder(an, m, e) -> 
            msg "ANALYZER_BUILDER" "Analyzer builder error" 
            <| sprintf "The analyzer '%s' threw an exception while building: %s; \n%s" an m e
        | AnalyzerNotFound(a) -> 
            msg "ANALYZER_NOT_FOUND" "Analyzer not found" <| sprintf "The analyzer '%s' was not found" a
        | ResourceNotFound(rn, rt) -> 
            msg "RESOURCE_NOT_FOUND" "Resource not found" <| sprintf "The resource '%s' of type %s was not found" rn rt
        | UnSupportedSimilarity(s) -> 
            msg "UNSUPPORTED_SIMILARITY" "Unsupported similarity" <| sprintf "Unsupported similarity: %s" s
        | UnSupportedIndexVersion(i) -> 
            msg "UNSUPPORTED_INDEX_VERSION" "Unsupported index version" <| sprintf "Unsupported index version: %s" i
        | UnsupportedDirectoryType(d) -> 
            msg "UNSUPPORTED_DIRECTORY_TYPE" "Unsupported directory type" <| sprintf "Unsupported directory type: %s" d
        | UnSupportedFieldType(fn, ft) -> 
            msg "UNSUPPORTED_FIELD_TYPE" "Unsupported field type" 
            <| sprintf "Unsupported field type '%s' for field '%s'" fn ft
        | ScriptCannotBeCompiled(e) -> 
            msg "SCRIPT_CANNOT_BE_COMPILED" "Script cannot be compiled" <| sprintf "Script cannot be compiled: \n%s" e
        | AnalyzerNotSupportedForFieldType(f, a) -> 
            msg "ANALYZER_NOT_SUPPORTED" "Analyzer not supported for field" 
            <| sprintf "Analyzer '%s' not supported for field '%s'" f a
        // Search Realted
        | QueryNotFound(q) -> msg "QUERY_NOT_FOUND" "Query not found" <| sprintf "Query not found: %s" q
        | InvalidFieldName(f) -> msg "INVALID_FIELD_NAME" "Invalid field name" <| sprintf "Invalid field name: %s" f
        | StoredFieldCannotBeSearched(f) -> 
            msg "STORED_FIELD_CANNOT_BE_SEARCHED" "Stored field cannot be searched" 
            <| sprintf "Stored field cannot be searched: %s" f
        | MissingFieldValue(f) -> msg "MISSING_FIELD_VALUE" "Missing field value" <| sprintf "Missing field value: %s" f
        | UnknownMissingVauleOption(f) -> 
            msg "UNKNOWN_MISSING_VALUE_OPTION" "Unknown missing value option" 
            <| sprintf "Unknown missing field value option: %s" f
        | DataCannotBeParsed(f, e) -> 
            msg "DATA_CANNOT_BE_PARSED" "Data cannot be parsed" 
            <| sprintf "Data cannot be parsed for field '%s'. Expected data type %s." f e
        | ExpectingNumericData(f) -> 
            msg "EXPECTING_NUMERIC_DATA" "Expecting numeric data" <| sprintf "Expecting numeric data: %s" f
        | QueryOperatorFieldTypeNotSupported(f) -> 
            msg "QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED" "Query operator field type not supported" 
            <| sprintf "Query operator field type not supported for field '%s'" f
        | QueryStringParsingError(e) -> 
            msg "QUERY_STRING_PARSING_ERROR" "Query string parsing error" 
            <| sprintf "Query string parsing error: \n%s" e
        | UnknownSearchProfile(i, p) -> 
            msg "UNKNOWN_SEARCH_PROFILE" "Unknown search profile" 
            <| sprintf "Unknown search profile '%s' for index '%s'" p i
        | PurelyNegativeQueryNotSupported -> 
            msg "NEGATIVE_QUERY_NOT_SUPPORTED" "Purely negative queries (not top query) not supported" ""
        // Indexing related errors
        | IndexAlreadyExists(i) -> 
            msg "INDEX_ALREADY_EXISTS" "Index already exists" <| sprintf "Index '%s' already exists" i
        | IndexShouldBeOnline(i) -> 
            msg "INDEX_SHOULD_BE_ONLINE" "Index should be online" <| sprintf "Index '%s' should be online" i
        | IndexIsAlreadyOnline(i) -> 
            msg "INDEX_IS_ALREADY_ONLINE" "Index is already online" <| sprintf "Index '%s' is already online" i
        | IndexIsAlreadyOffline(i) -> 
            msg "INDEX_IS_ALREADY_OFFLINE" "Index is already offline" <| sprintf "Index '%s' is already offline" i
        | IndexInOpenState(i) -> 
            msg "INDEX_IN_OPEN_STATE" "Index is in an open state" <| sprintf "Index '%s' is in an open state" i
        | IndexInInvalidState(i) -> 
            msg "INDEX_IN_INVALID_STATE" "Index is in an invalid state" <| sprintf "Index '%s' is in an invalid state" i
        | ErrorOpeningIndexWriter(ip, e, d) -> 
            msg "ERROR_OPENING_INDEX_WRITER" "Error opening index writer" 
            <| sprintf "Error opening index writer at path '%s': \nException: %s\n%A" ip e d
        | IndexNotFound(i) -> msg "INDEX_NOT_FOUND" "Index not found" <| sprintf "Index '%s' was not found" i
        | DocumentIdAlreadyExists(idx, id) -> 
            msg "DOCUMENT_ID_ALREADY_EXISTS" "Document ID already exists" 
            <| sprintf "Document ID '%s' already exists for index '%s'" id idx
        | DocumentIdNotFound(idx, id) -> 
            msg "DOCUMENT_ID_NOT_FOUND" "Document ID not found" 
            <| sprintf "Document ID '%s' not found on index '%s'" id idx
        | IndexingVersionConflict(idx, id, v) -> 
            msg "INDEXING_VERSION_CONFLICT" "Indexing version conflict" 
            <| sprintf "Indexing version conflict for index '%s': given ID is %s, but the exising version is %s" idx id 
                   v
        // Modules related
        | ModuleNotFound(mn, mt) -> 
            msg "MODULE_NOT_FOUND" "Module not found" <| sprintf "Module '%s' of type '%s' was not found" mn mt
        | ModuleInitializationError(mn, mt, e) -> 
            msg "MODULE_INITIALIZATION_ERROR" "Module initialization error" 
            <| sprintf "An error occurred while initializing the module '%s' of type '%s': \n%s" mn mt e
        // Concurrent Dictionary
        | UnableToUpdateMemory -> msg "UNABLE_TO_UPDATE_MEMORY" "Unable to update memory" ""
        // Http server related
        | HttpUnableToParse(e) -> 
            msg "HTTP_UNABLE_TO_PARSE" "Unable to parse HTTP message" 
            <| sprintf "Unable to deserialize the HTTP body: \n%s" e
        | HttpUnsupportedContentType -> 
            msg "HTTP_UNSUPPORTED_CONTENT_TYPE" "Unsupported content type for the HTTP message" ""
        | HttpNoBodyDefined -> msg "HTTP_NO_BODY_DEFINED" "No body defined for the HTTP message" ""
        | HttpNotSupported -> msg "HTTP_NOT_SUPPORTED" "" ""
        | HttpUriIdNotSupplied -> msg "HTTP_URI_ID_NOT_SUPPLIED" "" ""
        // Configuration related
        | UnableToParseConfig(p, e) -> 
            msg "UNABLE_TO_PARSE_CONFIG" "Unable to parse configuration file" 
            <| sprintf "Unable to parse configuration file from address '%s': \n%s" p e
        // File related error
        | FileNotFound(p) -> msg "FILE_NOT_FOUND" "File not found" <| sprintf "File not found at address: %s" p
        | FileReadError(f, e) -> 
            msg "FILE_READ_ERROR" "File read error" 
            <| sprintf "There was an error reading the file at path '%s': \n%s" f e
        | FileWriteError(f, e) -> 
            msg "FILE_WRITE_ERROR" "File write error" 
            <| sprintf "There was an error writing to the file at path '%s': \n%s" f e
        | PathDoesNotExist(p) -> msg "PATH_DOES_NOT_EXIST" "Path does not exist" <| sprintf "Path does not exist: %s" p
        | StoreUpdateError -> msg "STORE_UPDATE_ERROR" "" ""
        // Generic error to be used by plugins
        | GenericError(u, d) -> 
            msg "GENERIC_ERROR" "Generic error in plugin" <| sprintf "Generic error in plugin: %s \nData: %A" u d
        // CSV file header does not exist or could not be generated
        | HeaderRowIsEmpty -> msg "HEADER_ROW_IS_EMPTY" "" ""
        | JobNotFound(j) -> msg "JOB_ID_NOT_FOUND" "Job ID not found" <| sprintf "Job ID '%s' not found" j
        | NotImplemented -> msg "NOT_IMPLEMENTED" "Feature not implemented" ""

/// <summary>
/// Represents the lookup name for the plug-in
/// </summary>
[<MetadataAttribute>]
[<Sealed>]
type NameAttribute(name : string) = 
    inherit Attribute()
    member this.Name = name

/// Implements the Freezable pattern
[<InterfaceAttribute>]
type IFreezable = 
    abstract Freeze : unit -> unit

/// To be used by all Dto's which are used in REST webservices
[<AbstractClassAttribute>]
type DtoBase() = 
    let mutable isFrozen = false
    abstract Validate : unit -> Choice<unit, Error>
    interface IFreezable with
        member __.Freeze() = isFrozen <- true

[<AutoOpen>]
module Operators = 
    open System.Collections
    
    let wrap f state = 
        f()
        state
    
    let (|>>) state f = wrap f state
    
    let unwrap f combined = 
        let (c, state) = combined
        let result = f state
        (result, state)
    
    let (==>) combined f = unwrap f combined
    let ignoreWrap f combined = f; combined
    
    let combine f g h = 
        let r1 = f h
        let r2 = g h
        (r1, r2)
    
    /// Wraps a value in a Success
    let inline ok<'a, 'b> (x : 'a) : Choice<'a, 'b> = Choice1Of2(x)
    
    /// Wraps a message in a Failure
    let inline fail<'a, 'b> (msg : 'b) : Choice<'a, 'b> = Choice2Of2 msg
    
    /// Returns true if the result was not successful.
    let inline failed result = 
        match result with
        | Choice2Of2 _ -> true
        | _ -> false
    
    /// Returns true if the result was successful.
    let inline succeeded result = not <| failed result
    
    /// Takes a Result and maps it with fSuccess if it is a Success otherwise it maps it with fFailure.
    let inline either fSuccess fFailure trialResult = 
        match trialResult with
        | Choice1Of2(x) -> fSuccess (x)
        | Choice2Of2(msgs) -> fFailure (msgs)
    
    /// If the given result is a Success the wrapped value will be returned. 
    ///Otherwise the function throws an exception with Failure message of the result.
    let inline returnOrFail result = 
        match result with
        | Choice1Of2(x) -> x
        | Choice2Of2(err) -> raise (ValidationException(err))
    
    /// Takes a bool value and returns ok for sucess and predefined error
    /// for failure
    let inline boolToResult err result = 
        match result with
        | true -> ok()
        | false -> fail (err)
    
    /// Take a Choice result and return true for Choice1 and false for Choice2
    let inline resultToBool result = 
        match result with
        | Choice1Of2(_) -> true
        | _ -> false
    
    //    /// If the given result is a Success the wrapped value will be returned. 
    //    ///Otherwise the function throws an exception with Failure message of the result.
    //    let inline returnOrFail result = 
    //        let inline raiseExn msgs = 
    //            msgs
    //            |> Seq.map (sprintf "%O")
    //            |> String.concat (Environment.NewLine + "\t")
    //            |> failwith
    //        either fst raiseExn result
    //    
    /// Appends the given messages with the messages in the given result.
    let inline mergeMessages msgs result = 
        let inline fSuccess (x, msgs2) = Choice1Of2(x, msgs @ msgs2)
        let inline fFailure errs = Choice2Of2(errs @ msgs)
        either fSuccess fFailure result
    
    /// If the result is a Success it executes the given function on the value.
    /// Otherwise the exisiting failure is propagated.
    let inline bind f result = 
        let inline fSuccess (x) = f x
        let inline fFailure (msg) = Choice2Of2 msg
        either fSuccess fFailure result
    
    /// If the result is a Success it executes the given function on the value. 
    /// Otherwise the exisiting failure is propagated.
    /// This is the infix operator version of ErrorHandling.bind
    let inline (>>=) result f = bind f result
    
    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    let inline apply wrappedFunction result = 
        match wrappedFunction, result with
        | Choice1Of2 f, Choice1Of2 x -> Choice1Of2(f x)
        | Choice2Of2 err, Choice1Of2 _ -> Choice2Of2(err)
        | Choice1Of2 _, Choice2Of2 err -> Choice2Of2(err)
        | Choice2Of2 err1, Choice2Of2 err2 -> Choice2Of2(err1)
    
    let inline extract result = 
        match result with
        | Choice1Of2(a) -> a
        | Choice2Of2(e) -> failwithf "%s" (e.ToString())
    
    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    /// This is the infix operator version of ErrorHandling.apply
    let inline (<*>) wrappedFunction result = apply wrappedFunction result
    
    /// Lifts a function into a Result container and applies it on the given result.
    let inline lift f result = apply (ok f) result
    
    /// Lifts a function into a Result and applies it on the given result.
    /// This is the infix operator version of ErrorHandling.lift
    let inline (<!>) f result = lift f result
    
    /// If the result is a Success it executes the given success function on the value and the messages.
    /// If the result is a Failure it executes the given failure function on the messages.
    /// Result is propagated unchanged.
    let inline eitherTee fSuccess fFailure result = 
        let inline tee f x = 
            f x
            x
        tee (either fSuccess fFailure) result
    
    /// If the result is a Success it executes the given function on the value and the messages.
    /// Result is propagated unchanged.
    let inline successTee f result = eitherTee f ignore result
    
    /// If the result is a Failure it executes the given function on the messages.
    /// Result is propagated unchanged.
    let inline failureTee f result = eitherTee ignore f result
    
    /// Converts an option into a Result.
    let inline failIfNone message result = 
        match result with
        | Some x -> ok x
        | None -> fail message
    
    [<Sealed>]
    type ErrorHandlingBuilder() = 
        
        member inline __.Bind(v, f) = 
            match v with
            | Choice1Of2(x) -> f x
            | Choice2Of2(s) -> Choice2Of2(s)
        
        member inline __.ReturnFrom v = v
        member inline __.Return v = Choice1Of2(v)
        member inline __.Zero() = Choice1Of2()
        
        member inline __.Combine(a, b) = 
            match a, b with
            | Choice1Of2 a', Choice1Of2 b' -> Choice1Of2 b'
            | Choice2Of2 a', Choice1Of2 b' -> Choice2Of2 a'
            | Choice1Of2 a', Choice2Of2 b' -> Choice2Of2 b'
            | Choice2Of2 a', Choice2Of2 b' -> Choice2Of2 a'
        
        member inline __.Delay(f) = f()
        
        member inline this.TryFinally(body, compensation) = 
            try 
                this.ReturnFrom(body())
            finally
                compensation()
        
        member inline __.TryWith(expr, handler) = 
            try 
                expr()
            with ex -> handler ex
        
        member inline this.For(collection : seq<_>, func) = 
            // The whileLoop operator
            let rec whileLoop pred body = 
                if pred() then this.Bind(body(), (fun _ -> whileLoop pred body))
                else this.Zero()
            using (collection.GetEnumerator()) 
                (fun it -> whileLoop (fun () -> it.MoveNext()) (fun () -> it.Current |> func))
        
        member inline this.Using(disposable : #System.IDisposable, body) = 
            let body' = fun () -> body disposable
            this.TryFinally(body', (fun () -> disposable.Dispose()))
    
    /// Wraps computations in an error handling computation expression.
    let maybe = ErrorHandlingBuilder()

[<AutoOpenAttribute>]
module Validators = 
    /// Checks of the given input array has any duplicates    
    let hasDuplicates groupName fieldName (input : array<string>) = 
        if input.Count() = input.Distinct().Count() then ok()
        else fail (DuplicateFieldValue(groupName, fieldName))
    
    /// Checks if a given value is greater than the lower limit
    let gt fieldName lowerLimit input = 
        if input > lowerLimit then ok()
        else fail (GreaterThan(fieldName, lowerLimit.ToString(), input.ToString()))
    
    /// Checks if the passed value is greater than or equal to the lower limit
    let gte fieldName lowerLimit input = 
        if input >= lowerLimit then ok()
        else fail (GreaterThanEqual(fieldName, lowerLimit.ToString(), input.ToString()))
    
    /// Checks if a given value is less than the upper limit
    let lessThan fieldName upperLimit input = 
        if input < upperLimit then ok()
        else fail (LessThan(fieldName, upperLimit.ToString(), input.ToString()))
    
    /// Checks if the passed value is less than or equal to the upper limit
    let lessThanEqual fieldName upperLimit input = 
        if input <= upperLimit then ok()
        else fail (LessThanEqual(fieldName, upperLimit.ToString(), input.ToString()))
    
    /// Checks if the given string is null or empty
    let notBlank fieldName input = 
        if not (String.IsNullOrWhiteSpace(input)) then ok()
        else fail (NotBlank(fieldName))
    
    /// Checks if a given value satisfies the provided regex expression 
    let regexMatch fieldName regexExpr input = 
        let m = System.Text.RegularExpressions.Regex.Match(input, regexExpr)
        if m.Success then ok()
        else fail (RegexMatch(fieldName, regexExpr))
    
    /// Validates if the property name satisfies the naming rules
    let propertyNameRegex fieldName input = 
        match input |> regexMatch fieldName "^[a-z0-9_]*$" with
        | Choice1Of2(_) -> ok()
        | Choice2Of2(_) -> fail <| InvalidPropertyName(fieldName, input)
    
    /// Checks if the property name is not in the restricted field names
    let invalidPropertyName fieldName input = 
        if String.Equals(input, Constants.IdField) || String.Equals(input, Constants.LastModifiedField) then 
            fail (InvalidPropertyName(fieldName, input))
        else ok()
    
    /// Validates a given value against the property name rules
    let propertyNameValidator fieldName input = 
        notBlank fieldName input >>= fun _ -> propertyNameRegex fieldName input 
        >>= fun _ -> invalidPropertyName fieldName input
    
    /// Validates a given sequence in which each element implements IValidate    
    let seqValidator (input : seq<DtoBase>) = 
        let res = 
            input
            |> Seq.map (fun x -> x.Validate())
            |> Seq.filter failed
            |> Seq.toArray
        if res.Length = 0 then ok()
        else res.[0]

[<AutoOpenAttribute>]
module DataDefaults = 
    
    let defString = String.Empty
    let defStringDict() = new Dictionary<string, string>()
    let defStringList = Array.empty<string>
    let defArray<'T> = Array.empty<'T>
    let defInt64 = 0L
    let defDouble = 0.0

//let defOf<'T> = Unchecked.defaultof<'T>
[<AutoOpenAttribute>]
module DictionaryHelpers = 
    /// Convert a .net dictionary to java based hash map
    [<CompiledNameAttribute("DictToMap")>]
    let inline dictToMap (dict : Dictionary<string, string>) = 
        let map = new java.util.HashMap()
        dict |> Seq.iter (fun pair -> map.Add(pair.Key, pair.Value))
        map
    
    let inline keyExists (value, error) (dict : IDictionary<string, _>) = 
        match dict.TryGetValue(value) with
        | true, v -> Choice1Of2(v)
        | _ -> Choice2Of2(error (value))
    
    let inline keyExists2 (value, error) (dict : IReadOnlyDictionary<string, _>) = 
        match dict.TryGetValue(value) with
        | true, v -> Choice1Of2(v)
        | _ -> Choice2Of2(error (value))
    
    let inline tryGet (key) (dict : IDictionary<string, _>) = 
        match dict.TryGetValue(key) with
        | true, v -> Choice1Of2(v)
        | _ -> Choice2Of2(KeyNotFound(key))
    
    let inline remove (value) (dict : ConcurrentDictionary<string, _>) = dict.TryRemove(value) |> ignore
    let inline conDict<'T>() = new ConcurrentDictionary<string, 'T>(StringComparer.OrdinalIgnoreCase)
    let inline dict<'T>() = new Dictionary<string, 'T>(StringComparer.OrdinalIgnoreCase)
    let inline strDict() = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    let inline tryAdd<'T> (key, value : 'T) (dict : ConcurrentDictionary<string, 'T>) = dict.TryAdd(key, value)
    let inline add<'T> (key, value : 'T) (dict : ConcurrentDictionary<string, 'T>) = dict.TryAdd(key, value) |> ignore
    
    let inline tryRemove<'T> (key) (dict : ConcurrentDictionary<string, 'T>) = 
        match dict.TryRemove(key) with
        | true, _ -> true
        | _ -> false
    
    let inline tryUpdate<'T> (key, value : 'T) (dict : ConcurrentDictionary<string, 'T>) = 
        match dict.TryGetValue(key) with
        | true, value' -> dict.TryUpdate(key, value, value')
        | _ -> dict.TryAdd(key, value)
    
    let inline addOrUpdate<'T> (key, value : 'T) (dict : ConcurrentDictionary<string, 'T>) = 
        match dict.TryGetValue(key) with
        | true, v -> dict.TryUpdate(key, value, v) |> ignore
        | _ -> dict.TryAdd(key, value) |> ignore

// ----------------------------------------------------------------------------
// Logging Section
// ----------------------------------------------------------------------------
[<AutoOpen; RequireQualifiedAccess>]
module Log = 
    open System.Diagnostics
    
    let sourceName = "FlexSearch"
    let logName = "FlexSearch"
    let infomation = EventLogEntryType.Information
    let warning = EventLogEntryType.Warning
    let critical = EventLogEntryType.Error
    
    /// Default logger for FlexSearch
    let loggerInitError = 
        try 
            if EventLog.SourceExists(sourceName) then 
                if EventLog.LogNameFromSourceName(sourceName, ".") <> logName then
                    EventLog.DeleteEventSource(sourceName)
                    EventLog.CreateEventSource(sourceName, logName)
            else
                EventLog.CreateEventSource(sourceName, logName)
            true

        with _ -> false
    
    let writeEntry (message, logLevel) = 
        if not loggerInitError then EventLog.WriteEntry(sourceName, message, logLevel)
    
    let writeEntryId (id, message, logLevel) = EventLog.WriteEntry(sourceName, message, logLevel, id)
    let debug message = writeEntry (message, infomation)
    let debugEx (ex : Exception) = writeEntry (exceptionPrinter ex, infomation)
    let warn message = writeEntry (message, warning)
    let warnEx (ex : Exception) = writeEntry (exceptionPrinter ex, warning)
    let info message = writeEntry (message, infomation)
    let infoEx (ex : Exception) = writeEntry (exceptionPrinter ex, infomation)
    let error message = writeEntry (message, critical)
    let errorEx (ex : Exception) = writeEntry (exceptionPrinter ex, critical)
    let fatal message = writeEntry (message, critical)
    let fatalEx (ex : Exception) = writeEntry (exceptionPrinter ex, critical)
    let fatalWithMsg msg (ex : Exception) = writeEntry (sprintf "%s \n%s" msg (exceptionPrinter ex), critical)
    let addIndex (indexName : string, indexDetails : string) = 
        writeEntryId (1, sprintf "Adding new index %s. \nIndexDetails: %s" indexName indexDetails, infomation)
    let updateIndex (indexName : string, indexDetails : string) = 
        writeEntryId (2, sprintf "Updating index %s. \nIndexDetails: %s" indexName indexDetails, infomation)
    let deleteIndex (indexName : string) = writeEntryId (3, sprintf "Deleting index %s." indexName, infomation)
    let closeIndex (indexName) = writeEntryId (4, sprintf "Closing index %s." indexName, infomation)
    let openIndex (indexName) = writeEntryId (5, sprintf "Opening index %s." indexName, infomation)
    let loadingIndex (indexName, indexDetails) = 
        writeEntryId (6, sprintf "Loading index %s. \nIndexDetails: %s" indexName indexDetails, infomation)
    let indexLoadingFailed (indexName, indexDetails, ex) = 
        writeEntryId 
            (7, sprintf "Failed to load index %s. \nIndexDetails: \n%s \nError details: \n%s" indexName indexDetails ex, 
             critical)
    let componentLoaded (componentType, componentNames) = 
        writeEntryId 
            (8, sprintf "Loading Component of type: %s \nLoaded component details:\n%s" componentType componentNames, 
             infomation)
    let componentInitializationFailed (name, componentType, message) = 
        writeEntryId 
            (9, 
             sprintf "Component initialization failed: %s. Component type: %s \nError details: \n%s" name componentType 
                 message, critical)
    let startSession (details) = writeEntryId (10, sprintf "Staring FlexSearch.\nDetails: \n%s" details, infomation)
    let endSession() = writeEntryId (11, "Quiting FlexSearch.", infomation)
    let shutdown() = writeEntryId (12, "FlexSearch termination request received.", infomation)

/// Represents a thread-safe file writer that can be accessed by 
/// multiple threads concurrently.
/// Note : This is not meant to be used for huge files and should 
/// be used for writing configuration files.
[<Sealed>]
type ThreadSafeFileWriter(formatter : FlexSearch.Core.IFormatter) = 
    
    let getPathWithExtension (path) = 
        if Path.GetExtension(path) <> Constants.SettingsFileExtension then path + Constants.SettingsFileExtension
        else path
    
    member __.DeleteFile(filePath) = 
        let path = getPathWithExtension (filePath)
        if File.Exists(path) then 
            use mutex = new Mutex(false, path.Replace("\\", ""))
            File.Delete(path)
        ok()
    
    member __.ReadFile<'T>(filePath) = 
        let path = getPathWithExtension (filePath)
        if File.Exists(path) then 
            try 
                use stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                let response = formatter.DeSerialize<'T>(stream)
                ok <| response
            with e -> fail <| FileReadError(filePath, exceptionPrinter e)
        else fail <| FileNotFound(filePath)
    
    member __.WriteFile<'T>(filePath, content : 'T) = 
        let path = getPathWithExtension (filePath)
        use mutex = new Mutex(true, path.Replace("\\", ""))
        Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
        try 
            mutex.WaitOne(-1) |> ignore
            use file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
            let byteContent = System.Text.UTF8Encoding.UTF8.GetBytes(formatter.SerializeToString(content))
            file.Write(byteContent, 0, byteContent.Length)
            mutex.ReleaseMutex()
            ok()
        with e -> 
            mutex.ReleaseMutex()
            fail <| FileWriteError(filePath, exceptionPrinter e)

[<Sealed>]
type DtoStore<'T>(fileWriter : ThreadSafeFileWriter) = 
    let store = conDict<'T>()
    
    let getFolderName (typeName : string) = 
        let parts = typeName.Split([| '.'; '+' |], StringSplitOptions.RemoveEmptyEntries)
        if parts.Last() = "Dto" then parts.[parts.Length - 2]
        else parts.Last()
    
    let folderPath = Constants.ConfFolder +/ (getFolderName (typeof<'T>.FullName))
    
    member __.UpdateItem<'T>(key : string, item : 'T) = 
        let path = folderPath +/ key
        match store |> tryUpdate (key, item) with
        | true -> fileWriter.WriteFile(path, item)
        | false -> fail <| StoreUpdateError
    
    member __.DeleteItem<'T>(key : string) = 
        let path = folderPath +/ key
        match store.TryRemove(key) with
        | true, _ -> fileWriter.DeleteFile(path)
        | _ -> fail <| StoreUpdateError
    
    member __.GetItem(key : string) = 
        match store.TryGetValue(key) with
        | true, value -> ok <| value
        | _ -> fail <| KeyNotFound(key)
    
    member __.GetItems() = store.Values.ToArray()
    member __.LoadItem<'T>(key : string) = 
        match fileWriter.ReadFile<'T>(folderPath +/ key) with
        | Choice1Of2(item) -> 
            tryAdd (key, item) |> ignore
            ok()
        | Choice2Of2(error) -> fail <| error
