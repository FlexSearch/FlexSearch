#I "../src/build"
#I "../src/build/Lib"
#load "validator.fsx"
#r "../src/build/FlexSearch.Core.dll"
#r "../src/build/lib/newtonsoft.json.dll"
#r "lib/Handlebars.dll"

open System
open System.IO
open System.Collections.Generic
open System.Collections
open System.Linq
open System.Reflection
open FlexSearch.SigDocValidator
open FlexSearch.SigDocParser
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Newtonsoft.Json.Serialization
open HandlebarsDotNet

// ----------------------------------------------------------------------------
// This script generates the DTO and web service documentation
// ----------------------------------------------------------------------------
type MemberType =
    | SimpleType of string
    | ComplexType of string
    | ListType of string
    | DictionaryType of string * string

type TypeMember() =
    member val Name = "" with get, set
    member val PropertyType : MemberType = Unchecked.defaultof<_> with get, set
    member val Type = "" with get, set
    member val TypeDescription = "" with get, set
    member val IsBasicType = false with get, set
    member val IsEnum = false with get, set
    member val IsRequired = false with get, set
    member val DefaultValue = "" with get, set
    member val Description = "" with get, set

type TypeDefinition() =
    member val Name = "" with get, set
    member val Description = "" with get, set
    member val Properties = new List<TypeMember>() with get, set
    member val IsEnum = false with get, set
    member val YamlDefault = "" with get, set
    member val JsonDefault = "" with get, set

