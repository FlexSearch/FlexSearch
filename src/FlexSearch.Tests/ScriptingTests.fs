namespace FlexSearch.Tests

open FlexSearch.Tests
open FlexSearch.Api.Model
open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic
open System.IO
open System.Linq

type ``Scripting tests``() = 
    
    member __.``Script should compile``() = 
        let scriptSrc = """
#if INTERACTIVE
#r "../../FlexSearch.Api.dll"
#endif

module Script

open System
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open System.Collections.Generic
open Helpers

let calculate () = 3 + 4
let preIndex(document : Document) = 
    document.Set("test", "test1")
let preSearchTest(query : SearchQuery) = ()"""
        let fn = Path.GetTempFileName()
        let sn = Path.ChangeExtension(fn, "fsx")
        File.WriteAllText(sn, scriptSrc)
        let result = FSharpCompiler.compile (sn) |> extract
        test <@ result.PreIndexScript.IsSome @>
        test <@ result.PreSearchScripts.Keys.ToArray() = [| "Test" |] @>
        let doc = new Document()
        result.PreIndexScript.Value.Invoke(doc)
        test <@ doc.Fields.["test"] = "test1" @>
    
    member __.``End to end preIndex script test`` (ih : IntegrationHelper) = 
        let scriptSrc = """
module Script

open FlexSearch.Api.Model
open Helpers

let preIndex(document : Document) = 
    document.Set("i1", "100")"""
        ih |> addIndexPass
        let writer = extract <| ih.IndexService.IsIndexOnline(ih.IndexName)
        ih |> closeIndexPass
        // Dump the script to the configuration folder
        File.WriteAllText(writer.Settings.SettingsFolder +/ "script.fsx", scriptSrc)
        ih |> openIndexPass
        ih |> addDocByIdPass "1"
        ih |> refreshIndexPass
        // The above doc should have i1 = 100
        test <@ (extract <| ih.DocumentService.GetDocument(ih.IndexName, "1")).Fields.["i1"] = "100" @>
