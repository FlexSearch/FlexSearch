namespace FlexSearch.Validators
{
    using FlexSearch.Api.Types;

    using ServiceStack.FluentValidation;

    public class IndexConfigurationValidator : AbstractValidator<IndexConfiguration>
    {
        #region Constructors and Destructors

        public IndexConfigurationValidator()
        {
            this.RuleFor(x => x.CommitTimeSec).GreaterThanOrEqualTo(60);
            this.RuleFor(x => x.RefreshTimeMilliSec).GreaterThanOrEqualTo(25);
            this.RuleFor(x => x.Shards).GreaterThanOrEqualTo(1);
            this.RuleFor(x => x.RamBufferSizeMb).GreaterThanOrEqualTo(100);
            this.RuleFor(x => x.DirectoryType).NotNull();
        }

        #endregion
    }
}