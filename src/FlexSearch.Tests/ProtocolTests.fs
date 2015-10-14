module ProtocolTests

open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic
open FsCheck
open System

type DocumentProtocolTests() = 
    let doc = 
        let d = new Dictionary<string, string>()
        d.Add("f1", "v1")
        d.Add("f2", "v2")
        d.Add("f3", "v3")
        d

    let anyInt64CanBeEncodedAsTxId(num : int64) =
        use buffer = DocumentBuffer.create(BufferSize.Small)
        DocumentProtocol.encodeTransactionId num buffer
        let res = DocumentProtocol.decodeTransactionId buffer
        res = num
    
    member __.``Int64 max value can be encoded as TxId``() = 
        test <@ anyInt64CanBeEncodedAsTxId Int64.MaxValue = true  @>

    member __.``Int64 min value can be encoded as TxId``() = 
        test <@ anyInt64CanBeEncodedAsTxId Int64.MinValue = true  @>

    member __.``Any Int64 number can be encoded as TxId``() = 
        Check.VerboseThrowOnFailure anyInt64CanBeEncodedAsTxId