#load "Helpers.fsx"

open Helpers
open System.IO
open System.Text.RegularExpressions
open System.Collections
open System.Linq

let swaggerPartialPath = specDir <!!> "swagger-partial.json"
let swaggerFullPath = specDir <!!> "swagger-full.json"
let glossary = File.ReadAllText (specDir <!!> "Glossary.md")

let getGlossaryItem (name : string) =
    let m = Regex.Match(glossary, sprintf "# %s([^#]+)" name, RegexOptions.IgnoreCase)
    if m.Success then m.Groups.[1].Value.Trim() else ""
    |> fun s -> s.Replace("\r", "\\r").Replace("\n", "\\n")

let injectSwagger() =
    File.Copy(swaggerPartialPath, swaggerFullPath, true)
    let mutable swaggerFull = File.ReadAllText swaggerFullPath
    
    for m in Regex.Matches(swaggerFull, "<<([^>]+)>>") do
        let itemName = m.Groups.[1].Value
        let description = getGlossaryItem itemName
        swaggerFull <- Regex.Replace(swaggerFull, sprintf "<<%s>>" itemName, description)
    
    File.WriteAllText(swaggerFullPath, swaggerFull)