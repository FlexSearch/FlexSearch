namespace FlexSearch.Validators
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;

    using ServiceStack.FluentValidation;

    public class SearchProfileValidator : AbstractValidator<SearchProfileProperties>
    {
        #region Constructors and Destructors

        public SearchProfileValidator(Dictionary<string, IndexFieldProperties> fields)
        {
            this.RuleFor(x => x.SearchQuery).NotNull();
        }

        #endregion
    }
}