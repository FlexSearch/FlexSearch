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

type VariableName = string

type FunctionName = string

// Valid parameters that can be passed to a clause
type FunctionParameter = 
    | Variable of VariableName // e.g. @IGNORE, @firstname
    | Constant of string // e.g. 'Vladimir', '26'
    | Boost of int32 // e.g. Boost 12
    | ConstantScore of int32 // e.g. ConstantScore 12
    | UseDefault of string // e.g. UseDefault 'abc'
    | Filter
    | MatchAll
    | MatchNone
    | MatchDefault

/// Acceptable Predicates for a query
type Predicate = 
    | NotPredicate of Predicate
    | Clause of FunctionName * FieldName * FunctionParameter list
    | OrPredidate of Lhs : Predicate * Rhs : Predicate
    | AndPredidate of Lhs : Predicate * Rhs : Predicate

/// FlexParser interface
type IFlexParser = 
    abstract Parse : string -> Result<Predicate>

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
        between (pstring "\'") (pstring "\'") (manyChars (normalChar <|> escapedChar <|> backslash)) .>> ws
    
    let constant = stringLiteralAsString |>> Constant
    
    /// Identifier implementation. Alphanumeric character without spaces
    let identifier = 
        many1SatisfyL (fun c -> 
            " ():',"
            |> String.exists ((=) c)
            |> not) "Field name should be alpha number without '(', ')' and ' '."
        .>> ws
    
    let anyCheck checks item = checks |> Seq.fold (fun acc value -> acc || value item) false
    
    let funcName = 
        many1SatisfyL (anyCheck [ isLetter
                                  isDigit
                                  (=) '_' ]) "Function name should only have letters, digits and underscores"
        .>> ws
    
    // Field parser
    let field : Parser<FieldName, unit> = ws >>. identifier
    
    // Search Profile parser
    let variable = 
        str_ws "@" >>. identifier 
        .>> followedByL (choice [ str_ws ","
                                  str_ws ")" ]) 
                "The variable name should be followed by either a comma or a closing round bracket"
        |>> Variable
    
    let filter = str_ws "filter" |>> fun _ -> Filter
    let matchAll = str_ws "matchall" |>> fun _ -> MatchAll
    let matchNone = str_ws "matchNone" |>> fun _ -> MatchNone
    let matchDefault = str_ws "matchDefault" |>> fun _ -> MatchDefault
    let boost = str_ws "boost" >>. pint32 |>> Boost
    let constantScore = str_ws "constantscore" >>. pint32 |>> ConstantScore
    let useDefault = str_ws "UseDefault" >>. identifier |>> UseDefault
    let computableValue = 
        choice [ constant; variable; filter; matchAll; matchNone; matchDefault; boost; constantScore; useDefault ]
    let parameters = sepBy computableValue (str_ws ",")
    let clause = 
        pipe2 (ws >>. funcName) (str_ws "(" >>. field .>>. parameters .>> str_ws ")") 
            (fun funcName (fn, parameters) -> Clause(funcName, fn, parameters))
    
    let tryParsing parser text = 
        match run (ws >>. parser .>> eof) text with
        | Success(result, _, _) -> ok result
        | Failure(errorMsg, _, _) -> Operators.fail <| MethodCallParsingError(errorMsg)
    
    type Assoc = Associativity
    
    /// Generates all possible case combinations for the key words
    let private orCases = [ "or"; "oR"; "Or"; "OR" ]
    
    let private andCases = [ "and"; "anD"; "aNd"; "aND"; "And"; "AnD"; "ANd"; "AND" ]
    let private notCases = [ "not"; "noT"; "nOt"; "nOT"; "Not"; "NoT"; "NOt"; "NOT" ]
    
    /// Default Parser for query parsing. 
    /// Note: The reason to create a parser class is to hide FParsec OperatorPrecedenceParser
    /// as it is not thread safe. This class will be created using object pool
    [<Sealed>]
    type FlexParser() = 
        let opp = new OperatorPrecedenceParser<Predicate, unit, unit>()
        let expr = opp.ExpressionParser
        
        let term = 
            // Use >>? to avoid the usage of attempt
            choice [ (str_ws "(" >>? expr .>> str_ws ")")
                     clause ]
        
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
                | Failure(errorMsg, _, _) -> Operators.fail <| QueryStringParsingError(errorMsg, input)
    
    // ----------------------------------------------------------------------------
    // Function Parser for Computed Scripts
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
