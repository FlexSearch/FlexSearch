module ParserTests

open FParsec
open FlexSearch.Core
open FlexSearch.Core.Parsers
open FsUnit
open Fuchu
open System
open System.Threading

let parser = new FlexParser()

let test p str = 
    match FParsec.CharParsers.run p str with
    | Success(result, _, _) -> Assert.AreEqual(1, 1)
    | Failure(errorMsg, _, _) -> Assert.AreEqual(1, 2, errorMsg)

let test2 str = 
    match parser.Parse(str) with
    | Choice1Of2(a) -> Assert.AreEqual(1, 1)
    | Choice2Of2(errorMsg) -> Assert.AreEqual(1, 2, errorMsg.DeveloperMessage)

[<Tests>]
let parserTests = 
    testList "Parser Tests" 
        [ testCase "Single escape character should be accepted" 
          <| fun _ -> test FlexSearch.Core.Parsers.stringLiteral "'abc \\' pqr'"
          
          testList "List of Values test" 
              [ for exp in [ 1, "['abc']"
                             2, "['abc','pqr']"
                             3, "['abc'  ,  'pqr']"
                             4, "[         'abc'          ]"
                             5, "[    'abc'    ]" ] -> 
                    testCase (sprintf "%i: Function call should parse" (fst (exp))) 
                    <| fun _ -> test FlexSearch.Core.Parsers.listOfValues (snd (exp)) ]
          
          testList "Simple Expressions" 
              [ for exp in [ 1, "abc eq 'a'"
                             2, "not abc eq 'a'"
                             3, "(abc eq '1')"
                             4, "abc eq 'as' boost 21"
                             5, "abc eq 'a' and pqr eq 'b'"
                             6, "abc eq 'a' or pqr eq 'b'"
                             7, "abc eq 'a' and ( pqr eq 'b')"
                             8, "(abc eq 'a') and pqr eq 'b'"
                             9, "((((((abc eq 'a'))))) and (pqr eq 'b'))"
                             10, "abc eq 'a' and pqr eq 'b' or abc eq '1'"
                             11, "abc eq ['sdsd', '2', '3']"
                             12, "abc > '12'"
                             13, "abc >= '12'"
                             14, "abc >= '1\\'2'"
                             15, "not (abc eq 'sdsd' and abc eq 'asasa') and pqr eq 'asas'"
                             16, "abc eq 'a' AND pr eq 'b'" ] -> 
                    testCase (sprintf "%i: Function call should parse" (fst (exp))) <| fun _ -> test2 (snd (exp)) ] ]
