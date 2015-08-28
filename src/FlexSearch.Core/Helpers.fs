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

open Microsoft.FSharp.Core.Printf
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Dynamic
open System.Globalization
open System.IO
open System.Reflection
open System.Security.AccessControl
open System.Security.Principal
open System.Text
open System.Threading
open Newtonsoft.Json

[<Sealed>]
/// <summary>
/// Dynamic dictionary to allow easier code when using scripting
/// </summary>
type DynamicDictionary(source : Dictionary<string, string>) = 
    inherit DynamicObject()
    
    /// <summary>
    /// The method which is called when we try to access a value from the dictionary
    /// </summary>
    /// <param name="binder"></param>
    /// <param name="result"></param>
    override this.TryGetMember(binder : GetMemberBinder, result) = 
        // Converting the property name to lowercase 
        // so that property names become case-insensitive. 
        // Set the result and make sure it never throws
        result <- match source.TryGetValue(binder.Name.ToLowerInvariant()) with
                  | true, value -> value
                  | _ -> ""
        true
    
    /// <summary>
    /// The method which is called when we try to set an explicit value
    /// </summary>
    /// <param name="binder"></param>
    /// <param name="result"></param>
    override this.TrySetMember(binder : SetMemberBinder, result) = 
        failwithf "Dynamic dictionary does not support explicit setting of variables"

[<RequireQualifiedAccessAttribute>]
module Args = 
    let inline enum<'T when 'T :> Enum> (defValue : 'T) (arg : 'T) = 
        if arg.ToString() = "Undefined" then defValue
        else arg
    
    let inline getBool (arg : Nullable<bool>) = 
        if arg.HasValue then arg.Value
        else false
    
    let inline bool (defValue : Nullable<bool>) (arg : Nullable<bool>) = 
        if arg.HasValue then arg
        else defValue
    
    let inline int defValue arg = 
        if arg = 0 then defValue
        else arg
    
    let inline string defValue arg = 
        if String.IsNullOrWhiteSpace(arg) then defValue
        else arg
    
    let inline isNull defValue arg = 
        if obj.ReferenceEquals(arg, Unchecked.defaultof<_>) then defValue
        else arg

