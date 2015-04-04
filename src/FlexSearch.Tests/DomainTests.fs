module DomainTests

open FlexSearch.Core
open System.Collections.Generic
open Swensen.Unquote

module AnalyzerTests = 
    
    type BuilderTests() = 
        member __.``Should build successfully for a known tokenizer``(sut : Analyzer.Dto) = 
            sut.Filters.Clear()
            sut.Tokenizer <- new Tokenizer.Dto(TokenizerName = "standard")
            test <@ succeeded <| Analyzer.build sut = true @>
        
        member __.``Should not build successfully for an unknown tokenizer``(sut : Analyzer.Dto) = 
            sut.Filters.Clear()
            test <@ failed <| Analyzer.build sut = true @>