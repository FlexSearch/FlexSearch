namespace FlexSearch.Api.Document
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.Serialization;

    using FlexSearch.Api.Types;

    using ServiceStack.ServiceHost;

    [Api("Document")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/document/create", "POST", Summary = @"Create a new document in the index",
        Notes = "This will alway create a new document even if it already exists.")]
    [DataContract(Namespace = "")]
    public class CreateDocument
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [Description(ApiDescriptionGlobalTypes.Fields)]
        public KeyValuePairs Fields { get; set; }

        [DataMember(Order = 2)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.Id, ParameterType = "query", IsRequired = true)]
        public string Id { get; set; }

        [DataMember(Order = 3)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.IndexName, ParameterType = "query", IsRequired = true)]
        public string IndexName { get; set; }

        #endregion
    }
}