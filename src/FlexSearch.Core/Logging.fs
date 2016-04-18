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

open System
open System.Collections.Generic
open System.Diagnostics
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Console
open Microsoft.Extensions.Logging.EventLog
open Microsoft.Extensions.Logging.TraceSource

module Logging =
    let _sourceSwitch = 
        let s = new SourceSwitch("Standard")
        s.Level <- SourceLevels.Verbose
        s

    let _loggerFactory = 
        let f = new LoggerFactory()
        let logName = "FlexSearch Server Events"
        f.AddEventLog(new EventLogSettings(LogName = logName, SourceName = "FlexSearch Core")) |> ignore

        // Log Tracing events in the console
        new ConsoleTraceListener()
        |> Trace.Listeners.Add
        |> ignore

        f

    let printLogValues errorCode message props =
        let errorCodeStr = if String.IsNullOrEmpty(errorCode) then "" else sprintf "Error Code: %s%s" errorCode Environment.NewLine
        let msg = sprintf "Message: %s%s" message Environment.NewLine 
        let propsStr = if String.IsNullOrEmpty(props) then "" else  sprintf "Data: %s%s" props Environment.NewLine
        errorCodeStr + msg + propsStr

    let _logger = _loggerFactory.CreateLogger("FSLog")
    
    let logMethod keyword level (``exception`` : exn) (errorCode, message, props) = 
        let logMessage  = printLogValues errorCode message props
        
        match (keyword, level) with
        | MessageKeyword.Startup, MessageLevel.Critical -> _logger.LogCritical(new EventId(7000, "Startup_Critical"), ``exception``, logMessage)
        | MessageKeyword.Startup, MessageLevel.Error -> _logger.LogError(new EventId(7001, "Startup_Error"), ``exception``, logMessage)
        | MessageKeyword.Startup, MessageLevel.Warning -> _logger.LogWarning(new EventId(7002, "Startup_Warning"), ``exception``, logMessage)
        | MessageKeyword.Startup, MessageLevel.Info -> _logger.LogInformation(new EventId(7003, "Startup_Info"), ``exception``, logMessage)
        | MessageKeyword.Startup, MessageLevel.Verbose -> _logger.LogTrace(new EventId(7004, "Startup_Verbose"), ``exception``, logMessage)
        | MessageKeyword.Node, MessageLevel.Critical -> _logger.LogCritical(new EventId(1000, "Node_Critical"), ``exception``, logMessage)
        | MessageKeyword.Node, MessageLevel.Error -> _logger.LogError(new EventId(1001, "Node_Error"), ``exception``, logMessage)
        | MessageKeyword.Node, MessageLevel.Warning -> _logger.LogWarning(new EventId(1002, "Node_Warning"), ``exception``, logMessage)
        | MessageKeyword.Node, MessageLevel.Info -> _logger.LogInformation(new EventId(1003, "Node_Info"), ``exception``, logMessage)
        | MessageKeyword.Node, MessageLevel.Verbose -> _logger.LogTrace(new EventId(1004, "Node_Verbose"), ``exception``, logMessage)
        | MessageKeyword.Index, MessageLevel.Critical -> _logger.LogCritical(new EventId(2000, "Index_Critical"), ``exception``, logMessage)
        | MessageKeyword.Index, MessageLevel.Error -> _logger.LogError(new EventId(2001, "Index_Error"), ``exception``, logMessage)
        | MessageKeyword.Index, MessageLevel.Warning -> _logger.LogWarning(new EventId(2002, "Index_Warning"), ``exception``, logMessage)
        | MessageKeyword.Index, MessageLevel.Info -> _logger.LogInformation(new EventId(2003, "Index_Info"), ``exception``, logMessage)
        | MessageKeyword.Index, MessageLevel.Verbose -> _logger.LogTrace(new EventId(2004, "Index_Verbose"), ``exception``, logMessage)
        | MessageKeyword.Search, MessageLevel.Critical -> _logger.LogCritical(new EventId(3000, "Search_Critical"), ``exception``, logMessage)
        | MessageKeyword.Search, MessageLevel.Error -> _logger.LogError(new EventId(3001, "Search_Error"), ``exception``, logMessage)
        | MessageKeyword.Search, MessageLevel.Warning -> _logger.LogWarning(new EventId(3002, "Search_Warning"), ``exception``, logMessage)
        | MessageKeyword.Search, MessageLevel.Info -> _logger.LogInformation(new EventId(3003, "Search_Info"), ``exception``, logMessage)
        | MessageKeyword.Search, MessageLevel.Verbose -> _logger.LogTrace(new EventId(3004, "Search_Verbose"), ``exception``, logMessage)
        | MessageKeyword.Document, MessageLevel.Critical -> _logger.LogCritical(new EventId(4000, "Document_Critical"), ``exception``, logMessage)
        | MessageKeyword.Document, MessageLevel.Error -> _logger.LogError(new EventId(4001, "Document_Error"), ``exception``, logMessage)
        | MessageKeyword.Document, MessageLevel.Warning -> _logger.LogWarning(new EventId(4002, "Document_Warning"), ``exception``, logMessage)
        | MessageKeyword.Document, MessageLevel.Info -> _logger.LogInformation(new EventId(4003, "Document_Info"), ``exception``, logMessage)
        | MessageKeyword.Document, MessageLevel.Verbose -> _logger.LogTrace(new EventId(4004, "Document_Verbose"), ``exception``, logMessage)
        | MessageKeyword.Default, MessageLevel.Critical -> _logger.LogCritical(new EventId(5000, "Default_Critical"), ``exception``, logMessage)
        | MessageKeyword.Default, MessageLevel.Error -> _logger.LogError(new EventId(5001, "Default_Error"), ``exception``, logMessage)
        | MessageKeyword.Default, MessageLevel.Warning -> _logger.LogWarning(new EventId(5002, "Default_Warning"), ``exception``, logMessage)
        | MessageKeyword.Default, MessageLevel.Info -> _logger.LogInformation(new EventId(5003, "Default_Info"), ``exception``, logMessage)
        | MessageKeyword.Default, MessageLevel.Verbose -> _logger.LogTrace(new EventId(5004, "Default_Verbose"), ``exception``, logMessage)
        | MessageKeyword.Plugin, MessageLevel.Critical -> _logger.LogCritical(new EventId(5000, "Plugin_Critical"), ``exception``, logMessage)
        | MessageKeyword.Plugin, MessageLevel.Error -> _logger.LogError(new EventId(5001, "Plugin_Error"), ``exception``, logMessage)
        | MessageKeyword.Plugin, MessageLevel.Warning -> _logger.LogWarning(new EventId(5002, "Plugin_Warning"), ``exception``, logMessage)
        | MessageKeyword.Plugin, MessageLevel.Info -> _logger.LogInformation(new EventId(5003, "Plugin_Info"), ``exception``, logMessage)
        | MessageKeyword.Plugin, MessageLevel.Verbose -> _logger.LogTrace(new EventId(5004, "Plugin_Verbose"), ``exception``, logMessage)
        | _, MessageLevel.Nothing -> ()
        // This branch should never be hit. Otherwise, we need to consider handling some other combination
        | _, _ -> _logger.LogWarning(new EventId(7777, "Unexpected"), ``exception``, logMessage)

    let log (message: IMessage) =
        match message.LogProperty() with
        | _, MessageLevel.Nothing -> ()
        | keyword, level ->
            let om = message.OperationMessage()
            let properties = om.Properties |> Seq.fold (fun acc v -> acc + sprintf "%A; \r\n" v) ""
            logMethod keyword level null (om.OperationCode, om.Message, properties)
    
    let logExplicit (message : IMessage) keyword level =
        let om = message.OperationMessage()
        let properties = om.Properties |> Seq.fold (fun acc v -> acc + sprintf "%A; \r\n" v) ""
        logMethod keyword level null (om.OperationCode, om.Message, properties)

    let logErrorChoice (message : Result<_>) = 
        match message with
        | Fail(error) -> log (error)
        | _ -> ()
        message
       
open Logging
type Logger() =
    static member Log(msg : IMessage) = log(msg)

    static member Log(msg : IMessage, keyword : MessageKeyword, level : MessageLevel) = logExplicit msg keyword level 

    static member Log(message : Result<_>) = logErrorChoice(message)
    
    static member Log(msg: string, keyword : MessageKeyword, level: MessageLevel) =
        logMethod keyword level null (String.Empty, msg, String.Empty)
    
    /// This is an general exception logging method. This should only be used in limited cases
    /// where there is no specific message available to log the error.
    static member Log(ex: Exception, keyword : MessageKeyword, level: MessageLevel) =
        logMethod keyword level ex ("Generic", sprintf "%s \n%s" ex.Message (exceptionPrinter ex), String.Empty)

    /// This is an general exception logging method. This should only be used in limited cases
    /// where there is no specific message available to log the error.
    static member Log(msg: string, ex: Exception, keyword : MessageKeyword, level: MessageLevel) =
        logMethod keyword level ex ("Generic", sprintf "%s \n%s" msg (exceptionPrinter ex), String.Empty)

    static member LogR(msg : IMessage) = Logger.Log msg; msg

    static member LogR(msg : 'a :> IMessage) = Logger.LogR (msg :> IMessage)