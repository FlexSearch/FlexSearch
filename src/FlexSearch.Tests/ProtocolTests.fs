module ProtocolTests

open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic
open FsCheck
open System

type DocumentBufferTests() = 
    
    let anyInt64CanBeEncoded (num : int64) = 
        use buffer = DocumentBuffer.create (BufferSize.Small)
        DocumentBuffer.encodeInt64 num buffer
        // Reset the position before reading
        buffer.Position <- 0
        let res = DocumentBuffer.decodeInt64 buffer
        res = num
    
    let anyInt32CanBeEncoded (num : int32) = 
        use buffer = DocumentBuffer.create (BufferSize.Small)
        DocumentBuffer.encodeInt32 num buffer
        // Reset the position before reading
        buffer.Position <- 0
        let res = DocumentBuffer.decodeInt32 buffer
        res = num
    
    let anyStringCanBeEncoded (value : NonNull<string>) = 
        use buffer = DocumentBuffer.create (BufferSize.Small)
        let length = DocumentBuffer.encodeString value.Get buffer
        // Reset the position before reading
        buffer.Position <- 0
        let res = DocumentBuffer.decodeString length buffer
        res = value.Get
    
    member __.``Int64 max value can be encoded``() = test <@ anyInt64CanBeEncoded Int64.MaxValue @>
    member __.``Int64 min value can be encoded``() = test <@ anyInt64CanBeEncoded Int64.MinValue @>
    member __.``Any Int64 number can be encoded``() = Check.VerboseThrowOnFailure anyInt64CanBeEncoded
    member __.``Int32 max value can be encoded``() = test <@ anyInt32CanBeEncoded Int32.MaxValue @>
    member __.``Int32 min value can be encoded``() = test <@ anyInt32CanBeEncoded Int32.MinValue @>
    member __.``Any Int32 number can be encoded``() = Check.VerboseThrowOnFailure anyInt32CanBeEncoded
    member __.``Any String can be encoded``() = Check.VerboseThrowOnFailure anyStringCanBeEncoded
    member __.``String 'a' can be encoded``() = test <@ anyStringCanBeEncoded (NonNull.NonNull("a")) @>
    member __.``String 'az   ' can be encoded``() = test <@ anyStringCanBeEncoded (NonNull.NonNull("az   ")) @>

type DocumentProtocolTests() = 
    
    let doc = 
        let d = new Dictionary<string, string>()
        d.Add("f1", "v1")
        d.Add("f2", "v2")
        d.Add("f3", "v3")
        d
    
    let anyInt64CanBeEncodedAsTxId (num : int64) = 
        use buffer = DocumentBuffer.create (BufferSize.Small)
        DocumentProtocol.encodeTransactionId num buffer
        let res = DocumentProtocol.decodeTransactionId buffer
        res = num
    
    member __.``Int64 max value can be encoded as TxId``() = test <@ anyInt64CanBeEncodedAsTxId Int64.MaxValue @>
    member __.``Int64 min value can be encoded as TxId``() = test <@ anyInt64CanBeEncodedAsTxId Int64.MinValue @>
    member __.``Any Int64 number can be encoded as TxId``() = Check.VerboseThrowOnFailure anyInt64CanBeEncodedAsTxId
