namespace FlexSearch.Api.Document
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class CreateDocumentResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public string Message { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}