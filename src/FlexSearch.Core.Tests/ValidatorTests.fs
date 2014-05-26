namespace FlexSearch.Core.Tests

open Xunit
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.Validator
open NSubstitute
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Xunit
open Xunit.Extensions

module ``Property name validator tests`` = 
    [<InlineData("TEST")>][<InlineData("Test")>][<InlineData(Constants.IdField)>][<InlineData(Constants.LastModifiedField)>][<InlineData(Constants.TypeField)>][<InlineData("<test>")>][<Theory>]
    let ``Property cannot contain invalid values`` (sut : string) = TestChoice (sut.ValidatePropertyValue("test")) false
    
    [<InlineData("test")>][<InlineData("1234")>][<InlineData("_test")>][<Theory>]
    let ``Property can contain valid values`` (sut : string) = TestChoice (sut.ValidatePropertyValue("test")) true

module ``Token filter validator tests`` = 
    [<Theory>][<AutoMockData>]
    let ``Token filter should not validate`` (sut : TokenFilter, [<Frozen>] factoryCollection : IFactoryCollection) = 
        sut.FilterName <- "notexists"
        factoryCollection.FilterFactory.GetModuleByName("notexists")
                         .Returns(Choice2Of2(MessageConstants.FILTER_NOT_FOUND)) |> ignore
        ExpectErrorCode (sut.Validate(factoryCollection)) MessageConstants.FILTER_NOT_FOUND
        factoryCollection.FilterFactory.Received().GetModuleByName("notexists") |> ignore
    
    [<Theory>][<AutoMockData>]
    let ``Token filter should validate`` (sut : TokenFilter, [<Frozen>] factoryCollection : IFactoryCollection) = 
        sut.FilterName <- "exists"
        factoryCollection.FilterFactory.GetModuleByName("exists")
                         .Returns(Choice1Of2(new StandardFilterFactory() :> IFlexFilterFactory)) |> ignore
        TestChoice (sut.Validate(factoryCollection)) true
        factoryCollection.FilterFactory.Received().GetModuleByName("exists") |> ignore
