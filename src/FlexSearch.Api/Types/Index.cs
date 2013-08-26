namespace FlexSearch.Api.Types
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Index
    {
        #region Fields

        private Dictionary<string, AnalyzerProperties> analyzers =
            new Dictionary<string, AnalyzerProperties>(StringComparer.OrdinalIgnoreCase);

        private IndexConfiguration configuration = new IndexConfiguration();

        private Dictionary<string, IndexFieldProperties> fields =
            new Dictionary<string, IndexFieldProperties>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, ScriptProperties> scripts =
            new Dictionary<string, ScriptProperties>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, SearchProfileProperties> searchProfiles =
            new Dictionary<string, SearchProfileProperties>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public Dictionary<string, AnalyzerProperties> Analyzers
        {
            get
            {
                return this.analyzers;
            }
            set
            {
                if (value == null)
                {
                    this.analyzers = new Dictionary<string, AnalyzerProperties>(StringComparer.OrdinalIgnoreCase);
                    return;
                }

                this.analyzers = value;
            }
        }

        [DataMember(Order = 2)]
        public IndexConfiguration Configuration
        {
            get
            {
                return this.configuration;
            }
            set
            {
                this.configuration = value;
            }
        }

        [DataMember(Order = 3)]
        public Dictionary<string, IndexFieldProperties> Fields
        {
            get
            {
                return this.fields;
            }
            set
            {
                if (value == null)
                {
                    this.fields = new Dictionary<string, IndexFieldProperties>(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                this.fields = value;
            }
        }

        [DataMember(Order = 4)]
        public string IndexName { get; set; }

        [DataMember(Order = 5)]
        public Dictionary<string, ScriptProperties> Scripts
        {
            get
            {
                return this.scripts;
            }
            set
            {
                if (value == null)
                {
                    this.scripts = new Dictionary<string, ScriptProperties>(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                this.scripts = value;
            }
        }

        [DataMember(Order = 6)]
        public Dictionary<string, SearchProfileProperties> SearchProfiles
        {
            get
            {
                return this.searchProfiles;
            }
            set
            {
                if (value == null)
                {
                    this.searchProfiles =
                        new Dictionary<string, SearchProfileProperties>(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                this.searchProfiles = value;
            }
        }

        #endregion
    }
}