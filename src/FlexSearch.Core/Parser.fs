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

// Represents the parts/items which can be used in the query string
type FieldName = string
type Constant = 
    | SingleValue of string
    | ValueList of string list
    | SearchProfileField of FieldName
    | Function of Name : string * Params : Constant seq
type FieldFunction = FieldFunction of Name : string * FieldName : FieldName * Params : Constant seq
type Variable =
    | Field of FieldName
    | Function of FieldFunction

/// <summary>
/// Acceptable Predicates for a query
/// </summary>
type Predicate = 
    | NotPredicate of Predicate
    | Condition of Variable : Variable * Operator : string * Constant : Constant * Parameters : Dictionary<string, string> option
    | FuncCondition of FieldFunction
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
                
    let stringLiteral = stringLiteralAsString |>> SingleValue 
        
    let listOfValues = (str_ws "[" >>. sepBy1 stringLiteralAsString (str_ws ",") .>> str_ws "]") |>> ValueList .>> ws
    
    /// Identifier implementation. Alphanumeric character without spaces
    let identifier = 
        many1SatisfyL (fun c -> " ():'," |> String.exists ((=)c) |> not)
            "Field name should be alpha number without '(', ')' and ' '." .>> ws

    let anyCheck checks item = checks |> Seq.fold (fun acc value -> acc || value item) false

    let funcName = many1SatisfyL 
                        (anyCheck [isLetter; isDigit; (=) '_']) 
                        "Function name should only have letters, digits and underscores"
    // Field parser
    let field : Parser<FieldName, unit> = identifier
    
    // Search profile field parser
    let spField = str_ws "#" >>. identifier |>> SearchProfileField

    let constFunc, constFuncImpl = createParserForwardedToRef<Constant, unit>()
    let fieldFunc, fieldFuncImpl = createParserForwardedToRef<FieldFunction, unit>()

    // Constant function parser
    // E.g. average('24')
    //      upper_Case('lower')
    //      concat('T', lower('HIS'))
    //      concat(#firstname, ' is a badass')
    do constFuncImpl := 
        let searchProfileField = 
            spField .>> followedByL 
                (choice [str_ws ","; str_ws ")"])
                "The field name should be followed by either a comma or a closing round bracket" 
        let parameters = sepBy (choice [ stringLiteral; listOfValues; attempt constFunc; searchProfileField ])
                               (str_ws ",")
        pipe2 (ws >>. funcName )
              (str_ws "(" >>. parameters .>> str_ws ")")
              (fun name prms -> Constant.Function(name, prms))

    // FieldFunction parser
    // E.g. average(cost)
    //      upper_Case(firstname)
    //      endswith(firstname, 'Luke')
    do fieldFuncImpl :=
        let parameters = optional (str_ws ",") >>. sepBy (choice [ stringLiteral; listOfValues; attempt constFunc ])
                                      (str_ws ",")

        pipe2 (ws >>. funcName )
              (str_ws "(" >>. field .>>. parameters .>> str_ws ")")
              (fun funcName (fldName,prms) -> FieldFunction(funcName, fldName, prms))

    let ParseConstFunction text =
        match run (ws >>. constFunc .>> eof) text with
        | Success(result, _, _) -> ok result
        | Failure(errorMsg, _, _) -> Operators.fail <| MethodCallParsingError(errorMsg) 
    
    let ParseFieldFunction text =
        match run (ws >>. fieldFunc .>> eof) text with
        | Success(result, _, _) -> ok result
        | Failure(errorMsg, _, _) -> Operators.fail <| MethodCallParsingError(errorMsg) 

    // Constant parser
    // Note: THe order of choice is important as stringLiteral uses
    // character backtracking.This is done to avoid the use of attempt.
    let constant = choice [ spField; stringLiteral; listOfValues; constFunc ]

    // Variable parser
    let variable = choice [ attempt (field .>> notFollowedBy (pstring "(") |>> Field)
                            fieldFunc |>> Function ]

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
            | Failure(errorMsg, _, _) -> Operators.fail <| QueryStringParsingError(errorMsg, queryString)
        assert (input <> null)
        if withBrackets then queryStringParserWithBracket |> parse input
        else queryStringParser |> parse input
    
    /// <summary>
    /// Boost parser implemented using optional argument for optimization
    /// </summary>
    //let boost = opt (str_ws "boost" >>. pint32 .>> ws)
    let parameters = opt (ws >>. keyValuePairsBetweenBracket .>> ws)
    
    // Method to implement predicate matching
    // Syntax: {FieldName|FieldFunction} {Operator} {SingleValue|MultiFieldValue|Function} {optional Boost}
    //         OR 
    //         {FieldFunction}
    // Example: firstname eq 'a'
    //          endswith(firstname, 'blue')
    let predicate = 
        let normalCond = pipe4 variable identifier constant parameters (fun v o c b -> Condition(v, o, c, b))
        let funcCond = fieldFunc |>> FuncCondition
        choice [ attempt normalCond; funcCond]
    
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