module Docs =
    /// Output folder where dto information should be written
    printfn "SourceDirectory : %s" __SOURCE_DIRECTORY__
    let dtoFolderPath = Directory.CreateDirectory(__SOURCE_DIRECTORY__ + @"/docs/dto/").FullName
    let exampleFolderPath = Directory.CreateDirectory(__SOURCE_DIRECTORY__ + @"/docs/data/").FullName

    let jsonSettings = new JsonSerializerSettings()

    jsonSettings.Converters.Add(new StringEnumConverter())
    jsonSettings.Formatting <- Formatting.Indented
    jsonSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()

    // Helper methods
    let propVal instance propName (typ : Type) = typ.GetProperty(propName).GetValue(instance).ToString()

    let valFromKey (key : 'a) context (dict : Dictionary<'a, _>) =
        if dict.ContainsKey(key) then dict.[key]
        else failwithf "Couldn't find key '%A' in the dictionary:\n%A\nContext:\n%A" key dict context

    let typeToSchema (def : Definition) (coreType : Type) =
        let tDef = new TypeDefinition()
        tDef.Name <- def.Name
        tDef.Description <- def.Summary
        if coreType.IsEnum then
            tDef.IsEnum <- true
            coreType.GetFields()
            |> Seq.map (fun f ->
                   let typeMember = new TypeMember()
                   typeMember.Name <- f.Name
                   typeMember.IsEnum <- true
                   typeMember.IsRequired <- false
                   typeMember.IsBasicType <- true
                   typeMember.Description <- match def.Options.TryGetValue(f.Name) with
                                             | true, v -> v
                                             | _ -> ""
                   if f.Name.StartsWith("value__") <> true then Some(typeMember)
                   else None)
            |> Seq.filter (fun x -> x.IsSome)
            |> Seq.map (fun x -> x.Value)
            |> Seq.iter (fun x -> tDef.Properties.Add(x))
        else
            // Initialize a core type to get the default values
            let instance = coreType.GetConstructor([||]).Invoke([||])
            tDef.JsonDefault <- JsonConvert.SerializeObject(instance, jsonSettings)
            for prop in coreType.GetProperties() do
                let typeMember = new TypeMember()
                typeMember.Name <- prop.Name
                if prop.PropertyType.IsEnum then typeMember.IsEnum <- true
                typeMember.Description <- def.Properties |> valFromKey typeMember.Name def

                match prop.PropertyType with
                | p when p.IsGenericType ->
                    let param = prop.PropertyType.GetGenericArguments()
                    if prop.PropertyType.Name.StartsWith("List") then
                        typeMember.PropertyType <- ListType(param.[0].Name)
                        typeMember.TypeDescription <- "list of"
                        typeMember.Type <- param.[0].Name
                    else
                        typeMember.PropertyType <- DictionaryType(param.[0].Name, param.[1].Name)
                        typeMember.TypeDescription <- "map of"
                        // The first param is alway a string
                        typeMember.Type <- param.[1].Name
                | p when p.IsValueType || p.Name = "String" ->
                    typeMember.PropertyType <- SimpleType(prop.PropertyType.Name)
                    typeMember.Type <- prop.PropertyType.Name
                    typeMember.IsBasicType <- true
                    // Check if field is required
                    if typeMember.Type = "String" && (string) typeMember.DefaultValue = String.Empty then
                        typeMember.IsRequired <- true
                    // Default values are only applicable for simple types
                    typeMember.DefaultValue <- coreType |> propVal instance typeMember.Name
                | _ ->
                    typeMember.PropertyType <- ComplexType(prop.PropertyType.Name)
                    typeMember.Type <- prop.PropertyType.Name
                    printfn "%s - %s" prop.Name (prop.PropertyType.Name)
                tDef.Properties.Add(typeMember)
        tDef

    let dtoDefinitions() =
        let handleBar = Handlebars.Compile(File.ReadAllText(__SOURCE_DIRECTORY__ + @"/partials/properties.html"))

        /// Reads the core types using reflection and augments that with the information from the fsi file.
        /// The final result is written to a markdown file to be used as part of documentation
        let processDto coreTypes docTypes =
            coreTypes
            |> Seq.zip docTypes
            |> Seq.map (fun x -> typeToSchema (fst x) (snd x))
            |> Seq.iter (fun x ->
                   let filePath = Path.Combine(dtoFolderPath, x.Name + ".md")
                   printfn "Writing DTO information to file: %s" filePath
                   File.WriteAllText(filePath, handleBar.Invoke(x)))
        processDto coreDtos docDtos
        processDto coreEnums docEnums

    let generateSearchResults() =
        let handleBar = Handlebars.Compile(File.ReadAllText(__SOURCE_DIRECTORY__ + @"/partials/search_result.html"))
        Directory.EnumerateFiles(__SOURCE_DIRECTORY__ + @"/docs/data", "*.json")
        |> Seq.iter(fun x ->
            let filePath = Path.Combine(exampleFolderPath, Path.GetFileNameWithoutExtension(x).Trim() + ".md")
            let data = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(File.ReadAllText(x))
            File.WriteAllText(filePath, handleBar.Invoke(data)))

    let wsObjectGenerator (def : Definition) (coreType : Type) =
        let (httpMethod, httpUri) = getMethodAndUriFromType(coreType)
        def.HttpMethod <- httpMethod
        def.Uri <- httpUri
        def.HttpInputDto <-  coreType.BaseType.GenericTypeArguments.[0].Name
        def.HttpOutputDto <- coreType.BaseType.GenericTypeArguments.[1].Name
        def

    let generateWSDocs() =
        let handleBar = Handlebars.Compile(File.ReadAllText(__SOURCE_DIRECTORY__ + @"/partials/api.html"))
        coreWss
        |> Seq.zip docWss
        |> Seq.map (fun x -> wsObjectGenerator (fst x) (snd x))
        |> Seq.iter (fun x ->
            let filePath = Path.Combine(dtoFolderPath, x.Name + ".md")
            printfn "Writing WSS information to file: %s" filePath
            printfn "Passed Object: %s" (JsonConvert.SerializeObject(x))
            File.WriteAllText(filePath, handleBar.Invoke(x))
            )

Docs.dtoDefinitions()
Docs.generateSearchResults()
Docs.generateWSDocs()
