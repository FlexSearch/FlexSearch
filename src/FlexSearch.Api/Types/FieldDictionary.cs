namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [CollectionDataContract(Namespace = "", ItemName = "Field", KeyName = "FieldName", ValueName = "Properties")]
    public class FieldDictionary : Dictionary<string, IndexFieldProperties>
    {
    }
}