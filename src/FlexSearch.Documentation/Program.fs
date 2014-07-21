module Documentation

open System.Reflection
open System.Linq
open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open Newtonsoft.Json
open Newtonsoft.Json.Converters

let missingDefinitions = new List<string>()
type System.String with
    member this.CamelCaseToSeparate() =
        System.Text.RegularExpressions.Regex.Replace(this, "([A-Z])", " $1")

let GlossaryPath = "F:\SkyDrive\FlexSearch Documentation\source\docs\glossary"
let GlossaryFilePath = "F:\SkyDrive\FlexSearch Documentation\source\docs\glossary\Glossary.txt"
let ThriftFilePath = "F:\SkyDrive\FlexSearch Documentation\source\docs\glossary\Glossary.txt"
let jsonSettings = new JsonSerializerSettings()

jsonSettings.Converters.Add(new StringEnumConverter())

let ParseGlossaryFile() = 
    let dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    if File.Exists(GlossaryFilePath) then 
        let text = File.ReadAllText(GlossaryFilePath)
        let parts = text.Split([| "==" |], StringSplitOptions.RemoveEmptyEntries)
        for part in parts do
            let keyEnds = part.IndexOf("\r\n")
            let key = part.Substring(0, keyEnds).Trim()
            let keys = key.Split([| '|' |], StringSplitOptions.RemoveEmptyEntries)
            let value = part.Substring(keyEnds + 1)
            for k in keys do
                dictionary.Add(k.Trim(), value)
    printfn "%A" dictionary
    dictionary

type MemberType = 
    | SimpleType of string
    | ComplexType of string
    | ListType of string
    | DictionaryType of string * string

type TypeMember() = 
    member val MemberName = "" with get, set
    member val MemberType : MemberType = Unchecked.defaultof<_> with get, set
    member val IsComplexType = false with get, set
    member val ISEnum = false with get, set
    member val IsRequired = false with get, set
    member val DefaultValue = "" with get, set

let GetAllTypes() = 
    let enums = new Dictionary<string, List<string>>()
    let types = new Dictionary<string, List<TypeMember>>()
    let assembly = typeof<FlexSearch.Api.AnalyzerProperties>.Assembly
    for typ in assembly.GetTypes().Where(fun x -> x.IsPublic) do
        printfn "%s" typ.Name
        let typeName = typ.Name
        if typ.IsEnum then 
            let enumValues = new List<string>()
            for enumValue in Enum.GetValues(typ) do
                enumValues.Add(enumValue.ToString())
                printfn "%s" (enumValue.ToString())
            enums.Add(typeName, enumValues)
        else 
            let members = new List<TypeMember>()
            for prop in typ.GetProperties() do
                let typeMember = new TypeMember()
                typeMember.MemberName <- prop.Name
                if prop.PropertyType.IsGenericType then 
                    printf "%s - %s - " prop.Name prop.PropertyType.Name
                    let param = prop.PropertyType.GetGenericArguments()
                    if prop.PropertyType.Name.StartsWith("List") then typeMember.MemberType <- ListType(param.[0].Name)
                    else typeMember.MemberType <- DictionaryType(param.[0].Name, param.[1].Name)
                    for param in prop.PropertyType.GetGenericArguments() do
                        printf "%s " param.Name
                    printf "\n"
                else 
                    if prop.PropertyType.IsValueType || prop.PropertyType.Name = "String" then 
                        typeMember.MemberType <- SimpleType(prop.PropertyType.Name)
                    else typeMember.MemberType <- ComplexType(prop.PropertyType.Name)
                    printfn "%s - %s" prop.Name (prop.PropertyType.Name)
                if prop.PropertyType.IsEnum then typeMember.ISEnum <- true
                members.Add(typeMember)
            for con in typ.GetConstructors() do
                if con.GetParameters().Count() <> 0 then 
                    for conParamater in con.GetParameters() do
                        printfn "Required:%s" conParamater.Name
                        members.First(fun x -> x.MemberName = conParamater.Name).IsRequired <- true
            types.Add(typeName, members)
        printfn ""
    (enums, types)

