namespace FlexSearch.Validators
{
    using FlexSearch.Core;

    public interface IIndexNameValidator
    {
        #region Public Methods and Operators

        IndexState Validate(string indexName);

        #endregion
    }
}