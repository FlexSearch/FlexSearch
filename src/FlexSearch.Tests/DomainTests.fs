module DomainTests

open FlexSearch.Api.Models
open FlexSearch.Api
open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic

type ``Field Dto Default value tests``() = 
    let sut = new Field()
    member __.``'standardanalyzer' should be the default 'SearchAnalyzer'``() = 
        test <@ Constants.StandardAnalyzer = sut.SearchAnalyzer @>
    member __.``'standardanalyzer' should be the default 'IndexAnalyzer'``() = 
        test <@ Constants.StandardAnalyzer = sut.IndexAnalyzer @>
    member __.``'Analyze' should default to 'true'``() = test <@ sut.Analyze = true @>
    member __.``'Store' should default to 'true'``() = test <@ sut.Store = true @>
    member __.``'Index' should default to 'true'``() = test <@ sut.Index = true @>
    member __.``'FieldType' should default to 'Text'``() = test <@ sut.FieldType = Constants.FieldType.Text @>

type ``Index Field tests``() = 
    
    [<InlineDataAttribute(Constants.FieldType.Text)>]
    [<InlineDataAttribute(Constants.FieldType.Highlight)>]
    [<InlineDataAttribute(Constants.FieldType.Custom)>]
    [<Ignore>]
    member __.``Correct Index Analyzer should be specified for {fieldType} field types`` (fieldType : Constants.FieldType) = 
        let field = new Field()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ field.Validate() @>
    
    [<InlineDataAttribute(Constants.FieldType.Text)>]
    [<InlineDataAttribute(Constants.FieldType.Highlight)>]
    [<InlineDataAttribute(Constants.FieldType.Custom)>]
    [<Ignore>]
    member __.``Correct Search Analyzer should be specified for {fieldType} field types`` (fieldType : Constants.FieldType) = 
        let field = new Field()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        test <@ field.Validate() @>
    
    [<InlineDataAttribute(Constants.FieldType.Bool)>]
    [<InlineDataAttribute(Constants.FieldType.Date)>]
    [<InlineDataAttribute(Constants.FieldType.DateTime)>]
    [<InlineDataAttribute(Constants.FieldType.Double)>]
    [<InlineDataAttribute(Constants.FieldType.ExactText)>]
    [<InlineDataAttribute(Constants.FieldType.Int)>]
    [<InlineDataAttribute(Constants.FieldType.Stored)>]
    [<Ignore>]
    member __.``Search Analyzer is ignored for {fieldType} field types`` (fieldType : Constants.FieldType) = 
        let field = new Field("test")
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        test <@ field.Validate() @>
    
    [<InlineDataAttribute(Constants.FieldType.Bool)>]
    [<InlineDataAttribute(Constants.FieldType.Date)>]
    [<InlineDataAttribute(Constants.FieldType.DateTime)>]
    [<InlineDataAttribute(Constants.FieldType.Double)>]
    [<InlineDataAttribute(Constants.FieldType.ExactText)>]
    [<InlineDataAttribute(Constants.FieldType.Int)>]
    [<InlineDataAttribute(Constants.FieldType.Stored)>]
    [<Ignore>]
    member __.``Index Analyzer is ignored for {fieldType} field types`` (fieldType : Constants.FieldType) = 
        let field = new Field("test")
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ field.Validate() @>

type ``Index Dto tests``() = 
    
    member __.``Field cannot contain in-valid field name``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Fields <- [| new Field("INVALIDKEY") |]
        test <@ index.Validate() @>
    
    member __.``Scripts can have valid keys``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Fields <- [| new Field("validkey") |]
        test <@ index.Validate() @>
    
    member __.``Validation will fail even for a single invalid key``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Fields <- [|  new Field("validkey")
                            new Field("INVALIDKEY") |]
        test <@ index.Validate() @>
    
    member __.``Adding duplicate fields to the index should fail``() = 
        let index = new Index(IndexName = "valid")
        index.Fields <- [| new Field("test")
                           new Field("test") |]
        test <@ index.Validate() @>
