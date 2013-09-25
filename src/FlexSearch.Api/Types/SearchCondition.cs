namespace FlexSearch.Api.Types
{
    using System.Globalization;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class SearchCondition
    {
        #region Fields

        private MissingValueOption missingValueOption = MissingValueOption.ThrowError;

        private KeyValuePairs parameters = new KeyValuePairs();

        #endregion

        #region Constructors and Destructors

        public SearchCondition()
        {
        }

        public SearchCondition(string fieldName, string operatorName, StringList values)
        {
            this.FieldName = fieldName;
            this.Operator = operatorName;
            this.Values = values;
        }

        public SearchCondition(string fieldName, string operatorName, string value)
        {
            this.FieldName = fieldName;
            this.Operator = operatorName;
            this.Values = new StringList { value };
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public int Boost { get; set; }

        [DataMember(Order = 2)]
        public string FieldName { get; set; }

        [DataMember(Order = 3)]
        public MissingValueOption MissingValueOption
        {
            get
            {
                return this.missingValueOption;
            }
            set
            {
                this.missingValueOption = value;
            }
        }

        [DataMember(Order = 4)]
        public string Operator { get; set; }

        [DataMember(Order = 5)]
        public KeyValuePairs Parameters
        {
            get
            {
                return this.parameters;
            }
            set
            {
                if (value == null)
                {
                    this.parameters = new KeyValuePairs();
                    return;
                }

                this.parameters = value;
            }
        }

        [DataMember(Order = 6)]
        public StringList Values { get; set; }

        #endregion

        #region Public Methods and Operators

        public static SearchCondition GetNumericRangeCondition(
            string fieldName,
            string lowerBound,
            string upperBound,
            bool includeLowerBound,
            bool includeUpperBound)
        {
            return new SearchCondition(fieldName, "numeric_range", new StringList { lowerBound, upperBound })
                   {
                       Parameters
                           =
                           new KeyValuePairs
                           {
                               {
                                   "includelower",
                                   includeLowerBound
                                   .ToString
                                   (
                                       )
                               },
                               {
                                   "includeupper",
                                   includeUpperBound
                                   .ToString
                                   (
                                       )
                               }
                           }
                   };
        }

        public static SearchCondition GetPhraseMatchCondition(string fieldName, string value)
        {
            return new SearchCondition(fieldName, "phrase_match", new StringList { value });
        }

        public static SearchCondition GetPhraseMatchCondition(string fieldName, string value, int slop)
        {
            return new SearchCondition(fieldName, "phrase_match", new StringList { value })
                   {
                       Parameters =
                           new KeyValuePairs
                           {
                               {
                                   "slop",
                                   slop
                                   .ToString
                                   (
                                       CultureInfo
                                   .InvariantCulture)
                               }
                           }
                   };
        }

        public static SearchCondition GetFuzzyMatchCondition(string fieldName, string value, int slop)
        {
            return new SearchCondition(fieldName, "fuzzy_match", new StringList { value })
                   {
                       Parameters =
                           new KeyValuePairs
                           {
                               {
                                   "slop",
                                   slop.ToString(CultureInfo.InvariantCulture)
                               }
                           }
                   };
        }

        public static SearchCondition GetTermMatchCondition(string fieldName, string value)
        {
            return new SearchCondition(fieldName, "term_match", new StringList { value });
        }

        #endregion
    }
}