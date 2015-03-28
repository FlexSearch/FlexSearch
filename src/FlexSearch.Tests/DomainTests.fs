module DomainTests

open FlexSearch.Core
open System.Collections.Generic
open Swensen.Unquote

module AnalyzerTests = 
    open Analyzer
    
    type BuilderTests() = 
        member __.``Should build successfully``() = 
            let sut = 
                { Tokenizer = { Tokenizer.Default with TokenizerName = "standard" }
                  Filters = new List<TokenFilter>()
                  AnalyzerName = "test" }
            test <@ succeeded <| Analyzer.build sut = true @>
        
        member __.``Should not build successfully``() = 
            let sut = 
                { Tokenizer = { Tokenizer.Default with TokenizerName = "unknown" }
                  Filters = new List<TokenFilter>()
                  AnalyzerName = "test" }
            test <@ failed <| Analyzer.build sut = true @>