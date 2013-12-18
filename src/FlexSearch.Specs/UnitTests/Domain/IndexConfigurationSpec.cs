namespace FlexSearch.Specs.UnitTests.Domain
{
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit;

    public class IndexConfigurationSpec
    {
        #region Public Methods and Operators

        [Specification]
        public void DefaultValueTest()
        {
            Api.IndexConfiguration sut = null;
            "Given new index field properties".Given(() => sut = new Api.IndexConfiguration());

            "'CommitTimeSec' should be '60'".Then(() => sut.CommitTimeSec.Should().Be(60));
            "'DirectoryType' should be 'FileSystem'".Then(() => sut.DirectoryType.Should().Be(Api.DirectoryType.FileSystem));
            "'RamBufferSizeMb' should be '500'".Then(() => sut.RamBufferSizeMb.Should().Be(500));
            "'RefreshTimeMilliSec' should be '25'".Then(() => sut.RefreshTimeMilliSec.Should().Be(25));
            "'Shards' should be '1'".Then(() => sut.ShardConfiguration.ShardCount.Should().Be(1));
        }

        [Thesis]
        [UnitAutoFixture]
        public void IndexConfigurationValidatorTest()
        {
            Api.IndexConfiguration indexConfiguration = null;
            "Given new index field properties & index configuration validator".Given(
                () =>
                {
                    indexConfiguration = new Api.IndexConfiguration();
                });

            "'CommitTimeSec' cannot be less than '60'".Then(
                () =>
                {
                    indexConfiguration.CommitTimeSec = 59;
                    Assert.Throws<Validator.ValidationException>(() => Validator.IndexConfigurationValidator("", indexConfiguration));
                });

            "'RefreshTimeMilliSec' cannot be less than '25'".Then(
                () =>
                {
                    indexConfiguration.RefreshTimeMilliSec = 24;
                    Assert.Throws<Validator.ValidationException>(
                        () => Validator.IndexConfigurationValidator("", indexConfiguration));
                });

            "'Shards' cannot be less than '1'".Then(
                () =>
                {
                    indexConfiguration.ShardConfiguration.ShardCount = 0;
                    Assert.Throws<Validator.ValidationException>(
                        () => Validator.IndexConfigurationValidator("", indexConfiguration));
                });

            "'RamBufferSizeMb' cannot be less than '100'".Then(
                () =>
                {
                    indexConfiguration.RamBufferSizeMb = 99;
                    Assert.Throws<Validator.ValidationException>(
                        () => Validator.IndexConfigurationValidator("", indexConfiguration));
                });
        }

        #endregion
    }
}