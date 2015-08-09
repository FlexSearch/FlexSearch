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

open FlexLucene.Analysis.Custom
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Diagnostics
open System.IO
open System.Linq
open System.Threading

[<AutoOpen>]
[<RequireQualifiedAccess>]
/// Contains all the flex constants which do not change per instance
module Constants = 
    
    [<Literal>]
    let BloomFilter = "<bloom>"

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
    
    /// Default script folder
    let ScriptFolder = ConfFolder +/ "Scripts" |> createDir
    
    /// Flex plug-in folder
    let PluginFolder = rootFolder +/ "Plugins" |> createDir
    
    /// Flex logs folder
    let LogsFolder = rootFolder +/ "Logs" |> createDir
    
    /// Flex web files folder
    let WebFolder = rootFolder +/ "Web" |> createDir
    
    /// Resources folder to be used for saving analyzer resource files
    let ResourcesFolder = ConfFolder +/ "Resources" |> createDir
    
    /// Extension to be used by settings file
    let SettingsFileExtension = ".json"
    
    let CaseInsensitiveKeywordAnalyzer = 
        CustomAnalyzer.Builder().withTokenizer("keyword").addTokenFilter("lowercase").build() :> FlexLucene.Analysis.Analyzer

[<AutoOpen>]
module DateTimeHelpers = 
    open System.Globalization
    
    /// Internal format used to represent dates 
    let DateTimeFormat = "yyyyMMddHHmmssfff"
    
    /// Represents all the date time format supported by FlexSearch
    let SupportedDateFormat = [| "yyyyMMdd"; "yyyyMMddHHmm"; "yyyyMMddHHmmss"; DateTimeFormat |]
    
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

/// <summary>
/// Represents the lookup name for the plug-in
/// </summary>
[<MetadataAttribute>]
[<Sealed>]
type NameAttribute(name : string) = 
    inherit Attribute()
    member this.Name = name

/// Represents a class attribute that marks the class as internal, meaning
/// it's not exposed to the documentation.
[<MetadataAttribute>]
[<Sealed>]
type InternalAttribute() = inherit Attribute()

/// Implements the Freezable pattern
[<InterfaceAttribute>]
type IFreezable = 
    abstract Freeze : unit -> unit

/// To be used by all Dto's which are used in REST webservices
[<AbstractClassAttribute>]
type DtoBase() = 
    let mutable isFrozen = false
    abstract Validate : unit -> Result<unit>
    interface IFreezable with
        member __.Freeze() = isFrozen <- true

