namespace FlexSearch.Api.Index
{
    using System.Net;
    using System.Runtime.Serialization;

    [Api("Index")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/index/exists", "POST,GET", Summary = @"Check if a particular index exists",
        Notes = "This will return if the index exists irrespective of the fact that it is online or not.")]
    [DataContract(Namespace = "")]
    public class IndexExists
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.IndexName, ParameterType = "query", IsRequired = true)]
        public string IndexName { get; set; }

        #endregion
    }
}