[<AutoOpenAttribute>]
module DiscriminatedUnion = 
    open Microsoft.FSharp.Reflection
    
    let toString (x : 'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name
    
    let fromString<'a> (s : string) = 
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        | [| case |] -> Some(FSharpValue.MakeUnion(case, [||]) :?> 'a)
        | _ -> None
    
    /// Generate a hash set containing all the discriminated union case names
    let caseNameToHashSet<'T>() = 
        let hashMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        FSharpType.GetUnionCases typeof<'T> |> Array.iter (fun x -> hashMap.Add(x.Name) |> ignore)
        hashMap

[<AutoOpenAttribute>]
module Helpers = 
    open Microsoft.FSharp.Core.Printf
    open Microsoft.FSharp.Reflection
    open System.Collections.Generic
    
    let (+/) (path1 : string) (path2 : string) = Path.Combine([| path1; path2 |])
    let loopDir (dir : string) = Directory.EnumerateDirectories(dir)
    let loopFiles (dir : string) = Directory.EnumerateFiles(dir)
    let createDir (dir : string) = Directory.CreateDirectory(dir) |> ignore
    
    let emptyDir (path) = 
        loopDir path |> Seq.iter (loopFiles >> Seq.iter File.Delete)
        
    let delDir (path) = 
        emptyDir path
        Directory.Delete(path, true)
    
    /// Check for null
    let inline isNull (x : ^a when ^a : not struct) = obj.ReferenceEquals(x, Unchecked.defaultof<_>)
    
    let castAs<'T when 'T : null> (o : obj) = 
        match o with
        | :? 'T as res -> res
        | _ -> null

    /// Check if not null
    let inline notNull (x : ^a when ^a : not struct) = not (obj.ReferenceEquals(x, Unchecked.defaultof<_>))
    
    let inline isBlank (x : string) = String.IsNullOrWhiteSpace x
    let inline isNotBlank (x : string) = not (isBlank x)
    
    let inline throwIfNull (name) (x) = 
        if isNull (x) then failwithf "Internal Error: %s object cannot be null." name
    
    /// Returns the string value between starting and ending characters
    let inline between (startingChar : char) (endingChar : char) (input : string) =
        let startingPos = input.IndexOf(startingChar) + 1
        let endingPos = input.IndexOf(endingChar)
        if startingPos = -1 || endingPos = -1 || startingPos >= endingPos then
            String.Empty
        else
            input.Substring(startingPos, endingPos - startingPos)

    /// Returns the string after a given character
    let inline after (startingChar : char) (input: string) =
        let startingPos = input.IndexOf(startingChar) + 1
        if startingPos = -1 then
            String.Empty
        else
            input.Substring(startingPos)

    /// Simple exception formatter
    /// Based on : http://sergeytihon.wordpress.com/2013/04/08/f-exception-formatter/
    [<CompiledNameAttribute("ExceptionPrinter")>]
    let exceptionPrinter (e : Exception) = 
        let sb = StringBuilder()
        let delimeter = String.replicate 50 "*"
        let nl = Environment.NewLine
        
        let rec printException (e : Exception) count = 
            if (e :? TargetException && e.InnerException <> null) then printException (e.InnerException) count
            else 
                if (count = 1) then bprintf sb "%s%s%s" e.Message nl delimeter
                else bprintf sb "%s%s%d)%s%s%s" nl nl count e.Message nl delimeter
                bprintf sb "%sType: %s" nl (e.GetType().FullName)
                // Loop through the public properties of the exception object
                // and record their values.
                e.GetType().GetProperties() |> Array.iter (fun p -> 
                                                   // Do not log information for the InnerException or StackTrace.
                                                   // This information is captured later in the process.
                                                   if (p.Name <> "InnerException" && p.Name <> "StackTrace" 
                                                       && p.Name <> "Message" && p.Name <> "Data") then 
                                                       try 
                                                           let value = p.GetValue(e, null)
                                                           if (value <> null) then 
                                                               bprintf sb "%s%s: %s" nl p.Name (value.ToString())
                                                       with e2 -> bprintf sb "%s%s: %s" nl p.Name e2.Message)
                if (e.StackTrace <> null) then 
                    bprintf sb "%s%sStackTrace%s%s%s" nl nl nl delimeter nl
                    bprintf sb "%s%s" nl e.StackTrace
                if (e.InnerException <> null) then printException e.InnerException (count + 1)
        printException e 1
        sb.ToString()
    
    /// Utility method to load a file into text string
    let loadFile(filePath : string) = 
        if not <| File.Exists(filePath) then failwithf "File does not exist: %s" filePath
        File.ReadAllText(filePath)
    
    /// Deals with checking if the local admin privileges
    let isAdministrator() = 
        (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator)
    
    /// Generates an absolute path for a given relative path
    let generateAbsolutePath(path : string) = 
        if String.IsNullOrWhiteSpace(path) then failwith "internalmessage=No path is specified."
        else 
            let dataPath = 
                if path.StartsWith(".") then 
                    let mainDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                    let restPath = path.Substring(2)
                    Path.Combine(mainDirectory, restPath)
                else path
            if Directory.Exists(dataPath) || File.Exists(dataPath) then dataPath
            else 
                Directory.CreateDirectory(dataPath) |> ignore
                dataPath
    
    [<CompiledNameAttribute("Await")>]
    let await iar = Async.AwaitIAsyncResult iar |> ignore

    // Returns the size on disk of a folder by summing up the file sizes
    let getFolderSize (path : string) =
        let rec getFolderSizeRec sum (dir : DirectoryInfo) =
            dir.EnumerateFiles() 
            |> Seq.map (fun f -> f.Length)
            |> Seq.sum
            |> (+) (dir.EnumerateDirectories() |> Seq.sumBy (getFolderSizeRec sum))
        getFolderSizeRec 0 (new DirectoryInfo(path))
   
// ----------------------------------------------------------------------------
// Contains various data type validation related functions and active patterns
// ----------------------------------------------------------------------------
[<AutoOpen>]
module DataType = 
    open Microsoft.Owin
    
    let (|InvariantEqual|_|) (str : string) arg = 
        if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some()
        else None
    
    let (|DateTime|_|) str = 
        match DateTime.TryParse str with
        | true, dt -> Some(dt)
        | _ -> None
    
    let (|Int|_|) str = 
        match Int32.TryParse str with
        | true, num -> Some(num)
        | _ -> None
    
    let (|Float|_|) str = 
        match Double.TryParse str with
        | true, num -> Some(num)
        | _ -> None
    
    let (|Int64|_|) str = 
        match Int64.TryParse str with
        | true, num -> Some(num)
        | _ -> None
    
    let (|Bool|_|) str = 
        match Boolean.TryParse str with
        | true, num -> Some(num)
        | _ -> None
    
    let (|String|_|) str = 
        match String.IsNullOrWhiteSpace str with
        | true -> Some(str)
        | _ -> None
        
    let inline pBool (failureDefault) (value : string) = 
        if isNull value then
            failureDefault
        else
            match Boolean.TryParse(value) with
            | true, a -> a
            | _ -> failureDefault
    
    let inline pLong (failureDefault) (value : string) = 
        if isNull value then
            failureDefault
        else
            match Int64.TryParse(value) with
            | true, a -> a
            | _ -> failureDefault
    
    let inline pInt (failureDefault) (value : string) = 
        if isNull value then
            failureDefault
        else
            match Int32.TryParse(value) with
            | true, a -> a
            | _ -> failureDefault
    
    let inline pDouble (failureDefault) (value : string) = 
        if isNull value then
            failureDefault
        else
            match Double.TryParse(value) with
            | true, a -> a
            | _ -> failureDefault
    
    /// Get a value from a dictionary and perform a parsing operation. In case the
    /// operation fails or there is any other error it returns the default value
    let inline getFromDict<'T> key (existsCase : 'T -> string -> 'T) (defaultValue : 'T) 
               (dict : Dictionary<string, string>) = 
        if dict.Count = 0 then defaultValue
        else 
            match dict.TryGetValue(key) with
            | true, value -> value |> existsCase defaultValue
            | _ -> defaultValue
    
    /// Get a value from a dictionary and perform a parsing operation. In case the
    /// operation fails or there is any other error it returns the default value
    let inline getFromOptDict<'T> key (existsCase : 'T -> string -> 'T) (defaultValue : 'T) 
               (dict : Dictionary<string, string> option) = 
        match dict with
        | Some(dict) -> dict |> getFromDict key existsCase defaultValue
        | None -> defaultValue
    
    /// Get a value from a readonly collection and perform a parsing operation. In case the
    /// operation fails or there is any other error it returns the default value
    let inline getFromCollection<'T> key (existsCase : 'T -> string -> 'T) (defaultValue : 'T) 
               (coll : IReadableStringCollection) = 
        match coll.Get key with
        | null -> defaultValue
        | value -> value |> existsCase defaultValue
    
    /// Get string from string collection
    let inline stringFromQueryString key defaultValue (owin : IOwinContext) =
        match owin.Request.Query.Get key with
        | null -> defaultValue
        | value -> value

    /// Get integer from dictionary
    let inline intFromDict key defaultValue (dict : Dictionary<string, string>) = 
        dict |> getFromDict key pInt defaultValue
    
    /// Get integer from optional dictionary
    let inline intFromOptDict key defaultValue (dict : Dictionary<string, string> option) = 
        dict |> getFromOptDict key pInt defaultValue
    
    /// Get integer from string collection
    let inline intFromQueryString key defaultValue (owin : IOwinContext) = 
        owin.Request.Query |> getFromCollection key pInt defaultValue
    
    /// Get long from dictionary
    let inline longFromDict key defaultValue (dict : Dictionary<string, string>) = 
        dict |> getFromDict key pLong defaultValue
    
    /// Get long from optional dictionary
    let inline longFromOptDict key defaultValue (dict : Dictionary<string, string> option) = 
        dict |> getFromOptDict key pLong defaultValue
    
    /// Get long from string collection
    let inline longFromQueryString key defaultValue (owin : IOwinContext) = 
        owin.Request.Query |> getFromCollection key pLong defaultValue
    
    /// Get double from dictionary
    let inline doubleFromDict key defaultValue (dict : Dictionary<string, string>) = 
        dict |> getFromDict key pDouble defaultValue
    
    /// Get double from optional dictionary
    let inline doubleFromOptDict key defaultValue (dict : Dictionary<string, string> option) = 
        dict |> getFromOptDict key pDouble defaultValue
    
    /// Get double from string collection
    let inline doubleFromQueryString key defaultValue (owin : IOwinContext) = 
        owin.Request.Query |> getFromCollection key pDouble defaultValue
    
    /// Get bool from dictionary
    let inline boolFromDict key defaultValue (dict : Dictionary<string, string>) = 
        dict |> getFromDict key pBool defaultValue
    
    /// Get integer from optional dictionary
    let inline boolFromOptDict key defaultValue (dict : Dictionary<string, string> option) = 
        dict |> getFromOptDict key pBool defaultValue
    
    /// Get integer from string collection
    let inline boolFromQueryString key defaultValue (owin : IOwinContext) = 
        owin.Request.Query |> getFromCollection key pBool defaultValue

