namespace FlexSearch.Specs.Helpers
{
    using Ploeh.AutoFixture.Xunit;

    public class IntegrationInlineAutoFixture : InlineAutoDataAttribute
    {
        #region Constructors and Destructors

        public IntegrationInlineAutoFixture(params object[] values)
            : base(new IntegrationAutoFixture(), values)
        {
        }

        #endregion
    }
}