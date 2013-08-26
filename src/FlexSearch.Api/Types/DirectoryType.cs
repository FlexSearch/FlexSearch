namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum DirectoryType
    {
        FileSystem,

        MemoryMapped,

        Ram
    }
}