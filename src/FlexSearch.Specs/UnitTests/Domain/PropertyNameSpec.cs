namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers.SubSpec;

    using Ploeh.AutoFixture.Xunit;

    using Xunit;

    public class PropertyNameSpec
    {
        #region Public Methods and Operators

        [Thesis]
        [InlineAutoData("TEST")]
        [InlineAutoData("Test")]
        [InlineAutoData("id")]
        [InlineAutoData("type")]
        [InlineAutoData("lastmodified")]
        public void PropertyNameValueIsInvalid(string propertyNameValue)
        {
            "Given a property name validator".Given(() => { });
            string.Format(
                "when a propertyNameValue of '{0}' is passed, then there should be validation error",
                propertyNameValue)
                .Then(
                    () =>
                        Assert.Throws<Validator.ValidationException>(
                            () => Validator.propertyNameValidator("", propertyNameValue)));
        }

        [Thesis]
        [InlineAutoData("test")]
        [InlineAutoData("1234")]
        public void PropertyNameValueIsValid(string propertyNameValue)
        {
            "Given a property name validator".Given(() => { });
            string.Format(
                "when a propertyNameValue of '{0}' is passed, then there should be no validation error",
                propertyNameValue)
                .Then(() => Assert.DoesNotThrow(() => Validator.propertyNameValidator("", propertyNameValue)));
        }

        #endregion
    }
}