namespace FlexSearch.Tests

open FlexSearch.Core
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open Swensen.Unquote

type SchemaTest() = 
    let getSchema(luceneFieldType : LuceneFieldType, allowSort : bool, fieldType : FieldType) =
        let field = new Field("test", AllowSort = allowSort, FieldType =  fieldType)
        let indentity = FieldSchema.createFromFieldType luceneFieldType field
        {   FieldName = ""
            SchemaName = ""
            FieldType = IntField.Instance
            TypeIdentity = indentity
            Similarity = Similarity.BM25
            Analyzers = None }
                  
    member __.IntFieldTest() = 
        let schema = getSchema(IntField.Instance.LuceneFieldType, true, FieldType.Int)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.IntFieldUnSortedTest() = 
        let schema = getSchema(IntField.Instance.LuceneFieldType, false, FieldType.Int)            
        test <@ FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.LongFieldTest() = 
        let schema = getSchema(LongField.Instance.LuceneFieldType, true, FieldType.Long)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.DoubleFieldTest() = 
        let schema = getSchema(DoubleField.Instance.LuceneFieldType, true, FieldType.Double)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.FloatFieldTest() = 
        let schema = getSchema(FloatField.Instance.LuceneFieldType, true, FieldType.Float)            
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ FieldSchema.isNumericField schema @>

    member __.IdFieldTest() = 
        let schema = getSchema(IdField.Instance.LuceneFieldType, false, FieldType.Keyword) 
        test <@ FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.ExactFieldTest() = 
        let schema = getSchema(ExactTextField.Instance.LuceneFieldType, true, FieldType.Keyword) 
        test <@ FieldSchema.isStored schema @>
        test <@ FieldSchema.hasDocValues schema @>
        test <@ FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.TextFieldTest() = 
        let schema = getSchema(ExactTextField.Instance.LuceneFieldType, false, FieldType.Text) 
        test <@ FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.StoredFieldTest() = 
        let schema = getSchema(StoredField.Instance.LuceneFieldType, false, FieldType.Stored) 
        test <@ FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ not <| FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

    member __.SearchOnlyFieldTest() =
        let schema = getSchema(SearchOnlyField.Instance.LuceneFieldType, false, FieldType.SearchOnly) 
        test <@ not <| FieldSchema.isStored schema @>
        test <@ not <| FieldSchema.hasDocValues schema @>
        test <@ not <| FieldSchema.allowSorting schema @>
        test <@ FieldSchema.isSearchable schema @>
        test <@ not <| FieldSchema.isNumericField schema @>

