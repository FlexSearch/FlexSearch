namespace FlexSearch.Api.Document
{
    using System.Runtime.Serialization;

    using ServiceStack.ServiceInterface.ServiceModel;

    [DataContract]
    public class UpdateDocumentResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public string Message { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}