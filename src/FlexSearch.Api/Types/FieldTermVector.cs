namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public enum FieldTermVector
    {
        [EnumMember]
        DoNotStoreTermVector,

        [EnumMember]
        StoreTermVector,

        [EnumMember]
        StoreTermVectorsWithPositions,

        [EnumMember]
        StoreTermVectorsWithPositionsandOffsets
    }
}