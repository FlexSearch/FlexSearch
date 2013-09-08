namespace FlexSearch.Validators
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.FluentValidation;

    public class TokenizerValidator : AbstractValidator<Tokenizer>
    {
        #region Constructors and Destructors

        public TokenizerValidator(Interface.IFactoryCollection factoryCollection)
        {
            this.RuleFor(x => x.TokenizerName).SetValidator(new PropertyNameValidator("TokenizerName"));
            this.RuleFor(x => x.TokenizerName)
                .Must(x => factoryCollection.TokenizerFactory.ModuleExists(x))
                .WithName("TokenizerName")
                .WithMessage("Tokenizer does not exist.");
        }

        #endregion
    }
}