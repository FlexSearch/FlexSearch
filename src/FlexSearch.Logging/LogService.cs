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

using System;
using System.Reflection;
using System.Text;
using Microsoft.Diagnostics.Tracing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlexSearch.Logging
{
    /// <summary>
    /// Default Logging service for FlexSearch. This is based upon
    /// Event tracing for windows and is extremely fast.
    /// </summary>
    [EventSource(Name = "FlexSearch")]
    public sealed class LogService : EventSource
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings();
        private static readonly LogService Log = new LogService();
        private static ConsoleEventListener consoleEventListener = null;

        /// <summary>
        /// Default Log message format
        /// </summary>
        private const string Message = "Code: {0} \n{1}";

        private LogService()
        {
            JsonSerializerSettings.Converters.Add(new StringEnumConverter());
            JsonSerializerSettings.Formatting = Formatting.Indented;
        }

        public static LogService GetLogger(bool testLogger)
        {
            if (!testLogger || consoleEventListener != null) return Log;
            consoleEventListener = new ConsoleEventListener();
            consoleEventListener.EnableEvents(Log, EventLevel.LogAlways, EventKeywords.All);
            return Log;
        }

        public class Keywords
        {
            public const EventKeywords Node = (EventKeywords)1;
            public const EventKeywords Index = (EventKeywords)2;
            public const EventKeywords Search = (EventKeywords)4;
            public const EventKeywords Document = (EventKeywords)8;
            public const EventKeywords Default = (EventKeywords)16;
            public const EventKeywords Plugin = (EventKeywords)32;
        }

        /// <summary>
        /// Returns if the logger is enabled or not
        /// </summary>
        public new bool IsEnabled { get { return IsEnabled(); } }

        #region Node events
        [Event(1000, Message = Message, Level = EventLevel.Critical, Keywords = Keywords.Node, Channel = EventChannel.Admin)]
        public void NodeCritical(string errorCode, string msg, string data)
        {
            WriteEvent(1000, errorCode, msg, data);
        }

        [Event(1001, Message = Message, Level = EventLevel.Error, Keywords = Keywords.Node, Channel = EventChannel.Admin)]
        public void NodeError(string errorCode, string msg, string data)
        {
            WriteEvent(1001, errorCode, msg, data);
        }

        [Event(1002, Message = Message, Level = EventLevel.Warning, Keywords = Keywords.Node, Channel = EventChannel.Admin)]
        public void NodeWarning(string errorCode, string msg, string data)
        {
            WriteEvent(1002, errorCode, msg, data);
        }
        
        [Event(1003, Message = Message, Level = EventLevel.Informational, Keywords = Keywords.Node, Channel = EventChannel.Admin)]
        public void NodeInfo(string errorCode, string msg, string data)
        {
            WriteEvent(1003, errorCode, msg, data);
        }

        [Event(1004, Message = Message, Level = EventLevel.Verbose, Keywords = Keywords.Node, Channel = EventChannel.Operational)]
        public void NodeVerbose(string errorCode, string msg, string data)
        {
            WriteEvent(1004, errorCode, msg, data);
        }
        
        [Event(1005, Message = Message, Level = EventLevel.LogAlways, Keywords = Keywords.Node, Channel = EventChannel.Operational)]
        public void NodeLogAlways(string errorCode, string msg, string data)
        {
            WriteEvent(1005, errorCode, msg, data);
        }
        #endregion

        #region Index events
        [Event(2000, Message = Message, Level = EventLevel.Critical, Keywords = Keywords.Index, Channel = EventChannel.Admin)]
        public void IndexCritical(string errorCode, string msg, string data)
        {
            WriteEvent(2000, errorCode, msg, data);
        }

        [Event(2001, Message = Message, Level = EventLevel.Error, Keywords = Keywords.Index, Channel = EventChannel.Admin)]
        public void IndexError(string errorCode, string msg, string data)
        {
            WriteEvent(2001, errorCode, msg, data);
        }

        [Event(2002, Message = Message, Level = EventLevel.Warning, Keywords = Keywords.Index, Channel = EventChannel.Admin)]
        public void IndexWarning(string errorCode, string msg, string data)
        {
            WriteEvent(2002, errorCode, msg, data);
        }

        [Event(2003, Message = Message, Level = EventLevel.Informational, Keywords = Keywords.Index, Channel = EventChannel.Admin)]
        public void IndexInfo(string errorCode, string msg, string data)
        {
            WriteEvent(2003, errorCode, msg, data);
        }

        [Event(2004, Message = Message, Level = EventLevel.Verbose, Keywords = Keywords.Index, Channel = EventChannel.Operational)]
        public void IndexVerbose(string errorCode, string msg, string data)
        {
            WriteEvent(2004, errorCode, msg, data);
        }

        [Event(2005, Message = Message, Level = EventLevel.LogAlways, Keywords = Keywords.Index, Channel = EventChannel.Operational)]
        public void IndexLogAlways(string errorCode, string msg, string data)
        {
            WriteEvent(2005, errorCode, msg, data);
        }
        #endregion

        #region Search events
        [Event(3000, Message = Message, Level = EventLevel.Critical, Keywords = Keywords.Search, Channel = EventChannel.Admin)]
        public void SearchCritical(string errorCode, string msg, string data)
        {
            WriteEvent(3000, errorCode, msg, data);
        }

        [Event(3001, Message = Message, Level = EventLevel.Error, Keywords = Keywords.Search, Channel = EventChannel.Admin)]
        public void SearchError(string errorCode, string msg, string data)
        {
            WriteEvent(3001, errorCode, msg, data);
        }

        [Event(3002, Message = Message, Level = EventLevel.Warning, Keywords = Keywords.Search, Channel = EventChannel.Admin)]
        public void SearchWarning(string errorCode, string msg, string data)
        {
            WriteEvent(3002, errorCode, msg, data);
        }

        [Event(3003, Message = Message, Level = EventLevel.Informational, Keywords = Keywords.Search, Channel = EventChannel.Admin)]
        public void SearchInfo(string errorCode, string msg, string data)
        {
            WriteEvent(3003, errorCode, msg, data);
        }

        [Event(3004, Message = Message, Level = EventLevel.Verbose, Keywords = Keywords.Search, Channel = EventChannel.Operational)]
        public void SearchVerbose(string errorCode, string msg, string data)
        {
            WriteEvent(3004, errorCode, msg, data);
        }

        [Event(3005, Message = Message, Level = EventLevel.LogAlways, Keywords = Keywords.Search, Channel = EventChannel.Operational)]
        public void SearchLogAlways(string errorCode, string msg, string data)
        {
            WriteEvent(3005, errorCode, msg, data);
        }
        #endregion

        #region Document events
        [Event(4000, Message = Message, Level = EventLevel.Critical, Keywords = Keywords.Document, Channel = EventChannel.Admin)]
        public void DocumentCritical(string errorCode, string msg, string data)
        {
            WriteEvent(4000, errorCode, msg, data);
        }

        [Event(4001, Message = Message, Level = EventLevel.Error, Keywords = Keywords.Document, Channel = EventChannel.Admin)]
        public void DocumentError(string errorCode, string msg, string data)
        {
            WriteEvent(4001, errorCode, msg, data);
        }

        [Event(4002, Message = Message, Level = EventLevel.Warning, Keywords = Keywords.Document, Channel = EventChannel.Admin)]
        public void DocumentWarning(string errorCode, string msg, string data)
        {
            WriteEvent(4002, errorCode, msg, data);
        }

        [Event(4003, Message = Message, Level = EventLevel.Informational, Keywords = Keywords.Document, Channel = EventChannel.Admin)]
        public void DocumentInfo(string errorCode, string msg, string data)
        {
            WriteEvent(4003, errorCode, msg, data);
        }

        [Event(4004, Message = Message, Level = EventLevel.Verbose, Keywords = Keywords.Document, Channel = EventChannel.Operational)]
        public void DocumentVerbose(string errorCode, string msg, string data)
        {
            WriteEvent(4004, errorCode, msg, data);
        }

        [Event(4005, Message = Message, Level = EventLevel.LogAlways, Keywords = Keywords.Document, Channel = EventChannel.Operational)]
        public void DocumentLogAlways(string errorCode, string msg, string data)
        {
            WriteEvent(4005, errorCode, msg, data);
        }
        #endregion

        #region Default events
        [Event(5000, Message = Message, Level = EventLevel.Critical, Keywords = Keywords.Default, Channel = EventChannel.Admin)]
        public void DefaultCritical(string errorCode, string msg, string data)
        {
            WriteEvent(5000, errorCode, msg, data);
        }

        [Event(5001, Message = Message, Level = EventLevel.Error, Keywords = Keywords.Default, Channel = EventChannel.Admin)]
        public void DefaultError(string errorCode, string msg, string data)
        {
            WriteEvent(5001, errorCode, msg, data);
        }

        [Event(5002, Message = Message, Level = EventLevel.Warning, Keywords = Keywords.Default, Channel = EventChannel.Admin)]
        public void DefaultWarning(string errorCode, string msg, string data)
        {
            WriteEvent(5002, errorCode, msg, data);
        }

        [Event(5003, Message = Message, Level = EventLevel.Informational, Keywords = Keywords.Default, Channel = EventChannel.Admin)]
        public void DefaultInfo(string errorCode, string msg, string data)
        {
            WriteEvent(5003, errorCode, msg, data);
        }

        [Event(5004, Message = Message, Level = EventLevel.Verbose, Keywords = Keywords.Default, Channel = EventChannel.Operational)]
        public void DefaultVerbose(string errorCode, string msg, string data)
        {
            WriteEvent(5004, errorCode, msg, data);
        }

        [Event(5005, Message = Message, Level = EventLevel.LogAlways, Keywords = Keywords.Default, Channel = EventChannel.Operational)]
        public void DefaultLogAlways(string errorCode, string msg, string data)
        {
            WriteEvent(5005, errorCode, msg, data);
        }
        #endregion

        #region Plugin events
        [Event(6000, Message = Message, Level = EventLevel.Critical, Keywords = Keywords.Plugin, Channel = EventChannel.Admin)]
        public void PluginCritical(string errorCode, string msg, string data)
        {
            WriteEvent(6000, errorCode, msg, data);
        }

        [Event(6001, Message = Message, Level = EventLevel.Error, Keywords = Keywords.Plugin, Channel = EventChannel.Admin)]
        public void PluginError(string errorCode, string msg, string data)
        {
            WriteEvent(6001, errorCode, msg, data);
        }

        [Event(6002, Message = Message, Level = EventLevel.Warning, Keywords = Keywords.Plugin, Channel = EventChannel.Admin)]
        public void PluginWarning(string errorCode, string msg, string data)
        {
            WriteEvent(6002, errorCode, msg, data);
        }

        [Event(6003, Message = Message, Level = EventLevel.Informational, Keywords = Keywords.Plugin, Channel = EventChannel.Admin)]
        public void PluginInfo(string errorCode, string msg, string data)
        {
            WriteEvent(6003, errorCode, msg, data);
        }

        [Event(6004, Message = Message, Level = EventLevel.Verbose, Keywords = Keywords.Plugin, Channel = EventChannel.Operational)]
        public void PluginVerbose(string errorCode, string msg, string data)
        {
            WriteEvent(6004, errorCode, msg, data);
        }

        [Event(6005, Message = Message, Level = EventLevel.LogAlways, Keywords = Keywords.Plugin, Channel = EventChannel.Operational)]
        public void PluginLogAlways(string errorCode, string msg, string data)
        {
            WriteEvent(6005, errorCode, msg, data);
        }
        #endregion
    }
}