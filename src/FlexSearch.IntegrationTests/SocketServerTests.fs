module SocketServerTests
open FlexSearch.Core
open FlexSearch.Api
open FlexSearch.Core.Socket
open System
open System.Threading
open SuperSocket.ClientEngine
open SuperSocket.SocketBase.Protocol
open SuperSocket.SocketBase
open SuperSocket.SocketBase.Config
open SuperSocket.SocketBase.Logging
open SuperSocket.Facility.Protocol
open SuperSocket.SocketBase.Protocol
open System.Collections.Concurrent
open System.Runtime.Serialization

[<DataContract(Namespace="")>]
type SampleClass() =
    [<DataMember(Order = 1)>]
    member val Name = "test" with get, set

//[<Tests>]
let socketServerTests =
    let port = 10000
    ()
//    testList "Socket server tests" [
//        let requestHandler (session: ProtoBufferSession) (requestInfo: BinaryRequestInfo) = 
//            match requestInfo.Key with
//            // Echo
//            | "21" -> session.Send(requestInfo.Body, 0, requestInfo.Body.Length)
//            
//            // No Reply
//            | "22" -> ()
//            
//            // Delayed reply
//            | "23" -> 
//                Thread.Sleep(300)
//                session.Send(requestInfo.Body, 0, requestInfo.Body.Length)
//            | _ -> ()
//            
//         
//        let server = new Socket.TcpSocketServer(port, new RequestHandler<ProtoBufferSession, BinaryRequestInfo>(requestHandler)) :> IServer
//        server.Start()
//        
//        yield testCase "Server should echo the same response back to client" <| fun _ ->
//            let sampleClass = new SampleClass()
//            use tcpClient = new Socket.TcpClient(Net.IPAddress.Parse("127.0.0.1"), port)
//            for i = 0 to 2 do
//                sampleClass.Name <- Guid.NewGuid().ToString()
//                let test = HttpHelpers.protoSerialize(sampleClass)
//                let result = tcpClient.Get<SampleClass>(21uy, test) 
//                result.Value.Name |> should equal sampleClass.Name
//
//        yield testCase "Server will take a long time to respond causing a timeout in client" <| fun _ ->
//            let sampleClass = new SampleClass()
//            use tcpClient = new Socket.TcpClient(Net.IPAddress.Parse("127.0.0.1"), port)
//            sampleClass.Name <- Guid.NewGuid().ToString()
//            let test = HttpHelpers.protoSerialize(sampleClass)
//            let result = tcpClient.Get<SampleClass>(23uy, test, 10) 
//            match result with
//            | Some(a) -> 
//                // We don't expect any result so using 1 == 2 to fail the test
//                Assert.AreEqual(1, 2)
//            | _ -> Assert.AreEqual(1, 1)
//
//
//        yield testCase "Server should echo the same response back to multiple client" <| fun _ ->        
//            let factory() = new Socket.TcpClient(Net.IPAddress.Parse("127.0.0.1"), port)
//            use tcpClientPool = new Pool.ObjectPool<Socket.TcpClient>(factory, 5) 
//            for i = 0 to 10 do
//                let sampleClass = new SampleClass()
//                sampleClass.Name <- Guid.NewGuid().ToString()
//                let test = HttpHelpers.protoSerialize(sampleClass)
//                use client = tcpClientPool.Acquire()
//                let result = client.Get<SampleClass>(21uy, test) 
//                result.Value.Name |> should equal sampleClass.Name
//        
//
//        yield testCase "Tear down" <| fun _ ->    
//            server.Stop()
//        ]