namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class SearchFilter
    {
        #region Fields

        private FilterType filterType = FilterType.And;

        #endregion

        #region Constructors and Destructors

        public SearchFilter()
        {
        }

        public SearchFilter(FilterType filterType, SearchCondition[] conditions, SearchFilter[] subFilters)
        {
            this.FilterType = filterType;
            this.Conditions = conditions;
            this.SubFilters = subFilters;
        }

        public SearchFilter(FilterType filterType, SearchCondition[] conditions)
        {
            this.FilterType = filterType;
            this.Conditions = conditions;
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public SearchCondition[] Conditions { get; set; }

        [DataMember(Order = 2)]
        public int ConstantScore { get; set; }

        [DataMember(Order = 3)]
        public FilterType FilterType
        {
            get
            {
                return this.filterType;
            }
            set
            {
                this.filterType = value;
            }
        }

        [DataMember(Order = 4)]
        public SearchFilter[] SubFilters { get; set; }

        #endregion
    }
}