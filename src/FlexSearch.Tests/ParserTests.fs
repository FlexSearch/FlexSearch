module ParserTests

open FsUnit
open Fuchu
open FlexSearch.Core
open FlexSearch.Core.FlexParser
open System
open System.Threading

let parser = new FlexSearch.Core.FlexParser.Parser()

let test str =
    try
        let res = parser.Parse(str)
        ()
    with
    | ex -> failtest(ex.Message)
    Assert.AreEqual(1, 1)


[<Tests>]
let parserTests =
    testList "Parser Tests" [
        testList "Simple Expressions" [
            for exp in
                [
                1, "abc eq 'a'"
                2, "abc not eq 'a'"
                3, "abc eq 'as' boost 21"
                4, "abc eq 'a' and pqr eq 'b'"
                5, "abc eq 'a' or pqr eq 'b'"
                6, "abc eq 'a' and (pqr eq 'b')"
                7, "(abc eq 'a') and pqr eq 'b'"
                8, "((((((abc eq 'a'))))) and (pqr eq 'b'))"
                ] ->
            testCase (sprintf "%i: Function call should parse" (fst(exp))) <| fun _ -> test (snd(exp))
        ]
    ]



