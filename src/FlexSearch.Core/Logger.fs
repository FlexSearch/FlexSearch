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
module Logger = 
    open FlexSearch.Api
    open Microsoft.Diagnostics.Tracing
    
    [<Sealed>]
    [<EventSource(Name = "FlexSearch")>]
    type FlexLogger() = 
        inherit EventSource()
        [<Event(1, Channel = EventChannel.Admin, Level = EventLevel.Informational, Message = "Adding index {0}")>]
        member this.AddIndex(indexName : string, indexDetails : string) = 
            if this.IsEnabled() then this.WriteEvent(1, indexName, indexDetails)
    
    let Logger = new FlexLogger()
    let addIndex (indexName : string, indexDetails : Index) = Logger.AddIndex(indexName, indexDetails.ToString())
