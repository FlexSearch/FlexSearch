namespace FlexSearch.Specs.Helpers
{
    using Ploeh.AutoFixture.Xunit;

    public class UnitAutoFixture : AutoDataAttribute
    {
        #region Constructors and Destructors

        public UnitAutoFixture()
            : base(MockHelpers.UnitFixtureSetup())
        {
        }

        #endregion
    }
}