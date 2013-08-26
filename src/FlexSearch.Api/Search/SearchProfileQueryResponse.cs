namespace FlexSearch.Api.Search
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using FlexSearch.Api.Types;

    using ServiceStack.ServiceInterface.ServiceModel;

    [DataContract]
    public class SearchProfileQueryResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public List<Document> Documents { get; set; }

        [DataMember(Order = 2)]
        public int RecordsReturned { get; set; }

        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}