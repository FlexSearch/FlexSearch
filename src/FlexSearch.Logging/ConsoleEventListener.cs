// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing;

namespace FlexSearch.Logging
{
    /// <summary>
    /// An EventListener is the most basic 'sink' for EventSource events.   All other sinks of 
    /// EventSource data can be thought of as 'built in' EventListeners.    In any particular 
    /// AppDomain all the EventSources send messages to any EventListener in the same
    /// AppDomain that have subscribed to them (using the EnableEvents API).
    /// </summary>
    public class ConsoleEventListener : EventListener
    {
        /// <summary>
        /// We override this method to get a call-back on every event we subscribed to with EnableEvents
        /// </summary>
        /// <param name="eventData"></param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // report all event information
            Console.WriteLine("Event {0} ", eventData.EventName);

            // Events can have formatting strings 'the Message property on the 'Event' attribute.  
            // If the event has a formatted message, print that, otherwise print out argument values.  
            if (eventData.Message != null)
                Console.WriteLine(eventData.Message, eventData.Payload.ToArray());
            else
            {
                var sargs = eventData.Payload != null
                    ? eventData.Payload.Select(o => o.ToString()).ToArray()
                    : null;
                Console.WriteLine("({0}).", sargs != null ? string.Join(", ", sargs) : "");
            }
        }
    }
}