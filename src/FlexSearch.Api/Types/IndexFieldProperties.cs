namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class IndexFieldProperties
    {
        #region Fields

        private bool analyze;

        private FieldTermVector fieldTermVector;

        private FieldType fieldType;

        private bool index;

        private string indexAnalyzer;

        private string searchAnalyzer;

        private bool store;

        #endregion

        public IndexFieldProperties()
        {
            analyze = true;
            index = true;
            store = true;
            fieldTermVector = FieldTermVector.StoreTermVectorsWithPositionsandOffsets;
            fieldType = FieldType.Text;
            indexAnalyzer = "standardanalyzer";
            searchAnalyzer = "standardanalyzer";

        }

        #region Public Properties

        [DataMember(Order = 1)]
        public bool Analyze
        {
            get
            {
                return this.analyze;
            }

            set
            {
                this.analyze = value;
            }
        }

        [DataMember(Order = 2)]
        public FieldTermVector FieldTermVector
        {
            get
            {
                return this.fieldTermVector;
            }

            set
            {
                this.fieldTermVector = value;
            }
        }

        [DataMember(Order = 3)]
        public FieldType FieldType
        {
            get
            {
                return this.fieldType;
            }

            set
            {
                this.fieldType = value;
            }
        }

        [DataMember(Order = 4)]
        public bool Index
        {
            get
            {
                return this.index;
            }

            set
            {
                this.index = value;
            }
        }

        [DataMember(Order = 5)]
        public string IndexAnalyzer
        {
            get
            {
                return this.indexAnalyzer;
            }

            set
            {
                this.indexAnalyzer = value;
            }
        }

        [DataMember(Order = 6)]
        public string ScriptName { get; set; }

        [DataMember(Order = 7)]
        public string SearchAnalyzer
        {
            get
            {
                return this.searchAnalyzer;
            }

            set
            {
                this.searchAnalyzer = value;
            }
        }

        [DataMember(Order = 8)]
        public bool Store
        {
            get
            {
                return this.store;
            }

            set
            {
                this.store = value;
            }
        }

        #endregion
    }
}