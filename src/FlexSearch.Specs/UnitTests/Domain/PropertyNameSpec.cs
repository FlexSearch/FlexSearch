namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Specs.Helpers.SubSpec;
    using FlexSearch.Validators;

    using FluentAssertions;

    using Ploeh.AutoFixture.Xunit;

    using ServiceStack.FluentValidation.Results;

    public class PropertyNameSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [InlineAutoData("TEST")]
        [InlineAutoData("Test")]
        [InlineAutoData("id")]
        [InlineAutoData("type")]
        [InlineAutoData("lastmodified")]
        public void PropertyNameValueIsInvalid(string propertyNameValue, PropertyNameValidator validator)
        {
            ValidationResult result = null;
            "Given a property name validator".Given(() => { });
            string.Format("when a propertyNameValue of '{0}' is passed", propertyNameValue)
                .When(() => result = validator.Validate(propertyNameValue));
            "then there should be validation error".Then(() => result.IsValid.Should().BeFalse());
        }

        [Thesis]
        [InlineAutoData("test")]
        [InlineAutoData("1234")]
        public void PropertyNameValueIsValid(string propertyNameValue, PropertyNameValidator validator)
        {
            ValidationResult result = null;
            "Given a property name validator".Given(() => { });
            string.Format("when a propertyNameValue of '{0}' is passed", propertyNameValue)
                .When(() => result = validator.Validate(propertyNameValue));
            "then there should be no validation error".Then(() => result.IsValid.Should().BeTrue());
        }

        #endregion
    }
}