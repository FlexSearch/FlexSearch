namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum ScriptType
    {
        SearchProfileSelector,

        CustomScoring,

        ComputedField
    }
}