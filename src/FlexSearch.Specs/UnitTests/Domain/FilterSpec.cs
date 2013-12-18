namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit;

    public class FilterSpec
    {
        #region Public Methods and Operators

        //[Specification]
        //public void FilterApiTypeRelated()
        //{
        //    Filter sut = null;
        //    "Given a new filter".Given(() => sut = new Filter());

        //    "'Parameters' should not be null".Then(() => sut.Parameters.Should().NotBeNull());
        //    "'FilterName' should default to 'standardfilter'".Then(() => sut.FilterName.Should().Be("standardfilter"));
        //    "'Parameters' can not be set to null".Then(
        //        () =>
        //        {
        //            sut.Parameters = null;
        //            sut.Parameters.Should().NotBeNull();
        //        });
        //    "'FilterName' can not be set to null".Then(
        //        () =>
        //        {
        //            sut.FilterName = null;
        //            sut.FilterName.Should().NotBeNull();
        //        });
        //    "'FilterName' can not be set to whitespace".Then(
        //        () =>
        //        {
        //            sut.FilterName = string.Empty;
        //            sut.FilterName.Should().NotBeNull();
        //        });
        //    "'FilterName' can not be to non null string".Then(
        //        () =>
        //        {
        //            sut.FilterName = "test";
        //            sut.FilterName.Should().Be("test");
        //        });
        //    "'Parameter' can be set to non null values".Then(
        //        () =>
        //        {
        //            sut.Parameters = new KeyValuePairs { { "test", "test" } };
        //            sut.Parameters.Should().ContainKey("test");
        //        });
        //}

        //[Thesis]
        //[UnitAutoFixture]
        //public void FilterValidatorRelated(Interface.IFactoryCollection factory)
        //{
        //    "Given a new filter validator".Given(() => { });

        //    "Setting invalid 'FilterName' should fail validation".Then(
        //        () =>
        //        {
        //            var filter = new Filter { FilterName = "test" };
        //            Assert.Throws<Validator.ValidationException>(() => Validator.FilterValidator(factory, filter));
        //        });

        //    "Setting valid 'FilterName' should pass validation".Then(
        //        () =>
        //        {
        //            // standardfilter is the default filter
        //            var filter = new Filter();
        //            Assert.DoesNotThrow(() => Validator.FilterValidator(factory, filter));
        //        });
        //}

        #endregion
    }
}