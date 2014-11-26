namespace FlexSearch.Core.Tests

open Xunit
open FParsec
open FlexSearch.Core
open FlexSearch.Core.Parsers
open NSubstitute
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Xunit
open System
open System.Collections.Generic
open System.Threading

open Xunit.Extensions

module ``Parser Tests`` = 
    let parser = new FlexParser() :> IFlexParser
    
    let test p str = 
        match FParsec.CharParsers.run p str with
        | Success(result, _, _) -> Assert.True(true)
        | Failure(errorMsg, _, _) -> Assert.True(false, errorMsg)
    
    let test2 str = 
        match parser.Parse(str) with
        | Choice1Of2(a) -> Assert.True(true)
        | Choice2Of2(errorMsg) -> Assert.True(false, errorMsg.DeveloperMessage)


    [<Fact>]
    let ``Single escape character should be accepted`` () =
        test FlexSearch.Core.Parsers.stringLiteral "'abc \\' pqr'"

    [<Theory>]
    [<InlineData("['abc']")>]
    [<InlineData("['abc','pqr']")>]
    [<InlineData("['abc'  ,  'pqr']")>]
    [<InlineData("[         'abc'          ]")>]
    [<InlineData("[    'abc'    ]")>]
    let ``Input should be parsed for the 'List of Values'`` (sut: string) =
        test FlexSearch.Core.Parsers.listOfValues sut
    
    [<Theory>]
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
    let ``Simple expression should parse`` (sut: string) =
        test2 sut
    
    [<Theory>]
    [<InlineData("f1: 'v1',f2 : 'v2'", 2)>]
    [<InlineData(" f1:  'v1' , f2 : 'v2'", 2)>]
    [<InlineData("   f1           : 'v1'     ", 1)>]
    [<InlineData("        f1: 'v1',f2:'v2',f3 : 'v3'", 3)>]
    [<InlineData("f1 : 'v\\'1',f2 : 'v2'", 2)>]
    let ``Search Profile QueryString should parse`` (sut: string, expected: int) =
        match ParseQueryString(sut, false) with
        | Choice1Of2(result) -> Assert.Equal(result.Count, expected)
        | Choice2Of2(_) -> Assert.True(false,  "Expected query string to pass")
    
    [<Theory>]
    [<InlineData("abc ='1234'")>]
    [<InlineData("abc ='a1234'")>]
    let ``Expressions with spacing issues should parse``(sut: string) =
        test2 sut