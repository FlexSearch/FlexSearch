namespace FlexSearch.Tests

open FlexSearch.Core
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open Swensen.Unquote

type SchemaTest() = 
    member __.IntFieldTest() = 
        let field = new Field("test", AllowSort = true)
        let indentity = FieldSchema.createFromFieldType IntField.Instance.LuceneFieldType field
        
        let schema = 
            { FieldName = ""
              SchemaName = ""
              FieldType = IntField.Instance
              TypeIdentity = indentity
              Source = None
              Similarity = Similarity.BM25
              Analyzers = None }
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>

    member __.IdFieldTest() = 
        let field = new Field("test")
        let indentity = FieldSchema.createFromFieldType IdField.Instance.LuceneFieldType field
        
        let schema = 
            { FieldName = ""
              SchemaName = ""
              FieldType = IntField.Instance
              TypeIdentity = indentity
              Source = None
              Similarity = Similarity.BM25
              Analyzers = None }
        test <@ FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>