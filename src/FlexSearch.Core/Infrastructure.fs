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
open Microsoft.Isam.Esent.Collections.Generic
open Newtonsoft.Json
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Linq
open System.Runtime.Serialization
open System.Threading

[<AutoOpen>]
[<RequireQualifiedAccess>]
/// Contains all the flex constants which do not change per instance
module Constants = 
    [<Literal>]
    let generationLabel = "generation"
    
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
    let private confFolder = Path.Combine(rootFolder, "Conf")
    let private pluginFolder = Path.Combine(rootFolder, "Plugins")
    let private logsFolder = Path.Combine(rootFolder, "Logs")
    
    /// Flex index folder
    let ConfFolder = 
        Directory.CreateDirectory(confFolder) |> ignore
        confFolder
    
    /// Flex plug-in folder
    let PluginFolder = 
        Directory.CreateDirectory(pluginFolder) |> ignore
        pluginFolder
    
    /// Flex logs folder
    let LogsFolder = 
        Directory.CreateDirectory(logsFolder) |> ignore
        logsFolder
    
    /// Extension to be used by settings file
    let SettingsFileExtension = ".yml"
    
    let CaseInsensitiveKeywordAnalyzer = 
        CustomAnalyzer.Builder().withTokenizer("keyword").addTokenFilter("lowercase").build()

type Error = 
    // Generic Validation Errors
    | GreaterThan of fieldName : string * lowerLimit : string * value : string
    | LessThan of fieldName : string * upperLimit : string * value : string
    | GreaterThanEqual of fieldName : string * lowerLimit : string * value : string
    | LessThanEqual of fieldName : string * lowerLimit : string * value : string
    | NotBlank of fieldName : string
    | RegexMatch of fieldName : string * regexExpr : string
    | KeyNotFound of key : string
    // Domain related
    | InvalidPropertyName of fieldName : string * value : string
    | AnalyzerIsMandatory of fieldName : string
    | DuplicateFieldValue of groupName : string * fieldName : string
    | ScriptNotFound of scriptName : string * fieldName : string
    // Builder related errors
    | AnalyzerBuilder of analyzerName : string * message : string * exp : string
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
    // Indexing related errrors
    | IndexAlreadyExists of indexName : string
    | IndexShouldBeOnline of indexName : string
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
    // Http server related
    | HttpUnableToParse of error : string
    | HttpUnsupportedContentType
    | HttpNoBodyDefined
    | HttpNotSupported
    | HttpUriIdNotSupplied
    // Configuration related
    | UnableToParseConfig of error : string
    // Generic error to be used by plugins
    | GenericError of userMessage : string * data : ResizeArray<KeyValuePair<string, string>>

exception ValidationException of Error

[<CLIMutableAttribute>]
type OperationMessage = 
    { DeveloperMessage : string
      UserMessage : string
      ErrorCode : string }

