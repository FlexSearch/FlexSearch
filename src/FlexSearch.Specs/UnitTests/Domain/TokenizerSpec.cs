namespace FlexSearch.Specs.UnitTests.Domain
{
    //using FlexSearch.Api;
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit;

    public class TokenizerSpec
    {
        #region Public Methods and Operators

        //[Specification]
        //public void TokenizerApiTypeRelated()
        //{
        //    Tokenizer sut = null;
        //    "Given a new tokenizer".Given(() => sut = new Tokenizer());

        //    "'Parameters' should not be null".Then(() => sut.Parameters.Should().NotBeNull());

        //    "'TokenizerName' should default to 'standard tokenizer'".Then(
        //        () => sut.TokenizerName.Should().Be("standardtokenizer"));
        //    "'Parameters' can not be set to null".Then(
        //        () =>
        //        {
        //            sut.Parameters = null;
        //            sut.Parameters.Should().NotBeNull();
        //        });

        //    "when 'TokenizerName' is set to null, then 'standardtokenizer' is used".Then(
        //        () =>
        //        {
        //            sut.TokenizerName = null;
        //            sut.TokenizerName.Should().Be("standardtokenizer");
        //        });

        //    "when 'TokenizerName' is set to whitespace, then 'standardtokenizer' is used".Then(
        //        () =>
        //        {
        //            sut.TokenizerName = string.Empty;
        //            sut.TokenizerName.Should().Be("standardtokenizer");
        //        });

        //    "'TokenizerName' can be set to non-null string".Then(
        //        () =>
        //        {
        //            sut.TokenizerName = "test";
        //            sut.TokenizerName.Should().Be("test");
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
        //public void TokenizerValidatorRelated(Interface.IFactoryCollection factory)
        //{
        //    "Given a new tokenizer validator".Given(() => { });

        //    "Setting invalid 'TokenizerName' should fail validation".Then(
        //        () =>
        //        {
        //            var tokenizer = new Tokenizer { TokenizerName = "test" };
        //            Assert.Throws<Validator.ValidationException>(() => Validator.TokenizerValidator(factory, tokenizer));
        //        });

        //    "Setting valid 'TokenizerName' should pass validation".Then(
        //        () =>
        //        {
        //            // standardtokenizer is the default tokenizer
        //            var tokenizer = new Tokenizer();
        //            Assert.DoesNotThrow(() => Validator.TokenizerValidator(factory, tokenizer));
        //        });
        //}

        #endregion
    }
}