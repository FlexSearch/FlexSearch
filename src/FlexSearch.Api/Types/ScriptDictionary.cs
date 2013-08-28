namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [CollectionDataContract(Namespace = "", ItemName = "Script", KeyName = "ScriptName", ValueName = "Properties")]
    public class ScriptDictionary : Dictionary<string, ScriptProperties>
    {
    }
}