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
    | Switch of Name : string * value : string option // e.g. -UseDefault, -Filter, -ConstantScore: '32'

/// Acceptable Predicates for a query
type Predicate = 
    | NotPredicate of Predicate
    | Clause of FunctionName * FieldName * FunctionParameter list
    | OrPredidate of Lhs : Predicate * Rhs : Predicate
    | AndPredidate of Lhs : Predicate * Rhs : Predicate
    static member Or x y = OrPredidate(x, y)
    static member And x y = AndPredidate(x, y)
    static member Not x = NotPredicate(x)

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
    
    /// Identifier implementation. Alphanumeric character without spaces
    ///Ex: identifier, firstname
    let identifier = 
        many1SatisfyL (fun c -> 
            " ():',"
            |> String.exists ((=) c)
            |> not) "Field/Operator name should be alpha number without '(', ')' and ' '."
        .>> ws
    
    /// A field to be used in a function
    let field : Parser<FieldName, unit> = ws >>. identifier .>> str_ws ","
    
    let funcName : Parser<FieldName, unit> = ws >>. identifier
    
    /// Format: String literal enclosed between single quotes 
    let constant = stringLiteralAsString |>> Constant
    
    /// Format: Starts with @ followed by an identifier
    /// Ex: @firstname 
    let variable = str_ws "@" >>. identifier |>> Variable
    
    /// Format: Starts with - followed by an identifier. A switch 
    /// may end with a value
    /// -boost '32', -useDefault 
    let switch = pipe2 (str_ws "-" >>. identifier) (opt stringLiteralAsString) (fun name value -> Switch(name, value))
    
    /// NOTE: The current choice can be replaced with a more efficient
    /// low level parser implementation to improve performance 
    let functionParameters = choice [ constant; variable; switch ]
    
    let parameters = sepBy functionParameters (str_ws ",")
    
    /// FORMAT: functionName ( fieldName, parameters )
    let clause = 
        pipe3 (ws >>. funcName) (str_ws "(" >>. field) (parameters .>> str_ws ")") 
            (fun funcName fn parameters -> Clause(funcName, fn, parameters))
    
    // Generate all possible case combinations for the key words
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
        
        let parser : Parser<_, unit> = ws >>. expr .>> eof
        
        do 
            opp.TermParser <- term
            orCases |> List.iter (fun x -> opp.AddOperator(InfixOperator(x, ws, 1, Associativity.Left, Predicate.Or)))
            andCases |> List.iter (fun x -> opp.AddOperator(InfixOperator(x, ws, 2, Associativity.Left, Predicate.And)))
            notCases |> List.iter (fun x -> opp.AddOperator(PrefixOperator(x, ws, 3, true, Predicate.Not)))
        
        interface IFlexParser with
            member __.Parse(input : string) = 
                assert (isNotNull input)
                match run parser input with
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
        assert (isNotNull input)
        funParser |> parse input
