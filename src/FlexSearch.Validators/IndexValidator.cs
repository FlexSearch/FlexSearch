namespace FlexSearch.Validators
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.FluentValidation;
    using ServiceStack.FluentValidation.Results;

    public class IndexValidator : AbstractValidator<Index>
    {
        #region Fields

        private readonly Interface.IFactoryCollection factoryCollection;

        private readonly IndexValidationParameters parameters;

        #endregion

        #region Constructors and Destructors

        public IndexValidator(Interface.IFactoryCollection factoryCollection, IndexValidationParameters parameters)
        {
            this.factoryCollection = factoryCollection;
            this.parameters = parameters;
        }

        #endregion

        #region Public Methods and Operators

        public override ValidationResult Validate(Index index)
        {
            var propertyNameValidator = new PropertyNameValidator("dummy");
            var indexNameValidationResult = propertyNameValidator.Validate("IndexName", index.IndexName);
            if (!indexNameValidationResult.IsValid)
            {
                return indexNameValidationResult;
            }

            if (this.parameters.ValidateConfiguration)
            {
                var configurationValidator = new IndexConfigurationValidator();
                var configurationValidationResult = configurationValidator.Validate(index.Configuration);
                if (!configurationValidationResult.IsValid)
                {
                    return configurationValidationResult;
                }
            }

            if (this.parameters.ValidateAnalyzers)
            {
                var analyzerValidator = new AnalyzerValidator(this.factoryCollection);
                foreach (var analyzer in index.Analyzers)
                {
                    var analyzerNameValidationResult = propertyNameValidator.Validate("AnalyzerName", analyzer.Key);
                    if (!analyzerNameValidationResult.IsValid)
                    {
                        return analyzerNameValidationResult;
                    }

                    var analyzerValidationResult = analyzerValidator.Validate(analyzer.Value);
                    if (!analyzerValidationResult.IsValid)
                    {
                        return analyzerValidationResult;
                    }
                }
            }
            if (this.parameters.ValidateScripts)
            {
                var scriptValidator = new ScriptValidator(this.factoryCollection);
                foreach (var script in index.Scripts)
                {
                    var scriptNameValidationResult = propertyNameValidator.Validate("ScriptName", script.Key);
                    if (!scriptNameValidationResult.IsValid)
                    {
                        return scriptNameValidationResult;
                    }

                    var scriptValidationResult = scriptValidator.Validate(script.Value);
                    if (!scriptValidationResult.IsValid)
                    {
                        return scriptValidationResult;
                    }
                }
            }

            if (this.parameters.ValidateFields)
            {
                var fieldValidator = new IndexFieldValidator(this.factoryCollection, index.Analyzers, index.Scripts);
                foreach (var field in index.Fields)
                {
                    var fieldNameValidationResult = propertyNameValidator.Validate("FieldName", field.Key);
                    if (!fieldNameValidationResult.IsValid)
                    {
                        return fieldNameValidationResult;
                    }

                    var fieldValidationResult = fieldValidator.Validate(field.Value);
                    if (!fieldValidationResult.IsValid)
                    {
                        return fieldValidationResult;
                    }
                }
            }

            if (this.parameters.ValidateSearchProfiles)
            {
                var profileValidator = new SearchProfileValidator(index.Fields);
                foreach (var profile in index.SearchProfiles)
                {
                    var profileNameValidationResult = propertyNameValidator.Validate("SearchProfileName", profile.Key);
                    if (!profileNameValidationResult.IsValid)
                    {
                        return profileNameValidationResult;
                    }

                    var profileValidationResult = profileValidator.Validate(profile.Value);
                    if (!profileValidationResult.IsValid)
                    {
                        return profileValidationResult;
                    }
                }
            }

            return new ValidationResult();
        }

        #endregion
    }
}