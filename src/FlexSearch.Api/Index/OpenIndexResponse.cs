namespace FlexSearch.Api.Index
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class OpenIndexResponse
    {
        #region Public Properties

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}