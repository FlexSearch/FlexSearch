namespace FlexSearch.Core.Tests

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

module ``Property name validator tests`` = 
    [<InlineData("TEST")>]
    [<InlineData("Test")>]
    [<InlineData(Constants.IdField)>]
    [<InlineData(Constants.LastModifiedField)>]
    [<InlineData(Constants.TypeField)>]
    [<InlineData("<test>")>]
    [<Theory>]
    let ``Property cannot contain invalid values`` (sut : string) = TestChoice (sut.ValidatePropertyValue("test")) false
    
    [<InlineData("test")>]
    [<InlineData("1234")>]
    [<InlineData("_test")>]
    [<Theory>]
    let ``Property can contain valid values`` (sut : string) = TestChoice (sut.ValidatePropertyValue("test")) true

module ``Index configuration tests`` = 
    [<Theory>]
    [<InlineAutoMockData(60)>]
    [<InlineAutoMockData(61)>]
    let ``Commit time should be greater than or equal to x`` (value : int, sut : IndexConfiguration) = 
        sut.CommitTimeSec <- value
        sut.RefreshTimeMilliSec <- 25
        sut.RamBufferSizeMb <- 100
        ExpectSuccess(sut.Validate())
    
    [<Theory>]
    [<InlineAutoMockData(59)>]
    let ``Commit time should not be smaller than x`` (value : int, sut : IndexConfiguration) = 
        sut.CommitTimeSec <- value
        sut.RefreshTimeMilliSec <- 25
        sut.RamBufferSizeMb <- 100
        sut.Validate() |> ExpectErrorCode MessageConstants.GREATER_THAN_EQUAL_TO

module ``Default value tests`` = 
    let sut = new FieldProperties()
    
    [<Fact>]
    let ``'standardanalyzer' should be the default 'SearchAnalyzer'``() = 
        Assert.Equal<string>("standardanalyzer", sut.SearchAnalyzer)
    
    [<Fact>]
    let ``'standardanalyzer' should be the default 'IndexAnalyzer'``() = 
        Assert.Equal<string>("standardanalyzer", sut.IndexAnalyzer)
    
    [<Fact>]
    let ``'Analyze' should default to 'true'``() = Assert.Equal(true, sut.Analyze)
    
    [<Fact>]
    let ``'Store' should default to 'true'``() = Assert.Equal(true, sut.Store)
    
    [<Fact>]
    let ``'Index' should default to 'true'``() = Assert.Equal(true, sut.Index)
    
    [<Fact>]
    let ``'FieldType' should default to 'Text'``() = Assert.Equal(FieldType.Text, sut.FieldType)
    
    [<Fact>]
    let ``'TermVector' should default to 'StoreTermVectorsWithPositions'``() = 
        Assert.Equal(FieldTermVector.StoreTermVectorsWithPositions, sut.TermVector)

module ``Index Field tests`` = 
    [<Theory>]
    [<InlineAutoMockDataAttribute(FieldType.Bool)>]
    [<InlineAutoMockDataAttribute(FieldType.Date)>]
    [<InlineAutoMockDataAttribute(FieldType.DateTime)>]
    [<InlineAutoMockDataAttribute(FieldType.Double)>]
    [<InlineAutoMockDataAttribute(FieldType.ExactText)>]
    [<InlineAutoMockDataAttribute(FieldType.Int)>]
    [<InlineAutoMockDataAttribute(FieldType.Stored)>]
    let ``Index Analyzer is ignored for X field types`` (fieldType : FieldType, sut : FieldProperties, 
                                                         [<Frozen>] factoryCollection : IFactoryCollection, 
                                                         analyzers : Dictionary<string, AnalyzerProperties>, 
                                                         scripts : Dictionary<string, ScriptProperties>) = 
        sut.ScriptName <- ""
        sut.FieldType <- fieldType
        sut.IndexAnalyzer <- "dummy"
        ExpectSuccess(sut.Validate(factoryCollection, analyzers, scripts, "test"))
    
    [<Theory>]
    [<InlineAutoMockDataAttribute(FieldType.Bool)>]
    [<InlineAutoMockDataAttribute(FieldType.Date)>]
    [<InlineAutoMockDataAttribute(FieldType.DateTime)>]
    [<InlineAutoMockDataAttribute(FieldType.Double)>]
    [<InlineAutoMockDataAttribute(FieldType.ExactText)>]
    [<InlineAutoMockDataAttribute(FieldType.Int)>]
    [<InlineAutoMockDataAttribute(FieldType.Stored)>]
    let ``Search Analyzer is ignored for X field types`` (fieldType : FieldType, sut : FieldProperties, 
                                                          [<Frozen>] factoryCollection : IFactoryCollection, 
                                                          analyzers : Dictionary<string, AnalyzerProperties>, 
                                                          scripts : Dictionary<string, ScriptProperties>) = 
        sut.ScriptName <- ""
        sut.FieldType <- fieldType
        sut.IndexAnalyzer <- "dummy"
        ExpectSuccess(sut.Validate(factoryCollection, analyzers, scripts, "test"))
    
    [<Theory>]
    [<InlineAutoMockDataAttribute(FieldType.Text)>]
    [<InlineAutoMockDataAttribute(FieldType.Highlight)>]
    [<InlineAutoMockDataAttribute(FieldType.Custom)>]
    let ``Correct Index Analyzer should be specified for X field types`` (fieldType : FieldType, sut : FieldProperties, 
                                                                          [<Frozen>] factoryCollection : IFactoryCollection, 
                                                                          analyzers : Dictionary<string, AnalyzerProperties>, 
                                                                          scripts : Dictionary<string, ScriptProperties>) = 
        sut.ScriptName <- ""
        sut.FieldType <- fieldType
        sut.IndexAnalyzer <- "dummy"
        sut.Validate(factoryCollection, analyzers, scripts, "test") 
        |> ExpectErrorCode MessageConstants.ANALYZER_NOT_FOUND
    
    [<Theory>]
    [<InlineAutoMockDataAttribute(FieldType.Text)>]
    [<InlineAutoMockDataAttribute(FieldType.Highlight)>]
    [<InlineAutoMockDataAttribute(FieldType.Custom)>]
    let ``Correct Search Analyzer should be specified for X field types`` (fieldType : FieldType, sut : FieldProperties, 
                                                                           [<Frozen>] factoryCollection : IFactoryCollection, 
                                                                           analyzers : Dictionary<string, AnalyzerProperties>, 
                                                                           scripts : Dictionary<string, ScriptProperties>) = 
        sut.ScriptName <- ""
        sut.FieldType <- fieldType
        sut.IndexAnalyzer <- "dummy"
        sut.Validate(factoryCollection, analyzers, scripts, "test") 
        |> ExpectErrorCode MessageConstants.ANALYZER_NOT_FOUND
