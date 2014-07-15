namespace ``Analysis Extension Tests``

open Xunit
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.Validator
open FlexSearch.TestSupport
open NSubstitute
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Xunit
open System.Collections.Generic
open Xunit.Extensions

module Helpers = 
    let GetFactory() = 
        let factory = Substitute.For<IFactoryCollection>()
        factory.FilterFactory.GetModuleByName("notexists").Returns(Choice2Of2(MessageConstants.FILTER_NOT_FOUND)) 
        |> ignore
        factory.FilterFactory.GetModuleByName("exists")
               .Returns(Choice1Of2(new StandardFilterFactory() :> IFlexFilterFactory)) |> ignore
        factory.TokenizerFactory.GetModuleByName("notexists").Returns(Choice2Of2(MessageConstants.TOKENIZER_NOT_FOUND)) 
        |> ignore
        factory.TokenizerFactory.GetModuleByName("exists")
               .Returns(Choice1Of2(new StandardTokenizerFactory() :> IFlexTokenizerFactory)) |> ignore
        factory

module ``Token filter validator tests`` = 
    [<Theory>]
    [<AutoMockData>]
    let ``Token filter should not validate`` (sut : TokenFilter) = 
        sut.FilterName <- "notexists"
        let factoryCollection = Helpers.GetFactory()
        sut.Validate(factoryCollection) |> ExpectErrorCode MessageConstants.FILTER_NOT_FOUND
        factoryCollection.FilterFactory.Received().GetModuleByName("notexists") |> ignore
    
    [<Theory>]
    [<AutoMockData>]
    let ``Token filter should validate`` (sut : TokenFilter) = 
        sut.FilterName <- "exists"
        let factoryCollection = Helpers.GetFactory()
        TestChoice (sut.Validate(factoryCollection)) true
        factoryCollection.FilterFactory.Received().GetModuleByName("exists") |> ignore

module ``Tokenizer validator tests`` = 
    [<Theory>]
    [<AutoMockData>]
    let ``Tokenizer should not validate`` (sut : Tokenizer) = 
        sut.TokenizerName <- "notexists"
        let factoryCollection = Helpers.GetFactory()
        sut.Validate(factoryCollection) |> ExpectErrorCode MessageConstants.TOKENIZER_NOT_FOUND
        factoryCollection.TokenizerFactory.Received().GetModuleByName("notexists") |> ignore
    
    [<Theory>]
    [<AutoMockData>]
    let ``Tokenizer should validate`` (sut : Tokenizer) = 
        sut.TokenizerName <- "exists"
        let factoryCollection = Helpers.GetFactory()
        TestChoice (sut.Validate(factoryCollection)) true
        factoryCollection.TokenizerFactory.Received().GetModuleByName("exists") |> ignore

module ``Analyzer properties tests`` = 
    [<Theory>]
    [<AutoMockData>]
    let ``Atleast 1 filter should be specified`` (sut : AnalyzerProperties) = 
        sut.Tokenizer.TokenizerName <- "exists"
        let factoryCollection = Helpers.GetFactory()
        sut.Filters.Clear()
        sut.Validate("test", factoryCollection) |> ExpectErrorCode MessageConstants.ATLEAST_ONE_FILTER_REQUIRED
        factoryCollection.TokenizerFactory.Received().GetModuleByName("exists") |> ignore

    [<Theory>]
    [<AutoMockData>]
    let ``Valid Tokenizer should be specified`` (sut : AnalyzerProperties) = 
        sut.Tokenizer.TokenizerName <- "notexists"
        let factoryCollection = Helpers.GetFactory()
        sut.Validate("test", factoryCollection) |> ExpectErrorCode MessageConstants.TOKENIZER_NOT_FOUND
        factoryCollection.TokenizerFactory.Received().GetModuleByName("notexists") |> ignore

    [<Theory>]
    [<AutoMockData>]
    let ``Filter should exist`` (sut : AnalyzerProperties) = 
        sut.Tokenizer.TokenizerName <- "exists"
        let factoryCollection = Helpers.GetFactory()
        sut.Filters.Clear()
        sut.Filters.Add(new TokenFilter("notexists"))
        sut.Validate("test", factoryCollection) |> ExpectErrorCode MessageConstants.FILTER_NOT_FOUND
        factoryCollection.TokenizerFactory.Received().GetModuleByName("exists") |> ignore
        factoryCollection.TokenizerFactory.Received().GetModuleByName("exists") |> ignore

    [<Theory>]
    [<AutoMockData>]
    let ``All Filters should validate`` (sut : AnalyzerProperties) = 
        sut.Tokenizer.TokenizerName <- "exists"
        let factoryCollection = Helpers.GetFactory()
        sut.Filters.Clear()
        sut.Filters.Add(new TokenFilter("exists"))
        sut.Filters.Add(new TokenFilter("notexists"))
        sut.Validate("test", factoryCollection) |> ExpectErrorCode MessageConstants.FILTER_NOT_FOUND
        factoryCollection.TokenizerFactory.Received().GetModuleByName("exists") |> ignore
        factoryCollection.TokenizerFactory.Received().GetModuleByName("exists") |> ignore