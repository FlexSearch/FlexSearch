namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum FilterType
    {
        [EnumMember]
        And,

        [EnumMember]
        Or
    }
}