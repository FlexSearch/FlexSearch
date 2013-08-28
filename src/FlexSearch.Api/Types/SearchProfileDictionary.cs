namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [CollectionDataContract(Namespace = "", ItemName = "SearchProfile", KeyName = "ProfileName",
        ValueName = "Properties")]
    public class SearchProfileDictionary : Dictionary<string, SearchProfileProperties>
    {
    }
}