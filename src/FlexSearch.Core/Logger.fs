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

open EventSourceProxy.NuGet
open FlexSearch.Api
open Microsoft.Diagnostics.Tracing

[<EventSourceImplementation(Name = "FlexSearch")>]
type IFlexLogger = 
    
    [<Event(1, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Adding index {0}")>] abstract AddIndex : string * Index -> unit
    
    [<Event(2, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Updating index {0}")>] abstract UpdateIndex : string * Index -> unit
    
    [<Event(3, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Deleting index {0}")>] abstract DeleteIndex : string -> unit
    
    [<Event(4, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Index {0} is offline")>] abstract IndexIsOnline : string -> unit
    
    [<Event(5, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Index {0} is online")>] abstract IndexIsOffline : string -> unit
