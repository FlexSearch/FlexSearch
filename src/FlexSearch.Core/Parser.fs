// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open System
open System.Collections.Generic
open System.Linq

/// <summary>
/// Represents the Values which can be used in the query string
/// </summary>
type Value = 
    | SingleValue of string
    | ValueList of string list

/// <summary>
/// Acceptable Predicates for a query
/// </summary>
type Predicate = 
    | NotPredicate of Predicate
    | Condition of FieldName : string * Operator : string * Value : Value * Parameters : Dictionary<string, string> option
    | OrPredidate of Lhs : Predicate * Rhs : Predicate
    | AndPredidate of Lhs : Predicate * Rhs : Predicate

/// <summary>
/// FlexParser interface
/// </summary>
type IFlexParser = 
    abstract Parse : string -> Choice<Predicate, IMessage>

[<AutoOpen>]
module Parsers = 
    open FParsec
    open FParsec.CharParsers
    open FParsec.Primitives
    open System
    open System.Collections.Generic
    open System.Linq
    
    let ws = spaces
    let str_ws s = pstringCI s .>> ws
    
    /// String literal parser. Takes '\' as espace character
    /// Based on: http://www.quanttec.com/fparsec/tutorial.html
    let stringLiteralAsString : Parser<string, unit> = 
        let normalChar = satisfy (fun c -> c <> '\\' && c <> '\'')
        let escapedChar = pstring "\\'" |>> (fun _ -> '\'')
        let backslash = (pchar '\\') .>> followedBy (satisfy <| (<>) '\'')
        between (pstring "\'") (pstring "\'")
            (manyChars (normalChar <|> escapedChar <|> backslash))
        .>> ws
                
    let stringLiteral = stringLiteralAsString |>> SingleValue 
        
    let listOfValues = (str_ws "[" >>. sepBy1 stringLiteralAsString (str_ws ",") .>> str_ws "]") |>> ValueList .>> ws
    
    /// Value parser
    /// Note: THe order of choice is important as stringLiteral uses
    /// character backtracking.This is done to avoid the use of attempt.
    let value = choice [ stringLiteral; listOfValues ]
    
    /// Identifier implementation. Alphanumeric character without spaces
    let identifier = 
        many1SatisfyL (fun c -> c <> ' ' && c <> '(' && c <> ')' && c <> ':' && c <> ''') 
            "Field name should be alpha number without '(', ')' and ' '." .>> ws
    
    let DictionaryOfList(elements : (string * string) list) = 
        let result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        for (key, value) in elements do
            result.Add(key, value)
        result
    
    // ----------------------------------------------------------------------------
    // Query string parser 
    // Format: fieldname:'value',fieldname:'value',fieldname:'value'
    // ----------------------------------------------------------------------------
    let private keyValue = identifier .>>. (str_ws ":" >>. ws >>. stringLiteralAsString) .>> ws
    let private keyValuePairs = (sepBy keyValue (str_ws ",")) |>> DictionaryOfList .>> ws
    let private keyValuePairsBetweenBracket = between (str_ws "{") (str_ws "}") keyValuePairs .>> ws
    let private queryStringParser : Parser<_, unit> = ws >>. keyValuePairs .>> eof
    let private queryStringParserWithBracket : Parser<_, unit> = ws >>. keyValuePairsBetweenBracket .>> eof
    
    /// <summary>
    /// Search profile query string parser 
    /// Format: fieldname:'value',fieldname:'value',fieldname:'value'
    /// </summary>
    /// <param name="input"></param>
    let ParseQueryString(input : string, withBrackets : bool) = 
        let parse (queryString) (parser) = 
            match run parser queryString with
            | Success(result, _, _) -> ok result
            | Failure(errorMsg, _, _) -> Operators.fail <| QueryStringParsingError(errorMsg)
        assert (input <> null)
        if withBrackets then queryStringParserWithBracket |> parse input
        else queryStringParser |> parse input
    
    /// <summary>
    /// Boost parser implemented using optional argument for optimization
    /// </summary>
    //let boost = opt (str_ws "boost" >>. pint32 .>> ws)
    let parameters = opt (ws >>. keyValuePairsBetweenBracket .>> ws)
    
    // ----------------------------------------------------------------------------
    /// <summary>
    /// Method to implement predicate matching
    /// Syntax: {FieldName} {Operator} {SingleValue|MultiFieldValue} {optional Boost}
    /// Example: firstname eq 'a'
    /// </summary>
    let predicate = pipe4 identifier identifier value parameters (fun l o r b -> Condition(l, o, r, b))
    
    type Assoc = Associativity
    
    /// Generates all possible case combinations for the key words
    let private orCases = [ "or"; "oR"; "Or"; "OR" ]
    
    let private andCases = [ "and"; "anD"; "aNd"; "aND"; "And"; "AnD"; "ANd"; "AND" ]
    let private notCases = [ "not"; "noT"; "nOt"; "nOT"; "Not"; "NoT"; "NOt"; "NOT" ]
    
    /// <summary>
    /// Default Parser for query parsing. 
    /// Note: The reason to create a parser class is to hide FParsec OperatorPrecedenceParser
    /// as it is not thread safe. This class will be created using object pool
    /// </summary> 
    [<Sealed>]
    type FlexParser() = 
        inherit PooledObject()
        let opp = new OperatorPrecedenceParser<Predicate, unit, unit>()
        let expr = opp.ExpressionParser
        
        let term = 
            // Use >>? to avoid the usage of attempt
            choice [ (str_ws "(" >>? expr .>> str_ws ")")
                     predicate ]
        
        let Parser : Parser<_, unit> = ws >>. expr .>> eof
        
        do 
            opp.TermParser <- term
            orCases 
            |> List.iter (fun x -> opp.AddOperator(InfixOperator(x, ws, 1, Assoc.Left, fun x y -> OrPredidate(x, y))))
            andCases 
            |> List.iter (fun x -> opp.AddOperator(InfixOperator(x, ws, 2, Assoc.Left, fun x y -> AndPredidate(x, y))))
            notCases |> List.iter (fun x -> opp.AddOperator(PrefixOperator(x, ws, 3, true, fun x -> NotPredicate(x))))
        
        interface IFlexParser with
            member __.Parse(input : string) = 
                assert (input <> null)
                match run Parser input with
                | Success(result, _, _) -> ok result
                | Failure(errorMsg, _, _) -> Operators.fail <| QueryStringParsingError errorMsg

    // ----------------------------------------------------------------------------
    // Function Parser 
    // Format: functionName('param1','param2','param3')
    // ----------------------------------------------------------------------------
    let private funParameter = stringLiteralAsString .>> ws
    let private funParameters = (sepBy funParameter (str_ws ",")) .>> ws
    let private commaSeparatedParamsBetweenBrackets = between (str_ws "(") (str_ws ")") funParameters .>> ws
    let private funParser = pipe2 identifier commaSeparatedParamsBetweenBrackets (fun name p -> (name, p.ToArray()))
    
    /// Parses a function call with format functionName('param1','param2','param3')
    let ParseFunctionCall(input : string) = 
        let parse (queryString) (parser) = 
            match run parser queryString with
            | Success(result, _, _) -> ok result
            | Failure(errorMsg, _, _) -> Operators.fail <| MethodCallParsingError(errorMsg)
        assert (input <> null)
        funParser |> parse input




    // ----------------------------------------------------------------------------
    // Signature Documentation Parser
    // ----------------------------------------------------------------------------

    /// Generic definition
    type Definition(typ) =
        member val Type = typ with get, set
        member val TypeName = defString with get, set
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

    let test p text =
        match run p text with
        | Success(r,_,_) -> r
        | Failure(e,_,_) -> printfn "%A" e; failwith e