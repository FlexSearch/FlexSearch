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
    let generateSchemaFromType (typ : Type) = 
        let sBuilderType = (typedefof<SchemaBuilder<_>>).MakeGenericType(typ)
        let sBuilder = sBuilderType.GetConstructor([||]).Invoke([||])
        match typ.GetConstructor([||]) with
        | null -> ()
        | ctor -> let typInstance = ctor.Invoke([||])
                  sBuilderType.GetMethod("Default").Invoke(sBuilder, [| typInstance |]) |> ignore
        sBuilderType.GetMethod("Build").Invoke(sBuilder, [||]) :?> Schema
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


    let typeToSwaggerSchema (def : Definition) (coreType : Type) =  
        try
            // Initialize a core type to get the default values
            let instance = coreType.GetConstructor([||]).Invoke([||])

            let sBuilderType = (typedefof<SchemaBuilder<_>>).MakeGenericType(coreType)
            let sBuilder = sBuilderType.GetConstructor([||]).Invoke([||])

            // Set the default value if the type can be initialized
            match coreType.GetConstructor([||]) with
            | null -> ()
            | ctor -> let typInstance = ctor.Invoke([||])
                      sBuilderType.GetMethod("Default").Invoke(sBuilder, [| typInstance |]) |> ignore

            // Build the schema
            let schema = sBuilderType.GetMethod("Build").Invoke(sBuilder, [||]) :?> Schema

            // Populate the description
            schema.Description <- def.Summary

            // Populate the other fields
            schema.Properties
            |> Seq.iter (fun kv -> 
                kv.Value.Description <- def.Properties |> valFromKey kv.Key def
                kv.Value.Default <- coreType |> propVal instance kv.Key)

            // This is an exception for the Document dto that has a circular 
            // reference to itself. Swagger UI does not support this, therefore
            // I will remove that property
            if def.Name = "Document" then
                schema.Properties.Remove("Default") |> ignore

            (def.Name, schema)
        with
        | e -> failwithf "An error occurred while converting type %s to Swagger model:\n%s" def.Name (e |> exceptionPrinter)

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
                        rBuilder.Schema (retType |> generateSchemaFromType) |> ignore)
                    |> ignore

                    // Generate the POST or query parameters
                    let bodyType = handler |> getBodyType
                    
                    if httpMethod = "POST" || httpMethod = "PUT" then
                        let bpb = new BodyParameterBuilder()
                        bpb.Name bodyType.Name |> ignore
                        // TODO add description
                        bpb.Schema (bodyType |> generateSchemaFromType) |> ignore
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
                           |> Seq.map (fun x -> typeToSwaggerSchema (fst x) (snd x))
    let enumDefinitions() = coreEnums |> Seq.zip docEnums
                            |> Seq.map (fun x -> enumToSwaggerSchema (fst x) (snd x))
    let wsApis() = coreWss |> Seq.zip docWss
                 |> Seq.map (fun x -> wsToSwaggerPath (fst x) (snd x))
                 |> Seq.concat
                 // Concatenate the operations of the webservices that have the same uri
                 |> Seq.groupBy fst
                 |> Seq.map (fun (key,wss) ->
                        let pathItem1 = wss |> Seq.head |> snd
                        wss
                        |> Seq.skip 1
                        |> Seq.map snd
                        |> Seq.fold (fun (acc : PathItem) value -> acc.Combine value) pathItem1
                        |> ignore
                        pathItem1.Parameters <- null
                        (key, pathItem1))

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