namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;

    public class Schema
    {
        #region Public Properties

        public Dictionary<string, IndexField> Fields { get; set; }
        public string SchemaName { get; set; }

        #endregion
    }
}