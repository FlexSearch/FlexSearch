namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [CollectionDataContract(Namespace = "", ItemName = "KeyValuePair", KeyName = "Key", ValueName = "Value")]
    public class KeyValuePairs : Dictionary<string, string>
    {
    }
}