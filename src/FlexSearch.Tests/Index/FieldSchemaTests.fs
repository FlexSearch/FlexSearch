namespace FlexSearch.Tests

open FlexSearch.Core
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open Swensen.Unquote

type SchemaTest() = 
    let getSchema(allowSort : bool, fieldType : IndexField) =
        {   FieldName = "test"
            SchemaName = "test"
            FieldType = fieldType
            Similarity = Similarity.BM25
            DocValues = allowSort
            Analyzers = None }
                  
    member __.IntFieldTest() = 
        let schema = getSchema(true, IntField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.IntFieldUnSortedTest() = 
        let schema = getSchema(false, IntField.Instance)            
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.LongFieldTest() = 
        let schema = getSchema(true, LongField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.DoubleFieldTest() = 
        let schema = getSchema(true, DoubleField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.FloatFieldTest() = 
        let schema = getSchema(true, SingleField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.IdFieldTest() = 
        let schema = getSchema(true, IdField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.ExactFieldTest() = 
        let schema = getSchema(true, KeywordField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.TextFieldTest() = 
        let schema = getSchema(true, TextField.Instance)            
        test <@ schema.FieldType.StoredOnly = false @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.StoredFieldTest() = 
        let schema = getSchema(true, StoredField.Instance)            
        test <@ schema.FieldType.StoredOnly @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ not <| FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

