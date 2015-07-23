#I "../src/build"
#I "../src/build/Lib"
#load "validator.fsx"
#r "../src/build/FlexSearch.Core.dll"
#r "../src/build/Lib/nancy.dll"
#r "../src/build/Lib/swagger.objectmodel.dll"
#r "../src/build/Lib/nancy.swagger.dll"

open System
open System.IO
open Nancy.Swagger
open Swagger.ObjectModel.ApiDeclaration
open FlexSearch.Core
open FlexSearch.SigDocValidator

// ----------------------------------------------------------------------------
// Generator for the Swagger JSON file
// ----------------------------------------------------------------------------
module SwaggerGenerator =
    let swaggerPath = __SOURCE_DIRECTORY__ + @"\swagger.json"

    let generateDefinitions (dtos : Type seq) =
        dtos
        |> Seq.map (fun dto -> 
            let model = (new SwaggerModelData(dto)).ToModel() 
                        |> Seq.find (fun m -> m.Id = dto.Name)
            model.ToJson())
        |> Seq.fold (fun acc value -> acc + "," + value) ""
        |> fun json -> "{definitions:[" + json.Substring(1) + "]}"
        |> fun json -> File.WriteAllText(swaggerPath, json)

    generateDefinitions coreDtos

    