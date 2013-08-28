namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class Index
    {
        #region Fields

        private AnalyzerDictionary analyzers = new AnalyzerDictionary();

        private IndexConfiguration configuration = new IndexConfiguration();

        private FieldDictionary fields = new FieldDictionary();

        private ScriptDictionary scripts = new ScriptDictionary();

        private SearchProfileDictionary searchProfiles = new SearchProfileDictionary();

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public AnalyzerDictionary Analyzers
        {
            get
            {
                return this.analyzers;
            }
            set
            {
                if (value == null)
                {
                    this.analyzers = new AnalyzerDictionary();
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
        public FieldDictionary Fields
        {
            get
            {
                return this.fields;
            }
            set
            {
                if (value == null)
                {
                    this.fields = new FieldDictionary();
                    return;
                }
                this.fields = value;
            }
        }

        [DataMember(Order = 4)]
        public string IndexName { get; set; }

        [DataMember(Order = 5)]
        public bool Online { get; set; }

        [DataMember(Order = 6)]
        public ScriptDictionary Scripts
        {
            get
            {
                return this.scripts;
            }
            set
            {
                if (value == null)
                {
                    this.scripts = new ScriptDictionary();
                    return;
                }
                this.scripts = value;
            }
        }

        [DataMember(Order = 7)]
        public SearchProfileDictionary SearchProfiles
        {
            get
            {
                return this.searchProfiles;
            }
            set
            {
                if (value == null)
                {
                    this.searchProfiles = new SearchProfileDictionary();

                    return;
                }
                this.searchProfiles = value;
            }
        }

        #endregion
    }
}