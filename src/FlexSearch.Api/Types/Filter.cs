namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class Filter
    {
        #region Fields

        private KeyValuePairs parameters;

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public string FilterName { get; set; }

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