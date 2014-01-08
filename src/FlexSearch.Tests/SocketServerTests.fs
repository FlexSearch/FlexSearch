module SocketServerTests
open FsUnit
open Fuchu
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

[<Tests>]
let socketServerTests =
    testList "Socket server tests" [
        let requestHandler (session: ProtoBufferSession) (requestInfo: BinaryRequestInfo) = 
            requestInfo.Key |> should equal "21"
            session.Send(requestInfo.Body, 0, requestInfo.Body.Length)
            ()
         
        let server = new Socket.TcpSocketServer(9900, new RequestHandler<ProtoBufferSession, BinaryRequestInfo>(requestHandler)) :> IServer
        server.Start()
        let tcpClient = new Socket.TcpClient(Net.IPAddress.Parse("127.0.0.1"), 9900)
        yield testCase (sprintf "Pool Item count should be") <| fun _ ->
            let sampleClass = new SampleClass()

            for i = 0 to 100 do
                sampleClass.Name <- Guid.NewGuid().ToString()
                let test = HttpHelpers.protoSerialize(sampleClass)
                let result = tcpClient.Get<string>(21uy, test) 
                result |> should equal sampleClass.Name
        ]