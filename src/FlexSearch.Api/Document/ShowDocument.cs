namespace FlexSearch.Api.Document
{
    using System.Net;
    using System.Runtime.Serialization;

    using ServiceStack.ServiceHost;

    [Api("Document")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/document/show", Verbs = "GET,POST", Summary = @"Show an existing document in the index", Notes = "")]
    [DataContract]
    public class ShowDocument
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.Id, ParameterType = "query", IsRequired = true)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.IndexName, ParameterType = "query", IsRequired = true)]
        public string IndexName { get; set; }

        #endregion
    }
}