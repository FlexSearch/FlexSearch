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
namespace FlexSearch.Core

open FlexSearch
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Linq

/// <summary>
/// Default console logger to be used when no other logger is found
/// </summary>
[<Name("console")>]
[<Sealed>]
type ConsoleLogService() = 
    interface ILogService with
        member x.AddIndex(indexName : string, indexDetails : Index) : unit = 
            Console.WriteLine("Adding index {0}. IndexDetails:{1}", indexName, indexDetails.ToString())
        member x.CloseIndex(indexName : string) : unit = Console.WriteLine("Closing index {0}.", indexName)
        member x.ComponentLoaded(name : string, componentType : string) : unit = 
            Console.WriteLine("Component:{0} Type:{1} loaded.", name, componentType)
        member x.DeleteIndex(indexName : string) : unit = Console.WriteLine("Deleting index {0}.", indexName)
        member x.EndSession() : unit = 
            Console.WriteLine
                ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() 
                 + " ended.")
        member x.IndexValidationFailed(indexName : string, indexDetails : Index, 
                                       validationObject : Message.OperationMessage) : unit = 
            Console.WriteLine
                ("Updating index {0}. IndexDetails:{1}. Message:{2}", indexName, indexDetails.ToString(), 
                 validationObject.ToString())
        member x.OpenIndex(indexName : string) : unit = Console.WriteLine("Opening index {0}.", indexName)
        member x.Shutdown() : unit = Console.WriteLine("Shutdown request received.")
        member x.StartSession() : unit = 
            Console.WriteLine
                ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() 
                 + " started.")
        member x.TraceCritical(ex : System.Exception) : unit = 
            Console.WriteLine("Critical application failure happened. {0}", ex.Message)
        member x.TraceError(error : string, ex : System.Exception) : unit = Console.WriteLine(error + " Exception:", ex)
        member x.TraceErrorMessage(error : string) : unit = Console.WriteLine(error)
        member x.TraceOperationMessageError(error : string, ex : Message.OperationMessage) : unit = 
            Console.WriteLine(error + " Exception:", ex.ToString())
        member x.UpdateIndex(indexName : string, indexDetails : Index) : unit = 
            Console.WriteLine("Updating index {0}. IndexDetails:{1}", indexName, indexDetails.ToString())
