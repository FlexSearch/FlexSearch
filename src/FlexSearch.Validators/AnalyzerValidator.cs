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
            this.RuleFor(x => x.TokenizerName).SetValidator(new PropertyNameValidator("TokenizerName"));
            this.RuleFor(x => x.TokenizerName)
                .Must(x => factoryCollection.TokenizerFactory.ModuleExists(x))
                .WithMessage("Tokenizer does not exist.");
            this.RuleFor(x => x.Filters).SetCollectionValidator(new FilterValidator(factoryCollection));
        }

        #endregion
    }
}