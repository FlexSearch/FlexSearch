namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum ScriptOption
    {
        [EnumMember]
        SingleLine,

        [EnumMember]
        MultiLine,

        [EnumMember]
        FileBased
    }
}