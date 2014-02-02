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
open FlexSearch.Core.State
module ClusterService =
    
    /// Returns the tentative leader for the cluster
    let getCandidateLeader (state: NodeState) =
        let nodes = state.Nodes.ToArray()
        
        let rec loop n = 
            if n < state.Nodes.Count then 
                match state.ConnectedNodesLookup.TryGetValue(nodes.[n].Address) with
                | true, a  -> n
                | _ -> loop (n + 1)
            else 
                -1

        let candidateNumber = loop 0
        if candidateNumber <> -1 then
            Some(nodes.[candidateNumber])
        else
            None
                

        