let GenerateGlossary() = 
    let definition = ParseGlossaryFile()
    let (enums, types) = GetAllTypes()
    
    let getDefinition (typeName, key) = 
        match definition.TryGetValue((sprintf "%s.%s" typeName key)) with
        | true, text -> text
        | _ -> 
            match definition.TryGetValue(key) with
            | true, text -> text
            | _ -> 
                printfn "Missing glossary for: %s" key
                missingDefinitions.Add(sprintf "%s.%s" typeName key)
                "FIX:Missing definition in glossary file"
    
    let rec generateTypeTable (typeName : string, t : List<TypeMember>, output : List<string>) = 
        for m in t do
            let mutable propertyName = ""
            if m.IsRequired then 
                propertyName <- """[image:/images/icons/star32.png[Required, 16,16, title="Required"]] """
            else propertyName <- """[image:/images/icons/undefined.png[Optional, 16,16, title="Optional"]] """
            propertyName <- propertyName + m.MemberName
            match m.MemberType with
            | SimpleType(a) -> 
                if m.ISEnum then
                    output.Add(sprintf "| %s : link:/docs/glossary/%s[`%s`]" propertyName a a)
                    output.Add(sprintf "| %s" (getDefinition (typeName, m.MemberName)))
                else
                    output.Add(sprintf "| %s : `%s`" propertyName a)
                    output.Add(sprintf "| %s" (getDefinition (typeName, m.MemberName)))
            | ComplexType(a) -> output.Add(sprintf "2+| %s : link:/docs/glossary/%s[`%s`]" propertyName a a)
            | ListType(a) -> 
                if a = "String" then 
                    output.Add
                        (sprintf 
                             """2+| [image:/images/icons/linedpaperplus32.png[List, 16,16, title="List"]] %s : List<`%s`>""" 
                             propertyName a)
                else 
                    output.Add
                        (sprintf 
                             """2+| [image:/images/icons/linedpaperplus32.png[List, 16,16, title="List"]] %s : List<link:/docs/glossary/%s[`%s`]>""" 
                             propertyName a a)
            | DictionaryType(a, b) -> 
                output.Add
                    (sprintf 
                         """2+| [image:/images/icons/linedpaperplus32.png[Dictionary, 16,16, title="Dictionary"]] %s : Dictionary<`String` , link:/docs/glossary/%s[`%s`]>""" 
                         propertyName b b)
            output.Add("")
    
    for e in enums do
        let output = new List<string>()
        output.Add(sprintf "[[%s]]" (e.Key.ToLowerInvariant()))
        output.Add(sprintf "=== %s" (e.Key.CamelCaseToSeparate()))
        output.Add("")
        output.Add(getDefinition (e.Key, e.Key))
        output.Add("")
        // Add table 
        output.Add("""[cols="1,1", options="header", role="ui celled table segment"]""")
        output.Add(".Properties")
        output.Add("|===")
        output.Add("|Enumeration Value | Description")
        output.Add("")
        for v in e.Value do
            output.Add(sprintf "| %s" v)
            output.Add(sprintf "| %s" (getDefinition(e.Key, v)))
            output.Add("")
        output.Add("|===")
        output.Add("")
        File.WriteAllLines(Path.Combine(GlossaryPath, sprintf "%s.html.adoc" e.Key), output)

    for t in types do
        //for t in types.Where(fun x -> x.Key = "Index") do
        let output = new List<string>()
        // [[index]]
        // ==== Index
        output.Add(sprintf "[[%s]]" (t.Key.ToLowerInvariant()))
        output.Add(sprintf "=== %s" (t.Key.CamelCaseToSeparate()))
        output.Add("")
        output.Add(getDefinition (t.Key, t.Key))
        output.Add("")
        // Add table 
        output.Add("""[cols="1,1", options="header", role="ui celled table segment"]""")
        output.Add(".Properties")
        output.Add("|===")
        output.Add("|Property Name | Description")
        output.Add("")
        generateTypeTable (t.Key, t.Value, output)
        output.Add("|===")
        let value = (typeof<FlexSearch.Api.AnalyzerProperties>.Assembly).GetTypes().First(fun x -> x.Name = t.Key)
        try 
            let instance = Activator.CreateInstance(value)
            let json = JsonConvert.SerializeObject(instance, Formatting.Indented, jsonSettings)
            output.Add("""[source,javascript]
.Defaults
---------------------------------------------------------------""")
            output.Add(json)
            output.Add("---------------------------------------------------------------")
            output.Add("")
            File.WriteAllLines(Path.Combine(GlossaryPath, sprintf "%s.html.adoc" t.Key), output)
        with e -> ()

[<EntryPoint>]
let main argv = 
    GenerateGlossary()
    printfn "Missing Definition"
    for def in missingDefinitions do
        printfn "%s" def
    printfn "Missing def count:%i" (missingDefinitions.Count)
    Console.ReadKey() |> ignore
    0 // return an integer exit code
