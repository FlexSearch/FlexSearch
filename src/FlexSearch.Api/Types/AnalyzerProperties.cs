namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class AnalyzerProperties
    {
        #region Constants

        private const string DefaultTokenizerName = "standardtokenizer";

        #endregion

        #region Fields

        private string tokenizerName = DefaultTokenizerName;

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public List<Filter> Filters { get; set; }

        [DataMember(Order = 2)]
        public string TokenizerName
        {
            get
            {
                return this.tokenizerName;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.tokenizerName = DefaultTokenizerName;
                    return;
                }

                this.tokenizerName = value;
            }
        }

        #endregion
    }
}