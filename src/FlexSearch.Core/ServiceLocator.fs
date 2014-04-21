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

/// Even though service locater is an anti pattern, after experimenting with DI and other things 
/// this seems like a more natural fit for f#
// All the services exposed here are singleton across the application with no state. 
// Note: This will not affect unit testing when simulating multiple nodes as these services are
// same across all the nodes and don't have any node specific functionality
[<RequireQualifiedAccess>]
module ServiceLocator = 
    open FlexSearch.Core
    open FlexSearch.Core.State
    open System.Collections.Generic
    
    let mutable HttpModule : Dictionary<string, HttpModuleBase> = Unchecked.defaultof<_>
