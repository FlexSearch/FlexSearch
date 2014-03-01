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
    open System.Linq
    
    /// <summary>
    /// Represents the Values which can be used in the querystring
    /// </summary>
    type Value = 
        | SingleValue of string
        | ValueList of string list
        
        member this.GetValueAsList() = 
            match this with
            | SingleValue(v) -> [ v ]
            | ValueList(v) -> v
        
        member this.GetValueAsArray() = 
            match this with
            | SingleValue(v) -> 
                if String.IsNullOrWhiteSpace(v) then Choice2Of2(MessageConstants.MISSING_FIELD_VALUE)
                else Choice1Of2([| v |])
            | ValueList(v) -> 
                if v.Length = 0 then Choice2Of2(MessageConstants.MISSING_FIELD_VALUE)
                else Choice1Of2(v.ToArray())
    
    /// <summary>
    /// Acceptable Predicates for a query
    /// </summary>
    type Predicate = 
        | NotPredicate of Predicate
        | Condition of FieldName : string * Operator : string * Value : Value * Boost : int option
        | OrPredidate of Lhs : Predicate * Rhs : Predicate
        | AndPredidate of Lhs : Predicate * Rhs : Predicate
    
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
    /// Boost parser implemented using optional argument for optimization
    /// </summary>
    let boost = opt (str_ws "boost" >>. pint32 .>> ws)
    
    /// <summary>
    /// Indentifier implementation. Alphanumric character without spaces
    /// </summary>
    let identifier = 
        many1SatisfyL (fun c -> c <> ' ' && c <> '(' && c <> ')') 
            "Field name should be alphanumber without '(', ')' and ' '." .>> ws
    
    /// <summary>
    /// Method to implement predicate matching
    /// Syntax: {FieldName} {Operator} {SingleValue|MultiFieldValue} {optional Boost}
    /// Example: firstname eq 'a'
    /// </summary>
    let predicate = pipe4 identifier identifier value boost (fun l o r b -> Condition(l, o, r, b))
    
    type Assoc = Associativity
    
    /// <summary>
    /// Default Parser for query parsing. 
    /// Note: The reason to create a parser class is to hide FParsec OperatorPrecedenceParser
    /// as it is not thread safe. This class will be created using object pool
    /// </summary>    
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
            opp.AddOperator(InfixOperator("or", ws, 1, Assoc.Left, fun x y -> OrPredidate(x, y)))
            opp.AddOperator(InfixOperator("and", ws, 2, Assoc.Left, fun x y -> AndPredidate(x, y)))
            opp.AddOperator(PrefixOperator("not", ws, 3, true, fun x -> NotPredicate(x)))
        
        member this.Parse(input : string) = 
            match run Parser input with
            | Success(result, _, _) -> Choice1Of2(result)
            | Failure(errorMsg, _, _) -> 
                Choice2Of2(OperationMessage.WithDeveloperMessage(MessageConstants.QUERYSTRING_PARSING_ERROR, errorMsg))
    
    /// <summary>
    /// Generates an object pool for the parser
    /// </summary>
    /// <param name="poolSize"></param>
    let getParserPool (poolSize : int) = 
        let factory() = new FlexParser()
        new ObjectPool<FlexParser>(factory, poolSize)
