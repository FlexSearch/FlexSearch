namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class Filter
    {
        #region Public Properties

        [DataMember(Order = 1)]
        public string FilterName { get; set; }

        [DataMember(Order = 2)]
        public KeyValuePairs Parameters { get; set; }

        #endregion
    }
}