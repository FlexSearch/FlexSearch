module DomainTests

open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic

module AnalyzerTests = 
    type BuilderTests() = 
        
        member __.``Should build successfully for a known tokenizer`` (sut : Analyzer.Dto) = 
            sut.Filters.Clear()
            sut.Tokenizer <- new Tokenizer.Dto(TokenizerName = "standard")
            test <@ succeeded <| Analyzer.build sut = true @>
        
        member __.``Should not build successfully for an unknown tokenizer`` (sut : Analyzer.Dto) = 
            sut.Filters.Clear()
            test <@ failed <| Analyzer.build sut = true @>
        
        member __.``Should build successfully for a known filter`` (sut : Analyzer.Dto, filter : TokenFilter.Dto) = 
            sut.Filters.Clear()
            filter.FilterName <- "lowercase"
            filter.Parameters.Clear()
            sut.Filters.Add(filter)
            sut.Tokenizer <- new Tokenizer.Dto(TokenizerName = "standard")
            test <@ succeeded <| Analyzer.build sut = true @>
        
        member __.``Should not build successfully for an unknown filter`` (sut : Analyzer.Dto, filter : TokenFilter.Dto) = 
            sut.Filters.Clear()
            filter.FilterName <- "unknown"
            filter.Parameters.Clear()
            sut.Filters.Add(filter)
            sut.Tokenizer <- new Tokenizer.Dto(TokenizerName = "standard")
            test <@ failed <| Analyzer.build sut = true @>

type ``Field Dto Default value tests``() = 
    let sut = new Field.Dto()
    member __.``'standardanalyzer' should be the default 'SearchAnalyzer'``() = 
        test <@ "standardanalyzer" = sut.SearchAnalyzer @>
    member __.``'standardanalyzer' should be the default 'IndexAnalyzer'``() = 
        test <@ "standardanalyzer" = sut.IndexAnalyzer @>
    member __.``'Analyze' should default to 'true'``() = test <@ sut.Analyze = true @>
    member __.``'Store' should default to 'true'``() = test <@ sut.Store = true @>
    member __.``'Index' should default to 'true'``() = test <@ sut.Index = true @>
    member __.``'FieldType' should default to 'Text'``() = test <@ sut.FieldType = FieldType.Dto.Text @>

type ``Index Field tests``() = 
    
    [<InlineDataAttribute(FieldType.Dto.Text)>]
    [<InlineDataAttribute(FieldType.Dto.Highlight)>]
    [<InlineDataAttribute(FieldType.Dto.Custom)>]
    member __.``Correct Index Analyzer should be specified for {fieldType} field types`` (fieldType : FieldType.Dto) = 
        let field = new Field.Dto()
        field.FieldName <- "notblank"
        field.FieldType <- fieldType
        field.IndexAnalyzer <- ""
        test <@ failed <| field.Validate() @>
    
    [<InlineDataAttribute(FieldType.Dto.Text)>]
    [<InlineDataAttribute(FieldType.Dto.Highlight)>]
    [<InlineDataAttribute(FieldType.Dto.Custom)>]
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
