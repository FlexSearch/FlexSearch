namespace FlexSearch.Validators
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.FluentValidation;

    public class AnalyzerValidator : AbstractValidator<AnalyzerProperties>
    {
        #region Constructors and Destructors

        public AnalyzerValidator(Interface.IFactoryCollection factoryCollection)
        {
            this.CascadeMode = CascadeMode.StopOnFirstFailure;
            this.RuleFor(x => x.Tokenizer).SetValidator(new TokenizerValidator(factoryCollection));
            this.RuleFor(x => x.Filters).Must(x => x.Count >= 1).WithMessage("Atleast one filter should be specified.");
            this.RuleFor(x => x.Filters).SetCollectionValidator(new FilterValidator(factoryCollection));
        }

        #endregion
    }
}