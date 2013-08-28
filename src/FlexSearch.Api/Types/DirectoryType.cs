namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum DirectoryType
    {
        [EnumMember]
        FileSystem,

        [EnumMember]
        MemoryMapped,

        [EnumMember]
        Ram
    }
}