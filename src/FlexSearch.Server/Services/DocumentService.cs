// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IndexService.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace FlexSearch.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using FlexSearch.Api;
    using FlexSearch.Api.Document;
    using FlexSearch.Api.Search;
    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using ServiceStack.ServiceHost;
    using ServiceStack.ServiceInterface;
    using ServiceStack.ServiceInterface.ServiceModel;



    public class DocumentService : Service
    {
        #region Public Properties

        public Interface.IIndexService IndexingService { get; set; }

        #endregion

        #region Public Methods and Operators

        public DestroyDocumentResponse Delete(DestroyDocument request)
        {
            return this.ProcessDeleteDocumentRequest(request);
        }

        public ShowDocumentResponse Get(ShowDocument request)
        {
            return this.ProcessGetDocumentRequest(request);
        }

        public CreateDocumentResponse Post(CreateDocument request)
        {
            return this.ProcessCreateDocumentRequest(request);
        }

        public UpdateDocumentResponse Post(UpdateDocument request)
        {
            return this.ProcessUpdateDocumentRequest(request);
        }

        public DestroyDocumentResponse Post(DestroyDocument request)
        {
            return this.ProcessDeleteDocumentRequest(request);
        }

        public ShowDocumentResponse Post(ShowDocument request)
        {
            return this.ProcessGetDocumentRequest(request);
        }

        public CreateDocumentResponse Put(CreateDocument request)
        {
            return this.ProcessCreateDocumentRequest(request);
        }

        #endregion

        #region Methods

        private CreateDocumentResponse ProcessCreateDocumentRequest(CreateDocument request)
        {
            Tuple<bool, string> result =
                this.IndexingService.PerformCommand(request.IndexName, IndexCommand.NewCreate(request.Id, request.Fields));
            return new CreateDocumentResponse { Message = result.Item2 };
        }

        private DestroyDocumentResponse ProcessDeleteDocumentRequest(DestroyDocument request)
        {
            Tuple<bool, string> result =
                this.IndexingService.PerformCommand(request.IndexName, IndexCommand.NewDelete(request.Id));
            return new DestroyDocumentResponse { Message = result.Item2 };
        }

        private ShowDocumentResponse ProcessGetDocumentRequest(ShowDocument request)
        {
            var searchQuery = new SearchQuery();
            searchQuery.Columns = new[] { "*" };
            searchQuery.IndexName = request.IndexName;
            searchQuery.Count = 1;
            searchQuery.Query = new SearchFilter
                {
                    FilterType = FilterType.And,
                    SubFilters = null,
                    Conditions =
                        new[]
                            {
                                new SearchCondition
                                    {
                                        FieldName = "id",
                                        Operator = "term_match",
                                        MissingValueOption = MissingValueOption.ThrowError,
                                        Params = null,
                                        Values = new[] { request.Id }
                                    }
                            }
                };
            
            var result = this.IndexingService.PerformQuery(request.IndexName, IndexQuery.NewSearchQuery(searchQuery));
            return new ShowDocumentResponse { Document = result.Documents.First() };
        }

        private UpdateDocumentResponse ProcessUpdateDocumentRequest(UpdateDocument request)
        {
            Tuple<bool, string> result =
                this.IndexingService.PerformCommand(request.Id, IndexCommand.NewUpdate(request.Id, request.Fields));
            return new UpdateDocumentResponse { Message = result.Item2 };
        }

        #endregion
    }
}