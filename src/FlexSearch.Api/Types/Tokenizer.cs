namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class Tokenizer
    {
        #region Constants

        private const string DefaultTokenizerName = "standardtokenizer";

        #endregion

        #region Fields

        private KeyValuePairs parameters;

        private string tokenizerName;

        #endregion

        #region Constructors and Destructors

        public Tokenizer()
        {
            this.parameters = new KeyValuePairs();
            this.tokenizerName = DefaultTokenizerName;
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public KeyValuePairs Parameters
        {
            get
            {
                return this.parameters;
            }
            set
            {
                if (value == null)
                {
                    this.parameters = new KeyValuePairs();
                    return;
                }

                this.parameters = value;
            }
        }

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