namespace FlexSearch.Api.Index
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class CloseIndexResponse
    {
        #region Public Properties

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}