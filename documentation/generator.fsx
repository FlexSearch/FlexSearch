#I "../src/build"
#I "../src/build/Lib"
#load "validator.fsx"
#r "../src/build/FlexSearch.Core.dll"
#r "lib/Nancy.dll"
#r "lib/Swagger.ObjectModel.dll"
#r "lib/Nancy.Swagger.dll"

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open System.Collections
open System.Linq
open System.Reflection
open Nancy.Swagger
open Swagger.ObjectModel
open Swagger.ObjectModel.Builders
open FlexSearch.Core
open FlexSearch.SigDocValidator
open FlexSearch.SigDocParser

// ----------------------------------------------------------------------------
// Generator for the Swagger JSON file
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SwaggerGenerator =
    let swaggerPath = __SOURCE_DIRECTORY__ + @"\swagger.json"
    let swaggerSchemaCache = new ConcurrentDictionary<string, Schema>()

    // Helper methods
    let propVal instance propName (typ : Type) = typ.GetProperty(propName).GetValue(instance)
    let valFromKey (key : 'a) context (dict : Dictionary<'a,_>)  =
        if dict.ContainsKey(key) then dict.[key]
        else failwithf "Couldn't find key '%A' in the dictionary:\n%A\nContext:\n%A" key dict context
    let nameToHttpMethod name =
        match name with
        | "GET" -> HttpMethod.Get
        | "PUT" -> HttpMethod.Put
        | "POST" -> HttpMethod.Post
        | "DELETE" -> HttpMethod.Delete
        | _ -> failwithf "HTTP Method not implemented: %s" name
    let getBodyType (handler : Type) = handler.BaseType.GenericTypeArguments.[0]
    let getReturnType (handler : Type) = handler.BaseType.GenericTypeArguments.[1]
    let toSwaggerUri (uri : string) = 
        let parts = uri.Split('/') 
                    |> Seq.map (fun x -> if x.Length > 0 && x.[0] = ':' 
                                         then "{" + (x.Substring(1)) + "}" 
                                         else x)
        String.Join("/",parts)
    let getTypeDescription (typ : Type) =
        match defs |> Seq.tryFind (fun d -> d.Name = typ.Name) with
        | Some(d) -> if String.IsNullOrWhiteSpace d.Summary 
                     then if String.IsNullOrWhiteSpace d.Description then "N/A" else d.Description
                     else d.Summary
        | None -> "N/A"
    let allTags = new List<string>()
    let getTagFromUri (uri : string) =
        let parts = uri.Split('/')
        let tag = if parts.[1] = "" then "root" else parts.[1]
        if allTags |> Seq.exists ((=)tag) |> not then allTags.Add tag
        tag
    let getTagFromDef (def : Definition) =
        let tag = if def.WsCategory = "" then "common" else def.WsCategory
        if allTags |> Seq.exists ((=)tag) |> not then allTags.Add tag
        tag

    let rec typeToSwaggerSchema (coreType : Type) =  
        // First check if we already converted this type
        match swaggerSchemaCache.TryGetValue(coreType.Name) with
        | (true, schema) -> (coreType.Name, schema)
        | _ -> 
            // First try and find a definition we can use to complement the schema
            let def = defs |> Seq.tryFind (fun x -> x.Name = coreType.Name)
            try
                let sBuilderType = (typedefof<SchemaBuilder<_>>).MakeGenericType(coreType)
                let sBuilder = sBuilderType.GetConstructor([||]).Invoke([||])

                // Set the default value if the type can be initialized. Don't bother
                // with System types.
                // Return the instance of the current type
                let instance = 
                    if coreType.Namespace.StartsWith("System") then None
                    else 
                        match coreType.GetConstructor([||]) with
                        | null -> None
                        | ctor -> let typInstance = ctor.Invoke([||])
                                  sBuilderType.GetMethod("Default").Invoke(sBuilder, [| typInstance |]) |> ignore
                                  Some(typInstance)

                // Build the schema
                let schema = sBuilderType.GetMethod("Build").Invoke(sBuilder, [||]) :?> Schema

                // Populate the other fields of the schema only if we were able to generate an instance
                match instance with
                | Some (inst) -> 
                    // Populate the properties 
                    coreType.GetProperties(BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.Static)
                    |> Seq.iter (fun prop ->
                        // Handle recursive properties
                        if prop.PropertyType = coreType 
                        then schema.Properties.Add(prop.Name, schema)
                        else schema.Properties.Add(prop.Name, 
                                                   typeToSwaggerSchema prop.PropertyType |> snd))

                    // Populate the description of the type and the properties
                    match def with
                    | Some(d) -> 
                        schema.Description <- d.Summary
                        schema.Properties
                        |> Seq.iter (fun kv -> 
                            kv.Value.Description <- d.Properties |> valFromKey kv.Key d)

                        // This is an exception for the Document dto that has a circular 
                        // reference to itself. Swagger UI does not support this, therefore
                        // I will remove that property
                        if d.Name = "Document" then
                            schema.Properties.Remove("Default") |> ignore
                    | None -> ()

                    // Populate the defaults of the properties
                    schema.Properties
                    |> Seq.iter (fun kv -> kv.Value.Default <- coreType |> propVal inst kv.Key)
                | None -> ()

                // Add the result to the cache, then return it
                swaggerSchemaCache.TryAdd(coreType.Name, schema) |> ignore
                (coreType.Name, schema)
            with
            | e -> failwithf "An error occurred while converting type %s to Swagger model:\n%s" coreType.Name (e |> exceptionPrinter)

    let enumToSwaggerSchema (def : Definition) (coreEnum : Type) =
        try
            let schema = new Schema()
            schema.Description <- def.Summary
            schema.Properties <- new Dictionary<string, Schema>()
            let enumProp = new Schema()
            enumProp.Type <- "string"
            enumProp.Enum <- (Enum.GetValues(coreEnum)).Cast<Object>() |> Seq.map (fun x -> x.ToString())
            schema.Properties.Add(def.Name, enumProp)
            (def.Name, schema)
        with
        | e -> failwithf "An error occurred while converting Enum %s to Swagger model:\n%s" def.Name (e |> exceptionPrinter)

    let wsToSwaggerPath (ws : Definition) (handler : Type) =
        try
            let (meth,uri) = handler |> getMethodAndUriFromType
            
            meth.Split('|')
            |> Seq.map (fun httpMethod ->
                let piBuilder = new PathItemBuilder(httpMethod |> nameToHttpMethod)
                piBuilder.Operation(fun opBuilder -> 
                    //opBuilder.Tag "All" |> ignore
                    opBuilder.Tag (ws |> getTagFromDef) |> ignore
                    opBuilder.Summary ws.Summary |> ignore
                    opBuilder.Description ws.Description |> ignore
                    opBuilder.OperationId (handler.Name.Replace("Handler", "")) |> ignore
                    opBuilder.Response (fun rBuilder -> 
                        let retType = handler |> getReturnType
                        rBuilder.Description (retType |> getTypeDescription)  |> ignore
                        rBuilder.Schema (retType |> typeToSwaggerSchema |> snd) |> ignore)
                    |> ignore

                    // Generate the POST or query parameters
                    let bodyType = handler |> getBodyType
                    
                    if httpMethod = "POST" || httpMethod = "PUT" then
                        let bpb = new BodyParameterBuilder()
                        bpb.Name bodyType.Name |> ignore
                        // TODO add description
                        bpb.Schema (bodyType |> typeToSwaggerSchema |> snd) |> ignore
                        bpb.Build() |> opBuilder.Parameter |> ignore
                    else
                        ws.Params 
                        |> Seq.iter (fun kv -> 
                            let pb = new Parameter()
                            pb.Name <- kv.Key
                            pb.In <- ParameterIn.Query
                            pb.Description <- kv.Value
                            pb |> opBuilder.Parameter |> ignore)
                        
                    // Generate the Path/URI parameters
                    getUriParams uri
                    |> Seq.iter (fun uriParam ->
                        let pb = new Parameter()
                        pb.Name <- uriParam
                        pb.In <- ParameterIn.Path
                        pb.Required <- Nullable true
                        match ws.UriParams.TryGetValue(uriParam) with
                        | (true,uri) -> pb.Description <- uri
                        | _ -> ()
                        
                        pb |> opBuilder.Parameter |> ignore)) 
                |> ignore
                
                (uri |> toSwaggerUri, piBuilder.Build()))
        with
        | e -> failwithf "An error occurred while converting Web Service %s to Swagger operation:\n%s" ws.Name (e |> exceptionPrinter)

    let dtoDefinitions() = coreDtos |> Seq.zip docDtos
                           |> Seq.map (fun (doc, core) -> typeToSwaggerSchema core)
    let enumDefinitions() = coreEnums |> Seq.zip docEnums
                            |> Seq.map (fun (doc, core) -> enumToSwaggerSchema doc core)
    let wsApis() = coreWss |> Seq.zip docWss
                   |> Seq.map (fun (def, core) -> wsToSwaggerPath def core)
                   |> Seq.concat
                   // Concatenate the operations of the webservices that have the same uri
                   |> Seq.groupBy fst
                   |> Seq.map (fun (key,wss) ->
                        let pathItem1 = wss |> Seq.head |> snd
                        let combined = 
                            wss
                            |> Seq.skip 1
                            |> Seq.map snd
                            |> Seq.fold (fun (acc : PathItem) value -> acc.Combine value) pathItem1
                            
                        combined.Parameters <- null
                        (key, combined))

    let generateApiDeclaration() =
        let rb = new SwaggerRootBuilder()
        rb.Info (new InfoBuilder("FlexSearchAPI", "1.0")) |> ignore
        rb.Host "localhost:9800" |> ignore

        wsApis()
        |> Seq.iter (fun wsApi -> rb.Path(fst wsApi, snd wsApi) |> ignore)
        dtoDefinitions()
        |> Seq.iter (fun def -> rb.Definition(fst def, snd def) |> ignore)
        enumDefinitions()
        |> Seq.iter (fun def -> rb.Definition(fst def, snd def) |> ignore)

        rb.Tag (new Tag(Name = "All")) |> ignore
        allTags
        |> Seq.iter (fun t -> rb.Tag (new Tag(Name = t)) |> ignore)

        rb.Build()

    let generateJson() =
        validateDocumentation()
        try
            let json = generateApiDeclaration().ToJson()
            File.WriteAllText(swaggerPath, json)
        with
        | e -> failwithf "%s" (exceptionPrinter e)

    generateJson()