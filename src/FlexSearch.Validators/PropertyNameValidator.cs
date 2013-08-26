namespace FlexSearch.Validators
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using ServiceStack.FluentValidation;
    using ServiceStack.FluentValidation.Results;

    public class PropertyNameValidator: AbstractValidator<string>
    {
        public PropertyNameValidator(string propertyName)
        {
            this.RuleFor(x => x)
                .NotNull()
                .NotEmpty()
                .Matches("^[a-z0-9]*$")
                .Must(x => !string.Equals(x, "id") && !string.Equals(x, "lastmodified") && !string.Equals(x, "type"))
                .WithName(propertyName)
                .WithErrorCode("InvalidPropertyName")
                .WithMessage(
                    "Property name does not satisfy the required naming convention: not empty, must match regex expression ^[a-z0-9]*$ and cannot be 'id', 'type' and 'lastmodified'.");
        }

        #region Public Methods and Operators

        public ValidationResult Validate(string propertyName, string propertyValue)
        {
            var result = new List<ValidationFailure>();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                result.Add(
                    new ValidationFailure(
                        propertyName,
                        "Property name cannot null or empty.",
                        "InvalidPropertyName",
                        propertyName));
                return new ValidationResult(result);
            }

            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                result.Add(
                    new ValidationFailure(
                        propertyName,
                        "Property value cannot null or empty.",
                        "InvalidPropertyName",
                        propertyValue));
                return new ValidationResult(result);
            }

            if (string.Equals(propertyValue, "id") || string.Equals(propertyValue, "lastmodified")
                || string.Equals(propertyValue, "type"))
            {
                result.Add(
                    new ValidationFailure(
                        propertyName,
                        "Property name cannot be 'id', 'type' and 'lastmodified'.",
                        "InvalidPropertyName",
                        propertyValue));
                return new ValidationResult(result);
            }

            if (!Regex.IsMatch(propertyValue, "^[a-z0-9]*$"))
            {
                result.Add(
                    new ValidationFailure(
                        propertyName,
                        "Property name must match regex expression ^[a-z0-9]*$",
                        "InvalidPropertyName",
                        propertyValue));
                return new ValidationResult(result);
            }

            return new ValidationResult();
        }

        #endregion
    }
}