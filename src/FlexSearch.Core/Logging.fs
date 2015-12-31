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

type MessageLogValues(errorCode, message, props) =
    interface ILogValues with
        member this.GetValues() = 
            [ new KeyValuePair<string, obj>("Error Code", errorCode)
              new KeyValuePair<string, obj>("Message", message)
              new KeyValuePair<string, obj>("Properties", props) ]
            |> List.toSeq

    override this.ToString() =
        sprintf "Error Code: %s%sMessage: %s%sData: %s%s" errorCode Environment.NewLine message Environment.NewLine props Environment.NewLine


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

    let _logger = _loggerFactory.CreateLogger("FSLog")
    
    let logMethod keyword level ``exception`` (logValues : ILogValues) = 
        match (keyword, level) with
        | MessageKeyword.Startup, MessageLevel.Critical -> _logger.LogCritical(7000, logValues, ``exception``)
        | MessageKeyword.Startup, MessageLevel.Error -> _logger.LogError(7001, logValues, ``exception``)
        | MessageKeyword.Startup, MessageLevel.Warning -> _logger.LogWarning(7002, logValues, ``exception``)
        | MessageKeyword.Startup, MessageLevel.Info -> _logger.LogInformation(7003, logValues, ``exception``)
        | MessageKeyword.Startup, MessageLevel.Verbose -> _logger.LogTrace(7004, logValues, ``exception``)
        | MessageKeyword.Node, MessageLevel.Critical -> _logger.LogCritical(1000, logValues, ``exception``)
        | MessageKeyword.Node, MessageLevel.Error -> _logger.LogError(1001, logValues, ``exception``)
        | MessageKeyword.Node, MessageLevel.Warning -> _logger.LogWarning(1002, logValues, ``exception``)
        | MessageKeyword.Node, MessageLevel.Info -> _logger.LogInformation(1003, logValues, ``exception``)
        | MessageKeyword.Node, MessageLevel.Verbose -> _logger.LogTrace(1004, logValues, ``exception``)
        | MessageKeyword.Index, MessageLevel.Critical -> _logger.LogCritical(2000, logValues, ``exception``)
        | MessageKeyword.Index, MessageLevel.Error -> _logger.LogError(2001, logValues, ``exception``)
        | MessageKeyword.Index, MessageLevel.Warning -> _logger.LogWarning(2002, logValues, ``exception``)
        | MessageKeyword.Index, MessageLevel.Info -> _logger.LogInformation(2003, logValues, ``exception``)
        | MessageKeyword.Index, MessageLevel.Verbose -> _logger.LogTrace(2004, logValues, ``exception``)
        | MessageKeyword.Search, MessageLevel.Critical -> _logger.LogCritical(3000, logValues, ``exception``)
        | MessageKeyword.Search, MessageLevel.Error -> _logger.LogError(3001, logValues, ``exception``)
        | MessageKeyword.Search, MessageLevel.Warning -> _logger.LogWarning(3002, logValues, ``exception``)
        | MessageKeyword.Search, MessageLevel.Info -> _logger.LogInformation(3003, logValues, ``exception``)
        | MessageKeyword.Search, MessageLevel.Verbose -> _logger.LogTrace(3004, logValues, ``exception``)
        | MessageKeyword.Document, MessageLevel.Critical -> _logger.LogCritical(4000, logValues, ``exception``)
        | MessageKeyword.Document, MessageLevel.Error -> _logger.LogError(4001, logValues, ``exception``)
        | MessageKeyword.Document, MessageLevel.Warning -> _logger.LogWarning(4002, logValues, ``exception``)
        | MessageKeyword.Document, MessageLevel.Info -> _logger.LogInformation(4003, logValues, ``exception``)
        | MessageKeyword.Document, MessageLevel.Verbose -> _logger.LogTrace(4004, logValues, ``exception``)
        | MessageKeyword.Default, MessageLevel.Critical -> _logger.LogCritical(5000, logValues, ``exception``)
        | MessageKeyword.Default, MessageLevel.Error -> _logger.LogError(5001, logValues, ``exception``)
        | MessageKeyword.Default, MessageLevel.Warning -> _logger.LogWarning(5002, logValues, ``exception``)
        | MessageKeyword.Default, MessageLevel.Info -> _logger.LogInformation(5003, logValues, ``exception``)
        | MessageKeyword.Default, MessageLevel.Verbose -> _logger.LogTrace(5004, logValues, ``exception``)
        | MessageKeyword.Plugin, MessageLevel.Critical -> _logger.LogCritical(5000, logValues, ``exception``)
        | MessageKeyword.Plugin, MessageLevel.Error -> _logger.LogError(5001, logValues, ``exception``)
        | MessageKeyword.Plugin, MessageLevel.Warning -> _logger.LogWarning(5002, logValues, ``exception``)
        | MessageKeyword.Plugin, MessageLevel.Info -> _logger.LogInformation(5003, logValues, ``exception``)
        | MessageKeyword.Plugin, MessageLevel.Verbose -> _logger.LogTrace(5004, logValues, ``exception``)
        | _, MessageLevel.Nothing -> ()
        // This branch should never be hit. Otherwise, we need to consider handling some other combination
        | _, _ -> _logger.LogWarning(7777, logValues, ``exception``)

    let log (message: IMessage) =
        match message.LogProperty() with
        | _, MessageLevel.Nothing -> ()
        | keyword, level ->
            let om = message.OperationMessage()
            let properties = om.Properties |> Seq.fold (fun acc v -> acc + sprintf "%A; \r\n" v) ""
            logMethod keyword level null <| new MessageLogValues(om.ErrorCode, om.Message, properties)
    
    let logErrorChoice (message : Result<_>) = 
        match message with
        | Fail(error) -> log (error)
        | _ -> ()
        message
       
open Logging
type Logger() =
    static member Log(msg : IMessage) = log(msg)

    static member Log(message : Result<_>) = logErrorChoice(message)
    
    static member Log(msg: string, keyword : MessageKeyword, level: MessageLevel) =
        logMethod keyword level null <| new MessageLogValues(String.Empty, msg, String.Empty)
    
    /// This is an general exception logging method. This should only be used in limited cases
    /// where there is no specific message available to log the error.
    static member Log(ex: Exception, keyword : MessageKeyword, level: MessageLevel) =
        logMethod keyword level ex <| new MessageLogValues("Generic", sprintf "%s \n%s" ex.Message (exceptionPrinter ex), String.Empty)

    /// This is an general exception logging method. This should only be used in limited cases
    /// where there is no specific message available to log the error.
    static member Log(msg: string, ex: Exception, keyword : MessageKeyword, level: MessageLevel) =
        logMethod keyword level ex <| new MessageLogValues("Generic", sprintf "%s \n%s" msg (exceptionPrinter ex), String.Empty)

    static member LogR(msg : IMessage) = Logger.Log msg; msg

    static member LogR(msg : 'a :> IMessage) = Logger.LogR (msg :> IMessage)