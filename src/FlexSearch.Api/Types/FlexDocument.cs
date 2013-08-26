namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Document
    {
        #region Constructors and Destructors

        public Document()
        {
            this.Fields = new Dictionary<string, string>();
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public Dictionary<string, string> Fields { get; set; }

        [DataMember(Order = 2)]
        public List<string> Highlights { get; set; }

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