module DomainTests

open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic

type ``Field Dto Default value tests``() = 
    let sut = new Field.Dto()
    member __.``'standardanalyzer' should be the default 'SearchAnalyzer'``() = 
        test <@ Constants.StandardAnalyzer = sut.SearchAnalyzer @>
    member __.``'standardanalyzer' should be the default 'IndexAnalyzer'``() = 
        test <@ Constants.StandardAnalyzer = sut.IndexAnalyzer @>
    member __.``'Analyze' should default to 'true'``() = test <@ sut.Analyze = true @>
    member __.``'Store' should default to 'true'``() = test <@ sut.Store = true @>
    member __.``'Index' should default to 'true'``() = test <@ sut.Index = true @>
    member __.``'FieldType' should default to 'Text'``() = test <@ sut.FieldType = FieldType.Dto.Text @>

type ``Index Field tests``() = 
    
    [<InlineDataAttribute(FieldType.Dto.Text)>]
    [<InlineDataAttribute(FieldType.Dto.Highlight)>]
    [<InlineDataAttribute(FieldType.Dto.Custom)>]
    [<Ignore>]
    member __.``Correct Index Analyzer should be specified for {fieldType} field types`` (fieldType : FieldType.Dto) = 
        let field = new Field.Dto()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ failed <| field.Validate() @>
    
    [<InlineDataAttribute(FieldType.Dto.Text)>]
    [<InlineDataAttribute(FieldType.Dto.Highlight)>]
    [<InlineDataAttribute(FieldType.Dto.Custom)>]
    [<Ignore>]
    member __.``Correct Search Analyzer should be specified for {fieldType} field types`` (fieldType : FieldType.Dto) = 
        let field = new Field.Dto()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        test <@ failed <| field.Validate() @>
    
    [<InlineDataAttribute(FieldType.Dto.Bool)>]
    [<InlineDataAttribute(FieldType.Dto.Date)>]
    [<InlineDataAttribute(FieldType.Dto.DateTime)>]
    [<InlineDataAttribute(FieldType.Dto.Double)>]
    [<InlineDataAttribute(FieldType.Dto.ExactText)>]
    [<InlineDataAttribute(FieldType.Dto.Int)>]
    [<InlineDataAttribute(FieldType.Dto.Stored)>]
    [<Ignore>]
    member __.``Search Analyzer is ignored for {fieldType} field types`` (fieldType : FieldType.Dto) = 
        let field = new Field.Dto("test")
        field.FieldType <- fieldType
        field.SearchAnalyzer <- ""
        test <@ succeeded <| field.Validate() @>
    
    [<InlineDataAttribute(FieldType.Dto.Bool)>]
    [<InlineDataAttribute(FieldType.Dto.Date)>]
    [<InlineDataAttribute(FieldType.Dto.DateTime)>]
    [<InlineDataAttribute(FieldType.Dto.Double)>]
    [<InlineDataAttribute(FieldType.Dto.ExactText)>]
    [<InlineDataAttribute(FieldType.Dto.Int)>]
    [<InlineDataAttribute(FieldType.Dto.Stored)>]
    [<Ignore>]
    member __.``Index Analyzer is ignored for {fieldType} field types`` (fieldType : FieldType.Dto) = 
        let field = new Field.Dto("test")
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ succeeded <| field.Validate() @>

type ``Index Dto tests``() = 
    
    member __.``Script cannot contain in-valid script name``() = 
        let index = new Index.Dto()
        index.IndexName <- "valid"
        index.Scripts <- [| new Script.Dto(ScriptName = "INVALIDKEY", Source = "dummy") |]
        test <@ failed <| index.Validate() @>
    
    member __.``Scripts can have valid keys``() = 
        let index = new Index.Dto()
        index.IndexName <- "valid"
        index.Scripts <- [| new Script.Dto(ScriptName = "validkey", Source = "dummy") |]
        test <@ succeeded <| index.Validate() @>
    
    member __.``Validation will fail even for a single invalid key``() = 
        let index = new Index.Dto()
        index.IndexName <- "valid"
        index.Scripts <- [| new Script.Dto(ScriptName = "INVALIDKEY", Source = "dummy")
                            new Script.Dto(ScriptName = "INVALIDKEY", Source = "dummy") |]
        test <@ failed <| index.Validate() @>
    
    member __.``Adding duplicate fields to the index should fail``() = 
        let index = new Index.Dto(IndexName = "valid")
        index.Fields <- [| new Field.Dto("test")
                           new Field.Dto("test") |]
        test <@ failed <| index.Validate() @>
