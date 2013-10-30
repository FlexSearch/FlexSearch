namespace FlexSearch.Api.Document
{
    using System.Runtime.Serialization;

    using FlexSearch.Api.Types;

    [DataContract(Namespace = "")]
    public class ShowDocumentResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public Document Document { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}