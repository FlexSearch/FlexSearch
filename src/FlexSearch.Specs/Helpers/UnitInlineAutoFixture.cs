namespace FlexSearch.Specs.Helpers
{
    using Ploeh.AutoFixture.Xunit;

    public class UnitInlineAutoFixture : InlineAutoDataAttribute
    {
        #region Constructors and Destructors

        public UnitInlineAutoFixture(params object[] values)
            : base(new UnitAutoFixture(), values)
        {
        }

        #endregion
    }
}