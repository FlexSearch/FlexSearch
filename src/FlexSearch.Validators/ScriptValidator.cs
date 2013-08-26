// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScriptValidator.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace FlexSearch.Validators
{
    using System;
    using System.Collections.Generic;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.FluentValidation;
    using ServiceStack.FluentValidation.Results;

    public class ScriptValidator : AbstractValidator<ScriptProperties>
    {
        #region Fields

        private Interface.IFactoryCollection factoryCollection;

        #endregion

        #region Constructors and Destructors

        public ScriptValidator(Interface.IFactoryCollection factoryCollection)
        {
            this.factoryCollection = factoryCollection;
            this.RuleFor(x => x.ScriptType).NotNull();
            this.RuleFor(x => x.ScriptOption).NotNull();
            this.RuleFor(x => x.ScriptSource).NotNull();
        }

        #endregion

        #region Public Methods and Operators

        public override ValidationResult Validate(ScriptProperties instance)
        {
            // Executes the validations defined in the constructor
            var result = base.Validate(instance);
            if (!result.IsValid)
            {
                return result;
            }

            try
            {
                switch (instance.ScriptType)
                {
                    case ScriptType.SearchProfileSelector:
                        this.factoryCollection.ScriptFactoryCollection.ProfileSelectorScriptFactory.CompileScript(
                            instance);
                        break;
                    case ScriptType.CustomScoring:
                        this.factoryCollection.ScriptFactoryCollection.CustomScoringScriptFactory.CompileScript(
                            instance);
                        break;
                    case ScriptType.ComputedField:
                        this.factoryCollection.ScriptFactoryCollection.ComputedFieldScriptFactory.CompileScript(
                            instance);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                return
                    new ValidationResult(
                        new List<ValidationFailure>()
                        {
                            new ValidationFailure(
                                "Script",
                                string.Format("Script cannot be compiled. {0}", e.Message),
                                "ScriptCompilationError")
                        });
            }

            return new ValidationResult();
        }

        #endregion
    }
}