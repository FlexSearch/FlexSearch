#r "../src/build/Lib/fparseccs.dll"
#r "../src/build/Lib/fparsec.dll"

namespace FlexSearch

open System
open System.Collections.Generic
open System.IO
open System.Reflection

[<AutoOpen>]
module ScriptSettings = 
    //let corePath = "../src/FlexSearch.Core"
    //let buildPath = "../src/build"
    let corePath = @"C:\git\FlexSearch\src\FlexSearch.Core"
    let buildPath = @"C:\git\FlexSearch\src\build"

// ----------------------------------------------------------------------------
// Signature Documentation Parser
// ----------------------------------------------------------------------------
[<AutoOpen>]
module SigDocParser =
    
    open FParsec.CharParsers
    open FParsec

    let defString = String.Empty
    let defStringDict() = new Dictionary<string,string>()

    /// Generic definition
    type Definition(typ) =
        member val Type = typ with get, set
        member val Name = defString with get, set
        member val Summary = defString with get, set
        member val Method = defString with get, set
        member val Uri = defString with get, set
        member val Params = defStringDict() with get, set
        member val Description = defString with get, set
        member val Examples = new List<string>() with get, set
        member val Options = defStringDict() with get, set
        member val Properties = defStringDict() with get, set
        override this.ToString() = 
            sprintf "%A;\n%A;\n%A;\n%A;\n%A;\n%A;\n%A;\n%A;\n%A;\n%A" this.Type this.Name this.Summary this.Method this.Uri this.Params this.Description this.Examples this.Options this.Properties

    let tryAdd (dict : Dictionary<string,string>) item = 
        try dict.Add item
        with ex -> printfn "Tried adding item %A to the dictionary containing:\n%A" item dict
                   raise ex
            

    /// Helper parsers
    let ws = spaces
    let fignore = fun _-> ignore
    let tripleQuote = pstring "\"\"\""
    let singleQuote = pstring "\""
    let tripleQuoteContent = ws >>. manyCharsTill anyChar tripleQuote
    let singleQuoteContent = manySatisfy <| (<>) '"' 
    let quote3Text = tripleQuote >>. tripleQuoteContent .>> ws
    let quote1Text = (between singleQuote singleQuote singleQuoteContent) .>> ws
    let endDef = ["# ws_"; "# dto_"; "#if dto_"; "#if enum_"]
                    |> List.map (fun str -> followedBy (pstring str))
                    |> List.append [eof]
                    |> choice
    let endif = pstring "#endif"
    let ifContent = ws >>. manyCharsTill anyChar endif
    let ifMap = manyCharsTill anyChar spaces1 .>>. ifContent

    /// Information / Property parsers
    let summary = 
        manyCharsTill anyChar spaces1
        .>>.
        (quote3Text <|> ifContent)
        |>> fun x -> fun (def : Definition) -> def.Name <- fst x; def.Summary <- snd x
    let meth =
        pstring "# meth" >>. ws >>. quote1Text
        |>> fun x -> fun (def : Definition) -> def.Method <- x
    let uri = 
        pstring "# uri" >>. ws >>. quote1Text
        |>> fun x -> fun (def : Definition) -> def.Uri <- x
    let param =
        pstring "# param_"
        >>. manyCharsTill anyChar spaces1 
        .>>. quote3Text
        |>> fun x -> fun (def : Definition) -> x |> tryAdd def.Params
    let description =
        pstring "# description" >>. ws >>. quote3Text
        |>> fun x -> fun (def : Definition) -> def.Description <- x
    let examples =
        pstring "# examples" >>. ws >>. quote3Text
        |>> fun x -> fun (def : Definition) -> 
            x.Split([| "\r\n"; "\n" |], StringSplitOptions.None) 
            |> Seq.filter (String.IsNullOrEmpty >> not)
            |> Seq.iter def.Examples.Add 
            |> ignore
    let dtoProps = 
        pstring "#if prop_" 
        >>. ifMap
        |>> fun x -> fun (def: Definition) -> x |> tryAdd def.Properties
    let enumOpt =
        pstring "#if opt_" 
        >>. ifMap
        |>> fun x -> fun (def: Definition) -> x |> tryAdd def.Options

    // Combine the parsers into a choice
    let infoCode = [meth; uri; param; description; examples; dtoProps; enumOpt]
    let ignoredContent : Parser<Definition -> unit, unit> = 
        manyCharsTill anyChar ((followedBy <| (pstring "#")) <|> eof)
        |>> fignore
    let props = choice (infoCode @ [ignoredContent])

    /// Parses any kind of definition starting after the "# def_" or "#if dto_" or "#if enum_" declaration    
    let defParser = summary .>>. manyTill props endDef

    /// Constructs a definition from the functions returned by the parsers
    let toDefinition defType (f,fs)  =
        let ws = new Definition(defType) 
        f ws
        fs |> Seq.iter (fun f -> f ws) |> ignore
        ws
    let enum_def = pstring "#if enum_" >>. defParser |>> toDefinition "enum"
    let dto_def = pstring "#if dto_" >>. defParser |>> toDefinition "dto"
    let ws_def = pstring "# ws_" >>. defParser |>> toDefinition "ws"

    /// Definitions can be either DTOs or Web Services
    let definitions : Parser<Definition list, unit> = ignoredContent >>. manyTill (ws_def <|> dto_def <|> enum_def) eof

    let exec p text =
        match run p text with
        | Success(r,_,_) -> r
        | Failure(e,_,_) -> printfn "%A" e; failwith e