// ----------------------------------------------------------------------------
// Formatter section : All the various media formatter to be used in 
// Flexsearch
// ----------------------------------------------------------------------------
/// Formatter interface for supporting multiple formats in the HTTP engine
type IFormatter = 
    abstract SupportedHeaders : string []
    abstract Serialize : body:obj * stream:Stream -> unit
    abstract SerializeToString : body:obj -> string
    abstract DeSerialize<'T> : stream:Stream -> 'T

[<Sealed>]
type NewtonsoftJsonFormatter() = 
    let options = new Newtonsoft.Json.JsonSerializerSettings()
    let serializer = JsonSerializer.Create()
    do 
        serializer.Converters.Add(new StringEnumConverter())
        options.Converters.Add(new StringEnumConverter())
    interface IFormatter with
        member __.SerializeToString(body : obj) = JsonConvert.SerializeObject(body, options)
        
        member __.DeSerialize<'T>(stream : Stream) = 
            use reader = new StreamReader(stream)
            use jsonTextReader = new JsonTextReader(reader)
            serializer.Deserialize<'T>(jsonTextReader)
            
        member __.Serialize(body : obj, stream : Stream) : unit = 
            use writer = new StreamWriter(stream)
            use jsonWriter = new JsonTextWriter(writer)
            serializer.Serialize(writer, body)
        
        member __.SupportedHeaders = 
            [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8"; 
               "application/javascript" |]