[<AutoOpen>]
module Errors = 
    let inline toMessage (error : Error) = 
        { DeveloperMessage = ""
          UserMessage = ""
          ErrorCode = "" }
    
    let inline ex (error) = raise (ValidationException(error))
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
    let MISSING_FIELD_VALUE = "MISSING_FIELD_VALUE:No value provided for the field."
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
        
        let bind v f = 
            match v with
            | Choice1Of2(x) -> f x
            | Choice2Of2(s) -> Choice2Of2(s)
        
        let combine a b = 
            match a, b with
            | Choice1Of2 a', Choice1Of2 b' -> Choice1Of2 b'
            | Choice2Of2 a', Choice1Of2 b' -> Choice2Of2 a'
            | Choice1Of2 a', Choice2Of2 b' -> Choice2Of2 b'
            | Choice2Of2 a', Choice2Of2 b' -> Choice2Of2 a'
        
        let zero = Choice1Of2()
        let returnFrom v = v
        let delay f = f()
        
        let tryFinally body compensation = 
            try 
                returnFrom (body())
            finally
                compensation()
        
        let using (disposable : #System.IDisposable) body = 
            let body' = fun () -> body disposable
            tryFinally body' (fun () -> disposable.Dispose())
        
        // The whileLoop operator
        let rec whileLoop pred body = 
            if pred() then bind (body()) (fun _ -> whileLoop pred body)
            else zero
        
        let tryWith expr handler = 
            try 
                expr()
            with ex -> handler ex
        
        // The forLoop operator
        let forLoop (collection : seq<_>) func = 
            using (collection.GetEnumerator()) 
                (fun it -> whileLoop (fun () -> it.MoveNext()) (fun () -> it.Current |> func))
        member __.Bind(v, f) = bind v f
        member __.ReturnFrom v = returnFrom v
        member __.Return v = Choice1Of2(v)
        member __.Zero() = zero
        member __.Combine(a, b) = combine a b
        member __.Delay(f) = delay f
        member __.TryFinally(body, compensation) = tryFinally body compensation
        member __.TryWith(expr, handler) = tryWith expr handler
        member __.For(collection : seq<_>, func) = forLoop collection func
        member __.Using(disposable : #System.IDisposable, body) = using disposable body
    
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
    open System.Collections.ObjectModel

    let defString = String.Empty
    let defStringDict = new Dictionary<string, string>()
    let defStringList = Enumerable.Empty<String>()
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
    
    let inline addOrUpdate<'T> (key, value : 'T) (dict : ConcurrentDictionary<string, 'T>) = 
        match dict.TryGetValue(key) with
        | true, v -> dict.TryUpdate(key, value, v) |> ignore
        | _ -> dict.TryAdd(key, value) |> ignore

module LazyFactory = 
    open Microsoft.Isam.Esent.Collections.Generic
    open Newtonsoft.Json
    open System.Linq
    
    /// Item to be stored in the item
    [<Serializable; CLIMutableAttribute>]
    type private StoreItem = 
        { Key : string
          Type : string
          Data : string }
    
    /// Store is used to save persistant settings.
    type Store(?inMemory : bool) = 
        let inMemory = defaultArg inMemory false
        
        let db = 
            match inMemory with
            | true -> conDict<StoreItem>() :> IDictionary<string, StoreItem>
            | false -> new PersistentDictionary<string, StoreItem>("Conf") :> IDictionary<string, StoreItem>
        
        member __.GetItem<'T>(key : string) = 
            let key = String.Concat(typeof<'T>.Name, key)
            match db.TryGetValue(key) with
            | true, v -> ok <| JsonConvert.DeserializeObject<'T>(v.Data)
            | _ -> fail <| KeyNotFound key
        
        member __.GetItems<'T>() = 
            db.Values.Where(fun x -> x.Type = typeof<'T>.Name)
              .Select(fun x -> JsonConvert.DeserializeObject<'T>(x.Data))
        
        /// Add or Update an item in the store by key 
        member __.UpdateItem<'T>(key : string, item : 'T) = 
            assert (not (isNull key))
            let key = String.Concat(typeof<'T>.Name, key)
            
            let item = 
                { Key = key
                  Type = typeof<'T>.Name
                  Data = JsonConvert.SerializeObject(item) }
            match db.TryGetValue(key) with
            | true, v -> db.[key] <- item
            | _ -> db.Add(key, item)
        
        member __.DeleteItem<'T>(key : string) = 
            assert (not (isNull key))
            let key = String.Concat(typeof<'T>.Name, key)
            db.Remove(key) |> ignore
    
    type FactoryItem<'LuceneObject, 'FlexMeta, 'FlexState> = 
        { MetaData : 'FlexMeta
          State : 'FlexState
          Value : 'LuceneObject option }
    
    type T<'LuceneObject, 'FlexMeta, 'FlexState> = 
        { PersistenceStore : Store
          ObjectStore : ConcurrentDictionary<string, FactoryItem<'LuceneObject, 'FlexMeta, 'FlexState>>
          Generator : option<'FlexMeta -> Choice<'LuceneObject, Error>> }
    
    /// Create a new LazyFactory object
    let create<'LuceneObject, 'FlexMeta, 'FlexState> generator store = 
        { PersistenceStore = store
          ObjectStore = conDict<FactoryItem<'LuceneObject, 'FlexMeta, 'FlexState>>()
          Generator = generator }
    
    /// Update an item by key
    let inline private updateItemInObjectStore (key) (value) (store : T<_, _, _>) = 
        let _, oldVal = store.ObjectStore.TryGetValue(key)
        store.ObjectStore.TryUpdate(key, value, oldVal) |> ignore
    
    /// Get the state of the object associated with the key
    let inline getState (key) (store : T<_, _, _>) = 
        match store.ObjectStore.TryGetValue(key) with
        | true, a -> ok a.State
        | _ -> fail (KeyNotFound(key))
    
    /// Update the state of the object
    let inline updateState (key, newState : 'FlexState) (store : T<_, _, 'FlexState>) = 
        match store.ObjectStore.TryGetValue(key) with
        | true, oldValue -> 
            let newValue = 
                { MetaData = oldValue.MetaData
                  State = newState
                  Value = oldValue.Value }
            store.ObjectStore.TryUpdate(key, newValue, oldValue) |> ignore
            ok()
        | _ -> fail (KeyNotFound(key))
    
    /// Get an item by key
    let inline getItem (key) (store : T<_, _, _>) = 
        match store.ObjectStore.TryGetValue(key) with
        | true, a -> ok a
        | _ -> fail (KeyNotFound(key))
    
    /// Returns the item if it exists otherwise executes the custom error handler
    let inline getItemOrError (key) (error) (factory : T<_, _, _>) = 
        match factory.ObjectStore.TryGetValue(key) with
        | true, item -> ok (item)
        | _ -> fail (error (key))
    
    /// Returns the item if it exists otherwise executes the custom error handler
    let inline getAsTuple (key) (error) (factory : T<_, _, _>) = 
        match factory.ObjectStore.TryGetValue(key) with
        | true, item -> 
            if item.Value.IsNone then 
                failwithf "Internal Error: GetAsTuple should only be used with initialized instance."
            ok (item.MetaData, item.State, item.Value.Value)
        | _ -> fail (error (key))
    
    /// Get an instance of the object by key. If the instance does not exist then
    /// the factory will create a new instance provided the object is in correct state.
    let inline getInstance (key) (store : T<_, _, _>) = 
        match store.ObjectStore.TryGetValue(key) with
        | true, a -> 
            match a.Value with
            | Some(v) -> ok <| v
            | _ -> 
                match store.Generator with
                | Some(generator) -> 
                    match generator (a.MetaData) with
                    | Choice1Of2(value) -> 
                        let newItem = { a with Value = Some(value) }
                        store |> updateItemInObjectStore key newItem
                        ok <| value
                    | Choice2Of2(error) -> fail <| error
                | None -> 
                    failwithf 
                        "Internal Error: Generating value from factory is not possible when generator is not defined."
        | _ -> fail (KeyNotFound(key))
    
    /// Get the meta data associated with a key
    let inline getMetaData (key) (store : T<_, _, _>) = 
        match store.ObjectStore.TryGetValue(key) with
        | true, a -> ok a.MetaData
        | _ -> fail (KeyNotFound(key))
    
    /// Delete an item with the key
    let inline deleteItem (key) (store : T<_, _, _>) = 
        store.ObjectStore.TryRemove(key) |> ignore
        store.PersistenceStore.DeleteItem(key) |> ignore
    
    ///  Add or update an item
    let inline updateMetaData (key, state, metaData : 'FlexMeta) (store : T<_, _, _>) = 
        let value = 
            { MetaData = metaData
              State = state
              Value = None }
        // Remove the older item if it exists. In case other objects have reference to the 
        // value they can keep on using it. Once those objects are disposed the value should
        // get garbage collected.
        store.ObjectStore.TryRemove(key) |> ignore
        store.ObjectStore.TryAdd(key, value) |> ignore
        store.PersistenceStore.UpdateItem(key, metaData)
    
    /// Checks if a given key exists in the store
    let inline exists (key) (error) (factory : T<_, _, _>) = 
        match factory.ObjectStore.TryGetValue(key) with
        | true, v -> ok()
        | _ -> fail (error (key))
    
    /// Returns both state and the instance 
    let inline getStateAndInstance (key) (factory : T<_, _, _>) = 
        match factory |> getInstance key with
        | Choice1Of2(instance) -> ok (factory.ObjectStore.[key].State, instance)
        | Choice2Of2(error) -> fail error
    
    /// Update the instance in a factory. This ahould be used for complex object generation where
    /// providing a simple generator is not possible
    let inline updateInstance (key, instance) (factory : T<_, _, _>) = 
        match factory.ObjectStore.TryGetValue(key) with
        | true, value -> 
            let newItem = { value with Value = Some(instance) }
            factory |> updateItemInObjectStore key value
            ok()
        | _ -> fail (KeyNotFound(key))

// ----------------------------------------------------------------------------
// Logging Section
// ----------------------------------------------------------------------------
open Microsoft.Diagnostics.Tracing
open Microsoft.FSharp.Core.LanguagePrimitives

/// Simple enum wrapper for possible Task values
module Tasks = 
    [<Literal>]
    let WebRequest : EventTask = enum 1

/// Simple enum wrapper for possible Keyword values
module Keywords = 
    [<Literal>]
    let IndexManagement : EventKeywords = EnumOfValue<int64, EventKeywords> 1L
    
    [<Literal>]
    let Node : EventKeywords = EnumOfValue<int64, EventKeywords> 2L
    
    [<Literal>]
    let Components : EventKeywords = EnumOfValue<int64, EventKeywords> 3L

/// Generic logger interface
[<EventSourceImplementation(Name = "FlexSearch")>]
[<Interface>]
type ILogService = 
    
    [<EventAttribute(1, Message = "Adding new index {0}. \nIndexDetails: {1}", Level = EventLevel.Informational, 
                     Channel = EventChannel.Admin, Keywords = Keywords.IndexManagement)>]
    abstract AddIndex : indexName:string * indexDetails:string -> unit
    
    [<EventAttribute(2, Message = "Updating index {0}. \nIndexDetails: {1}", Level = EventLevel.Informational, 
                     Channel = EventChannel.Admin, Keywords = Keywords.IndexManagement)>]
    abstract UpdateIndex : indexName:string * indexDetails:string -> unit
    
    [<EventAttribute(3, Message = "Deleting index {0}", Level = EventLevel.Informational, Channel = EventChannel.Admin, 
                     Keywords = Keywords.IndexManagement)>]
    abstract DeleteIndex : indexName:string -> unit
    
    [<EventAttribute(4, Message = "Closing index {0}.", Level = EventLevel.Informational, Channel = EventChannel.Admin, 
                     Keywords = Keywords.IndexManagement)>]
    abstract CloseIndex : indexName:string -> unit
    
    [<EventAttribute(5, Message = "Opening index {0}.", Level = EventLevel.Informational, Channel = EventChannel.Admin, 
                     Keywords = Keywords.IndexManagement)>]
    abstract OpenIndex : indexName:string -> unit
    
    [<EventAttribute(6, Message = "Loading index {0}. \nIndexDetails: {1}", Level = EventLevel.Informational, 
                     Channel = EventChannel.Admin, Keywords = Keywords.IndexManagement)>]
    abstract LoadingIndex : indexName:string * indexDetails:string -> unit
    
    [<EventAttribute(7, Message = "Failed to load index {0}. \nIndexDetails: \n{1} \nError details: \n{2}", 
                     Level = EventLevel.Error, Channel = EventChannel.Admin, Keywords = Keywords.IndexManagement)>]
    abstract IndexLoadingFailed : indexName:string * indexDetails:string * validationObject:string -> unit
    
    [<EventAttribute(8, Message = "Loading Component of type: {0} \nLoaded component details:\n{1}", 
                     Level = EventLevel.Informational, Channel = EventChannel.Admin, Keywords = Keywords.Components)>]
    abstract ComponentLoaded : componentType:string * componentNames:string -> unit
    
    [<EventAttribute(9, Message = "Component initialization failed: {0}. Component type: {1} \nError details: \n{2}", 
                     Level = EventLevel.Error, Channel = EventChannel.Admin, Keywords = Keywords.Components)>]
    abstract ComponentInitializationFailed : name:string * componentType:string * message:string -> unit
    
    [<EventAttribute(10, Message = "Staring FlexSearch.\nDetails: \n{0}", Level = EventLevel.Informational, 
                     Channel = EventChannel.Admin, Keywords = Keywords.Node)>]
    abstract StartSession : details:string -> unit
    
    [<EventAttribute(11, Message = "Quiting FlexSearch.", Level = EventLevel.Informational, Channel = EventChannel.Admin, 
                     Keywords = Keywords.Node)>]
    abstract EndSession : unit -> unit
    
    [<EventAttribute(12, Message = "FlexSearch termination request received.", Level = EventLevel.Informational, 
                     Channel = EventChannel.Admin, Keywords = Keywords.Node)>]
    abstract Shutdown : unit -> unit
    
    [<EventAttribute(13, Message = "{0}", Level = EventLevel.Critical, Channel = EventChannel.Admin, 
                     Keywords = EventKeywords.None)>]
    abstract Fatal : message:string -> unit
    
    [<EventAttribute(14, Message = "{0}", Level = EventLevel.Verbose, Channel = EventChannel.Admin, 
                     Keywords = EventKeywords.None)>]
    abstract Debug : message:string -> unit
    
    [<EventAttribute(15, Message = "{0}", Level = EventLevel.Informational, Channel = EventChannel.Admin, 
                     Keywords = EventKeywords.None)>]
    abstract Info : message:string -> unit
    
    [<EventAttribute(16, Message = "{0}", Level = EventLevel.Warning, Channel = EventChannel.Admin, 
                     Keywords = EventKeywords.None)>]
    abstract Warn : message:string -> unit
    
    [<EventAttribute(17, Message = "{0}", Level = EventLevel.Error, Channel = EventChannel.Admin, 
                     Keywords = EventKeywords.None)>]
    abstract Error : message:string -> unit

[<AutoOpen>]
module Log = 
    /// Default logger for FlexSearch
    let logger = EventSourceImplementer.GetEventSourceAs<ILogService>()
    
    let debug message = logger.Debug(message)
    let debugEx (ex : Exception) = logger.Debug(exceptionPrinter ex)
    let warn message = logger.Warn(message)
    let warnEx (ex : Exception) = logger.Warn(exceptionPrinter ex)
    let info message = logger.Info(message)
    let infoEx (ex : Exception) = logger.Info(exceptionPrinter ex)
    let error message = logger.Error(message)
    let errorEx (ex : Exception) = logger.Error(exceptionPrinter ex)
    let fatal message = logger.Fatal(message)
    let fatalEx (ex : Exception) = logger.Fatal(exceptionPrinter ex)
    let fatalWithMsg msg (ex : Exception) = logger.Fatal(sprintf "%s \n%s" msg (exceptionPrinter ex))
