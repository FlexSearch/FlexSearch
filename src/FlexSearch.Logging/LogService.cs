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

using System;
using System.Reflection;
using System.Text;
using Microsoft.Diagnostics.Tracing;
using FlexSearch.Core;
using FlexSearch.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlexSearch.Logging
{
    /// <summary>
    /// Default Logging service for FlexSearch. This is based upon
    /// Event tracing for windows and is extremely fast.
    /// </summary>
    [EventSource(Name = "FlexSearch")]
    public sealed class LogService : EventSource, ILogService
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings();
        private static readonly LogService Log = new LogService();
        private static ConsoleEventListener consoleEventListener = null;

        private LogService()
        {
            JsonSerializerSettings.Converters.Add(new StringEnumConverter());
            JsonSerializerSettings.Formatting = Formatting.Indented;
        }

        public static ILogService GetLogger(bool testLogger)
        {
            if (!testLogger || consoleEventListener != null) return Log;
            consoleEventListener = new ConsoleEventListener();
            consoleEventListener.EnableEvents(Log, EventLevel.LogAlways, EventKeywords.All);
            return Log;
        }

        [Event(1, Message = "Adding index {0}. \nIndexDetails: {1}", Level = EventLevel.Informational,
            Channel = EventChannel.Admin, Keywords = Keywords.Index)]
        private void AddIndex(string indexName, string indexInfo)
        {
            if (IsEnabled())
            {
                WriteEvent(1, indexName, indexInfo);
            }
        }

        [Event(2, Message = "Updating index {0}. \nIndex Details: {1}", Level = EventLevel.Informational,
            Channel = EventChannel.Admin, Keywords = Keywords.Index)]
        private void UpdateIndex(string indexName, string indexInfo)
        {
            if (IsEnabled())
            {
                WriteEvent(2, indexName, indexInfo);
            }
        }

        [Event(3, Message = "Deleting index {0}.", Level = EventLevel.Informational, Channel = EventChannel.Admin,
            Keywords = Keywords.Index)]
        private void DeleteIndex(string indexName)
        {
            if (IsEnabled())
            {
                WriteEvent(3, indexName);
            }
        }

        [Event(4, Message = "Opening index {0}", Level = EventLevel.Informational, Channel = EventChannel.Admin,
            Keywords = Keywords.Index)]
        private void OpenIndex(string indexName)
        {
            if (IsEnabled())
            {
                WriteEvent(4, indexName);
            }
        }

        [Event(5, Message = "Closing index {0}.", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        private void CloseIndex(string indexName)
        {
            if (IsEnabled())
            {
                WriteEvent(5, indexName);
            }
        }

        [Event(6, Message = "Failed to validate index details {0}. \nIndexDetails: {1}. \nMessage: {2}",
            Level = EventLevel.Informational, Channel = EventChannel.Admin, Keywords = Keywords.Index)]
        private void IndexValidationFailed(string indexName, string indexInfo, string validationError)
        {
            if (IsEnabled())
            {
                WriteEvent(6, indexName, indexInfo, validationError);
            }
        }

        [Event(7, Message = "Staring FlexSearch", Level = EventLevel.Informational, Channel = EventChannel.Admin,
                Keywords = Keywords.Node)]
        private void StartSession()
        {
            if (!IsEnabled()) return;
            var sb = new StringBuilder();
            sb.AppendLine(
                String.Format(
                    "Version: {0}", Assembly.GetExecutingAssembly().GetName().Version));
            sb.AppendLine(string.Format("ETW GUID: {0}", Guid));
            sb.AppendLine(string.Format("ETW Logger Name: {0}", Name));
            WriteEvent(7, sb.ToString());
        }

        [Event(8, Message = "Stopping FlexSearch", Level = EventLevel.Informational, Channel = EventChannel.Admin,
            Keywords = Keywords.Node)]
        private void EndSession()
        {
            if (IsEnabled()
                )
            {
                WriteEvent(8);
            }
        }

        [Event(9, Message = "Shutdown request received", Level = EventLevel.Informational, Channel = EventChannel.Admin,
            Keywords = Keywords.Node)]
        private void Shutdown()
        {
            if (IsEnabled())
            {
                WriteEvent(9);
            }
        }

        [Event(10, Message = "Component Loaded: {0}. \nComponent Type: {1}", Level = EventLevel.Informational,
            Channel = EventChannel.Admin, Keywords = Keywords.Node)]
        private void ComponentLoaded(string componentName, string componentType)
        {
            if (IsEnabled())
            {
                WriteEvent(10, componentName, componentType);
            }
        }

        [Event(11, Message = "Critical application failure occurred. \n{0}", Level = EventLevel.Critical,
            Channel = EventChannel.Admin)]
        private void TraceCritical(string ex)
        {
            if (IsEnabled())
            {
                WriteEvent(11, ex);
            }
        }

        [Event(12, Message = "Application error occurred: {0}. \n{1}", Level = EventLevel.Error,
            Channel = EventChannel.Admin,
            Keywords = Keywords.Error)]
        private void TraceError2(string error, string ex)
        {
            if (IsEnabled())
            {
                WriteEvent(12, error, ex);
            }
        }

        [Event(13, Message = "Application error occurred: \n{0}.", Level = EventLevel.Error,
            Channel = EventChannel.Admin,
            Keywords = Keywords.Error)]
        private void TraceError(string error)
        {
            if (IsEnabled())
            {
                WriteEvent(13, error);
            }
        }

        [Event(14, Message = "Application information message: {0}. \nMessage details: {1}",
            Level = EventLevel.Informational,
            Channel = EventChannel.Admin, Keywords = Keywords.General)]
        private void TraceInfomation(string infoMessage, string message)
        {
            if (IsEnabled())
            {
                WriteEvent(14, infoMessage, message);
            }
        }

        public class Keywords
        {
            public const EventKeywords Node = (EventKeywords)0x0001;
            public const EventKeywords Index = (EventKeywords)0x0002;
            public const EventKeywords Search = (EventKeywords)0x0004;
            public const EventKeywords Error = (EventKeywords)0x00008;
            public const EventKeywords General = (EventKeywords)0x0010;
        }

        void ILogService.AddIndex(string indexName, Api.Index indexDetails)
        {
            AddIndex(indexName, JsonConvert.SerializeObject(indexDetails, JsonSerializerSettings));
        }

        void ILogService.CloseIndex(string indexName)
        {
            CloseIndex(indexName);
        }

        void ILogService.ComponentLoaded(string name, string componentType)
        {
            ComponentLoaded(name, componentType);
        }

        void ILogService.DeleteIndex(string indexName)
        {
            DeleteIndex(indexName);
        }

        void ILogService.EndSession()
        {
            EndSession();
        }

        void ILogService.IndexValidationFailed(string indexName, Api.Index indexDetails,
            Api.OperationMessage validationObject)
        {
            IndexValidationFailed(indexName, JsonConvert.SerializeObject(indexDetails, JsonSerializerSettings),
                JsonConvert.SerializeObject(validationObject, JsonSerializerSettings));
        }

        void ILogService.OpenIndex(string indexName)
        {
            OpenIndex(indexName);
        }

        void ILogService.Shutdown()
        {
            Shutdown();
        }

        void ILogService.StartSession()
        {
            StartSession();
        }

        void ILogService.TraceCritical(Exception ex)
        {
            TraceCritical(Helpers.ExceptionPrinter(ex));
        }

        void ILogService.TraceError(string error, Api.OperationMessage ex)
        {
            TraceError2(error, JsonConvert.SerializeObject(ex, JsonSerializerSettings));
        }

        void ILogService.TraceError(string error)
        {
            TraceError(error);
        }

        void ILogService.TraceError(string error, Exception ex)
        {
            TraceError2(error, Helpers.ExceptionPrinter(ex));
        }

        void ILogService.UpdateIndex(string indexName, Api.Index indexDetails)
        {
            UpdateIndex(indexName, JsonConvert.SerializeObject(indexDetails, JsonSerializerSettings));
        }

        void ILogService.TraceInformation(string informationMessage, string messageDetails) 
        {
            TraceInfomation(informationMessage, messageDetails);
        }
    }
}