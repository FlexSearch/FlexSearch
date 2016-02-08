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

// Note: The naming of the **Function**s is given by their first parameter:
// 1. If the 1st parameter is something that is computable without Lucene, then it's a ComputableFunction
// 2. If the 1st parameter is a Field, then it's a FieldFunction
// 3. If the 1st parameter is a SearchQuery, then it's a QueryFunction

// Computable values are values / functions / expressions that can be calculated before submitting a query (without Lucene).
// They ultimately translate/compute into a string value.
type ComputableValue =
    | Variable of VariableName                                      // e.g. @IGNORE, @firstname
    | Constant of string                                            // e.g. 'Vladimir', '26'
    | ComputableFunction of FunctionName * ComputableValue list     // e.g. min('34', '25'), min('34', add('1', @firstname))
// The computable function cannot appear in a query by itself. We cannot have a query 
// that only contains: q="min('10','5')". These are functions that are contained in 
// higher order functions, such as FieldFunctions

// These are functions that can appear in a query by themselves. 
// They ultimately translate into a SearchQuery.
type Function =
    // e.g. anyOf(firstname, 'Vladimir', 'Seemant'); like(addressLine1, concat('Flat ', @flatNumber))
    // In the example above, anyOf & like are FieldFunctions because they operate on a field. The field
    // will always be the first parameter. 
    // However, 'concat' is not a FieldFunction because it can be computed without using Lucene; it's 
    // a ComputableFunction.
    | FieldFunction of FunctionName * FieldName * ComputableValue list
    // e.g. boost(anyOf(firstname, 'Vladimir', 'Seemant'), 32)
    // This is a function that has only two parameters: 1st one is of type Function (that ultimately
    // translates to a SearchQuery), the second one is the value to apply to the Search Query
    | QueryFunction of FunctionName * Function * ComputableValue

/// <summary>
/// Acceptable Predicates for a query
/// </summary>
type Predicate = 
    | NotPredicate of Predicate
    | Clause of Function
    | OrPredidate of Lhs : Predicate * Rhs : Predicate
    | AndPredidate of Lhs : Predicate * Rhs : Predicate

/// <summary>
/// FlexParser interface
/// </summary>
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
        between (pstring "\'") (pstring "\'")
            (manyChars (normalChar <|> escapedChar <|> backslash))
        .>> ws
                
    let constant = stringLiteralAsString |>> Constant 
        
    /// Identifier implementation. Alphanumeric character without spaces
    let identifier = 
        many1SatisfyL (fun c -> " ():'," |> String.exists ((=)c) |> not)
            "Field name should be alpha number without '(', ')' and ' '." .>> ws

    let anyCheck checks item = checks |> Seq.fold (fun acc value -> acc || value item) false

    let funcName = many1SatisfyL 
                        (anyCheck [isLetter; isDigit; (=) '_']) 
                        "Function name should only have letters, digits and underscores"
                   .>> ws

    // Field parser
    let field : Parser<FieldName, unit> = ws >>. identifier
    
    // Search Profile parser
    let variable = 
        str_ws "@" >>. identifier 
        .>> followedByL (choice [str_ws ","; str_ws ")"])
                        "The variable name should be followed by either a comma or a closing round bracket"  
        |>> Variable

    let computableFunc, computableFuncImpl = createParserForwardedToRef<ComputableValue, unit>()
    let fieldFunc, fieldFuncImpl = createParserForwardedToRef<Function, unit>()
    let queryFunc, queryFuncImpl = createParserForwardedToRef<Function, unit>()

    let ``function`` = choice [ attempt fieldFunc; queryFunc ]
    let computableValue = choice [ constant; variable; attempt computableFunc ]

    // Computable function parser
    // E.g. average('24')
    //      upper_Case('lower')
    //      concat('T', lower('HIS'))
    //      concat(@firstname, ' is a badass')
    do computableFuncImpl := 
        let parameters = sepBy computableValue
                               (str_ws ",")
        pipe2 (ws >>. funcName)
              (str_ws "(" >>. parameters .>> str_ws ")")
              (fun name prms -> ComputableFunction(name, prms))

    // FieldFunction parser
    // E.g. exact(fistname, 'Vladimir')
    //      atLeast2Of(firstname, 'Vladimir', 'Alexandru', 'Negacevschi')
    //      upTo3WordsApart(firstname, concat('Luke ', @nickname))
    do fieldFuncImpl :=
        let parameters = optional (str_ws ",") >>. sepBy computableValue
                                                         (str_ws ",")

        pipe2 (ws >>. funcName)
              (str_ws "(" >>. field .>>. parameters .>> str_ws ")")
              (fun funcName (fldName,prms) -> FieldFunction(funcName, fldName, prms))

    // QueryFunction parser
    // E.g. boost(anyOf(firstname, 'Vladimir', 'Alexandru'), 32)
    //      boost(anyOf(firstname, 'Vladimir', 'Alexandru'), @boostValue)
    //      boost(anyOf(firstname, 'Vladimir', 'Alexandru'), min(@boostValue, 32))
    do queryFuncImpl :=
        pipe3 (ws >>. funcName) 
              (str_ws "(" >>. ``function``)
              (str_ws "," >>. computableValue .>> str_ws ")")
              (fun funcName ff cv -> QueryFunction(funcName, ff, cv))

    // Method to implement clause matching.
    // A clause can be either a query function or a field function
    // E.g. exact(firstname, 'Vladimir')
    //      boost(endswith(firstname, 'blue'))
    let clause = choice [ attempt fieldFunc; queryFunc] |>> Clause

    let tryParsing parser text =
        match run (ws >>. parser .>> eof) text with
        | Success(result, _, _) -> ok result
        | Failure(errorMsg, _, _) -> Operators.fail <| MethodCallParsingError(errorMsg) 

    // Used for testing
    let ParseComputableFunction text = tryParsing computableFunc text
    let ParseFieldFunction text = tryParsing fieldFunc text
    let ParseQueryFunction text = tryParsing queryFunc text

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
                | Failure(errorMsg, _, _) -> Operators.fail <| QueryStringParsingError (errorMsg, input)

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
