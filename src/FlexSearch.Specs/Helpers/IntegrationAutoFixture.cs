namespace FlexSearch.Specs.Helpers
{
    using Ploeh.AutoFixture.Xunit;

    public class IntegrationAutoFixture : AutoDataAttribute
    {
        #region Constructors and Destructors

        public IntegrationAutoFixture()
            : base(MockHelpers.IntegartionFixtureSetup())
        {
        }

        #endregion
    }
}