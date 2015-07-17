#r "../build-debug/YamlDotNet.dll"
#r "../build-debug/Newtonsoft.Json.dll"
#r "../build-debug/FlexSearch.Core.dll"

open FlexSearch.Core
open System.Reflection
open System
open System.Collections.Generic
open YamlDotNet.Serialization
open YamlDotNet.RepresentationModel
open Newtonsoft.Json.Serialization

open Newtonsoft.Json.Schema
open Newtonsoft.Json
open YamlDotNet.Serialization
open System.Collections.Generic
open System.Reflection
open System.Linq
open System.IO


type Schema() = 
    member val definitions: Dictionary<string, JsonSchema> = null with get, set

let options = YamlDotNet.Serialization.SerializationOptions.Roundtrip
let serializer = new YamlDotNet.Serialization.Serializer(options)


let input = new StringReader(File.ReadAllText(@"C:\git\FlexSearch\documentation\definitions\all_with_refs.yml"))
let ds = new Deserializer(ignoreUnmatched = true)
let s = new JsonSchema()
let schema = ds.Deserialize(input, typeof<Schema>) :?> Schema

printfn "This is the schema:\n%A" (schema.definitions |> Seq.filter (fun x -> x.Key = "Analyzer"))
