namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Filter
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public string FilterName { get; set; }

        [DataMember(Order = 2)]
        public Dictionary<string, string> Parameters { get; set; }

        #endregion
    }
}