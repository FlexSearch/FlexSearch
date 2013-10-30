namespace FlexSearch.Api.Types
{
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.Serialization;

    [Api("Search")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/search/profile", "POST,GET", Summary = "Search for documents in the index using a search profile",
        Notes = "")]
    [DataContract(Namespace = "")]
    public class SearchProfileQuery
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [Description(ApiDescriptionGlobalTypes.Fields)]
        public KeyValuePairs Fields { get; set; }

        [DataMember(Order = 2)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.IndexName, ParameterType = "query", IsRequired = true)]
        public string IndexName { get; set; }

        [DataMember(Order = 3)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.SearchProfile, ParameterType = "query", IsRequired = true)]
        public string SearchProfileName { get; set; }

        [DataMember(Order = 4)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.SearchProfileSelector, ParameterType = "query",
            IsRequired = true)]
        public string SearchProfileSelector { get; set; }

        #endregion
    }
}