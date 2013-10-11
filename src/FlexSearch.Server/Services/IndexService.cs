namespace FlexSearch.Server.Services
{
    using System;

    using FlexSearch.Api.Index;
    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using ServiceStack.OrmLite;
    using ServiceStack.ServiceInterface;

    public class IndexService : Service
    {
        #region Public Properties

        public Interface.IIndexService IndexingService { get; set; }

        #endregion

        #region Public Methods and Operators

        public ShowIndexResponse Any(ShowIndex request)
        {
            var indexRecord = this.Db.FirstOrDefault<Index>("IndexName={0}", request.IndexName);
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

        public OpenIndexResponse Post(OpenIndex request)
        {
            this.IndexingService.OpenIndex(request.IndexName);
            return new OpenIndexResponse();
        }

        public CloseIndexResponse Post(CloseIndex request)
        {
            this.IndexingService.CloseIndex(request.IndexName);
            return new CloseIndexResponse();
        }

        public CreateIndexResponse Post(CreateIndex request)
        {
            request.Index.Online = request.OpenIndex;
            this.IndexingService.AddIndex(request.Index);
            return new CreateIndexResponse();
        }

        public UpdateIndexResponse Post(UpdateIndex request)
        {
            this.IndexingService.UpdateIndex(request.Index);
            return new UpdateIndexResponse();
        }

        public DestroyIndexResponse Post(DestroyIndex request)
        {
            this.IndexingService.DeleteIndex(request.IndexName);
            return new DestroyIndexResponse();
        }

        #endregion
    }
}