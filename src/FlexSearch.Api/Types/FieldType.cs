namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum FieldType
    {
        Int,

        Double,

        ExactText,

        Text,

        Highlight,

        Bool,

        Date,

        DateTime,

        Custom,

        Stored
    }
}