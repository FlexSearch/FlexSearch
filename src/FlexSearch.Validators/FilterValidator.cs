namespace FlexSearch.Validators
{
    using System;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using Microsoft.FSharp.Core;

    using ServiceStack.FluentValidation;
    using ServiceStack.FluentValidation.Results;

    public class FilterValidator : AbstractValidator<Filter>
    {
        #region Constructors and Destructors

        public FilterValidator(Interface.IFactoryCollection factoryCollection)
        {
            this.RuleFor(x => x.FilterName).SetValidator(new PropertyNameValidator("FilterName"));
            this.RuleFor(x => x.FilterName)
                .Must(x => factoryCollection.FilterFactory.ModuleExists(x))
                .WithMessage("Filter does not exist.");

            this.Custom(
                filter =>
                {
                    FSharpOption<Interface.IFlexFilterFactory> filterInstance =
                        factoryCollection.FilterFactory.GetModuleByName(filter.FilterName);
                    try
                    {
                        filterInstance.Value.Initialize(filter.Parameters, factoryCollection.ResourceLoader);
                    }
                    catch (Exception e)
                    {
                        return new ValidationFailure(
                            "FilterName",
                            string.Format("Filter cannot be initialized. {0}", e.Message),
                            "FilterInitError",
                            filter);
                    }

                    return null;
                });
        }

        #endregion
    }
}