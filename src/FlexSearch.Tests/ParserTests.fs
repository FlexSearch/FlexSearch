module ParserTests

open FParsec
open FlexSearch.Core

let parser = new FlexParser() :> IFlexParser

let test p str = 
    match FParsec.CharParsers.run p str with
    | Success(_, _, _) -> ()
    | Failure(errorMsg, _, _) -> raise <| invalidOp (errorMsg)

let test2 str = 
    match parser.Parse(str) with
    | Choice1Of2(_) -> ()
    | Choice2Of2(errorMsg) -> raise <| invalidOp (errorMsg.ToString())

type SearchParserTests() = 
    member __.``Single escape character should be accepted``() = 
        test FlexSearch.Core.Parsers.stringLiteral "'abc \\' pqr'"
    
    [<InlineData("['abc']")>]
    [<InlineData("['abc','pqr']")>]
    [<InlineData("['abc'  ,  'pqr']")>]
    [<InlineData("[         'abc'          ]")>]
    [<InlineData("[    'abc'    ]")>]
    [<Ignore>]
    member __.``Input should be parsed for the 'List of Values'`` (sut : string) = 
        test FlexSearch.Core.Parsers.listOfValues sut
    
    [<InlineData("abc eq 'a'")>]
    [<InlineData("not abc eq 'a'")>]
    [<InlineData("(abc eq '1')")>]
    [<InlineData("abc eq 'as' {boost: '21'}")>]
    [<InlineData("abc eq 'as' {boost:'21',applydelete:'true'}")>]
    [<InlineData("abc eq 'a' and pqr eq 'b'")>]
    [<InlineData("abc eq 'a' or pqr eq 'b'")>]
    [<InlineData("abc eq 'a' and ( pqr eq 'b')")>]
    [<InlineData("(abc eq 'a') and pqr eq 'b'")>]
    [<InlineData("((((((abc eq 'a'))))) and (pqr eq 'b'))")>]
    [<InlineData("abc eq 'a' and pqr eq 'b' or abc eq '1'")>]
    [<InlineData("abc eq ['sdsd', '2', '3']")>]
    [<InlineData("abc > '12'")>]
    [<InlineData("abc >= '12'")>]
    [<InlineData("abc >= '1\\'2'")>]
    [<InlineData("not (abc eq 'sdsd' and abc eq 'asasa') and pqr eq 'asas'")>]
    [<InlineData("abc eq 'a' AND pr eq 'b'")>]
    [<Ignore>]
    member __.``Simple expression should parse`` (sut : string) = test2 sut
    
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
        | Choice1Of2(result) -> <@ result.Count = expected @>
        | Choice2Of2(e) -> raise <| invalidOp (sprintf "%A" e)
    
    [<InlineData("abc ='1234'")>]
    [<InlineData("abc ='a1234'")>]
    [<Ignore>]
    member __.``Expressions with spacing issues should parse`` (sut : string) = test2 sut

let test3 str = 
    match ParseFunctionCall(str) with
    | Choice1Of2(ast) -> ast
    | Choice2Of2(errorMsg) -> raise <| invalidOp (errorMsg.ToString())

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
