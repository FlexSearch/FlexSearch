namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [CollectionDataContract(Namespace = "", ItemName = "Value")]
    public class StringList : List<string>
    {
    }
}