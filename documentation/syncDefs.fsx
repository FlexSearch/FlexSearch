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

let baseDto = typeof<FlexSearch.Core.DtoBase>
let dtoTypes = Assembly.GetAssembly(baseDto).GetTypes()
               |> Seq.filter (fun t -> t.IsSubclassOf baseDto)

open Newtonsoft.Json.Schema
open Newtonsoft.Json
open YamlDotNet.Serialization

//dtoTypes 
//// To JSON
//|> Seq.fold 
//    (fun (dict: Dictionary<_,_>) target -> 
//        let schema = new JsonSchemaGenerator()
//        let json = schema.Generate(target)
//        let name = target.FullName.Replace("+Dto", "").Substring(target.FullName.LastIndexOf(".") + 1)
//        dict.Add(name, json)
//        dict) 
//    (new Dictionary<string, JsonSchema>())
//// To Yaml
//|> (fun dict -> 
//        let ser = new Serializer()
//        use tw = System.IO.File.CreateText(@"C:\git\FlexSearch\documentation\definitions\all.yml")
//        ser.Serialize(tw, dict))

//let target = typeof<FlexSearch.Core.Index.Dto>
//let schema = new JsonSchemaGenerator()
//let result = schema.Generate(target)
//let result1 = schema.Generate(typeof<FlexSearch.Core.IndexConfiguration.Dto>)
//
//let resultObj = new Dictionary<string, JsonSchema>()
//resultObj.Add("ShardConfiguration", result1)
//resultObj.Add("Index", result)
//
//printfn "YAML Output"
//let ser = new Serializer()
//ser.Serialize(System.Console.Out, resultObj)


//let generateSchema target =
//    let schema = new JsonSchemaGenerator()
//    let result = schema.Generate(target)
//
//    printfn "JSON Output"
//    printfn "%s" (JsonConvert.SerializeObject(result))
//
//    printfn "YAML Output"
//    let ser = new Serializer()
////    use tw = System.IO.File.CreateText(@"C:\git\FlexSearch\documentation\definitions\" + target.FullName + ".txt")
//    use tw = System.IO.File.CreateText(@"C:\git\FlexSearch\documentation\definitions\all.txt")
//    ser.Serialize(tw, result)





open Newtonsoft.Json.Schema
open Newtonsoft.Json
open YamlDotNet.Serialization
open System.Collections.Generic
open System.Reflection
open System.Linq

let generateSchema() = 
    dtoTypes // To JSON
             |> Seq.fold (fun (dict : Dictionary<_, _>) target -> 
                    let schema = new JsonSchemaGenerator()
                    let json = schema.Generate(target)
                    let name = target.FullName.Replace("+Dto", "").Substring(target.FullName.LastIndexOf(".") + 1)

                    System.IO.File.WriteAllText((@"C:\git\FlexSearch\documentation\definitions\" + name + ".json"),
                        JsonConvert.SerializeObject(json))
                    
                    dict.Add(name, json)
                    dict) (new Dictionary<string, JsonSchema>())

let replaceProperties (copy : Dictionary<string, JsonSchema>) (original : Dictionary<string, JsonSchema>) =
    for pair in copy do
        for prop in pair.Value.Properties do
            // If the property is an array then replace the "Items" type
            if prop.Value.Type.Value.ToString() = "Array, Null" then
                // Handle the cases where the property name is e.g. "Fields" and the type name is "Field"
                let originalSchema = original |> Seq.tryFind (fun x -> prop.Key.Contains(x.Key))
                match originalSchema with
                | Some(kv) -> 
                    original.[pair.Key].Properties.[prop.Key].Items.[0] <- kv.Value
                | _ -> ()
            else 
                match original.TryGetValue(prop.Key) with
                | true, v -> 
                    if prop.Value.Type <> new System.Nullable<JsonSchemaType>(JsonSchemaType.Boolean) then 
                        original.[prop.Key] <- original.[pair.Key].Properties.[prop.Key]
                | _ -> ()

let generateYaml() = 
    let result = generateSchema()
    let copy = generateSchema()
    result |> replaceProperties copy

    let ser = new Serializer()
    use tw = System.IO.File.CreateText(@"C:\git\FlexSearch\documentation\definitions\all.yml")
    let dict = result.Reverse().ToDictionary((fun x -> x.Key), (fun (x : KeyValuePair<string, JsonSchema>) -> x.Value))
    ser.Serialize(tw, dict)


generateYaml()

