namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class AnalyzerProperties
    {
        #region Fields

        private List<Filter> filters = new List<Filter>();

        private Tokenizer tokenizer = new Tokenizer();

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public List<Filter> Filters
        {
            get
            {
                return this.filters;
            }
            set
            {
                if (value == null)
                {
                    this.filters = new List<Filter>();
                    return;
                }

                this.filters = value;
            }
        }

        [DataMember(Order = 2)]
        public Tokenizer Tokenizer
        {
            get
            {
                return this.tokenizer;
            }

            set
            {
                if (value == null)
                {
                    this.tokenizer = new Tokenizer();
                    return;
                }

                this.tokenizer = value;
            }
        }

        #endregion
    }
}