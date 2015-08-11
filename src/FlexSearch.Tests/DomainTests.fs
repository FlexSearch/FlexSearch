module DomainTests

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
    member __.``'FieldType' should default to 'Text'``() = test <@ sut.FieldType = FieldDataType.Text @>

type ``Index Field tests``() = 
    
    [<InlineDataAttribute(FieldDataType.Text)>]
    [<InlineDataAttribute(FieldDataType.Highlight)>]
    [<InlineDataAttribute(FieldDataType.Custom)>]
    [<Ignore>]
    member __.``Correct Index Analyzer should be specified for {fieldType} field types`` (fieldType : FieldDataType) = 
        let field = new Field()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ failed <| field.Validate() @>
    
    [<InlineDataAttribute(FieldDataType.Text)>]
    [<InlineDataAttribute(FieldDataType.Highlight)>]
    [<InlineDataAttribute(FieldDataType.Custom)>]
    [<Ignore>]
    member __.``Correct Search Analyzer should be specified for {fieldType} field types`` (fieldType : FieldDataType) = 
        let field = new Field()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        test <@ failed <| field.Validate() @>
    
    [<InlineDataAttribute(FieldDataType.Bool)>]
    [<InlineDataAttribute(FieldDataType.Date)>]
    [<InlineDataAttribute(FieldDataType.DateTime)>]
    [<InlineDataAttribute(FieldDataType.Double)>]
    [<InlineDataAttribute(FieldDataType.ExactText)>]
    [<InlineDataAttribute(FieldDataType.Int)>]
    [<InlineDataAttribute(FieldDataType.Stored)>]
    [<Ignore>]
    member __.``Search Analyzer is ignored for {fieldType} field types`` (fieldType : FieldDataType) = 
        let field = new Field("test")
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        test <@ succeeded <| field.Validate() @>
    
    [<InlineDataAttribute(FieldDataType.Bool)>]
    [<InlineDataAttribute(FieldDataType.Date)>]
    [<InlineDataAttribute(FieldDataType.DateTime)>]
    [<InlineDataAttribute(FieldDataType.Double)>]
    [<InlineDataAttribute(FieldDataType.ExactText)>]
    [<InlineDataAttribute(FieldDataType.Int)>]
    [<InlineDataAttribute(FieldDataType.Stored)>]
    [<Ignore>]
    member __.``Index Analyzer is ignored for {fieldType} field types`` (fieldType : FieldDataType) = 
        let field = new Field("test")
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ succeeded <| field.Validate() @>

type ``Index Dto tests``() = 
    
    member __.``Field cannot contain in-valid field name``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Fields <- [| new Field("INVALIDKEY") |]
        test <@ failed <| index.Validate() @>
    
    member __.``Scripts can have valid keys``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Fields <- [| new Field("validkey") |]
        test <@ succeeded <| index.Validate() @>
    
    member __.``Validation will fail even for a single invalid key``() = 
        let index = new Index()
        index.IndexName <- "valid"
        index.Fields <- [|  new Field("validkey")
                            new Field("INVALIDKEY") |]
        test <@ failed <| index.Validate() @>
    
    member __.``Adding duplicate fields to the index should fail``() = 
        let index = new Index(IndexName = "valid")
        index.Fields <- [| new Field("test")
                           new Field("test") |]
        test <@ failed <| index.Validate() @>
