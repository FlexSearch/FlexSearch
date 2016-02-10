namespace FlexSearch.Tests

open FlexSearch.Core
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open Swensen.Unquote

type SchemaTest() = 
    let getSchema(luceneFieldType : LuceneFieldType, allowSort : bool) =
        let field = new Field("test", AllowSort = allowSort)
        let indentity = FieldSchema.createFromFieldType luceneFieldType field
        {   FieldName = ""
            SchemaName = ""
            FieldType = IntField.Instance
            TypeIdentity = indentity
            Source = None
            Similarity = Similarity.BM25
            Analyzers = None }
                  
    member __.IntFieldTest() = 
        let schema = getSchema(IntField.Instance.LuceneFieldType, true)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.LongFieldTest() = 
        let schema = getSchema(LongField.Instance.LuceneFieldType, true)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.DoubleFieldTest() = 
        let schema = getSchema(DoubleField.Instance.LuceneFieldType, true)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.FloatFieldTest() = 
        let schema = getSchema(FloatField.Instance.LuceneFieldType, true)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.IdFieldTest() = 
        let schema = getSchema(IdField.Instance.LuceneFieldType, false) 
        test <@ FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>
