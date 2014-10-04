// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch
open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Utility
open Microsoft.Owin
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open ProtoBuf
open System
open System.Collections.Concurrent
open System.IO
open System.Linq
open System.Net
open System.Text
open System.Threading

[<Sealed>]
type JilJsonFormatter() = 
    let options = new Jil.Options(false, false, true, Jil.DateTimeFormat.ISO8601, false)
    interface IFormatter with
        member x.SerializeToString(body : obj) = failwith "Not implemented yet"
        
        member x.DeSerialize<'T>(stream : Stream) = 
            use reader = new StreamReader(stream) :> TextReader
            Jil.JSON.Deserialize<'T>(reader, options)
        
        member x.Serialize(body : obj, stream : Stream) : unit = 
            use writer = new StreamWriter(stream)
            Jil.JSON.Serialize(body, writer, options)
            writer.Flush()
        
        member x.SupportedHeaders() : string [] = 
            [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8" |]

[<Sealed>]
type NewtonsoftJsonFormatter() = 
    let options = new Newtonsoft.Json.JsonSerializerSettings()
    do options.Converters.Add(new StringEnumConverter())
    interface IFormatter with
        member x.SerializeToString(body : obj) = failwith "Not implemented yet"
        
        member x.DeSerialize<'T>(stream : Stream) = 
            use reader = new StreamReader(stream)
            JsonConvert.DeserializeObject<'T>(reader.ReadToEnd(), options)
        
        member x.Serialize(body : obj, stream : Stream) : unit = 
            use writer = new StreamWriter(stream)
            let body = JsonConvert.SerializeObject(body, options)
            writer.Write(body)
            writer.Flush()
        
        member x.SupportedHeaders() : string [] = 
            [| "application/json"; "text/json"; "application/json;charset=utf-8"; "application/json; charset=utf-8" |]

[<Sealed>]
type ProtoBufferFormatter() = 
    interface IFormatter with
        member x.SerializeToString(body : obj) = failwith "Not implemented yet"
        member x.DeSerialize<'T>(stream : Stream) = ProtoBuf.Serializer.Deserialize<'T>(stream)
        member x.Serialize(body : obj, stream : Stream) : unit = ProtoBuf.Serializer.Serialize(stream, body)
        member x.SupportedHeaders() : string [] = [| "application/x-protobuf"; "application/octet-stream" |]

[<Sealed>]
type YamlFormatter() = 
    let options = YamlDotNet.Serialization.SerializationOptions.EmitDefaults
    let serializer = new YamlDotNet.Serialization.Serializer(options)
    let deserializer = new YamlDotNet.Serialization.Deserializer(ignoreUnmatched = true)
    interface IFormatter with
        
        member x.SerializeToString(body : obj) = 
            use textWriter = new StringWriter()
            serializer.Serialize(textWriter, body)
            textWriter.ToString()
        
        member x.DeSerialize<'T>(stream : Stream) = 
            use textReader = new StreamReader(stream)
            deserializer.Deserialize<'T>(textReader)
        
        member x.Serialize(body : obj, stream : Stream) : unit = 
            use TextWriter = new StreamWriter(stream)
            serializer.Serialize(TextWriter, body)
        
        member x.SupportedHeaders() : string [] = [| "application/yaml" |]
