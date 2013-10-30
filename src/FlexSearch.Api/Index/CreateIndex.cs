namespace FlexSearch.Api.Index
{
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.Serialization;

    using FlexSearch.Api.Types;

    [Api("Index")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/index/create", "POST", Summary = @"Create a new index",
        Notes = "This will not create a new index if there is already an index with the same name.")]
    [DataContract(Namespace = "")]
    public class CreateIndex
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [Description("Bring the newly created index online.")]
        public bool OpenIndex { get; set; }

        [DataMember(Order = 2)]
        [Description(ApiDescriptionGlobalTypes.Index)]
        public Index Index { get; set; }

        #endregion
    }
}