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

open System.Collections
open FlexSearch.Api
open System.Linq

module ClusterMaster =
    
    let init (state: NodeState) =
        ()//state.PersistanceStore.Nodes.GetAll() 

    let createIndex (index: Index) (state: NodeState) =
        SettingsBuilder.SettingsBuilder()
        