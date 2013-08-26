namespace FlexSearch.Api
{
    internal static class ApiDescriptionGlobalTypes
    {
        #region Constants

        public const string Analyzer =
            "An Analyzer is responsible for building a TokenStream which can be consumed by the indexing and searching processes.";

        public const string Fields = "Represents fields for indexing.";

        public const string Filter =
            "A Filter is also a TokenStream and is responsible for modifying tokens that have been created by the Tokenizer. Common modifications performed by a Filter are: deletion, stemming, synonym injection, and down casing. Not all Analyzers require TokenFilters.";

        public const string Id = "The name of the index";

        public const string Index = "The name of the index";

        public const string IndexName = "The name of the index";

        public const string Scripts = "The name of the index";

        public const string SearchProfile = "";

        public const string SearchProfileSelector = "The name of the index";

        public const string Tokenizer =
            "A Tokenizer is a TokenStream and is responsible for breaking up incoming text into tokens. In most cases, an Analyzer will use a Tokenizer as the first step in the analysis process.";

        #endregion
    }

    internal static class ApiDescriptionHttpResponse
    {
        #region Constants

        public const string Accepted =
            "The request has been accepted for processing, but the processing has not been completed. The request might or might not eventually be acted upon, as it might be disallowed when processing actually takes place";

        public const string BadRequest = "The request cannot be fulfilled due to bad syntax";

        public const string Created = "The request has been fulfilled and resulted in a new resource being created";

        public const string InternalServerError = "Something went horribly wrong";

        public const string Ok = "Success";

        #endregion
    }
}