namespace FlexSearch.Validators
{
    public class IndexValidationParameters
    {
        #region Constructors and Destructors

        public IndexValidationParameters(bool validateAll)
        {
            this.ValidateAll = validateAll;
            if (this.ValidateAll)
            {
                this.ValidateConfiguration = true;
                this.ValidateAnalyzers = true;
                this.ValidateScripts = true;
                this.ValidateFields = true;
                this.ValidateSearchProfiles = true;
            }
        }

        #endregion

        #region Public Properties

        public bool ValidateAll { get; set; }
        public bool ValidateAnalyzers { get; set; }
        public bool ValidateConfiguration { get; set; }
        public bool ValidateFields { get; set; }
        public bool ValidateScripts { get; set; }
        public bool ValidateSearchProfiles { get; set; }

        #endregion
    }
}