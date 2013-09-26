namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Api.Types;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;
    using FlexSearch.Validators;

    using FluentAssertions;

    using ServiceStack.FluentValidation.TestHelper;

    public class IndexConfigurationSpec
    {
        #region Public Methods and Operators

        [Specification]
        public void DefaultValueTest()
        {
            IndexConfiguration sut = null;
            "Given new index field properties".Given(() => sut = new IndexConfiguration());

            "'CommitTimeSec' should be '60'".Then(() => sut.CommitTimeSec.Should().Be(60));
            "'DirectoryType' should be 'FileSystem'".Then(() => sut.DirectoryType.Should().Be(DirectoryType.FileSystem));
            "'RamBufferSizeMb' should be '500'".Then(() => sut.RamBufferSizeMb.Should().Be(500));
            "'RefreshTimeMilliSec' should be '25'".Then(() => sut.RefreshTimeMilliSec.Should().Be(25));
            "'Shards' should be '1'".Then(() => sut.Shards.Should().Be(1));
        }

        [Thesis]
        [UnitAutoFixture]
        public void IndexConfigurationValidatorTest(
            IndexConfiguration indexConfiguration,
            IndexConfigurationValidator sut)
        {
            "Given new index field properties & index configuration validator".Given(() => { });
            "'CommitTimeSec' cannot be less than '60'".Then(
                () =>
                {
                    indexConfiguration.CommitTimeSec = 59;
                    sut.ShouldHaveValidationErrorFor(x => x.CommitTimeSec, indexConfiguration);
                });

            "'RefreshTimeMilliSec' cannot be less than '25'".Then(
                () =>
                {
                    indexConfiguration.RefreshTimeMilliSec = 24;
                    sut.ShouldHaveValidationErrorFor(x => x.RefreshTimeMilliSec, indexConfiguration);
                });

            "'Shards' cannot be less than '1'".Then(
                () =>
                {
                    indexConfiguration.Shards = 0;
                    sut.ShouldHaveValidationErrorFor(x => x.Shards, indexConfiguration);
                });

            "'RamBufferSizeMb' cannot be less than '100'".Then(
                () =>
                {
                    indexConfiguration.RamBufferSizeMb = 99;
                    sut.ShouldHaveValidationErrorFor(x => x.RamBufferSizeMb, indexConfiguration);
                });
        }

        #endregion
    }
}