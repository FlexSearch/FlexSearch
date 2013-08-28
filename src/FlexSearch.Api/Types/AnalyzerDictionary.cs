namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [CollectionDataContract(Namespace = "", ItemName = "Analyzer", KeyName = "AnalyzerName", ValueName = "Properties")]
    public class AnalyzerDictionary : Dictionary<string, AnalyzerProperties>
    {
    }
}