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

open System
open System.IO
open System.Threading

// ----------------------------------------------------------------------------
// A generic result object used by flex to pass around the result. Similar to
// choice 
// ----------------------------------------------------------------------------
type Result<'T> = 
    Success of 'T 
    | Error of  PropertyName : string * ErrorMessage : string * ErrorCode : string     

    type ValidationBuilder() =
      member this.Bind (v,f) =
        match v with
          | Success(x) -> f x
          | Error(x, y, z) -> Error(x, y, z)

      member this.Return v = v

// ----------------------------------------------------------------------------
// Contains various data type validation related functions and active patterns
// ----------------------------------------------------------------------------
[<AutoOpen>]
module DataType =
    
    let (|InvariantEqual|_|) (str:string) arg = 
        if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0
        then Some() else None
                    
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
// Contains various general purpose monads
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Monads =
    
    type BoolConditionBuilder() =
        member x.Bind(v, f) = if v then f() else false
        member x.Return(v) = v  

    type OptionConditionBuilder () =
      member this.Bind (v,f) =
        match v with
          | Some(x) -> f x
          | None -> None

      member this.Return v = v

// ----------------------------------------------------------------------------
// Contains various general purpose helpers
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Helpers =
    open System.Collections.Generic
    open System.Security.Principal
    open System.Security.AccessControl
    open System.Reflection

    // Returns current date time in Flex compatible format
    let inline GetCurrentTimeAsLong() = Int64.Parse(System.DateTime.Now.ToString("yyyyMMddHHmmss"))      

    // Utility method to load a file into text string
    let LoadFile(filePath: string) =
        if File.Exists(filePath) = false then
            failwithf "File does not exist: {0}" filePath
        File.ReadAllText(filePath)
    

    // Deals with checking if the local admin privledges
    let CheckIfAdministrator() =
        let currentUser: WindowsIdentity = WindowsIdentity.GetCurrent()
        if currentUser <> null then
            let wp = new WindowsPrincipal(currentUser)
            wp.IsInRole(WindowsBuiltInRole.Administrator)
        else
            false


    let GenerateAbsolutePath (path: string) =
        if String.IsNullOrWhiteSpace(path) then
            failwith "internalmessage=No path is specified."
        else            
            let dataPath = 
                if path.StartsWith(".") then
                    let mainDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                    let restPath = path.Substring(2)
                    Path.Combine(mainDirectory, restPath)
                else path
            
            if File.Exists(dataPath) then 
                dataPath        
            else
                failwithf "message=The specified path does not exist.; path=%s" dataPath
    

    // Wrapper around dict lookup. Useful for validation in tokenizers and filters
    let inline KeyExists(key, dict: Dictionary<string, string>) =
        match dict.TryGetValue(key) with
        | (true, value) -> value
        | _ -> failwithf "'%s' is required." key
             

    // Helper method to check if the passed key exists in the dictionary and if it does then the
    // specified value is in the enum list
    let inline ValidateIsInList(key ,param : Dictionary<string, string>, enumValues :HashSet<string>) =
        let value = KeyExists(key, param)
        match enumValues.Contains(value) with
        | true -> value
        | _ -> failwithf "'%s' is not a valid value for '%s'." value key


    let inline ParseValueAsInteger(key ,param : Dictionary<string, string>) =
        let value = KeyExists(key, param)
        match Int32.TryParse(value) with
        | (true, value) -> value
        | _ -> failwithf "%s should be of integer type." key
         

    let inline AddorUpdate(dict: Dictionary<string,string>, key, value) =
        match dict.ContainsKey(key) with
        | true -> dict.[key] <- value
        | _ -> dict.Add(key, value)
             

