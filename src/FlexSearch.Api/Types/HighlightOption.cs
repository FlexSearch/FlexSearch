namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class HighlightOption
    {
        #region Fields

        private int fragmentsToReturn = 2;

        private string postTag = "</B>";

        private string preTag = "<B>";

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public int FragmentsToReturn
        {
            get
            {
                return this.fragmentsToReturn;
            }

            set
            {
                this.fragmentsToReturn = value;
            }
        }

        [DataMember(Order = 2)]
        public StringList HighlightedFields { get; set; }

        [DataMember(Order = 3)]
        public string PostTag
        {
            get
            {
                return this.postTag;
            }

            set
            {
                this.postTag = value;
            }
        }

        [DataMember(Order = 4)]
        public string PreTag
        {
            get
            {
                return this.preTag;
            }

            set
            {
                this.preTag = value;
            }
        }

        #endregion
    }
}