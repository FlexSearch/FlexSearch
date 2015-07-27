#I "../src/build"
#I "../src/build/Lib"
#load "validator.fsx"
#r "../src/build/FlexSearch.Core.dll"
#r "../src/build/Lib/nancy.dll"
#r "../src/build/Lib/swagger.objectmodel.dll"
#r "../src/build/Lib/nancy.swagger.dll"

open System
open System.IO
open System.Collections.Generic
open System.Collections
open System.Linq
open Nancy.Swagger
open Swagger.ObjectModel.ApiDeclaration
open Swagger.ObjectModel.ResourceListing
open FlexSearch.Core
open FlexSearch.SigDocValidator
open FlexSearch.SigDocParser

// ----------------------------------------------------------------------------
// Generator for the Swagger JSON file
// ----------------------------------------------------------------------------
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

    let typeToSwaggerModel (def : Definition) (coreType : Type) =  
        try
            // Initialize a core type to get the default values
            let instance = coreType.GetConstructor([||]).Invoke([||])

            // Generate the Swagger model
            let model = (new SwaggerModelData(coreType)).ToModel() 
                        |> Seq.find (fun m -> m.Id = coreType.Name)

            // Populate the other fields
            model.Description <- def.Summary
            model.Properties
            |> Seq.iter (fun kv -> 
                kv.Value.Description <- def.Properties |> valFromKey kv.Key def
                kv.Value.DefaultValue <- coreType |> propVal instance kv.Key)

            // This is an exception for the Document dto that has a circular 
            // reference to itself. Swagger UI does not support this, therefore
            // I will remove that property
            if def.Name = "Document" then
                model.Properties.Remove("Default") |> ignore

            model
        with
        | e -> failwithf "An error occurred while converting type %s to Swagger model:\n%s" def.Name (e |> exceptionPrinter)

    let enumToSwaggerModel (def : Definition) (coreEnum : Type) =
        try
            // Generate the Swagger model
            let modelData = new SwaggerModelData(coreEnum)
            let enumProp = new SwaggerModelPropertyData(Name = coreEnum.Name, Type = typeof<string>)
            enumProp.Enum <- (Enum.GetValues(coreEnum)).Cast<Object>() |> Seq.map (fun x -> x.ToString())
            modelData.Properties <- [| enumProp |]
            let model = modelData.ToModel() |> Seq.head
            model.Description <- def.Summary
            model
        with
        | e -> failwithf "An error occurred while converting Enum %s to Swagger model:\n%s" def.Name (e |> exceptionPrinter)

    let wsToSwaggerApi (ws : Definition) (handler : Type) =
        try
            let api = new Api()
            api.Description <- ws.Description
            let (meth,uri) = handler |> getMethodAndUriFromType
            api.Path <- uri |> toSwaggerUri
            api.Operations <- 
                // Create an Operation for each HTTP method
                meth.Split('|')
                |> Seq.map (fun httpMethod ->
                    let route = new SwaggerRouteData()
                    route.ApiPath <- uri
                    route.OperationMethod <- nameToHttpMethod httpMethod
                    route.OperationSummary <- ws.Summary
                    route.OperationNotes <- ws.Description
                    route.OperationNickname <- handler.Name.Replace("Handler", "")
                    route.OperationModel <- handler |> getReturnType

                    // Generate the POST or query parameters
                    route.OperationParameters <- 
                        let bodyType = handler |> getBodyType

                        if httpMethod = "POST" || httpMethod = "PUT" then
                            let sp = new SwaggerParameterData()
                            sp.ParamType <- ParameterType.Body
                            sp.ParameterModel <- bodyType
                            sp.DefaultValue <- bodyType.GetConstructor([||]).Invoke([||])
                            [sp] |> List.toSeq
                        else
                            ws.Params 
                            |> Seq.map (fun kv -> 
                                let sp = new SwaggerParameterData()
                                sp.Name <- kv.Key
                                sp.Description <- kv.Value
                                sp.ParamType <- ParameterType.Query
                                sp.ParameterModel <- typeof<string>
                                sp)
                        |> fun pars -> pars.ToList()
                        
                    // Generate the Path/URI parameters
                    getUriParams uri
                    |> Seq.iter (fun uriParam ->
                        let sp = new SwaggerParameterData()
                        sp.Name <- uriParam
                        sp.ParamType <- ParameterType.Path
                        sp.ParameterModel <- typeof<string>
                        sp.Required <- true
                        match ws.UriParams.TryGetValue(uriParam) with
                        | (true,uri) -> sp.Description <- uri
                        | _ -> ()

                        route.OperationParameters.Add(sp))

                    route.ToOperation())
            api
        with
        | e -> failwithf "An error occurred while converting Web Service %s to Swagger operation:\n%s" ws.Name (e |> exceptionPrinter)

    let dtoModels() = coreDtos |> Seq.zip docDtos
                    |> Seq.map (fun x -> typeToSwaggerModel (fst x) (snd x))
    let enumModels() = coreEnums |> Seq.zip docEnums
                     |> Seq.map (fun x -> enumToSwaggerModel (fst x) (snd x))
    let wsApis() = coreWss |> Seq.zip docWss
                 |> Seq.map (fun x -> wsToSwaggerApi (fst x) (snd x))
                 // Concatenate the operations of the webservices that have the same uri
                 |> Seq.groupBy (fun x -> x.Path)
                 |> Seq.map (fun (key,wss) ->
                        let ws1 = wss |> Seq.head
                        ws1.Operations <-
                            wss 
                            |> Seq.fold (fun acc value -> acc |> Seq.append value.Operations) Seq.empty
                        ws1)

    let generateApiDeclaration() =
        let apiDecl = new ApiDeclaration()
        apiDecl.BasePath <- new Uri("http://localhost:9800")
        apiDecl.Apis <- wsApis()
        apiDecl.Models <- (enumModels() |> Seq.append <| dtoModels()).ToDictionary(fun x -> x.Id)
        apiDecl
        
    let generateJson() =
        validateDocumentation()
        try
            let json = generateApiDeclaration().ToJson()
            File.WriteAllText(swaggerPath, json)
        with
        | e -> failwithf "%s" (exceptionPrinter e)

    generateJson()