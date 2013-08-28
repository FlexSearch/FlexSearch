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
    [Route("/document/update", "POST",
        Summary = @"Update an existing document in the index and create it if it does not exist.",
        Notes =
            "This will create a new document if the document does not exist. It is better to use create api for document creation when creating a huge number of documents."
        )]
    [DataContract(Namespace = "")]
    public class UpdateDocument
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