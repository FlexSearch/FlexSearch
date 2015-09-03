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

[<NotForDocumentation>]
type MessageKeyword =
    | Node = 1
    | Index = 2 
    | Search = 3 
    | Document = 4 
    | Default = 5
    | Plugin = 6
    | Startup = 7

[<NotForDocumentation>]
type MessageLevel =
    | Critical = 1
    | Error = 2
    | Warning = 3
    | Info = 4
    | Verbose = 5
    | Nothing = 6

/// Interface to expose internal message format
type IMessage = 
    abstract OperationMessage : unit -> OperationMessage
    abstract LogProperty : unit -> MessageKeyword * MessageLevel

/// Represents the result of a computation.
type Result<'T> =
    | Ok of 'T
    | Fail of IMessage

/// General purpose exception to be used. It is better to use
/// DU based error than this exception.
exception ValidationException of IMessage

//    abstract UserMessage : unit -> string
//    abstract LogMessage : unit -> string
module MessageHelpers = 
    let private cases = new ConcurrentDictionary<System.Type * string, PropertyInfo []>()
    
    /// Get the label associated with a DU pattern and also returns the field info. 
    /// This uses a cache to avoid reflection.
    let getCaseInfo (x : 'a) = 
        let union = (FSharpValue.GetUnionFields(x, typeof<'a>) |> fst)

        match cases.TryGetValue((typeof<'a>, union.Name)) with
        | true, properties -> (union.Name, properties)
        | _ -> 
            assert (notNull union.Name)
            let case = (union.Name, union.GetFields())
            cases.TryAdd((typeof<'a>, union.Name), case |> snd) |> ignore
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
    
    let sf1 = Printf.StringFormat<string -> string>
    let sf2 = Printf.StringFormat<string -> string -> string>
    let sf3 = Printf.StringFormat<string -> string -> string -> string>
    let sf4 = Printf.StringFormat<string -> string -> string -> string -> string>
 
open MessageHelpers

[<AutoOpen>]
module ValidationError =
    
    let private greaterThan = sf3 "Field '%s' must be greater than %s, but found %s"
    let private lessThan = sf3 "Field '%s' must be less than %s, but found %s"
    let private greaterThanEqual = sf3 "Field '%s' must be greater than or equal to %s, but found %s"
    let private lessThanEqual = sf3 "Field '%s' must be less than or equal to %s, but found %s"
    let private notBlank = sf1 "Field '%s' must not be blank"
    let private regexMatch = sf2 "Field '%s' must match Regex expression: %s"
    let private keyNotFound = sf1 "Key not found: %s"
    
    type T = 
        | GreaterThan of fieldName : string * lowerLimit : string * value : string
        | LessThan of fieldName : string * upperLimit : string * value : string
        | GreaterThanEqual of fieldName : string * lowerLimit : string * value : string
        | LessThanEqual of fieldName : string * lowerLimit : string * value : string
        | NotBlank of fieldName : string
        | RegexMatch of fieldName : string * regexExpr : string
        | KeyNotFound of key : string
        override this.ToString() = sprintf "%A" this
        interface IMessage with
            member this.LogProperty() = (MessageKeyword.Default, MessageLevel.Nothing)
            member this.OperationMessage() = 
                match this with
                | GreaterThan(fn, ll, v) -> sprintf greaterThan fn ll v
                | LessThan(fn, ul, v) -> sprintf lessThan fn ul v
                | GreaterThanEqual(fn, ll, v) -> sprintf greaterThanEqual fn ll v
                | LessThanEqual(fn, ul, v) -> sprintf lessThanEqual fn ul v
                | NotBlank(fn) -> sprintf notBlank fn
                | RegexMatch(fn, re) -> sprintf regexMatch fn re
                | KeyNotFound(key) -> sprintf keyNotFound key
                |> caseToMsg this

[<AutoOpen>]
module AnalysisMessage =
    let private tokenizerNotFound = sf2 "Tokenizer with the name %s does not exist. Analyzer Name: %s"
    let private unableToInitializeTokenizer = sf4 "Tokenizer with the name %s cannot be initialized. Analyzer Name: %s. Error: %s. Exception: %s"
    let private filterNotFound = sf2 "Filter with the name %s does not exist. Analyzer Name: %s"
    let private unableToInitializeFilter = sf4 "Filter with the name %s cannot be initialized. Analyzer Name: %s. Error: %s. Exception: %s"
    let private analyzerBuilder = sf3 "The analyzer '%s' threw an exception while building: %s; \n%s"
    let private analyzerNotFound = sf1 "The analyzer '%s' was not found"

    type T = 
        | TokenizerNotFound of analyzerName : string * tokenizerName : string
        | UnableToInitializeTokenizer of analyzerName : string * tokenizerName : string * message : string * ``exception`` : string
        | FilterNotFound of analyzerName : string * filterName : string
        | UnableToInitializeFilter of analyzerName : string * filterName : string * message : string * ``exception`` : string
        | AnalyzerBuilder of analyzerName : string * message : string * ``exception`` : string
        | AnalyzerNotFound of analyzerName : string
        interface IMessage with
            member this.LogProperty() = (MessageKeyword.Node, MessageLevel.Verbose)
            member this.OperationMessage() = 
                match this with
                | TokenizerNotFound(an, tn) -> sprintf tokenizerNotFound tn an
                | UnableToInitializeTokenizer(an, tn, m, exp) -> sprintf unableToInitializeTokenizer tn an m exp
                | FilterNotFound(an, fn) -> sprintf filterNotFound fn an
                | UnableToInitializeFilter(an, fn, m, exp) -> sprintf unableToInitializeFilter fn an m exp
                | AnalyzerBuilder(an, m, e) -> sprintf analyzerBuilder an m e
                | AnalyzerNotFound(a) -> sprintf analyzerNotFound a
                |> caseToMsg this

[<AutoOpen>]
module BuilderError =
    let private invalidPropertyName = sf2 "Property name is invalid. Expected '%s' but found '%s'"
    let private analyzerIsMandatory = sf1 "Analyzer is mandatory for field '%s'"
    let private duplicateFieldValue = sf2 "A duplicate entry (%s) has been found in the group '%s'"
    let private scriptNotFound = sf2 "The script '%s' was not found against the field '%s'"
    let private resourceNotFound = sf2 "The resource '%s' of type %s was not found"
    let private unSupportedSimilarity = sf1 "Unsupported similarity: %s"
    let private unSupportedIndexVersion = sf1 "Unsupported index version: %s"
    let private unsupportedDirectoryType = sf1 "Unsupported directory type: %s"
    let private unSupportedFieldType = sf2 "Unsupported field type '%s' for field '%s'"
    let private scriptCannotBeCompiled = sf2 "Script '%s' cannot be compiled: \n%s"
    let private analyzerNotSupportedForFieldType = sf2 "Analyzer '%s' not supported for field '%s'"
    
    type T = 
        | InvalidPropertyName of fieldName : string * value : string
        | AnalyzerIsMandatory of fieldName : string
        | DuplicateFieldValue of groupName : string * fieldName : string
        | ScriptNotFound of scriptName : string * fieldName : string
        | ResourceNotFound of resourceName : string * resourceType : string
        | UnSupportedSimilarity of similarityName : string
        | UnSupportedIndexVersion of indexVersion : string
        | UnsupportedDirectoryType of directoryType : string
        | UnSupportedFieldType of fieldName : string * fieldType : string
        | ScriptCannotBeCompiled of scriptName : string * error : string
        | AnalyzerNotSupportedForFieldType of fieldName : string * analyzerName : string
        interface IMessage with
            member this.LogProperty() = (MessageKeyword.Index, MessageLevel.Error)
            member this.OperationMessage() = 
                match this with
                | InvalidPropertyName(fn, v) -> sprintf invalidPropertyName fn v
                | AnalyzerIsMandatory(fn) -> sprintf analyzerIsMandatory fn
                | DuplicateFieldValue(gn, fn) -> sprintf duplicateFieldValue fn gn
                | ScriptNotFound(sn, fn) -> sprintf scriptNotFound sn fn
                | ResourceNotFound(rn, rt) -> sprintf resourceNotFound rn rt
                | UnSupportedSimilarity(s) -> sprintf unSupportedSimilarity s
                | UnSupportedIndexVersion(i) -> sprintf unSupportedIndexVersion i
                | UnsupportedDirectoryType(d) -> sprintf unsupportedDirectoryType d
                | UnSupportedFieldType(fn, ft) -> sprintf unSupportedFieldType fn ft
                | ScriptCannotBeCompiled(sn, e) -> sprintf scriptCannotBeCompiled sn e
                | AnalyzerNotSupportedForFieldType(f, a) -> sprintf analyzerNotSupportedForFieldType f a
                |> caseToMsg this

type SearchMessage = 
    | SearchError of ``exception`` : string
    | QueryNotFound of queryName : string
    | InvalidFieldName of fieldName : string
    | StoredFieldCannotBeSearched of fieldName : string
    | MissingFieldValue of fieldName : string
    | FunctionNotFound of context : string
    | FunctionExecutionError of functionName : string * ``exception`` : Exception
    | UnknownMissingVauleOption of fieldName : string
    | SearchProfileUnsupportedFieldValue of fieldName : string
    | DataCannotBeParsed of fieldName : string * expectedDataType : string
    | ExpectingNumericData of fieldName : string
    | QueryOperatorFieldTypeNotSupported of fieldName : string
    | QueryStringParsingError of error : string * queryString : string
    | MethodCallParsingError of error : string
    | UnknownSearchProfile of indexName : string * profileName : string
    | PurelyNegativeQueryNotSupported
    | FieldNamesNotSupportedOutsideSearchProfile of functionName : string * fieldName : string
    | FunctionParamTypeMismatch of functionName : string * expectedType : string * actualType : string
    | NumberOfFunctionParametersMismatch of functionName : string * expected : int * actual : int
    | NotEnoughParameters of functionName : string
    interface IMessage with
        member this.LogProperty() = (MessageKeyword.Search, MessageLevel.Nothing)
        
        member this.OperationMessage() = 
            match this with
            | SearchError(e) -> sprintf "Internal Search error \n%s" e
            | QueryNotFound(q) -> sprintf "Query not found: %s" q
            | InvalidFieldName(f) -> sprintf "Invalid field name: %s" f
            | StoredFieldCannotBeSearched(f) -> sprintf "Stored field cannot be searched: %s" f
            | MissingFieldValue(f) -> sprintf "Missing field value: %s" f
            | FunctionNotFound(f) -> sprintf "Function not found: %s" f
            | FunctionExecutionError(n,e) -> sprintf "Error when executing function %s: %s" n e.Message
            | UnknownMissingVauleOption(f) -> sprintf "Unknown missing field value option: %s" f
            | DataCannotBeParsed(f, e) -> sprintf "Data cannot be parsed for field '%s'. Expected data type %s." f e
            | ExpectingNumericData(f) -> sprintf "Expecting numeric data: %s" f
            | QueryOperatorFieldTypeNotSupported(f) -> 
                sprintf "Query operator field type not supported for field '%s'" f
            | QueryStringParsingError(e,q) -> sprintf "Query string parsing error: \n%s\n\nQuery String:\n%s" e q
            | MethodCallParsingError(e) -> sprintf "Unable to parse the method call: \n%s" e
            | UnknownSearchProfile(i, p) -> sprintf "Unknown search profile '%s' for index '%s'" p i
            | PurelyNegativeQueryNotSupported -> "Purely negative queries (not top query) not supported"
            | SearchProfileUnsupportedFieldValue(fn) -> sprintf "Search Profile does not support array values as an input for field '%s'." fn
            | FieldNamesNotSupportedOutsideSearchProfile(func,fld) -> sprintf "Field names not supported outside Search Profiles. Occured in function %s for field %s" func fld
            | FunctionParamTypeMismatch(fn,e,a) -> sprintf "Function parameter type mismatch for function %s. Expected %s, but got %s." fn e a
            | NumberOfFunctionParametersMismatch(fn,e,a) -> sprintf "Expected %d parameters for function %s, but got %d" e fn a
            | NotEnoughParameters(fn) -> sprintf "Not enough parameters for function %s" fn
            |> caseToMsg this

type IndexingMessage = 
    | IndexAlreadyExists of indexName : string
    | IndexShouldBeOnline of indexName : string
    | IndexIsAlreadyOnline of indexName : string
    | IndexIsAlreadyOffline of indexName : string
    | IndexInOpenState of indexName : string
    | IndexInInvalidState of indexName : string
    | UnableToUpdateIndexStatus of indexName : string * oldStatus : string * newStatus : string
    | ErrorOpeningIndexWriter of indexPath : string * exp : string * data : ResizeArray<KeyValuePair<string, string>>
    | IndexNotFound of indexName : string
    | DocumentIdAlreadyExists of indexName : string * id : string
    | DocumentIdNotFound of indexName : string * id : string
    | IndexingVersionConflict of indexName : string * id : string * existingVersion : string
    interface IMessage with
        member this.LogProperty() = (MessageKeyword.Index, MessageLevel.Info)
        member this.OperationMessage() = 
            match this with
            | IndexAlreadyExists(i) -> sprintf "Index '%s' already exists" i
            | IndexShouldBeOnline(i) -> sprintf "Index '%s' should be online" i
            | IndexIsAlreadyOnline(i) -> sprintf "Index '%s' is already online" i
            | IndexIsAlreadyOffline(i) -> sprintf "Index '%s' is already offline" i
            | IndexInOpenState(i) -> sprintf "Index '%s' is in an open state" i
            | IndexInInvalidState(i) -> sprintf "Index '%s' is in an invalid state" i
            | UnableToUpdateIndexStatus(i, os, ns) -> sprintf "Unable to update the status of the index '%s' from '%s' to '%s'." i os ns
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
        member this.LogProperty() = (MessageKeyword.Node, MessageLevel.Error)
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
        member this.LogProperty() = (MessageKeyword.Node, MessageLevel.Info)
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
        member this.LogProperty() = (MessageKeyword.Node, MessageLevel.Info)
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
        member this.LogProperty() = (MessageKeyword.Index, MessageLevel.Error)
        member this.OperationMessage() = 
            match this with
            | FileNotFound(p) -> sprintf "File not found at address: %s" p
            | FileReadError(f, e) -> sprintf "There was an error reading the file at path '%s': \n%s" f e
            | FileWriteError(f, e) -> sprintf "There was an error writing to the file at path '%s': \n%s" f e
            | PathDoesNotExist(p) -> sprintf "Path does not exist: %s" p
            | StoreUpdateError -> ""
            |> caseToMsg this

type NodeMessage =
    | PluginsLoaded of pluginType: string * plugins : ResizeArray<string>
    | PluginLoadFailure of pluginName : string * pluginType: string * ``exception`` : string
    interface IMessage with
        member this.LogProperty() = 
            match this with
            | PluginsLoaded(_) -> (MessageKeyword.Node, MessageLevel.Info) 
            | PluginLoadFailure(_) -> (MessageKeyword.Node, MessageLevel.Error)

        member this.OperationMessage() = 
            match this with
            | PluginsLoaded(pt, p) -> sprintf "The following Plugins of type: {%s} are loaded successfully. %A" pt p
            | PluginLoadFailure(pn, pt, e) -> sprintf "The Plugin of type: {%s} with name: {%s} was not loaded due to an error. Please refer to the log for more information. /n %s" pt pn e
            |> caseToMsg this

type IndexMessage =
    | IndexLoadingFailure of indexName: string * indexDetails : string * ``exception`` : string
    | TransactionLogReadFailure of path: string * ``exception`` : string
    interface IMessage with
        member this.LogProperty() = 
            match this with
            | IndexLoadingFailure(_) -> (MessageKeyword.Index, MessageLevel.Error) 
            | TransactionLogReadFailure(_) -> (MessageKeyword.Index, MessageLevel.Error)

        member this.OperationMessage() = 
            match this with
            | IndexLoadingFailure(i, id, e) -> sprintf "Unable to load the index: %s" i 
            | TransactionLogReadFailure(p, _) -> sprintf "Unable to read the transaction logs from the path: %s" p
            |> caseToMsg this

// ----------------------------------------------------------------------------
// Logging Section
// ----------------------------------------------------------------------------
[<AutoOpen; RequireQualifiedAccess>]
module Log = 
    open System.Diagnostics
    
    let private logger = FlexSearch.Logging.LogService.GetLogger()
    let logNothing(a, b, c) = ()

    let logMethod (keyword, level) =
        match (keyword, level) with
        | MessageKeyword.Startup, MessageLevel.Critical -> logger.StartupCritical
        | MessageKeyword.Startup, MessageLevel.Error -> logger.StartupError
        | MessageKeyword.Startup, MessageLevel.Warning -> logger.StartupWarning
        | MessageKeyword.Startup, MessageLevel.Info -> logger.StartupInfo
        | MessageKeyword.Startup, MessageLevel.Verbose -> logger.StartupVerbose
        | MessageKeyword.Node, MessageLevel.Critical -> logger.NodeCritical
        | MessageKeyword.Node, MessageLevel.Error -> logger.NodeError
        | MessageKeyword.Node, MessageLevel.Warning -> logger.NodeWarning
        | MessageKeyword.Node, MessageLevel.Info -> logger.NodeInfo
        | MessageKeyword.Node, MessageLevel.Verbose -> logger.NodeVerbose
        | MessageKeyword.Index, MessageLevel.Critical -> logger.IndexCritical
        | MessageKeyword.Index, MessageLevel.Error -> logger.IndexError
        | MessageKeyword.Index, MessageLevel.Warning -> logger.IndexWarning
        | MessageKeyword.Index, MessageLevel.Info -> logger.IndexInfo
        | MessageKeyword.Index, MessageLevel.Verbose -> logger.IndexVerbose
        | MessageKeyword.Search, MessageLevel.Critical -> logger.SearchCritical
        | MessageKeyword.Search, MessageLevel.Error -> logger.SearchError
        | MessageKeyword.Search, MessageLevel.Warning -> logger.SearchWarning
        | MessageKeyword.Search, MessageLevel.Info -> logger.SearchInfo
        | MessageKeyword.Search, MessageLevel.Verbose -> logger.SearchVerbose
        | MessageKeyword.Document, MessageLevel.Critical -> logger.DocumentCritical
        | MessageKeyword.Document, MessageLevel.Error -> logger.DocumentError
        | MessageKeyword.Document, MessageLevel.Warning -> logger.DocumentWarning
        | MessageKeyword.Document, MessageLevel.Info -> logger.DocumentInfo
        | MessageKeyword.Document, MessageLevel.Verbose -> logger.DocumentVerbose
        | MessageKeyword.Default, MessageLevel.Critical -> logger.DefaultCritical
        | MessageKeyword.Default, MessageLevel.Error -> logger.DefaultError
        | MessageKeyword.Default, MessageLevel.Warning -> logger.DefaultWarning
        | MessageKeyword.Default, MessageLevel.Info -> logger.DefaultInfo
        | MessageKeyword.Default, MessageLevel.Verbose -> logger.DefaultVerbose
        | MessageKeyword.Plugin, MessageLevel.Critical -> logger.PluginCritical
        | MessageKeyword.Plugin, MessageLevel.Error -> logger.PluginError
        | MessageKeyword.Plugin, MessageLevel.Warning -> logger.PluginWarning
        | MessageKeyword.Plugin, MessageLevel.Info -> logger.PluginInfo
        | MessageKeyword.Plugin, MessageLevel.Verbose -> logger.PluginVerbose
        | _, MessageLevel.Nothing -> logNothing 
        | _, _ -> logNothing
         
    let log (message: IMessage) =
        match message.LogProperty() with
        | _, MessageLevel.Nothing -> ()
        | keyword, level ->
            let om = message.OperationMessage()
            let properties = om.Properties |> Seq.fold (fun acc v -> acc + sprintf "%A; \r\n" v) ""
            logMethod(keyword, level)(om.ErrorCode, om.Message, properties)

    let logErrorChoice (message : Result<_>) = 
        match message with
        | Fail(error) -> log (error)
        | _ -> ()
        message
       
type Logger() =
    static member Log(msg : IMessage) = log(msg)

    static member Log(message : Result<_>) = logErrorChoice(message)
    
    static member Log(msg: string, keyword : MessageKeyword, level: MessageLevel) =
        logMethod(keyword, level)(String.Empty, msg, String.Empty)
    
    /// This is an general exception logging method. This should only be used in limited cases
    /// where there is no specific message available to log the error.
    static member Log(ex: Exception, keyword : MessageKeyword, level: MessageLevel) =
        logMethod(keyword, level)("Generic", sprintf "%s \n%s" ex.Message (exceptionPrinter ex), String.Empty)

    /// This is an general exception logging method. This should only be used in limited cases
    /// where there is no specific message available to log the error.
    static member Log(msg: string, ex: Exception, keyword : MessageKeyword, level: MessageLevel) =
        logMethod(keyword, level)("Generic", sprintf "%s \n%s" msg (exceptionPrinter ex), String.Empty)

    static member LogR(msg : IMessage) = Logger.Log msg; msg

    static member LogR(msg : 'a :> IMessage) = Logger.LogR (msg :> IMessage)
