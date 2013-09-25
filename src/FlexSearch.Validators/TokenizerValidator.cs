namespace FlexSearch.Validators
{
    using System;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using Microsoft.FSharp.Core;

    using ServiceStack.FluentValidation;
    using ServiceStack.FluentValidation.Results;

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

            this.Custom(
                tokenizer =>
                {
                    FSharpOption<Interface.IFlexTokenizerFactory> tokenizerInstance =
                        factoryCollection.TokenizerFactory.GetModuleByName(tokenizer.TokenizerName);
                    try
                    {
                        tokenizerInstance.Value.Initialize(tokenizer.Parameters, factoryCollection.ResourceLoader);
                    }
                    catch (Exception e)
                    {
                        return new ValidationFailure(
                            "TokenizerName",
                            string.Format("Tokenizer cannot be initialized. {0}", e.Message),
                            "TokenizerInitError",
                            tokenizer);
                    }

                    return null;
                });
        }

        #endregion
    }
}