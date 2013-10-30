namespace FlexSearch.Api.Index
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class UpdateAnalyzersResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}