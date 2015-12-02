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
        f.MinimumLevel <- LogLevel.Debug
        f.AddTraceSource(_sourceSwitch, new EventLogTraceListener("FlexSearch")) |> ignore
        f

    let _logger = _loggerFactory.CreateLogger("FSLog")
    
    let logMethod keyword level ``exception`` (logValues : ILogValues) =
        match (keyword, level) with
        | MessageKeyword.Startup, MessageLevel.Critical -> _logger.LogCritical(7000, logValues, ``exception``)
        | MessageKeyword.Startup, MessageLevel.Error -> _logger.LogError(7001, logValues, ``exception``)
//        | MessageKeyword.Startup, MessageLevel.Warning -> _logger.StartupWarning
//        | MessageKeyword.Startup, MessageLevel.Info -> _logger.StartupInfo
//        | MessageKeyword.Startup, MessageLevel.Verbose -> _logger.StartupVerbose
//        | MessageKeyword.Node, MessageLevel.Critical -> _logger.NodeCritical
//        | MessageKeyword.Node, MessageLevel.Error -> _logger.NodeError
//        | MessageKeyword.Node, MessageLevel.Warning -> _logger.NodeWarning
//        | MessageKeyword.Node, MessageLevel.Info -> _logger.NodeInfo
//        | MessageKeyword.Node, MessageLevel.Verbose -> _logger.NodeVerbose
//        | MessageKeyword.Index, MessageLevel.Critical -> _logger.IndexCritical
//        | MessageKeyword.Index, MessageLevel.Error -> _logger.IndexError
//        | MessageKeyword.Index, MessageLevel.Warning -> _logger.IndexWarning
//        | MessageKeyword.Index, MessageLevel.Info -> _logger.IndexInfo
//        | MessageKeyword.Index, MessageLevel.Verbose -> _logger.IndexVerbose
//        | MessageKeyword.Search, MessageLevel.Critical -> _logger.SearchCritical
//        | MessageKeyword.Search, MessageLevel.Error -> _logger.SearchError
//        | MessageKeyword.Search, MessageLevel.Warning -> _logger.SearchWarning
//        | MessageKeyword.Search, MessageLevel.Info -> _logger.SearchInfo
//        | MessageKeyword.Search, MessageLevel.Verbose -> _logger.SearchVerbose
//        | MessageKeyword.Document, MessageLevel.Critical -> _logger.DocumentCritical
//        | MessageKeyword.Document, MessageLevel.Error -> _logger.DocumentError
//        | MessageKeyword.Document, MessageLevel.Warning -> _logger.DocumentWarning
//        | MessageKeyword.Document, MessageLevel.Info -> _logger.DocumentInfo
//        | MessageKeyword.Document, MessageLevel.Verbose -> _logger.DocumentVerbose
//        | MessageKeyword.Default, MessageLevel.Critical -> _logger.DefaultCritical
//        | MessageKeyword.Default, MessageLevel.Error -> _logger.DefaultError
//        | MessageKeyword.Default, MessageLevel.Warning -> _logger.DefaultWarning
//        | MessageKeyword.Default, MessageLevel.Info -> _logger.DefaultInfo
//        | MessageKeyword.Default, MessageLevel.Verbose -> _logger.DefaultVerbose
//        | MessageKeyword.Plugin, MessageLevel.Critical -> _logger.PluginCritical
//        | MessageKeyword.Plugin, MessageLevel.Error -> _logger.PluginError
//        | MessageKeyword.Plugin, MessageLevel.Warning -> _logger.PluginWarning
//        | MessageKeyword.Plugin, MessageLevel.Info -> _logger.PluginInfo
//        | MessageKeyword.Plugin, MessageLevel.Verbose -> _logger.PluginVerbose
//        | _, MessageLevel.Nothing -> logNothing 
        | _, _ -> _logger.LogCritical(7777, logValues, ``exception``)

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