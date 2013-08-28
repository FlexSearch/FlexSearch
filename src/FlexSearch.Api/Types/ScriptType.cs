namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum ScriptType
    {
        [EnumMember]
        SearchProfileSelector,

        [EnumMember]
        CustomScoring,

        [EnumMember]
        ComputedField
    }
}