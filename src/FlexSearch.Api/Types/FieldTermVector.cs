namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum FieldTermVector
    {
        DoNotStoreTermVector,

        StoreTermVector,

        StoreTermVectorsWithPositions,

        StoreTermVectorsWithPositionsandOffsets
    }
}