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

[<RequireQualifiedAccess>]
[<AutoOpen>]
module Logger = 
    open FlexSearch.Api
    open FlexSearch.Api.Message
    open Gibraltar.Agent
    open System
    
    let AddIndex(indexName : string, indexDetails : Index) = 
        Log.TraceInformation("Adding index {0}. IndexDetails:{1}", indexName, indexDetails.ToString())
    let UpdateIndex(indexName : string, indexDetails : Index) = 
        Log.TraceInformation("Updating index {0}. IndexDetails:{1}", indexName, indexDetails.ToString())
    let IndexValidationFailed(indexName : string, indexDetails : Index, validationObject : OperationMessage) = 
        Log.TraceError
            ("Updating index {0}. IndexDetails:{1}. Message:{2}", indexName, indexDetails.ToString(), 
             validationObject.ToString())
    let DeleteIndex(indexName : string) = Log.TraceInformation("Deleting index {0}.", indexName)
    let CloseIndex(indexName : string) = Log.TraceInformation("Closing index {0}.", indexName)
    let OpenIndex(indexName : string) = Log.TraceInformation("Opening index {0}.", indexName)
    let MefComponentLoaded(name : string, componentType : string) = 
        Log.TraceVerbose("Component:{0} Type:{1} loaded.", name, componentType)
    let StartSession() = 
        Log.StartSession
            ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() 
             + " started.")
    let EndSession() = 
        Log.EndSession
            ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " ended.")
    let Shutdown() = Log.TraceInformation("Shutdown request received.")
    let TraceCritical(ex : Exception) = Log.TraceCritical(ex, "Critical application failure happened. {0}", ex.Message)
    let TraceError(error : string, ex : Exception) = Log.TraceError(error + " Exception:", ex)
    let TraceErrorMessage(error : string) = Log.TraceError(error)
    let TraceOperationMessageError(error : string, ex : OperationMessage) = Log.TraceError(error + " Exception:", ex)