[<AutoOpen>]
module Operators = 
    open System.Collections
        
    /// Wraps a value in a Success
    let inline ok<'a> (x : 'a) = Ok(x)
    
    /// Wraps a message in a Failure
    let inline fail (msg : 'b) =
        Fail (msg :> IMessage)

    /// Wrap a unit value in success
    let okUnit = Ok()

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
        
    /// Returns true if the result was not successful.
    let inline failed result = 
        match result with
        | Fail _ -> true
        | _ -> false
    
    /// Returns true if the result was successful.
    let inline succeeded result = not <| failed result
    
    /// Takes a Result and maps it with fSuccess if it is a Success otherwise it maps it with fFailure.
    let inline either fSuccess fFailure trialResult = 
        match trialResult with
        | Ok(x) -> fSuccess (x)
        | Fail(msgs) -> fFailure (msgs)
    
    /// If the given result is a Success the wrapped value will be returned. 
    ///Otherwise the function throws an exception with Failure message of the result.
    let inline returnOrFail result = 
        match result with
        | Ok(x) -> x
        | Fail(err) -> raise (ValidationException(err))
    
    /// Takes a bool value and returns ok for sucess and predefined error
    /// for failure
    let inline boolToResult err result = 
        match result with
        | true -> ok()
        | false -> fail (err)
    
    /// Take a Choice result and return true for Choice1 and false for Choice2
    let inline resultToBool result = 
        match result with
        | Ok(_) -> true
        | _ -> false
        
    /// If the result is a Success it executes the given function on the value.
    /// Otherwise the exisiting failure is propagated.
    let inline bind f result = 
        let inline fSuccess (x) = f x
        let inline fFailure (msg) = fail msg
        either fSuccess fFailure result
    
    /// If the result is a Success it executes the given function on the value. 
    /// Otherwise the exisiting failure is propagated.
    /// This is the infix operator version of ErrorHandling.bind
    let inline (>>=) result f = bind f result
    
    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    let inline apply wrappedFunction result = 
        match wrappedFunction, result with
        | Ok f, Ok x -> ok(f x)
        | Fail err, Ok _ -> fail(err)
        | Ok _, Fail err -> fail(err)
        | Fail err1, Fail err2 -> fail(err1)
    
    let inline extract result = 
        match result with
        | Ok(a) -> a
        | Fail(e) -> failwithf "%s" (e.ToString())
    
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
        
        [<DebuggerStepThrough>]
        member inline __.Bind(v, f) = 
            match v with
            | Ok(x) -> f x
            | Fail(s) -> fail s
        
        [<DebuggerStepThrough>]
        member inline __.ReturnFrom v = v
        
        [<DebuggerStepThrough>]
        member inline __.Return v = ok v
        
        [<DebuggerStepThrough>]
        member inline __.Zero() = ok ()
        
        [<DebuggerStepThrough>]
        member inline __.Combine(a, b) = 
            match a, b with
            | Ok a', Ok b' -> ok b'
            | Fail a', Ok b' -> fail a'
            | Ok a', Fail b' -> fail b'
            | Fail a', Fail b' -> fail a'
        
        [<DebuggerStepThrough>]
        member inline __.Delay(f) = f()
        
        [<DebuggerStepThrough>]
        member inline this.TryFinally(body, compensation) = 
            try 
                this.ReturnFrom(body())
            finally
                compensation()
        
        [<DebuggerStepThrough>]
        member inline __.TryWith(expr, handler) = 
            try 
                expr()
            with ex -> handler ex
        
        [<DebuggerStepThrough>]
        member inline this.For(collection : seq<_>, func) = 
            // The whileLoop operator
            let rec whileLoop pred body = 
                if pred() then this.Bind(body(), (fun _ -> whileLoop pred body))
                else this.Zero()
            using (collection.GetEnumerator()) 
                (fun it -> whileLoop (fun () -> it.MoveNext()) (fun () -> it.Current |> func))
        
        [<DebuggerStepThrough>]
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
        else fail <| DuplicateFieldValue(groupName, fieldName)
    
    /// Checks if a given value is greater than the lower limit
    let gt fieldName lowerLimit input = 
        if input > lowerLimit then ok()
        else fail <| GreaterThan(fieldName, lowerLimit.ToString(), input.ToString())
    
    /// Checks if the passed value is greater than or equal to the lower limit
    let gte fieldName lowerLimit input = 
        if input >= lowerLimit then ok()
        else fail <| GreaterThanEqual(fieldName, lowerLimit.ToString(), input.ToString())
    
    /// Checks if a given value is less than the upper limit
    let lessThan fieldName upperLimit input = 
        if input < upperLimit then ok()
        else fail <| LessThan(fieldName, upperLimit.ToString(), input.ToString())
    
    /// Checks if the passed value is less than or equal to the upper limit
    let lessThanEqual fieldName upperLimit input = 
        if input <= upperLimit then ok()
        else fail <| LessThanEqual(fieldName, upperLimit.ToString(), input.ToString())
    
    /// Checks if the given string is null or empty
    let notBlank fieldName input = 
        if not (String.IsNullOrWhiteSpace(input)) then ok()
        else fail <| NotBlank(fieldName)
    
    /// Checks if a given value satisfies the provided regex expression 
    let regexMatch fieldName regexExpr input = 
        let m = System.Text.RegularExpressions.Regex.Match(input, regexExpr)
        if m.Success then ok()
        else fail <| RegexMatch(fieldName, regexExpr)
    
    /// Validates if the property name satisfies the naming rules
    let propertyNameRegex fieldName input = 
        match input |> regexMatch fieldName "^[a-z0-9_]*$" with
        | Ok(_) -> ok()
        | Fail(_) -> fail <| InvalidPropertyName(fieldName, input)
    
    /// Checks if the property name is not in the restricted field names
    let invalidPropertyName fieldName input = 
        if String.Equals(input, Constants.IdField) || String.Equals(input, Constants.LastModifiedField) then 
            fail <| InvalidPropertyName(fieldName, input)
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
        | true, v -> ok(v)
        | _ -> fail <| error (value)
    
    let inline keyExists2 (value, error) (dict : IReadOnlyDictionary<string, _>) = 
        match dict.TryGetValue(value) with
        | true, v -> ok(v)
        | _ -> fail <| error (value)
    
    let inline tryGet (key) (dict : IDictionary<string, _>) = 
        match dict.TryGetValue(key) with
        | true, v -> ok(v)
        | _ -> fail <| KeyNotFound(key)
    
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

/// Represents a thread-safe file writer that can be accessed by 
/// multiple threads concurrently.
/// Note : This is not meant to be used for huge files and should 
/// be used for writing configuration files.
[<Sealed>]
type ThreadSafeFileWriter() = 
    let options = new JsonSerializerSettings()
    do 
        options.Converters.Add(new StringEnumConverter())

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
                let response = JsonConvert.DeserializeObject<'T>(File.ReadAllText(path), options)
                ok <| response
            with e -> fail <| FileReadError(filePath, exceptionPrinter e)
        else fail <| FileNotFound(filePath)
    
    member __.WriteFile<'T>(filePath, content : 'T) = 
        let path = getPathWithExtension (filePath)
        use mutex = new Mutex(true, path.Replace("\\", ""))
        Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
        try 
            mutex.WaitOne(-1) |> ignore
            File.WriteAllText(path, JsonConvert.SerializeObject(content, Formatting.Indented, options))
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
        | Ok(item) -> 
            tryAdd (key, item) |> ignore
            ok()
        | Fail(error) -> fail <| error

type AtomicLong(value : int64) = 
    let refCell = ref value
    
    member __.Value 
        with get () = Interlocked.Read(refCell)
        and set (value) = Interlocked.Exchange(refCell, value) |> ignore
    
    /// Increments the ref cell and returns the incremented value
    member __.Increment() = Interlocked.Increment(refCell)
    
    /// Decrements the ref cell and returns the decremented value
    member __.Decrement() = Interlocked.Decrement(refCell)
    
    /// Reset the ref cell to initial value
    member __.Reset() = Interlocked.Exchange(refCell, value) |> ignore
    
    static member Create() = new AtomicLong(0L)
    static member Create(value) = new AtomicLong(value)