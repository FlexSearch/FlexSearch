namespace FlexSearch.Validators
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;

    using ServiceStack.FluentValidation;

    public class SearchFilterValidator : AbstractValidator<SearchFilter>
    {
        #region Constructors and Destructors

        public SearchFilterValidator(Dictionary<string, IndexFieldProperties> fields)
        {
            this.RuleFor(x => x.FilterType).NotNull();
            this.RuleFor(x => x.Conditions).NotNull().NotEmpty();
            this.When(
                x => x.ConstantScore != 0,
                () =>
                    this.RuleFor(x => x.ConstantScore)
                        .GreaterThan(1)
                        .WithMessage("Constant score should be greater than 1."));

            this.Custom(
                filter =>
                {
                    for (int index = 0; index < filter.Conditions.Count; index++)
                    {
                        int index1 = index;
                        this.RuleFor(x => x.Conditions[index1]).SetValidator(new SearchConditionValidator(fields));
                    }

                    return null;
                });

            this.Custom(
                filter =>
                {
                    if (filter.SubFilters == null)
                    {
                        return null;
                    }

                    // If we have sub filters then validate them
                    this.RuleFor(x => x.SubFilters).SetCollectionValidator(new SearchFilterValidator(fields));
                    return null;
                });
        }

        #endregion
    }
}