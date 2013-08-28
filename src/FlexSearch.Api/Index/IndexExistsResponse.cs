namespace FlexSearch.Api.Index
{
    using System.Runtime.Serialization;

    using ServiceStack.ServiceInterface.ServiceModel;

    [DataContract(Namespace = "")]
    public class IndexExistsResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public bool IndexExists { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}