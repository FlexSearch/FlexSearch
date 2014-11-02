namespace FlexSearch.Documention

module ReferenceDocumentation = 
    open FlexSearch.Api.Validation
    open Newtonsoft.Json
    open Newtonsoft.Json.Converters
    open Newtonsoft.Json.Serialization
    open System
    open System.Collections.Generic
    open System.ComponentModel
    open System.ComponentModel.DataAnnotations
    open System.IO
    open System.Linq
    open System.Reflection
    open System.Text.RegularExpressions
    
    let missingDefinitions = new List<string>()
    
    type System.String with
        member this.CamelCaseToSeparate() = System.Text.RegularExpressions.Regex.Replace(this, "([A-Z])", " $1")
    
    let GlossaryPath = @"G:\Bitbucket\flex-docs\src\data\glossary"
    let jsonSettings = new JsonSerializerSettings()
    
    jsonSettings.Converters.Add(new StringEnumConverter())
    jsonSettings.Formatting <- Formatting.Indented
    jsonSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()
    
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
        member val JsonSchema = "" with get, set
    
    // Check for description attribute    
    let GetDescription(t : Type) = 
        let descAttr = Attribute.GetCustomAttribute(t, typeof<DescriptionAttribute>)
        if descAttr <> null then (descAttr :?> DescriptionAttribute).Description
        else ""
    
    let GetAllTypes() = 
        let types = new List<TypeDefinition>()
        let assembly = typeof<FlexSearch.Api.Analyzer>.Assembly
        for t in assembly.GetTypes().Where(fun x -> x.IsPublic) do
            if t.FullName.StartsWith("FlexSearch.Api.Validation.") 
               || t.FullName.StartsWith("FlexSearch.Api.AttributeDocumentation") 
               || t.FullName.StartsWith("FlexSearch.Api.Resources") || t.FullName.StartsWith("FlexSearch.Api.Constants") 
               || t.FullName.StartsWith("FlexSearch.Api.OperationMessage") 
               || t.FullName.StartsWith("FlexSearch.Api.Error") || t.FullName.StartsWith("FlexSearch.Api.") = false then 
                ()
            else 
                let def = new TypeDefinition()
                def.Name <- t.Name
                def.Description <- GetDescription(t)
                printfn "%s" t.Name
                if t.IsEnum then 
                    def.IsEnum <- true
                    for f in t.GetFields() do
                        let typeMember = new TypeMember()
                        typeMember.Name <- f.Name
                        typeMember.IsEnum <- true
                        typeMember.IsRequired <- false
                        typeMember.IsBasicType <- true
                        // Check for description attribute
                        let descAttr = Attribute.GetCustomAttribute(f, typeof<DescriptionAttribute>)
                        if descAttr <> null then 
                            typeMember.Description <- (descAttr :?> DescriptionAttribute).Description
                        if f.Name.StartsWith("value__") <> true then def.Properties.Add(typeMember)
                else 
                    try 
                        let instance = Activator.CreateInstance(t)
                        def.JsonDefault <- JsonConvert.SerializeObject(instance, jsonSettings)
                    with e -> printfn "%A" e
                    for prop in t.GetProperties() do
                        let typeMember = new TypeMember()
                        typeMember.Name <- prop.Name
                        if prop.PropertyType.IsGenericType then 
                            printf "%s - %s - " prop.Name prop.PropertyType.Name
                            let param = prop.PropertyType.GetGenericArguments()
                            if prop.PropertyType.Name.StartsWith("List") then 
                                typeMember.PropertyType <- ListType(param.[0].Name)
                                typeMember.TypeDescription <- "list of"
                                typeMember.Type <- param.[0].Name
                            else 
                                typeMember.PropertyType <- DictionaryType(param.[0].Name, param.[1].Name)
                                typeMember.Type <- param.[1].Name
                                typeMember.TypeDescription <- "map of"
                            for param in prop.PropertyType.GetGenericArguments() do
                                printf "%s " param.Name
                            printf "\n"
                        else 
                            if prop.PropertyType.IsValueType || prop.PropertyType.Name = "String" then 
                                typeMember.PropertyType <- SimpleType(prop.PropertyType.Name)
                                typeMember.Type <- prop.PropertyType.Name
                                typeMember.IsBasicType <- true
                            else typeMember.PropertyType <- ComplexType(prop.PropertyType.Name)
                            typeMember.Type <- prop.PropertyType.Name
                            printfn "%s - %s" prop.Name (prop.PropertyType.Name)
                        if prop.PropertyType.IsEnum then typeMember.IsEnum <- true
                        // Check for required attribute
                        let requiredAttr = Attribute.GetCustomAttribute(prop, typeof<RequiredAttribute>)
                        if requiredAttr <> null then typeMember.IsRequired <- true
                        // Check for description attribute
                        let descAttr = Attribute.GetCustomAttribute(prop, typeof<DescriptionAttribute>)
                        if descAttr <> null then 
                            typeMember.Description <- (descAttr :?> DescriptionAttribute).Description
                        // Check for default value attribute
                        let defaultAttr = Attribute.GetCustomAttribute(prop, typeof<DefaultValueAttribute>)
                        if defaultAttr <> null then 
                            typeMember.DefaultValue <- (defaultAttr :?> DefaultValueAttribute).Value.ToString()
                        def.Properties.Add(typeMember)
                types.Add(def)
                printfn ""
        types
    
    let GenerateGlossary() = 
        let types = GetAllTypes()
        for t in types do
            let path = Path.Combine(GlossaryPath, t.Name.ToLowerInvariant() + ".json")
            File.WriteAllText(path, JsonConvert.SerializeObject(t, jsonSettings))
