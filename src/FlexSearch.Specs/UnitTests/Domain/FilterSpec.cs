namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Api.Types;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;
    using FlexSearch.Validators;

    using FluentAssertions;

    public class FilterSpec
    {
        #region Public Methods and Operators

        [Specification]
        public void FilterApiTypeRelated()
        {
            Filter sut = null;
            "Given a new filter".Given(() => sut = new Filter());

            "'Parameters' should not be null".Then(() => sut.Parameters.Should().NotBeNull());
            "'FilterName' should default to 'standardfilter'".Then(() => sut.FilterName.Should().Be("standardfilter"));
            "'Parameters' can not be set to null".Then(
                () =>
                {
                    sut.Parameters = null;
                    sut.Parameters.Should().NotBeNull();
                });
            "'FilterName' can not be set to null".Then(
                () =>
                {
                    sut.FilterName = null;
                    sut.FilterName.Should().NotBeNull();
                });
            "'FilterName' can not be set to whitespace".Then(
                () =>
                {
                    sut.FilterName = string.Empty;
                    sut.FilterName.Should().NotBeNull();
                });
            "'FilterName' can not be to non null string".Then(
                () =>
                {
                    sut.FilterName = "test";
                    sut.FilterName.Should().Be("test");
                });
            "'Parameter' can be set to non null values".Then(
                () =>
                {
                    sut.Parameters = new KeyValuePairs { { "test", "test" } };
                    sut.Parameters.Should().ContainKey("test");
                });
        }

        [Thesis]
        [UnitAutoFixture]
        public void FilterValidatorRelated(FilterValidator sut)
        {
            "Given a new filter validator".Given(() => { });

            "Setting invalid 'FilterName' should fail validation".Then(
                () =>
                {
                    var filter = new Filter { FilterName = "test" };
                    sut.Validate(filter).IsValid.Should().BeFalse();
                });

            "Setting valid 'FilterName' should pass validation".Then(
                () =>
                {
                    // standardfilter is the default filter
                    var filter = new Filter();
                    sut.Validate(filter).IsValid.Should().BeTrue();
                });
        }

        #endregion
    }
}