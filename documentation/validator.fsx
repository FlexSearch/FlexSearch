#I "../src/build"
#I "../src/build/Lib"
#load "parser.fsx"
#r "../src/build/FlexSearch.Core.dll"
#r "../src/build/Lib/microsoft.owin.dll"

namespace FlexSearch

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open FlexSearch.Core
open FlexSearch.ScriptSettings
open FlexSearch.SigDocParser

// ----------------------------------------------------------------------------
// Validator for the Swagger documentation in the .fsi files
// ----------------------------------------------------------------------------
module SigDocValidator =
    open MessageHelpers

    // Validation helpers
    type ValErr<'T,'U> =
        | IsEqual of expected : 'T * given : 'T * context : 'U
        | SameLength of expected : 'T seq * given : 'U seq
        | NotEmpty of given : string * context : 'U
        | InList of item : 'T * list : 'T seq
        interface IMessage with
            member this.LogProperty() = (MessageKeyword.Default, MessageLevel.Verbose)
            member this.OperationMessage() = 
                match this with
                | IsEqual(expected,given,context) -> sprintf "Expected %A, but given %A in the given context:\n%A" expected given context
                | SameLength(expected,given) -> 
                    sprintf 
                        "Expected length to be equal to %d, but found %d.\nGiven list:%A\nExpected List:%A" 
                        (expected |> Seq.length) (given |> Seq.length) given expected
                | NotEmpty(_,context) -> sprintf "The given string should not be empty:%A" context
                | InList(item,list) -> sprintf "The given item '%A' is not in the list: %A" item list
                |> caseToMsg this

    let isEqual given expected context = 
        if expected = given then ok()
        else fail <| IsEqual(expected, given, context)

    let notEmpty given context =
        if String.IsNullOrEmpty given then fail <| NotEmpty(given, context)
        else ok()

    let sameLen givenList expectedList =
        if (givenList |> Seq.length) = (expectedList |> Seq.length) then ok()
        else fail <| SameLength(expectedList, givenList)

    let isInList item (list : 'a seq) =
        if list |> Seq.exists (fun x -> x = item) then ok()
        else fail <| InList(item, list)

    let areEqual givenList expectedList =
        let rec areEqualR gl el lastCheck =
            match gl with
            | [] -> lastCheck >>= fun _ -> ok()
            | _ -> 
                let check = isEqual (List.head gl) (List.head el) expectedList
                lastCheck >>= fun _ -> areEqualR (List.tail gl) (List.tail el) check
        let initialCheck = sameLen givenList expectedList
        areEqualR givenList expectedList initialCheck

    let compareLists l1 l2 compareFunc = 
        // Check the collections have the same size
        sameLen l1 l2
        // Then compare each dto pair
        >>= fun _ ->
            Seq.zip l1 l2
            |> Seq.fold 
                (fun acc value -> acc >>= fun _ -> compareFunc (fst value) (snd value))
                (ok())

    let getMethodAndUriFromType (typ :Type) =
        typ.GetCustomAttribute(typeof<NameAttribute>) 
        :?> NameAttribute
        |> fun a -> a.Name
        |> fun name ->
            let r = name.Split('-')
            if r.Length <> 2 then failwith <| "HTTP name format not recognized: " + name
            else (r.[0], r.[1])

    let getUriParams (uri : string) =
        uri.Split('/') 
        |> Seq.filter (fun x -> x.Length > 0 && x.[0] = ':')
        |> Seq.map (fun x -> x.Substring(1))

    let isNotInternal (t : Type) = t.GetCustomAttribute(typeof<InternalAttribute>) = null

    let printDefsVsCores (defs : Definition seq) (cores : Type seq) =
        printfn "Documentation Definitions:"
        defs |> Seq.iter (fun x -> printfn "%A" x.Name)
        printfn "\nCore Types:"
        cores |> Seq.iter (fun x -> printfn "%A" x.Name)
        printfn ""

    // Definitions from .fsi files
    let defs =
        Directory.EnumerateFiles(corePath, "*.fsi")
        |> Seq.map File.ReadAllText
        |> Seq.map (exec definitions)
        |> Seq.collect id

    // Get the data needed for validation
    let docDtos = defs |> Seq.filter (fun x -> x.Type = "dto") 
                       |> Seq.sortBy (fun x -> x.Name)
    let coreDtos = Assembly.GetAssembly(typeof<DtoBase>).GetTypes()
                   |> Seq.filter (fun t -> t.IsSubclassOf(typeof<DtoBase>))
                   |> Seq.filter isNotInternal
                   |> Seq.sortBy (fun t -> t.Name)
    let docWss = defs |> Seq.filter (fun x -> x.Type = "ws")
                      |> Seq.sortBy (fun x -> x.Name)
    let coreWss = Assembly.GetAssembly(typeof<Http.IHttpHandler>).GetTypes()
                  |> Seq.filter (fun t -> not t.IsAbstract && t.GetInterfaces() |> Seq.contains typeof<Http.IHttpHandler>)
                  |> Seq.filter isNotInternal
                  |> Seq.sortBy (fun t -> t.Name)
    let docEnums = defs |> Seq.filter (fun x -> x.Type = "enum")
                        |> Seq.sortBy (fun x -> x.Name)
    let coreEnums = Assembly.GetAssembly(typeof<Index>).GetTypes()
                    |> Seq.filter (fun t -> t.IsSubclassOf(typeof<Enum>))
                    |> Seq.filter isNotInternal
                    |> Seq.sortBy (fun t -> t.Name)

    // Execute the actual validation
    let validateDtos() =
        //printDefsVsCores docDtos coreDtos

        let compareDto (doc : Definition) (core : Type) =
            printfn "Checking %s DTO" doc.Name
            isEqual doc.Type "dto" doc
            >>= fun _ -> isEqual doc.Name core.Name core
            >>= fun _ -> 
                core.GetProperties()
                // Public properties that have getter and setter
                |> Seq.where (fun p -> 
                    (p.GetSetMethod() <> null && p.GetGetMethod() <> null)
                    || (p.GetGetMethod() <> null && p.GetGetMethod().IsStatic))
                |> Seq.map (fun p -> p.Name)
                |> Seq.sort
                |> Seq.toList
                |> areEqual (doc.Properties.Keys |> Seq.sort |> Seq.toList)
        
        compareLists docDtos coreDtos compareDto

    let validateWss() =
        //printDefsVsCores docWss coreWss

        let compareWs (doc : Definition) (core: Type) =
            printfn "Checking %s HTTP Web Service" doc.Name
            isEqual doc.Type "ws" doc
            >>= fun _ -> isEqual doc.Name core.Name core
            >>= fun _ ->
                let (meth, uri) = core |> getMethodAndUriFromType
                notEmpty meth ("HTTP Method", core)
                >>= fun _ -> notEmpty uri ("HTTP URI", core)
                // Check that the URI parameter names actually match the ones in the uri
                >>= fun _ -> 
                    let uriParams = getUriParams uri
                    doc.UriParams.Keys
                    |> Seq.fold (fun acc value -> acc >>= fun _ -> isInList value uriParams) (ok())

        compareLists docWss coreWss compareWs

    let validateEnums() =
        //printDefsVsCores docEnums coreEnums

        let compareEnum (doc: Definition) (core: Type) =
            printfn "Checking %s Discriminated Union" doc.Name
            isEqual doc.Type "enum" doc
            >>= fun _ -> isEqual doc.Name core.Name core
            >>= fun _ ->
                Enum.GetNames(core) 
                |> Seq.sort
                |> Seq.toList
                |> areEqual (doc.Options.Keys |> Seq.sort |> Seq.toList)
                
        // Not all Enumerations need to be in the documentation. Some are only used internally
        compareLists docEnums coreEnums compareEnum

    let validationSequence =
        printfn "\nValidating DTOs\n"
        validateDtos() 
        >>= fun _ -> printfn "\nValidating HTTP Web Services\n"; ok()
        >>= validateWss 
        >>= fun _ -> printfn "\nValidating Discriminated Unions\n"; ok()
        >>= validateEnums
        >>= fun _ -> printfn "\n----------------\n"; ok()

    match validationSequence with
    | Choice1Of2(x) -> printfn "Validation Succeeded"
    | Choice2Of2(x) -> printfn "Validation Failed. Check Operational logs in Event Viewer for details."; Log.log x