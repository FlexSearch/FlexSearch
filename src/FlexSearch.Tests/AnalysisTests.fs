module AnalysisTests

open FlexSearch.Api.Models
open FlexSearch.Api.Constants
open FlexSearch.Core
open Swensen.Unquote
open System.IO

type FlexAnalyzerBuilderTests() = 
        
    member __.``Should build successfully for a known tokenizer`` (sut : Analyzer) = 
        sut.Filters <- [||]
        sut.Tokenizer <- new Tokenizer(TokenizerName = "standard")
        test <@ succeeded <| Analysis.buildFromAnalyzerDto sut = true @>
        
    member __.``Should not build successfully for an unknown tokenizer`` (sut : Analyzer) = 
        sut.Filters <- [||]
        test <@ Analysis.buildFromAnalyzerDto sut = fail (TokenizerNotFound(sut.AnalyzerName, sut.Tokenizer.TokenizerName)) @>
        
    member __.``Should build successfully for a known filter`` (sut : Analyzer, filter : Filter) = 
        filter.FilterName <- "lowercase"
        filter.Parameters.Clear()
        sut.Filters <- [| filter |]
        sut.Tokenizer <- new Tokenizer(TokenizerName = "standard")
        test <@ succeeded <| Analysis.buildFromAnalyzerDto sut = true @>
        
    member __.``Should not build successfully for an unknown filter`` (sut : Analyzer, filter : Filter) = 
        sut.Filters <- [||]
        filter.FilterName <- "unknown"
        filter.Parameters.Clear()
        sut.Filters <- [| filter |]
        sut.Tokenizer <- new Tokenizer(TokenizerName = "standard")
        test <@ Analysis.buildFromAnalyzerDto sut = fail (FilterNotFound(sut.AnalyzerName, filter.FilterName)) @>
    
    member __.``Should build a analyzer using synonym filter`` (sut : Analyzer, filter : Filter) =
        sut.Filters <- [||]
        filter.FilterName <- "synonym"
        filter.Parameters.Clear()
        File.WriteAllText(ResourcesFolder +/ "synonym.txt", "easy,simple,clear")
        filter.Parameters.Add("synonyms", "synonym.txt")
        sut.Tokenizer <- new Tokenizer(TokenizerName = "standard")
        sut.Filters <- [| filter |]
        let result = Analysis.buildFromAnalyzerDto sut
        test <@ succeeded <| result @>
        let analyzer = extract <| result
        let output = parseTextUsingAnalyzer(analyzer, "test", "easy")
        test <@ output.ToArray() = [| "easy"; "simple"; "clear" |] @>
