namespace FlexSearch.Validators
{
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.FluentValidation;

    public class IndexFieldValidator : AbstractValidator<IndexFieldProperties>
    {
        #region Constructors and Destructors

        public IndexFieldValidator(
            Interface.IFactoryCollection factoryCollection,
            Dictionary<string, AnalyzerProperties> analyzers,
            Dictionary<string, ScriptProperties> scripts)
        {
            this.CascadeMode = CascadeMode.StopOnFirstFailure;
            this.RuleFor(x => x.FieldType).NotNull();
            this.RuleFor(x => x.Store).NotNull().NotEmpty();
            this.When(
                x => !string.IsNullOrEmpty(x.ScriptName),
                () => this.RuleFor(x => x.ScriptName).Must(scripts.ContainsKey).WithMessage("Script does not exist"));

            this.When(
                x =>
                    (x.FieldType == FieldType.Custom || x.FieldType == FieldType.Highlight
                     || x.FieldType == FieldType.Text) && !string.IsNullOrEmpty(x.SearchAnalyzer),
                () =>
                {
                    this.RuleFor(x => x.SearchAnalyzer).SetValidator(new PropertyNameValidator("SearchAnalyzer"));
                    this.RuleFor(x => x.SearchAnalyzer)
                        .Must(x => factoryCollection.AnalyzerFactory.ModuleExists(x) || analyzers.ContainsKey(x))
                        .WithMessage("Search Analyzer does not exist.")
                        .WithErrorCode("InvalidAnalyzer");
                });

            this.When(
                x =>
                    (x.FieldType == FieldType.Custom || x.FieldType == FieldType.Highlight
                     || x.FieldType == FieldType.Text) && !string.IsNullOrEmpty(x.IndexAnalyzer),
                () =>
                {
                    this.RuleFor(x => x.IndexAnalyzer).SetValidator(new PropertyNameValidator("IndexAnalyzer"));
                    this.RuleFor(x => x.IndexAnalyzer)
                        .Must(x => factoryCollection.AnalyzerFactory.ModuleExists(x) || analyzers.ContainsKey(x))
                        .WithMessage("Index Analyzer does not exist.")
                        .WithErrorCode("InvalidAnalyzer");
                });
        }

        #endregion
    }
}