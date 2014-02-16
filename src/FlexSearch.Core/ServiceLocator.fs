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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

/// Even though service locator is an anti pattern, after experimenting with DI and other things 
/// this seems like a more natrual fit for f#

// All the services exposed here are singleton across the application with no state. 
// Note: This will not affect unit testing when simuating multiple nodes as these services are
// same across all the nodes and don't have any node specific functionality
 
module ServiceLocator =
    open System.Collections.Generic
    open  System.Diagnostics.Tracing
    open FlexSearch.Core.State

    let mutable FactoryCollection : IFactoryCollection = Unchecked.defaultof<_>
    let mutable SettingsBuilder : ISettingsBuilder = Unchecked.defaultof<_>
    let mutable HttpModule : Dictionary<string, HttpModule> = Unchecked.defaultof<_>
    let mutable NodeState : NodeState = Unchecked.defaultof<_>
    
    type Logger() as self =
        inherit EventSource()
            
            [<Event(1, Level = EventLevel.Informational)>]
            member this.ApplicationStarted() = self.WriteEvent(1)

    
    let EventLogger = new Logger()     