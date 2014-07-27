open System
open FlexSearch.Documention
let GenerateGlossary() = 
    ReferenceDocumentation.GenerateGlossary()
    printfn "Missing Definition"
    for def in ReferenceDocumentation.missingDefinitions do
        printfn "%s" def
    printfn "Missing def count:%i" (ReferenceDocumentation.missingDefinitions.Count)

[<EntryPoint>]
let main argv = 
    //GenerateGlossary()
    GenerateExamples.GenerateIndicesExamples()
    Console.ReadKey() |> ignore
    0 // return an integer exit code
