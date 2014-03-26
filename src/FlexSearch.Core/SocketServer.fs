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

module Socket =
    
    open System
    open System.Net
    open System.Collections.Generic
    open SuperSocket.ClientEngine
    open SuperSocket.SocketBase.Protocol
    open SuperSocket.SocketBase
    open SuperSocket.SocketBase.Config
    open SuperSocket.SocketBase.Logging
    open SuperSocket.Facility.Protocol
    open SuperSocket.SocketBase.Protocol
    open System.Collections.Concurrent
    open FlexSearch.Api
    open System.Threading

    /// Custom Flex based protocol implemetation on top of TCP
    /// It's a protocol like that:
    /// +-------+---+-------------------------------+
    /// |length | m |                               |
    /// |       | c |    request body               |
    /// |       |   |                               |
    /// |  (2)  |(1)|                               |
    /// +-------+---+-------------------------------+
    /// length -> length of the packet body
    ///     +------------------+------------------+
    ///     | bodylength / 256 | bodylength % 256 |
    ///     +------------------+------------------+
    /// mc -> method code
    type ProtoBufferReceiveFilter() =
        inherit FixedHeaderReceiveFilter<BinaryRequestInfo>(3)
            
            /// Returns the body length from the header 
            override this.GetBodyLengthFromHeader(header: byte[], offset: int, length: int) =
                (int header.[offset] * 256) + (int header.[offset + 1])

            /// Returns binaryrequest for the handler
            /// Key -> message code
            /// Body -> Protobuffer encoded message
            override this.ResolveRequestInfo(header: ArraySegment<byte>, bodyBuffer: byte[], offset: int, length: int) =
                new BinaryRequestInfo(header.Array.[2].ToString(), bodyBuffer.CloneRange(offset, length))
                

    /// Protobuffer based session wrapper around AppSession
    type ProtoBufferSession() =
        inherit AppSession<ProtoBufferSession, BinaryRequestInfo>()
        member this.useless() = ()

    /// Wrapper around socket server to handle protobuffer based communication 
    type ProtoBufferServer() =
        inherit AppServer<ProtoBufferSession, BinaryRequestInfo>(new DefaultReceiveFilterFactory<ProtoBufferReceiveFilter, BinaryRequestInfo>())
        do ()

    
    // ----------------------------------------------------------------------------
    /// TCP Socket server
    // ----------------------------------------------------------------------------
    type TcpSocketServer(port: int, requestHandler) =
       // let permission = new System.Net.SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts)
        let config = new ServerConfig(Port = port, Ip = "127.0.0.1", MaxConnectionNumber = 1000, Mode = SocketMode.Tcp, Name = "FlexTcpServer") :> IServerConfig
        let mutable server = Unchecked.defaultof<ProtoBufferServer>
        do
            try
                server <- new ProtoBufferServer()
                if server.Setup(config, null, null, new ConsoleLogFactory()) <> true then failwith "Unable to setup the server."
                server.add_NewRequestReceived(requestHandler)
            with 
            |ex -> Console.Write(ex.Message)

        interface IServer with 
            member this.Start() =
                if server.Start() <> true then failwith "Unable to start the server."
                    
            member this.Stop() =
                server.Stop()
    

    // ----------------------------------------------------------------------------
    /// TCP Socket client
    // ----------------------------------------------------------------------------
    type TcpClient(ipAddress: System.Net.IPAddress, port: int) =
        inherit Pool.PooledObject()
        let client = new SuperSocket.ClientEngine.AsyncTcpSession(new Net.IPEndPoint(ipAddress, port)) :> SuperSocket.ClientEngine.IClientSession
        let monitor = new Object()
        let resultExpected = ref false
        let res : byte[] = Array.zeroCreate(2048)
        let mutable length = 0

        do
            client.Connect()
            Thread.Sleep(100)
            if client.IsConnected <> true then failwithf "Client connection error."
            client.DataReceived.Add(fun x -> 
                    lock monitor (fun () ->
                        if(!resultExpected) then
                            Array.Copy(x.Data, x.Offset, res, 0, x.Length)
                            length <- x.Length
                            resultExpected := false
                            Monitor.Pulse(monitor)
                    )    
                )

        
        member this.Send(messageType: byte, data: byte[]) =
            let message = new List<ArraySegment<byte>>()
            message.Add(new ArraySegment<byte>([|(byte)(data.Length / 256); (byte)(data.Length % 255); (byte)21|]))
            message.Add(new ArraySegment<byte>(data))
            client.IsConnected && client.TrySend(message)


        member this.Get<'T>(messageType: byte, data: byte[], ?timeout : int) = 
            let timeout = defaultArg timeout 200
            let message = new List<ArraySegment<byte>>()
            message.Add(new ArraySegment<byte>([|(byte)(data.Length / 256); (byte)(data.Length % 255); messageType|]))
            message.Add(new ArraySegment<byte>(data))
            if client.IsConnected && client.TrySend(message) then
                let result = lock monitor (fun () ->
                    resultExpected := true
                    let success = Monitor.Wait(monitor, timeout)
                    resultExpected := false
                    if success then true
                    else false
                )
                                
                if result then
                    Some(HttpHelpers.protoDeserialize<'T>(Array.sub res 0 length))
                else
                    None
            else
                None


