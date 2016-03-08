namespace FlexSearch.Tests.ParserTests

open FlexSearch.Tests
open FParsec
open FlexSearch.Core

[<AutoOpen>]
module ParserTestHelpers = 
    let parser = new FlexParser() :> IFlexParser
    
    let test1 p str = 
        match FParsec.CharParsers.run p str with
        | Success(_, _, _) -> ()
        | Failure(errorMsg, _, _) -> raise <| invalidOp (sprintf "%A" errorMsg)
    
    let test2 str = 
        match parser.Parse(str) with
        | Ok(_) -> ()
        | Fail(errorMsg) -> raise <| invalidOp (sprintf "%A" errorMsg)
    
    let testFails str = 
        match parser.Parse str with
        | Ok(_) -> raise <| invalidOp (sprintf "Parser shouldn't parse expression: %A" str)
        | Fail(_) -> ()
    
    let test3 str = 
        match ParseFunctionCall(str) with
        | Ok(ast) -> ast
        | Fail(errorMsg) -> raise <| invalidOp (errorMsg.ToString())

open Swensen.Unquote

type SearchParserTests() = 
    member __.``Single escape character should be accepted``() = test1 FlexSearch.Core.Parsers.constant "'abc \\' pqr'"
    
    [<InlineData("anyof(abc, 'a')")>]
    [<InlineData("not anyOf(abc, 'a')")>]
    [<InlineData("(anyOf(abc, '1'))")>]
    [<InlineData("anyOf(abc, 'as', -filter)")>]
    [<InlineData("anyOf(abc, 'as', -filter, -boost '21')")>]
    [<InlineData("anyOf(abc, 'as', -boost '21', -filter)")>]
    [<InlineData("anyOf(abc, 'as', '21', 'true',-boost)")>]
    [<InlineData("anyOf(abc, 'a') and anyOf(pqr, 'b')")>]
    [<InlineData("anyOf(abc, 'a') or anyOf(pqr, 'b')")>]
    [<InlineData("anyOf(abc, 'a') and ( anyOf(pqr, 'b'))")>]
    [<InlineData("(anyOf(abc, 'a')) and anyOf(pqr, 'b')")>]
    [<InlineData("((((((anyOf(abc, 'a')))))) and (anyOf(pqr, 'b')))")>]
    [<InlineData("anyOf(abc, 'a') and anyOf(pqr, 'b') or anyOf(abc, '1')")>]
    [<InlineData("anyOf(abc, 'sdsd', '2', '3')")>]
    [<InlineData("gt(abc, '12')")>]
    [<InlineData("atLeast3Of(abc, '12')")>]
    [<InlineData("atLeastOf3(abc, '12')")>]
    [<InlineData("3LeastOf(abc, '12')")>]
    [<InlineData("ge(abc, '1\\'2')")>]
    [<InlineData("not (anyOf(abc, 'sdsd') and anyOf(abc, 'asasa')) and anyOf(pqr, 'asas')")>]
    [<InlineData("anyOf(abc, 'a') AND anyOf(pr, 'b')")>]
    [<InlineData("anyOf(i1, @i2,@i1,'-2')")>]
    member __.``Simple expression should parse`` (sut : string) = test2 sut
    
    
    [<InlineData("anyof(abc,'1234')")>]
    [<InlineData("anyOf ( abc ,'a1234')")>]
    [<InlineData("anyOf ( abc , 'a1234' )")>]
    member __.``Expressions with spacing issues should parse`` (sut : string) = test2 sut

    [<InlineData("anyof(a,'a') and anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') AND anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') And anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') or anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') OR anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') Or anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') and not anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') AND NOT anyof(a,'a')")>]
    [<InlineData("anyof(a,'a') And Not anyof(a,'a')")>]
    member __.``Operator casing tests`` (sut : string) = test2 sut

type MethodParserTests() = 
    
    member __.``Simple method call syntax should succeed``() = 
        let actual = test3 "functionName()"
        let expected = ("functionName", Array.empty<string>)
        test <@ actual = expected @>
    
    member __.``Method call will multiple params should succeed``() = 
        let actual = test3 "functionName('a' , 'b'             , 'c')"
        let expected = ("functionName", [| "a"; "b"; "c" |])
        test <@ actual = expected @>
    
    member __.``Invalid Method call will not succeed``() = test <@ failed <| ParseFunctionCall("functionName(   ") @>
