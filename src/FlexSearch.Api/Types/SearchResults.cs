namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class SearchResults
    {
        #region Constructors and Destructors

        public SearchResults()
        {
            this.Documents = new List<Document>();
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public List<Document> Documents { get; set; }

        [DataMember(Order = 2)]
        public int RecordsReturned { get; set; }

        #endregion
    }
}