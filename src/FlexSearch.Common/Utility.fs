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
namespace FlexSearch.Utility

open Microsoft.FSharp.Core.Printf
open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Reflection
open System.Security.AccessControl
open System.Security.Principal
open System.Text

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
    /// Returns current date time in Flex compatible format
    let inline GetCurrentTimeAsLong() = Int64.Parse(System.DateTime.Now.ToString("yyyyMMddHHmmssfff"))
    
    let inline ParseDate(date : string) = 
        match DateTime.TryParseExact
                  (date, [| "yyyyMMdd"; "yyyyMMddHHmm"; "yyyyMMddHHmmss" |], CultureInfo.InvariantCulture, 
                   DateTimeStyles.None) with
        | true, date -> Choice1Of2(date)
        | _ -> Choice2Of2("UNABLE_TO_PARSE_DATETIME:The specified date time is not in a supported format.")
    
    /// Utility method to load a file into text string
    let LoadFile(filePath : string) = 
        if File.Exists(filePath) = false then failwithf "File does not exist: {0}" filePath
        File.ReadAllText(filePath)
    
    /// Deals with checking if the local admin privileges
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
            else 
                Directory.CreateDirectory(dataPath) |> ignore
                dataPath
    
    [<CompiledNameAttribute("Await")>]
    let await iar = Async.AwaitIAsyncResult iar |> ignore
    
    /// <summary>
    /// Simple exception formatter
    /// Based on : http://sergeytihon.wordpress.com/2013/04/08/f-exception-formatter/
    /// </summary>
    /// <param name="e"></param>
    let ExceptionPrinter(e : Exception) = 
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

    // Debugging related
    let (!>) (message : string) = System.Diagnostics.Debug.WriteLine(message)