namespace FlexSearch.Validators
{
    using FlexSearch.Core;

    using ServiceStack.Common.Web;

    public class IndexNameValidator : IIndexNameValidator
    {
        #region Public Properties

        public Interface.IIndexService IndexingService { get; set; }

        #endregion

        #region Public Methods and Operators

        public IndexState Validate(string indexName)
        {
            if (!this.IndexingService.IndexExists(indexName))
            {
                throw HttpError.NotFound("Index does not exist.");
            }

            return this.IndexingService.IndexStatus(indexName);
        }

        #endregion
    }
}