[<Sealed>]
type ProtoBufferFormatter() = 
    let serialize (body : obj, stream : Stream) = ProtoBuf.Serializer.Serialize(stream, body)
    interface IFormatter with
        member __.SerializeToString(_ : obj) = failwith "Not implemented yet"
        member __.DeSerialize<'T>(stream : Stream) = ProtoBuf.Serializer.Deserialize<'T>(stream)
        member __.Serialize(body : obj, stream : Stream) : unit = serialize (body, stream)
        member __.SupportedHeaders = [| "application/x-protobuf"; "application/octet-stream" |]

[<Sealed>]
type YamlFormatter() = 
    let options = YamlDotNet.Serialization.SerializationOptions.EmitDefaults
    let serializer = new YamlDotNet.Serialization.Serializer(options)
    let deserializer = new YamlDotNet.Serialization.Deserializer(ignoreUnmatched = true)
    
    let serialize (body : obj, stream : Stream) = 
        use textWriter = new StreamWriter(stream)
        serializer.Serialize(textWriter, body)
    
    interface IFormatter with
        
        member __.SerializeToString(body : obj) = 
            use textWriter = new StringWriter()
            serializer.Serialize(textWriter, body)
            textWriter.ToString()
        
        member __.DeSerialize<'T>(stream : Stream) = 
            use textReader = new StreamReader(stream)
            deserializer.Deserialize<'T>(textReader)
        
        member __.Serialize(body : obj, stream : Stream) : unit = serialize (body, stream)
        member __.SupportedHeaders = [| "application/yaml" |]

[<AutoOpenAttribute>]
module Debug = 
    open System.Diagnostics
    
    let inline (!>) msg = Printf.kprintf Debug.WriteLine msg
    //let inline fail msg = Printf.kprintf Debug.Fail msg

/// Attribute used on types that are not needed in the Documentation
[<AttributeUsage(AttributeTargets.Class)>]
type NotForDocumentation() =
    inherit Attribute()