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

[<AutoOpen>]
module Parsers = 
    open FParsec
    open FParsec.CharParsers
    open FParsec.Primitives
    open FlexSearch.Api.Message
    open FlexSearch.Core.Pool
    open System
    open System.Collections.Generic
    open System.Linq
    
    let ws = spaces
    let str_ws s = pstringCI s .>> ws
    
    /// <summary>
    /// String literal parser. Takes '\' as espace character
    /// Based on: http://www.quanttec.com/fparsec/tutorial.html
    /// </summary>
    let stringLiteral = 
        let escape = anyOf "'" |>> function 
                     | c -> string c // every other char is mapped to itself
        between (pstring "\'") (pstring "\'") 
            (stringsSepBy (manySatisfy (fun c -> c <> '\'' && c <> '\\')) (pstring "\\" >>. escape)) |>> SingleValue 
        .>> ws
    
    let stringLiteralAsString = 
        let escape = anyOf "'" |>> function 
                     | c -> string c // every other char is mapped to itself
        between (pstring "\'") (pstring "\'") 
            (stringsSepBy (manySatisfy (fun c -> c <> '\'' && c <> '\\')) (pstring "\\" >>. escape)) .>> ws
    
    let stringLiteralList = 
        let escape = anyOf "'" |>> function 
                     | c -> string c // every other char is mapped to itself
        between (pstring "\'") (pstring "\'") 
            (stringsSepBy (manySatisfy (fun c -> c <> '\'' && c <> '\\')) (pstring "\\" >>. escape)) .>> ws
    
    let listOfValues = (str_ws "[" >>. sepBy1 stringLiteralList (str_ws ",") .>> str_ws "]") |>> ValueList .>> ws
    
    /// <summary>
    /// Value parser
    /// Note: THe order of choice is important as stringLiteral uses
    /// character backtracking.This is done to avoid the use of attempt.
    /// </summary>
    let value = choice [ stringLiteral; listOfValues ]
    
    /// <summary>
    /// Indentifier implementation. Alphanumric character without spaces
    /// </summary>
    let identifier = 
        many1SatisfyL (fun c -> c <> ' ' && c <> '(' && c <> ')' && c <> ':') 
            "Field name should be alphanumber without '(', ')' and ' '." .>> ws
    
    // ----------------------------------------------------------------------------
    // Query string parser 
    // Format: fieldname:'value',fieldname:'value',fieldname:'value'
    // ----------------------------------------------------------------------------
    let private keyValue = identifier .>>. (str_ws ":" >>. ws >>. stringLiteralAsString) .>> ws
    let private keyValuePairs = (sepBy keyValue (str_ws ",")) |>> Map.ofList .>> ws
    let private keyValuePairsBetweenBracket = between (str_ws "{") (str_ws "}") keyValuePairs .>> ws
    let private queryStringParser : Parser<_, unit> = ws >>. keyValuePairsBetweenBracket .>> eof
    
    /// <summary>
    /// Searchprofile query string parser 
    /// Format: fieldname:'value',fieldname:'value',fieldname:'value'
    /// </summary>
    /// <param name="input"></param>
    let ParseQueryString(input : string) = 
        match run queryStringParser input with
        | Success(result, _, _) -> Choice1Of2(result)
        | Failure(errorMsg, _, _) -> 
            Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.QUERYSTRING_PARSING_ERROR, errorMsg))
    
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
        
        member this.Parse(input : string) = 
            match run Parser input with
            | Success(result, _, _) -> Choice1Of2(result)
            | Failure(errorMsg, _, _) -> 
                Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.QUERYSTRING_PARSING_ERROR, errorMsg))
    
    /// <summary>
    /// Generates an object pool for the parser
    /// </summary>
    /// <param name="poolSize"></param>
    let GetParserPool (poolSize : int) = 
        let factory() = new FlexParser()
        new ObjectPool<FlexParser>(factory, poolSize)
