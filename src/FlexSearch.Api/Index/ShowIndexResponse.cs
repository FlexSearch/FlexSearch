namespace FlexSearch.Api.Index
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class ShowIndexResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public Types.Index IndexSettings { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}