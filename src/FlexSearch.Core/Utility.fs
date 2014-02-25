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
// ----------------------------------------------------------------------------
namespace FlexSearch.Utility

// ----------------------------------------------------------------------------
open FlexSearch.Api.Message
open System
open System.IO
open System.Threading

// ----------------------------------------------------------------------------
// A generic result object used by flex to pass around the result. Similar to
// choice 
// ----------------------------------------------------------------------------
type Result<'T> = 
    | Success of 'T
    | Error of MessageConstants

type ValidationBuilder() = 
    
    member this.Bind(v, f) = 
        match v with
        | Choice1Of2(x) -> f x
        | Choice2Of2(s) -> Choice2Of2(s)
    
    member this.ReturnFrom v = v
    member this.Return v = Choice1Of2(v)
    member this.Zero() = Choice1Of2()
    member this.Combine(a, b) = a
    member this.Delay(f) = f()

//    member this.Run(f) = f()

//    member this.TryWith(m, h) =
//        try this.ReturnFrom(m)
//        with e -> h e
//
//    member this.TryFinally(m, compensation) =
//        try this.ReturnFrom(m)
//        finally compensation()
//
//    member this.Using(res:#IDisposable, body) =
//        this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())
//    
//    member this.While(guard, f) =
//        if not (guard()) then this.Zero() else
//        this.Bind(f(), fun _ -> this.While(guard, f))
//
//    member this.For(sequence:seq<_>, body) =
//        this.Using(sequence.GetEnumerator(),
//                        fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

// ----------------------------------------------------------------------------
// Contains various data type validation related functions and active patterns
// ----------------------------------------------------------------------------
[<AutoOpen>]
module DataType = 
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

// ----------------------------------------------------------------------------
// Contains various general purpose helpers
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Helpers = 
    open System.Collections.Generic
    open System.Reflection
    open System.Security.AccessControl
    open System.Security.Principal
    
    let maybe = new ValidationBuilder()

    let loopValidation (list: 'T list) f =
        let rec loop (list: 'T list) f =
            match list with
            | head :: tail -> 
                match f (head) with
                | Choice1Of2(_) -> 
                    loop tail f
                | Choice2Of2(e) -> Choice2Of2(e)
            | [] -> Choice1Of2()
        loop list f

    let inline getValue (dictionary: Dictionary<string, 'T>) key (error : OperationMessage) =
        match  dictionary.TryGetValue(key) with
        | (true, x) -> Choice1Of2(x)
        | _ -> Choice2Of2(error)

    // Returns current date time in Flex compatible format
    let inline GetCurrentTimeAsLong() = Int64.Parse(System.DateTime.Now.ToString("yyyyMMddHHmmss"))
    
    // Utility method to load a file into text string
    let LoadFile(filePath : string) = 
        if File.Exists(filePath) = false then failwithf "File does not exist: {0}" filePath
        File.ReadAllText(filePath)
    
    // Deals with checking if the local admin privledges
    let CheckIfAdministrator() = 
        let currentUser : WindowsIdentity = WindowsIdentity.GetCurrent()
        if currentUser <> null then 
            let wp = new WindowsPrincipal(currentUser)
            wp.IsInRole(WindowsBuiltInRole.Administrator)
        else false
    
    let GenerateAbsolutePath(path : string) = 
        if String.IsNullOrWhiteSpace(path) then failwith "internalmessage=No path is specified."
        else 
            let dataPath = 
                if path.StartsWith(".") then 
                    let mainDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                    let restPath = path.Substring(2)
                    Path.Combine(mainDirectory, restPath)
                else path
            if Directory.Exists(dataPath) || File.Exists(dataPath) then dataPath
            else failwithf "message=The specified path does not exist.; path=%s" dataPath
    
    // Wrapper around dict lookup. Useful for validation in tokenizers and filters
    let inline KeyExists(key, dict : Dictionary<string, string>) = 
        match dict.TryGetValue(key) with
        | (true, value) -> value
        | _ -> failwithf "'%s' is required." key
    
    // Helper method to check if the passed key exists in the dictionary and if it does then the
    // specified value is in the enum list
    let inline ValidateIsInList(key, param : Dictionary<string, string>, enumValues : HashSet<string>) = 
        let value = KeyExists(key, param)
        match enumValues.Contains(value) with
        | true -> value
        | _ -> failwithf "'%s' is not a valid value for '%s'." value key
    
    let inline ParseValueAsInteger(key, param : Dictionary<string, string>) = 
        let value = KeyExists(key, param)
        match Int32.TryParse(value) with
        | (true, value) -> value
        | _ -> failwithf "%s should be of integer type." key
    
    let inline AddorUpdate(dict : Dictionary<string, string>, key, value) = 
        match dict.ContainsKey(key) with
        | true -> dict.[key] <- value
        | _ -> dict.Add(key, value)
    
    let await iar = Async.AwaitIAsyncResult iar |> ignore
