namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SearchProfileProperties
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public SearchQuery SearchQuery { get; set; }

        #endregion
    }
}