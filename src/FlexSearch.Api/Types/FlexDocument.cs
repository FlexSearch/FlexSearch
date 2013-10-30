namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class Document
    {
        #region Constructors and Destructors

        public Document()
        {
            this.Fields = new KeyValuePairs();
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public KeyValuePairs Fields { get; set; }

        [DataMember(Order = 2)]
        public StringList Highlights { get; set; }

        [DataMember(Order = 3)]
        public string Id { get; set; }

        [DataMember(Order = 4)]
        public string Index { get; set; }

        [DataMember(Order = 5)]
        public string LastModified { get; set; }

        [DataMember(Order = 6)]
        public double Score { get; set; }

        #endregion
    }
}