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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open System
open FParsec
open FParsec.Primitives
open FParsec.CharParsers

// ----------------------------------------------------------------------------
/// Default query parser used by Flex
/// Based on some inputs from 
/// http://stackoverflow.com/questions/9233666/parsing-parenthesized-expressions
/// Simple AST parser -> http://fssnip.net/iJ
/// http://stackoverflow.com/questions/9215975/differentiating-logical-from-other-infix-operators
// ----------------------------------------------------------------------------
module FlexParser = 
  
    type Comparison =
        | Eq
        | Ne
    
    type FunctionName =
        | StartsWith
        | EndsWith

    type Constant =
        | Int32 of int
        | Float of float
        | DateTime of DateTime
        | Bool of bool
        | String of string
        | Null
    
    //type FieldName = FieldName of string
    type LeftHandExpression =
        | FieldName of string
        | Function of FunctionName: string * FunctionValue: Constant * FieldName: string
    
    type RightHandExpression =
        | Constant of Constant
          
    type SearchCondition =
        | Comparison of Comparison * LeftHandExpression * RightHandExpression
        | Or of SearchCondition * SearchCondition
        | And of SearchCondition * SearchCondition
    
    type Assoc = Associativity
    let ws = spaces
    let str_ws s = pstring s .>> ws

    let lparen = pstring "(" >>. ws
    let rparen = pstring ")" >>. ws
    let tryBetweenParens p = lparen >>? (p .>>? rparen)
    
    let trueLiteral = stringCIReturn "true" (Bool true) .>> ws
    let falseLiteral = stringCIReturn "false" (Bool false) .>> ws
    let boolLiteral = (trueLiteral <|> falseLiteral)
    let nullLiteral = stringCIReturn "null" Null .>> ws

    let quoteChar = pstring "'"
    let pChar = satisfy ((<>) '\'')
    let stringLiteral = (quoteChar >>. (manyCharsTill pChar quoteChar)) |>> String .>> ws
    let floatLiteral = pfloat |>> Float .>> ws
    let int32Literal = pint32 |>> Int32 .>> ws
    let constant =
        [ boolLiteral; nullLiteral; int32Literal; floatLiteral; stringLiteral ]
        |> choice
        |>> Constant

    let strOrSymOp str sym x = ((stringCIReturn str x) <|> (stringCIReturn sym x)) .>> ws   
    let eqOp = strOrSymOp "eq" "=" Eq    
    let neOp = strOrSymOp "ne" "<>" Ne    
    let compareOp = [ eqOp; neOp ] |> choice  
  

//    let strOp str x = (stringCIReturn str x) .>> ws
//    let startsWithOp = strOp "startswith" StartsWith
//    let endsWithOp = strOp "endsWith" EndsWith
//    let identifierName = [startsWithOp; endsWithOp] |> choice
    

    let functionIdentifier = parse {
        let! funcName = manySatisfy (fun c -> c <> ' ' && c <> '(')
        //do! spaces
        do! skipStringCI "("
        do! spaces
        let! fieldName = manySatisfy (fun c -> c <> ' ' && c <> ',')
        do! spaces
        do! skipStringCI ","
        do! spaces
        let! fieldValue = stringLiteral
        do! spaces
        do! skipStringCI ")"
        do! spaces
        return Function(funcName, fieldValue, fieldName)
        }
    
    let fieldIdentifier = parse {
        let! funcName = manySatisfy (fun c -> c <> ' ')
        do! spaces
        return FieldName(funcName)
    }

    let identifier =
        (attempt functionIdentifier) <|> fieldIdentifier

    let comparison =
        let compareExpr = pipe3 identifier compareOp constant (fun l op r -> Comparison(op, l, r))
        compareExpr <|> tryBetweenParens compareExpr
   
//    let andTerm = stringCIReturn "and" (fun l r -> And(l, r)) .>> ws
//    let orTerm = stringCIReturn "or" (fun l r -> Or(l, r)) .>> ws       


    let condOpp = OperatorPrecedenceParser()
    let searchCondition = condOpp.ExpressionParser
    condOpp.TermParser <- (attempt comparison) <|> between lparen rparen searchCondition <|> searchCondition
    condOpp.AddOperator(InfixOperator("or", ws, 2, Assoc.Left, fun l r -> Or(l, r)))    
    condOpp.AddOperator(InfixOperator("and", ws, 1, Assoc.Left, fun l r -> And(l, r)))  


//    let searchCondition, searchConditionRef = createParserForwardedToRef()
//    do searchConditionRef:=
//        chainl1 (comparison <|> between lparen rparen searchCondition)
//                (andTerm <|> orTerm)

    let filter : Parser<_,unit> = ws >>. searchCondition .>> eof