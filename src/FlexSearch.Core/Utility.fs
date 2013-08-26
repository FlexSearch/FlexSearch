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
type Result<'T> = Success of 'T | Error of string     


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
            Error(sprintf "internalmessage=No path is specified.")
        else            
            let dataPath = 
                if path.StartsWith(".") then
                    //let mainDirectory = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location))
                    let mainDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                    System.Diagnostics.Trace.WriteLine("Main Directory: " + mainDirectory)
                    //let mainDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
                    let restPath = path.Substring(2)
                    Path.Combine(mainDirectory, restPath)
                else path
            
            if File.Exists(dataPath) then 
                Success(dataPath)        
            else
                Error(sprintf "message=The specified path does not exist.; path=%s" dataPath)
        
            
    let inline AddorUpdate(dict: Dictionary<string,string>, key, value) =
        match dict.ContainsKey(key) with
        | true -> dict.[key] <- value
        | _ -> dict.Add(key, value)
             

// ----------------------------------------------------------------------------
// Contains various general purpose helpers
// ----------------------------------------------------------------------------
[<AutoOpen>]
module DUnionHelpers =
    open Microsoft.FSharp.Reflection

    let toString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString (t:System.Type) (s:string) =
        match FSharpType.GetUnionCases t |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]))
        |_ -> None

// Usage:
// type A = X|Y|Z with
//     member this.toString = toString this
//     static member fromString s = fromString typeof<A> s

// > X.toString;;
// val it : string = "X"

// > A.fromString "X";;
// val it : obj option = Some X

// > A.fromString "W";;
// val it : obj option = None

// > toString X;;
// val it : string = "X"

// > fromString typeof<A> "X";;
// val it : obj option = Some X

