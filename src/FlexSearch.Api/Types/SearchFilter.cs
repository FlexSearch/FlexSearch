namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class SearchFilter
    {
        #region Fields

        private List<SearchCondition> conditions;

        private FilterType filterType = FilterType.And;

        #endregion

        #region Constructors and Destructors

        public SearchFilter()
        {
        }

        public SearchFilter(FilterType filterType, List<SearchCondition> conditions, List<SearchFilter> subFilters)
        {
            this.FilterType = filterType;
            this.Conditions = conditions;
            this.SubFilters = subFilters;
        }

        public SearchFilter(FilterType filterType, List<SearchCondition> conditions)
        {
            this.FilterType = filterType;
            this.Conditions = conditions;
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public List<SearchCondition> Conditions
        {
            get
            {
                return this.conditions ?? (this.conditions = new List<SearchCondition>());
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                this.conditions = value;
            }
        }

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
        public List<SearchFilter> SubFilters { get; set; }

        #endregion
    }
}