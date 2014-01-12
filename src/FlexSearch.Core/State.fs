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

module State = 
    open FlexSearch.Core
    open FlexSearch.Core.Interface
    open System.Collections.Immutable
    open System.Collections.Concurrent
    open System.Net
    open FlexSearch.Api
    open System.Reactive.Subjects

    type ClusterEvents = 
        | NodeDead of IPAddress
        | NodeJoin of IPAddress
        | HeartBeat of IPAddress
        | NewLeaderElection of IPAddress  

    type NodeProperties =
        {
            Name        :   string
            Address     :   System.Net.IPAddress
            Pool        :   Pool.ObjectPool<Socket.TcpClient>
            Priority    :   int
        }

    /// This will hold all the mutable data related to the node. Everything outside will be
    /// immutable. This will be passed around. 
    type NodeState =
        {
            PersistanceStore    :   IPersistanceStore
            ServerSettings      :   ServerSettings
            OutgoingConnections :   ImmutableDictionary<string, ISocketClient>
            Indices             :   ImmutableDictionary<string, Index>
            ConnectedNodes      :   BlockingCollection<NodeProperties>
            Nodes               :   BlockingCollection<NodeProperties>
            ConnectedNodesLookup:   ConcurrentDictionary<IPAddress, NodeProperties>
            TotalNodes          :   int
            ClusterEventBus     :   Subject<ClusterEvents>
        }
        with 
            member this.IndexExists(indexName: string) = 
                match this.Indices.TryGetValue(indexName) with
                | true, x   -> Some(x)
                | _         -> None
    


    // ----------------------------------------------------------------------------     
    /// Http module to handle to incoming requests
    // ----------------------------------------------------------------------------     
    type IHttpModule =
        abstract Process    :   System.Net.HttpListenerRequest -> System.Net.HttpListenerResponse -> NodeState -> unit   