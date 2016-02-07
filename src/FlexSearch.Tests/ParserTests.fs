module ParserTests

open FParsec
open FlexSearch.Core

let parser = new FlexParser() :> IFlexParser

let test p str = 
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

type SearchParserTests() = 
    member __.``Single escape character should be accepted``() = 
        test FlexSearch.Core.Parsers.constant "'abc \\' pqr'"
    
    [<InlineData("anyOf(abc, 'a')")>]
    [<InlineData("not anyOf(abc, 'a')")>]
    [<InlineData("(anyOf(abc, '1'))")>]
    [<InlineData("boost(anyOf(abc, 'as'), '21')")>]
    [<InlineData("applyDelete(boost(anyOf(abc, 'as'), '21'), 'true')")>]
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
    [<Ignore>]
    member __.``Simple expression should parse`` (sut : string) = test2 sut
    
    [<InlineData("anyOf(abc, add('1','2'))")>]
    [<InlineData("anyOf(abc, add('1'))")>]
    [<InlineData("anyOf(abc, add(@field1,@field2))")>]
    [<InlineData("anyOf(i1, add(@i2,@i1,'-2'))")>]
    [<InlineData("anyof(abc, add('1',max(@field1,@field2)))")>]
    [<InlineData("anyOf(abc, any('true','false','false'))")>]
    [<InlineData("gt(abc, sqrt(add(haversin(@delta),multiply(cos(@fi1),cos(@fi2)))))")>]
    [<InlineData("boost(endswith(field, 'value'), '32')")>]
    member __.``Expression with function should parse`` (sut : string) = test2 sut

    [<InlineData("anyOf(abc, isnull(fieldName))")>]
    [<InlineData("anyOf(abc, add('2', fieldName))")>]
    member __.``Expression with function that has field name without hashtag shouldn't parse`` (sut : string) = testFails sut

    [<InlineData("anyOf(abc, fieldName)")>]
    [<InlineData("anyOf(abc, fieldName1, fieldName2)")>]
    member __.``Expression with value as field name without quotes shouldn't parse`` (sut : string) = testFails sut

    [<InlineData("anyOf(abc, #fieldName)")>]
    [<InlineData("anyOf(abc, @fieldName))")>]
    [<InlineData("anyOf((abc, 'x')")>]
    [<InlineData("anyOf((abc, @fieldName))")>]
    [<InlineData("boost(anyOf(abc, @fieldName), anyOf(abc, @fieldName))")>]
    [<InlineData("boost('32', anyOf(abc, @fieldName))")>]
    member __.``Random expressions that should fail`` (sut : string) = testFails sut

    [<InlineData("f1: 'v1',f2 : 'v2'", 2)>]
    [<InlineData(" f1:  'v1' , f2 : 'v2'", 2)>]
    [<InlineData("   f1           : 'v1'     ", 1)>]
    [<InlineData("        f1: 'v1',f2:'v2',f3 : 'v3'", 3)>]
    [<InlineData("f1 : 'v\\'1',f2 : 'v2'", 2)>]
    [<InlineData("f1 : '1\\\2',f2 : 'v2'", 2)>]
    [<InlineData("name:'X Fit Gym Ltd',address1_line1:'Friday Street',address1_line2:'',address1_line3:'',address1_city:'CHORLEY',address1_postalcode:'PR6 OAA',emailaddress1:'matt.grimshaw-xfitgymchorley@hotmail.co.uk'", 7)>]
    [<Ignore>]
    member __.``Search Profile QueryString should parse`` (sut : string, expected : int) = 
        match ParseQueryString(sut, false) with
        | Ok(result) -> <@ result.Count = expected @>
        | Fail(e) -> raise <| invalidOp (sprintf "%A" e)
    
    [<InlineData("anyof(abc,'1234')")>]
    [<InlineData("anyOf ( abc ,'a1234')")>]
    [<InlineData("anyOf ( abc , 'a1234' )")>]
    [<Ignore>]
    member __.``Expressions with spacing issues should parse`` (sut : string) = test2 sut

let test3 str = 
    match ParseFunctionCall(str) with
    | Ok(ast) -> ast
    | Fail(errorMsg) -> raise <| invalidOp (errorMsg.ToString())

open Swensen.Unquote

type MethodParserTests() = 
    
    member __.``Simple method call syntax should succeed``() =
        let actual = test3 "functionName()"
        let expected = ("functionName", Array.empty<string>)
        test <@ actual = expected @>

    member __.``Method call will multiple params should succeed``() =
        let actual = test3 "functionName('a' , 'b'             , 'c')"
        let expected = ("functionName", [| "a"; "b"; "c"|])
        test <@ actual = expected @>

    member __.``Invalid Method call will not succeed``() =
        test <@ failed <| ParseFunctionCall("functionName(   ") @>
