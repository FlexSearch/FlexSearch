namespace FlexSearch.Api.Index
{
    using System.Runtime.Serialization;

    using ServiceStack.ServiceInterface.ServiceModel;

    [DataContract]
    public class OnlineResponse
    {
        #region Public Properties

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}