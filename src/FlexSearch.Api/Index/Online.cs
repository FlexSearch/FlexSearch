namespace FlexSearch.Api.Index
{
    using System.Net;
    using System.Runtime.Serialization;

    using ServiceStack.ServiceHost;

    [Api("Index")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/index/online", "POST", Summary = @"Check if an index is online or not", Notes = "")]
    [DataContract(Namespace = "")]
    public class Online
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.IndexName, ParameterType = "query", IsRequired = true)]
        public string IndexName { get; set; }

        #endregion
    }
}