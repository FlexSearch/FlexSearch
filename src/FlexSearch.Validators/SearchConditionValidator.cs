namespace FlexSearch.Validators
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.FluentValidation;

    public class SearchConditionValidator : AbstractValidator<SearchCondition>
    {
        #region Constructors and Destructors

        public SearchConditionValidator(Dictionary<string, IndexFieldProperties> fields)
        {
            this.RuleFor(x => x.FieldName).Must(fields.ContainsKey).WithMessage("Field name does not exist.");

            this.When(
                x => x.Boost != 0,
                () => this.RuleFor(x => x.Boost).GreaterThan(1).WithMessage("Boost should be greater than 1."));
            this.RuleFor(x => x.Operator)
                .NotNull()
                .NotEmpty()
                .Must(this.FactoryCollection.SearchQueryFactory.ModuleExists)
                .WithMessage("Search operation does not exist.");
        }

        #endregion

        #region Public Properties

        public Interface.IFactoryCollection FactoryCollection { get; set; }

        #endregion
    }
}