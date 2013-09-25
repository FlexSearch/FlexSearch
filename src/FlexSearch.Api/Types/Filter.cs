namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class Filter
    {
        #region Fields

        private string filterName;

        private KeyValuePairs parameters;

        #endregion

        #region Constructors and Destructors

        public Filter()
        {
            this.parameters = new KeyValuePairs();
            this.FilterName = "standardfilter";
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public string FilterName
        {
            get
            {
                return this.filterName;
            }
            set
            {
                if (value == null)
                {
                    this.filterName = "standardfilter";
                    return;
                }
                this.filterName = value;
            }
        }

        [DataMember(Order = 2)]
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

        #endregion
    }
}