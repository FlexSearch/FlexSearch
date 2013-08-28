namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum MissingValueOption
    {
        [EnumMember]
        ThrowError = 0,

        [EnumMember]
        Default = 1,

        [EnumMember]
        Ignore = 2
    }
}