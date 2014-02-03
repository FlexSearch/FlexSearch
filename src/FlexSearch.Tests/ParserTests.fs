module ParserTests

open FsUnit
open Fuchu
open FlexSearch.Core
open FlexSearch.Core.FlexParser
open System
open System.Threading
open FParsec

let test p str =
    match run p str with
    | Success(result, _, _)   -> Assert.AreEqual(1, 1)
    | Failure(errorMsg, _, _) -> failtest(errorMsg)

[<Tests>]
let parserTests =
    testList "Object pool tests" [
        testCase "Simple eq expression should parse" <| fun _ -> test filter "abc eq 'a'"
        testCase "Simple = expression should parse" <| fun _ -> test filter "abc = 'a'"
        testCase "Simple or expression should parse" <| fun _ -> test filter "abc eq 'a' or abc eq 'b'"
        testCase "Simple and expression should parse" <| fun _ -> test filter "abc eq 'a' and abc eq 'b'"
        testCase "Simple functions should parse" <| fun _ -> test filter "startsWith( abc , 'a' ) eq true"
        testCase "Simple mixed and/or expression should parse" <| fun _ -> test filter "pqr eq 'test' and (abc eq 'a' or abc eq 'b' )"

        testList "Function Calls" [
            for exp in 
                [
                1, "startsWith( abc , 'a' )"
                2, "startsWith( abc ,   'a' )"
                3, "startsWith(abc , 'a' )"
                4, "startsWith(abc,'a')" 
                5, "startsWith( abc ,'a' )" 
                6, "startsWith( abc , 'a' )"
                ] ->
            testCase (sprintf "%i: Function call should parse" (fst(exp))) <| fun _ -> test functionIdentifier (snd(exp))
        ]
    ]



