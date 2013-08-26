namespace FlexSearch.Api.Index
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.Serialization;

    using FlexSearch.Api.Types;

    using ServiceStack.ServiceHost;

    [Api("Index")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/index/updateanalyzers", "POST", Summary = @"Update the analyzers associated with an existing index",
        Notes = "Index should be offline to perform any settings update.")]
    [DataContract]
    public class UpdateAnalyzers
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [Description(ApiDescriptionGlobalTypes.Analyzer)]
        public Dictionary<string, AnalyzerProperties> Analyzers { get; set; }

        [DataMember(Order = 2)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.IndexName, ParameterType = "query", IsRequired = true)]
        public string IndexName { get; set; }

        #endregion
    }
}