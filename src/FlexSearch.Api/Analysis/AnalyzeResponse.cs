namespace FlexSearch.Api.Analysis
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class AnalyzeResponse
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public string AnalyzedText { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}