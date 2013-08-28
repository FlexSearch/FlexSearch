namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class IndexFieldProperties
    {
        #region Fields

        private bool analyze = true;

        private FieldTermVector fieldTermVector = FieldTermVector.StoreTermVectorsWithPositionsandOffsets;

        private FieldType fieldType = FieldType.Text;

        private bool index = true;

        private string indexAnalyzer = "standardanalyzer";

        private string searchAnalyzer = "standardanalyzer";

        private bool store = true;

        #endregion

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