namespace FlexSearch.Api.Job
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    using ServiceStack.ServiceHost;

    [Api("Status")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/job/getstatus", "GET,POST", Summary = @"Get the status of a job by Id",
        Notes = "The job id a guid and is returned by some long running services to implement long polling.")]
    [DataContract(Namespace = "")]
    public class GetStatus
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [ApiMember(Description = "Job Id for which status is to be determined", ParameterType = "query",
            IsRequired = true)]
        public Guid Id { get; set; }

        #endregion
    }
}