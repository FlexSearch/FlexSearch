namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum FilterType
    {
        And,

        Or
    }
}