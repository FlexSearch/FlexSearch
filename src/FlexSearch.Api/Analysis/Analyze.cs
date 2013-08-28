namespace FlexSearch.Api.Analysis
{
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.Serialization;

    using ServiceStack.ServiceHost;

    [Api("Analysis")]
    [ApiResponse(HttpStatusCode.BadRequest, ApiDescriptionHttpResponse.BadRequest)]
    [ApiResponse(HttpStatusCode.InternalServerError, ApiDescriptionHttpResponse.InternalServerError)]
    [ApiResponse(HttpStatusCode.OK, ApiDescriptionHttpResponse.Ok)]
    [Route("/analysis/analyze", "POST", Summary = @"Analyze the text using an analyzer",
        Notes = "This will analyze the sample text using the specified analyzer. This is helpful in determining if an analyzer is producing the desired tokens.")]
    [DataContract(Namespace = "")]
    public class Analyze
    {
        #region Public Properties

        [DataMember(Order = 1)]
        [Description(ApiDescriptionGlobalTypes.Analyzer)]
        [ApiMember(Description = ApiDescriptionGlobalTypes.Analyzer, ParameterType = "query", IsRequired = true)]
        public string AnalyzerName { get; set; }

        [DataMember(Order = 2)]
        [Description("Text to be analyzed")]
        public string Text { get; set; }

        #endregion
    }
}