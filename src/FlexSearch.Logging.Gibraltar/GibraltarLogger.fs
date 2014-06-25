namespace FlexSearch.Logging.Gibraltar

open FlexSearch.Api
open FlexSearch.Core
open Gibraltar.Agent

/// <summary>
/// Implementation of Gibraltar logger for FlexSearch
/// </summary>
[<Sealed>]
[<NameAttribute("Gibraltar")>]
type GibraltarLogger() = 
    interface ILogService with
        member x.AddIndex(indexName : string, indexDetails : Index) : unit = 
            Log.TraceInformation("Adding index {0}. IndexDetails:{1}", indexName, indexDetails.ToString())
        member x.CloseIndex(indexName : string) : unit = Log.TraceInformation("Closing index {0}.", indexName)
        member x.ComponentLoaded(name : string, componentType : string) : unit = 
            Log.TraceVerbose("Component:{0} Type:{1} loaded.", name, componentType)
        member x.DeleteIndex(indexName : string) : unit = Log.TraceInformation("Deleting index {0}.", indexName)
        member x.EndSession() : unit = 
            Log.EndSession
                ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() 
                 + " ended.")
        member x.IndexValidationFailed(indexName : string, indexDetails : Index, 
                                       validationObject : Message.OperationMessage) : unit = 
            Log.TraceError
                ("Updating index {0}. IndexDetails:{1}. Message:{2}", indexName, indexDetails.ToString(), 
                 validationObject.ToString())
        member x.OpenIndex(indexName : string) : unit = Log.TraceInformation("Opening index {0}.", indexName)
        member x.Shutdown() : unit = Log.TraceInformation("Shutdown request received.")
        member x.StartSession() : unit = 
            Log.StartSession
                ("FlexSearch " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() 
                 + " started.")
        member x.TraceCritical(ex : System.Exception) : unit = 
            Log.TraceCritical(ex, "Critical application failure happened. {0}", ex.Message)
        member x.TraceError(error : string, ex : System.Exception) : unit = Log.TraceError(error + " Exception:", ex)
        member x.TraceErrorMessage(error : string) : unit = Log.TraceError(error)
        member x.TraceOperationMessageError(error : string, ex : Message.OperationMessage) : unit = 
            Log.TraceError(error + " Exception:", ex.ToString())
        member x.UpdateIndex(indexName : string, indexDetails : Index) : unit = 
            Log.TraceInformation("Updating index {0}. IndexDetails:{1}", indexName, indexDetails.ToString())
