module ProtocolTests

open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic
open FsCheck
open System
open System.Linq

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
        let res = DocumentBuffer.decodeStringAt 0 length buffer.Buffer
        res = value.Get
    
    member __.``Int64 max value can be encoded``() = test <@ anyInt64CanBeEncoded Int64.MaxValue @>
    member __.``Int64 min value can be encoded``() = test <@ anyInt64CanBeEncoded Int64.MinValue @>
    member __.``Prop: Any Int64 number can be encoded``() = Check.VerboseThrowOnFailure anyInt64CanBeEncoded
    member __.``Int32 max value can be encoded``() = test <@ anyInt32CanBeEncoded Int32.MaxValue @>
    member __.``Int32 min value can be encoded``() = test <@ anyInt32CanBeEncoded Int32.MinValue @>
    member __.``Prop: Any Int32 number can be encoded``() = Check.VerboseThrowOnFailure anyInt32CanBeEncoded
    member __.``Prop: Any String can be encoded``() = Check.VerboseThrowOnFailure anyStringCanBeEncoded
    member __.``String 'a' can be encoded``() = test <@ anyStringCanBeEncoded (NonNull.NonNull("a")) @>
    member __.``String 'az   ' can be encoded``() = test <@ anyStringCanBeEncoded (NonNull.NonNull("az   ")) @>

type DocumentProtocolTests() = 
    
    let doc = 
        let d = new Dictionary<string, string>()
        d.Add("f1", "v1")
        d.Add("f2", "v2")
        d.Add("f3", "v3")
        d
    
    let anyDocumentCanBeEncoded (document : NonNull<Dictionary<string, string>>) = 
        let doc = document.Get
        let sut = DocumentProtocol.encodeDocument (1L, document.Get)
        let size = sut.Size
        test <@ DocumentProtocol.decodeVersion sut = 1 @>
        // Even an empty document should be this long
        test <@ DocumentProtocol.decodeSize sut = size @>
        test <@ DocumentProtocol.decodeFieldCount sut = document.Get.Count @>
        let values = doc.Values.ToArray()
        // Ensure that all the doc values are consistent
        for i = 0 to values.Length - 1 do
            if isBlank (values.[i]) then
                test <@ DocumentProtocol.decodeFieldData i sut = String.Empty @>
            else
                test <@ DocumentProtocol.decodeFieldData i sut = values.[i] @>
        true
    
    let anyInt64CanBeEncodedAsTxId (num : int64) = 
        use buffer = DocumentBuffer.create (BufferSize.Small)
        DocumentProtocol.encodeTransactionId num buffer
        let res = DocumentProtocol.decodeTransactionId buffer
        res = num
    
    member __.``Int64 max value can be encoded as TxId``() = test <@ anyInt64CanBeEncodedAsTxId Int64.MaxValue @>
    member __.``Int64 min value can be encoded as TxId``() = test <@ anyInt64CanBeEncodedAsTxId Int64.MinValue @>
    member __.``Prop: Any Int64 number can be encoded as TxId``() = 
        Check.VerboseThrowOnFailure anyInt64CanBeEncodedAsTxId
    
    member __.``Simple document can be encoded``() = 
        let dict = new Dictionary<string, string>()
        dict.Add("first name", "andrew")
        dict.Add("last name", "flint")
        test <@ anyDocumentCanBeEncoded (NonNull(dict)) @>
    
    member __.``Null document can be encoded``() = 
        let dict = new Dictionary<string, string>()
        dict.Add("", "")
        test <@ anyDocumentCanBeEncoded (NonNull(dict)) @>
    
    member __.``Document can be encoded - 2``() = 
        let dict = new Dictionary<string, string>()
        dict.Add("|", "")
        dict.Add("", "")
        test <@ anyDocumentCanBeEncoded (NonNull(dict)) @>

    member __.``Prop: Any document can be encoded``() = 
        Check.One({ Config.VerboseThrowOnFailure with MaxTest = 1000 }, anyDocumentCanBeEncoded)
