namespace FlexSearch.Server.Services
{
    using System;
    using System.Net;

    using FlexSearch.Api.Index;
    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Validators;

    using ServiceStack.Common.Web;
    using ServiceStack.FluentValidation;
    using ServiceStack.OrmLite;
    using ServiceStack.ServiceInterface;

    public class IndexService : Service
    {
        #region Enums

        private enum IndexComponent
        {
            Configuration,

            Analyzers,

            Scripts,

            Fields,

            SearchProfiles
        }

        #endregion

        #region Public Properties

        public IDbConnectionFactory DbConnectionFactory { get; set; }

        public IIndexNameValidator IndexNameValidator { get; set; }

        public Interface.IIndexService IndexingService { get; set; }

        public Interface.IFactoryCollection FactoryCollection { get; set; }

        #endregion

        #region Public Methods and Operators

        public ShowIndexResponse Any(ShowIndex request)
        {
            var indexRecord = this.Db.Single<Index>("IndexName={0}", request.IndexName);
            if (indexRecord == null)
            {
                throw new Exception("Index does not exist.");
            }

            return new ShowIndexResponse { IndexSettings = indexRecord };
        }

        public IndexExistsResponse Any(IndexExists request)
        {
            return new IndexExistsResponse { IndexExists = this.IndexingService.IndexExists(request.IndexName) };
        }

        public OnlineResponse Post(Online request)
        {
            IndexState state = this.IndexNameValidator.Validate(request.IndexName);
            if (state.IsOnline || state.IsOpening)
            {
                throw new Exception("Index is already online or is in opening state.");
            }

            var indexRecord = this.Db.Single<Index>("IndexName={0}", request.IndexName);
            var validationParameters = new IndexValidationParameters(false);
            var indexValidator = new IndexValidator(this.FactoryCollection, validationParameters);
            indexValidator.ValidateAndThrow(indexRecord);

            Tuple<bool, string> res = this.IndexingService.AddIndex(indexRecord);
            if (!res.Item1)
            {
                throw new Exception(res.Item2);
            }

            return new OnlineResponse();
        }

        public OfflineResponse Post(Offline request)
        {
            IndexState state = this.IndexNameValidator.Validate(request.IndexName);
            if (state.IsOffline || state.IsClosing)
            {
                throw new Exception("Index is already offline or is in closing state.");
            }

            Tuple<bool, string> res = this.IndexingService.CloseIndex(request.IndexName);
            if (!res.Item1)
            {
                throw new Exception(res.Item2);
            }

            return new OfflineResponse();
        }

        public UpdateScriptsResponse Post(UpdateScripts request)
        {
            this.UpdateIndexSettingsComponents(
                new Index { IndexName = request.IndexName, Scripts = request.Scripts },
                IndexComponent.Configuration);
            return new UpdateScriptsResponse();
        }

        public UpdateAnalyzersResponse Post(UpdateAnalyzers request)
        {
            this.UpdateIndexSettingsComponents(
                new Index { IndexName = request.IndexName, Analyzers = request.Analyzers },
                IndexComponent.Configuration);
            return new UpdateAnalyzersResponse();
        }

        public UpdateIndexFields Post(UpdateIndexFields request)
        {
            this.UpdateIndexSettingsComponents(
                new Index { IndexName = request.IndexName, Fields = request.IndexFields },
                IndexComponent.Configuration);
            return new UpdateIndexFields();
        }

        public UpdateConfigurationResponse Post(UpdateConfiguration request)
        {
            this.UpdateIndexSettingsComponents(
                new Index { IndexName = request.IndexName, Configuration = request.IndexConfiguration },
                IndexComponent.Configuration);
            return new UpdateConfigurationResponse();
        }

        public UpdateSearchProfilesResponse Post(UpdateSearchProfiles request)
        {
            IndexState state = this.IndexNameValidator.Validate(request.IndexName);
            if (state.IsOnline || state.IsOpening)
            {
                throw new Exception("Index should be offline before making configuration changes.");
            }

            var indexRecord = this.Db.Single<Index>("IndexName={0}", request.IndexName);
            var searchProfileValidator = new SearchProfileValidator(indexRecord.Fields);
            foreach (var searchProfile in request.SearchProfiles)
            {
                searchProfileValidator.ValidateAndThrow(searchProfile.Value);
            }

            indexRecord.SearchProfiles = request.SearchProfiles;
            this.Db.Update(indexRecord);
            return new UpdateSearchProfilesResponse();
        }

        public CreateIndexResponse Post(CreateIndex request)
        {
            if (this.IndexingService.IndexExists(request.Index.IndexName))
            {
                throw new HttpError(HttpStatusCode.BadRequest, "IndexAlreadyExists", "Index already exists. Use 'update_' commands to update index settings.");
            }

            var indexValidator = new IndexValidator(this.FactoryCollection, new IndexValidationParameters(true));
            indexValidator.ValidateAndThrow(request.Index);
            this.Db.Insert(request.Index);

            return new CreateIndexResponse();
        }

        public DestroyIndexResponse Post(DestroyIndex request)
        {
            IndexState state = this.IndexNameValidator.Validate(request.IndexName);
            if (state.IsOffline || state.IsClosing)
            {
                throw new HttpError(HttpStatusCode.BadRequest, "IndexAlreadyOpen", "Index is already offline or is in closing state.");
            }

            this.Db.Delete<Index>("IndexName={0}", request.IndexName);
            Tuple<bool, string> res = this.IndexingService.DeleteIndex(request.IndexName);
            if (!res.Item1)
            {
                throw new Exception(res.Item2);
            }

            return new DestroyIndexResponse();
        }

        #endregion

        #region Methods

        private void UpdateIndexSettingsComponents(Index index, IndexComponent indexComponent)
        {
            IndexState state = this.IndexNameValidator.Validate(index.IndexName);
            if (state.IsOnline || state.IsOpening)
            {
                throw new Exception("Index should be offline before making configuration changes.");
            }

            var indexRecord = this.Db.Single<Index>("IndexName={0}", index.IndexName);
            var validationParameters = new IndexValidationParameters(false);

            switch (indexComponent)
            {
                case IndexComponent.Configuration:
                    validationParameters.ValidateConfiguration = true;
                    indexRecord.Configuration = index.Configuration;
                    break;
                case IndexComponent.Analyzers:
                    validationParameters.ValidateAnalyzers = true;
                    indexRecord.Analyzers = index.Analyzers;
                    break;
                case IndexComponent.Scripts:
                    validationParameters.ValidateScripts = true;
                    indexRecord.Scripts = index.Scripts;
                    break;
                case IndexComponent.Fields:
                    index.Scripts = indexRecord.Scripts;
                    index.Analyzers = indexRecord.Analyzers;
                    validationParameters.ValidateFields = true;
                    indexRecord.Fields = index.Fields;
                    break;
                case IndexComponent.SearchProfiles:
                    index.Fields = indexRecord.Fields;
                    validationParameters.ValidateSearchProfiles = true;
                    indexRecord.SearchProfiles = index.SearchProfiles;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("indexComponent");
            }

            var indexValidator = new IndexValidator(this.FactoryCollection, validationParameters);
            indexValidator.ValidateAndThrow(index);
            this.Db.Update(indexRecord);
        }

        #endregion
    }
}