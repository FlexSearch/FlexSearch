namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum FieldType
    {
        [EnumMember]
        Int,

        [EnumMember]
        Double,

        [EnumMember]
        ExactText,

        [EnumMember]
        Text,

        [EnumMember]
        Highlight,

        [EnumMember]
        Bool,

        [EnumMember]
        Date,

        [EnumMember]
        DateTime,

        [EnumMember]
        Custom,

        [EnumMember]
        Stored
    }
}