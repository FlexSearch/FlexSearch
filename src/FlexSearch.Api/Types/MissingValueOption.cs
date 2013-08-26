namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum MissingValueOption
    {
        ThrowError = 0,

        Default = 1,

        Ignore = 2
    }
}