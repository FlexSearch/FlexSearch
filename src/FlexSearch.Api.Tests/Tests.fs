namespace FlexSearch.Api.Tests

open Xunit
open FlexSearch.Api
open FlexSearch.Api.Validation
open System.ComponentModel.DataAnnotations
open Xunit.Extensions
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Xunit
open System

/// <summary>
/// Auto fixture based Xunit attribute
/// </summary>
type AutoMockDataAttribute() = 
    inherit AutoDataAttribute((new Fixture()).Customize(new SupportMutableValueTypesCustomization()))

[<AutoOpen>]
module Helpers = 
    let ValidateSuccess(obj) = 
        let value = obj :> IValidator
        Assert.Equal<ValidationResult>(ValidationResult.Success, value.Validate())
    
    let ValidateHasErrors(obj) = 
        let value = obj :> IValidator
        let result = value.Validate()
        Assert.False(String.IsNullOrWhiteSpace(result.ErrorMessage), "Expecting the validator to return an error.")
        printfn "Error Message: %s" result.ErrorMessage

module ``Analyzer Properties Tests`` = 
    [<AutoMockData; Theory>]
    let ``Tokenizer is required`` (analyzer : Analyzer) = 
        analyzer.Tokenizer <- Unchecked.defaultof<Tokenizer>
        ValidateHasErrors analyzer
    
    [<AutoMockData; Theory>]
    let ``Filters is required`` (analyzer : Analyzer) = 
        analyzer.Filters <- null
        ValidateHasErrors analyzer
    
    [<AutoMockData; Theory>]
    let ``Atleast 1 Filter is required`` (analyzer : Analyzer) = 
        analyzer.Filters.Clear()
        ValidateHasErrors analyzer

module ``Property name validator tests`` = 
    [<InlineData("TEST")>]
    [<InlineData("Test")>]
    [<InlineData(Constants.IdField)>]
    [<InlineData(Constants.LastModifiedField)>]
    [<InlineData(Constants.TypeField)>]
    [<InlineData("<test>")>]
    [<Theory>]
    let ``Property cannot contain invalid values`` (sut : string) = 
        let tokenizer = new Tokenizer(sut)
        ValidateHasErrors tokenizer
    
    [<InlineData("test")>]
    [<InlineData("1234")>]
    [<InlineData("_test")>]
    [<Theory>]
    let ``Property can contain valid values`` (sut : string) = 
        let tokenizer = new Tokenizer(sut)
        ValidateSuccess tokenizer
    
    module ``Default value tests`` = 
        let sut = new Field()
        
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
        let ``'TermVector' should default to 'DoNotStoreTermVector'``() = 
            Assert.Equal(FieldTermVector.DoNotStoreTermVector, sut.TermVector)

module ``Index Field tests`` = 
    [<Theory>]
    [<InlineDataAttribute(FieldType.Text)>]
    [<InlineDataAttribute(FieldType.Highlight)>]
    [<InlineDataAttribute(FieldType.Custom)>]
    let ``Correct Index Analyzer should be specified for {fieldType} field types`` (fieldType : FieldType) = 
        let field = new Field()
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        ValidateHasErrors field
    
    [<Theory>]
    [<InlineDataAttribute(FieldType.Text)>]
    [<InlineDataAttribute(FieldType.Highlight)>]
    [<InlineDataAttribute(FieldType.Custom)>]
    let ``Correct Search Analyzer should be specified for {fieldType} field types`` (fieldType : FieldType) = 
        let field = new Field()
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        ValidateHasErrors field
    
    [<Theory>]
    [<InlineDataAttribute(FieldType.Bool)>]
    [<InlineDataAttribute(FieldType.Date)>]
    [<InlineDataAttribute(FieldType.DateTime)>]
    [<InlineDataAttribute(FieldType.Double)>]
    [<InlineDataAttribute(FieldType.ExactText)>]
    [<InlineDataAttribute(FieldType.Int)>]
    [<InlineDataAttribute(FieldType.Stored)>]
    let ``Search Analyzer is ignored for {fieldType} field types`` (fieldType : FieldType) = 
        let field = new Field("test")
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        ValidateSuccess field
    
    [<Theory>]
    [<InlineDataAttribute(FieldType.Bool)>]
    [<InlineDataAttribute(FieldType.Date)>]
    [<InlineDataAttribute(FieldType.DateTime)>]
    [<InlineDataAttribute(FieldType.Double)>]
    [<InlineDataAttribute(FieldType.ExactText)>]
    [<InlineDataAttribute(FieldType.Int)>]
    [<InlineDataAttribute(FieldType.Stored)>]
    let ``Index Analyzer is ignored for {fieldType} field types`` (fieldType : FieldType) = 
        let field = new Field("test")
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        ValidateSuccess field

module ``Index tests`` = 
    [<FactAttribute>]
    let ``Analyzers cannot contain in-valid keys``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Analyzers.Add("INVALIDKEY", new Analyzer())
        ValidateHasErrors index
    
    [<FactAttribute>]
    let ``Analyzers can have valid keys``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Analyzers.Add("validkey", new Analyzer())
        ValidateSuccess index
    
    [<FactAttribute>]
    let ``Validation will fail even for a single invalid key``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Analyzers.Add("validkey", new Analyzer())
        index.Analyzers.Add("INVALIDKEY", new Analyzer())
        ValidateHasErrors index
    
    [<FactAttribute>]
    let ``Adding duplicate fields to the index should fail``() =
        let index = new Index(IndexName = "valid")
        index.Fields.Add(new Field("test"))
        index.Fields.Add(new Field("test"))
        ValidateHasErrors index

    [<FactAttribute>]
    let ``Adding duplicate search profiles to the index should fail``() =
        let index = new Index(IndexName = "valid")
        index.SearchProfiles.Add(new SearchQuery("test", "test", QueryName = "test"))
        index.SearchProfiles.Add(new SearchQuery("test", "test", QueryName = "test"))
        ValidateHasErrors index

module ``Operation Message Tests`` = 
    [<Theory>]
    [<InlineDataAttribute(" 1000 ")>]
    [<InlineDataAttribute("1000 ")>]
    let ``Error code should be 1000``(input) = 
        let sut = sprintf "ErrorCode = '%s'" input
        Assert.Equal<int>(1000, OperationMessage.GetErrorCode(sut))
