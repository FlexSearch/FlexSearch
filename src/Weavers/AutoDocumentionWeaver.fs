namespace Weavers

open Mono.Cecil
open System
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.DataAnnotations
open System.IO
open System.Linq
open System.Runtime.Serialization
open System.Xml.Linq

/// <summary>
/// Generic .net comment parser to be used for auto-generating
/// documentation.
/// </summary>
type CommentParser(filePath : string) = 
    
    let xml = 
        if File.Exists(filePath) then XElement.Load(filePath)
        else failwithf "Documentation file not found: %s" filePath
    
    let xn s = XName.Get(s)
    let members = xml.Element(xn "members").Elements(xn "member")
    member this.GetPropertyElement(fullName : string) = 
        members 
        |> Seq.tryFind 
               (fun x -> 
               x.Attribute(xn "name").Value.StartsWith(fullName) 
               && x.Attribute(xn "name").Value.Length >= fullName.Length)
    member this.GetPropertySummary(fullname) = 
        match this.GetPropertyElement(fullname) with
        | Some(p) -> p.Element(xn "summary").Value.Replace("\n", "").Trim()
        | None -> ""

type AutoDocumentionWeaver() as self = 
    let mutable descriptionTypeRef = Unchecked.defaultof<TypeReference>
    let mutable descriptionCtor = Unchecked.defaultof<MethodReference>
    let mutable dataContractTypeRef = Unchecked.defaultof<TypeReference>
    let mutable dataContractCtor = Unchecked.defaultof<MethodReference>
    let mutable dataMemberTypeRef = Unchecked.defaultof<TypeReference>
    let mutable dataMemberCtor = Unchecked.defaultof<MethodReference>
    let mutable displayTypeRef = Unchecked.defaultof<TypeReference>
    let mutable displayCtor = Unchecked.defaultof<MethodReference>
    let mutable intTypeRef = Unchecked.defaultof<TypeReference>
    let mutable stringTypeRef = Unchecked.defaultof<TypeReference>
    let mutable parser = Unchecked.defaultof<CommentParser>
    // Ugly hack to track the last property will resolve it in future
    let mutable lastProperty = Unchecked.defaultof<PropertyDefinition>
    let missingProperties = new List<string>()
    
    let GetDescriptionAttribute(fullName : string) = 
        let description = parser.GetPropertySummary(fullName)
        
        let calculatedDesc = 
            if description <> "" then 
                if description = "AUTO" then 
                    self.Log(sprintf "> Type description is AUTO: %s" fullName)
                    self.Log(sprintf "> Getting description from: T:%s" lastProperty.PropertyType.FullName)
                    // Auto is only applicable for property
                    parser.GetPropertySummary(sprintf "T:%s" lastProperty.PropertyType.FullName)
                else 
                    self.Log(">> DescriptionAttribute added.")
                    description
            else 
                self.Log(">> Description is missing.")
                missingProperties.Add(fullName)
                description
        
        let descAttribute = new CustomAttribute(descriptionCtor)
        descAttribute.ConstructorArguments.Add(new CustomAttributeArgument(stringTypeRef, calculatedDesc))
        (descAttribute, calculatedDesc)
    
    let GetDisplayAttribute(name : string, description : string, customAttributes : ICustomAttributeProvider) = 
        let mutable alreadyExist = false
        
        let result = 
            let descAttr = 
                customAttributes.CustomAttributes.FirstOrDefault(fun x -> x.AttributeType.Name = displayTypeRef.Name)
            if descAttr = null then 
                let attr = new CustomAttribute(displayCtor)
                let mutable propertyName = ""
                for c in name do
                    if Char.IsUpper(c) then propertyName <- propertyName + " " + c.ToString()
                    else propertyName <- propertyName + c.ToString()
                self.Log(">> Display name: " + propertyName.Trim())
                attr.Properties.Add
                    (new CustomAttributeNamedArgument("Name", 
                                                      new CustomAttributeArgument(stringTypeRef, propertyName.Trim())))
                attr.Properties.Add
                    (new CustomAttributeNamedArgument("Description", 
                                                      new CustomAttributeArgument(stringTypeRef, description)))
                attr
            else 
                alreadyExist <- true
                descAttr
        (alreadyExist, result)
    
    let AddDataContractAttribute(t : TypeDefinition) = 
        if dataContractCtor <> Unchecked.defaultof<MethodReference> then 
            let attr = new CustomAttribute(dataContractCtor)
            t.CustomAttributes.Add(attr)
        else self.Log("> Method reference for DataContract .ctor is null.")
    
    /// Add order attribute to Class Properties, This is needed for Protobuf and other
    /// similar serializers to work properly
    let AddOrderAttribute(t : TypeDefinition, p : PropertyDefinition, counter : int) = 
        let attr = new CustomAttribute(dataMemberCtor)
        attr.Properties.Add(new CustomAttributeNamedArgument("Order", new CustomAttributeArgument(intTypeRef, counter)))
        self.Log(">> MemberAttribute added.")
        p.CustomAttributes.Add(attr)
    
    let AddAttributesToProperties(t : TypeDefinition, p : PropertyDefinition, counter : int) = 
        lastProperty <- p
        let propName = sprintf "P:%s.%s" t.FullName p.Name
        self.Log("> Property: " + propName)
        AddOrderAttribute(t, p, counter)
        let (attr, desc) = GetDescriptionAttribute(propName)
        p.CustomAttributes.Add(attr)
        match GetDisplayAttribute(p.Name, desc, p) with
        | true, _ -> self.Log(">> WARNING: DisplayAttribute already present.")
        | false, attr -> 
            self.Log(">> DisplayAttribute added.")
            p.CustomAttributes.Add(attr)
    
    let AddAttributesToFields(t : TypeDefinition, f : FieldDefinition) = 
        let fieldName = sprintf "F:%s.%s" t.FullName f.Name
        self.Log("> Field: " + fieldName)
        let (attr, desc) = GetDescriptionAttribute(fieldName)
        f.CustomAttributes.Add(attr)
        match GetDisplayAttribute(f.Name, desc, f) with
        | true, _ -> self.Log(">> WARNING: DisplayAttribute already present.")
        | false, attr -> 
            self.Log(">> DisplayAttribute added.")
            f.CustomAttributes.Add(attr)
    
    /// <summary>
    /// Injected Module definition
    /// </summary>
    member val ModuleDefinition : ModuleDefinition = null with get, set
    
    /// <summary>
    /// Injected Assembly FilePath
    /// </summary>
    member val AssemblyFilePath : string = null with get, set
    
    /// <summary>
    /// Injected Solution DirectoryPath
    /// </summary>
    member val SolutionDirectoryPath : string = null with get, set
    
    /// <summary>
    /// Injected Log Info
    /// </summary>
    member val LogInfo : Action<string> = null with get, set
    
    member this.Log(m) = this.LogInfo.Invoke(m)
    
    /// <summary>
    /// Set all type and method references
    /// </summary>
    member this.SetReferences() = 
        descriptionTypeRef <- this.ModuleDefinition.Import(typeof<DescriptionAttribute>)
        descriptionCtor <- this.ModuleDefinition.Import
                               (typeof<DescriptionAttribute>.GetConstructor([| typeof<string> |]))
        dataContractTypeRef <- this.ModuleDefinition.Import(typeof<DataContractAttribute>)
        dataContractCtor <- this.ModuleDefinition.Import(typeof<DataContractAttribute>.GetConstructor(Type.EmptyTypes))
        dataMemberTypeRef <- this.ModuleDefinition.Import(typeof<DataMemberAttribute>)
        dataMemberCtor <- this.ModuleDefinition.Import(typeof<DataMemberAttribute>.GetConstructor(Type.EmptyTypes))
        displayTypeRef <- this.ModuleDefinition.Import(typeof<DisplayAttribute>)
        displayCtor <- this.ModuleDefinition.Import(typeof<DisplayAttribute>.GetConstructor(Type.EmptyTypes))
        intTypeRef <- this.ModuleDefinition.Import(typeof<Int32>)
        stringTypeRef <- this.ModuleDefinition.Import(typeof<string>)
        ()
    
    /// <summary>
    /// Fody Entry point
    /// </summary>
    member this.Execute() = 
        this.Log("Executing AutoDocumention weaver")
        this.Log("Assembly Path: " + this.AssemblyFilePath)
        let assemblyDirectory = Path.GetDirectoryName(this.AssemblyFilePath)
        this.Log("Assembly Directory: " + assemblyDirectory)
        let documentationFilePath = this.AssemblyFilePath.Replace(".dll", ".xml").Replace("obj", "bin")
        if File.Exists(documentationFilePath) then this.Log("XML Documentation file: " + documentationFilePath)
        else this.Log("XML Documentation file not found: " + documentationFilePath)
        try 
            parser <- new CommentParser(documentationFilePath)
        with e -> this.Log("Loading of documentation file failed: " + e.Message)
        this.SetReferences()
        for t in this.ModuleDefinition.Types do
            if t.FullName.StartsWith("FlexSearch.Api.Validation.") 
               || t.FullName.StartsWith("FlexSearch.Api.AttributeDocumentation") 
               || t.FullName.StartsWith("FlexSearch.Api.Resources") || t.FullName.StartsWith("FlexSearch.Api.Constants") 
               || t.FullName.StartsWith("FlexSearch.Api.OperationMessage") 
               || t.FullName.StartsWith("FlexSearch.Api.Error") || t.FullName.StartsWith("FlexSearch.Api.") = false then 
                ()
            else 
                this.Log("Starting operation for type: " + t.FullName)
                AddDataContractAttribute(t)
                let (attr, desc) = GetDescriptionAttribute(sprintf "T:%s" t.FullName)
                t.CustomAttributes.Add(attr)
                if t.IsEnum then 
                    for f in t.Fields do
                        if f.Name.StartsWith("value__") then ()
                        else AddAttributesToFields(t, f)
                else 
                    let mutable i = 0
                    for prop in t.Properties do
                        if prop.Name.StartsWith("value__") then ()
                        else AddAttributesToProperties(t, prop, i)
                        i <- i + 1
                ()
        this.Log("----------------------------")
        this.Log(sprintf "Missing Properties Log: %i" missingProperties.Count)
        this.Log("----------------------------")
        missingProperties |> Seq.iter (fun x -> this.Log(x))
        ()
