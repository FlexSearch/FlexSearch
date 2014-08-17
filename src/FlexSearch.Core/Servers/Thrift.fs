// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core.Server

open FlexSearch.Core
open System
open FlexSearch.Api
open Thrift
open Thrift.Protocol
open Thrift.Server
open Thrift.Transport

module Thrift = 
    /// <summary>
    /// Thrift server
    /// </summary>
    [<Sealed>]
    type Server(port : int, processor : TProcessor, minThread, maxThread) = 
        let mutable server : TThreadPoolServer option = None
        
        do 
            let serverSocket = new TServerSocket(port, 0, false)
            let protocolFactory = new TBinaryProtocol.Factory(true, true)
            let transportFactory = new TFramedTransport.Factory()
            server <- Some
                          (new TThreadPoolServer(processor, serverSocket, transportFactory, transportFactory, 
                                                 protocolFactory, protocolFactory, minThread, maxThread, null))
        
        interface IServer with
            member this.Start() = server.Value.Serve()
            member this.Stop() = server.Value.Stop()
    
    /// <summary>
    /// FlexSearch service implementation for thrift support
    /// </summary>
    [<Sealed>]
    type FlexSearchService(indexService : IIndexService, documentService : IDocumentService, searchService : ISearchService, jobService : IJobService) = 
        
        /// General exception wrapper around Choice as 
        /// Thrift requires exceptions to be thrown in case of errors.
        let ExceptionHelper(result : Choice<'a, Message.OperationMessage>) = 
            match result with
            | Choice1Of2(a) -> a
            | Choice2Of2(error) -> raise (Message.InvalidOperation(Message = error))
        
        interface FlexSearch.Api.Service.FlexSearchService.Iface with
            member this.AddDocument(indexName : string, documentId : string, 
                                    document : Collections.Generic.Dictionary<string, string>) : unit = 
                ExceptionHelper(documentService.AddDocument(indexName, documentId, document))
            member this.AddIndex(index : Index) : unit = ExceptionHelper(indexService.AddIndex(index))
            member this.AddOrUpdateDocument(indexName : string, documentId : string, 
                                            document : Collections.Generic.Dictionary<string, string>) : unit = 
                ExceptionHelper(documentService.AddOrUpdateDocument(indexName, documentId, document))
            member this.CloseIndex(indexName : string) : unit = ExceptionHelper(indexService.CloseIndex(indexName))
            member this.DeleteDocument(indexName : string, documentId : string) : unit = 
                ExceptionHelper(documentService.DeleteDocument(indexName, documentId))
            member this.DeleteIndex(indexName : string) : unit = ExceptionHelper(indexService.DeleteIndex(indexName))
            member this.GetAllIndex() : Collections.Generic.List<Index> = ExceptionHelper(indexService.GetAllIndex())
            member this.GetDocument(indexName : string, documentId : string) : Collections.Generic.Dictionary<string, string> = 
                ExceptionHelper(documentService.GetDocument(indexName, documentId))
            member this.GetDocuments(indexName : string) : Collections.Generic.List<Collections.Generic.Dictionary<string, string>> = 
                ExceptionHelper(documentService.GetDocuments(indexName))
            member this.GetIndex(indexName : string) : Index = ExceptionHelper(indexService.GetIndex(indexName))
            member this.GetIndexStatus(indexName : string) : IndexState = 
                ExceptionHelper(indexService.GetIndexStatus(indexName))
            member this.GetJob(jobId : string) : Job = ExceptionHelper(jobService.GetJob(jobId))
            member this.IndexExists(indexName : string) : bool = indexService.IndexExists(indexName)
            member this.OpenIndex(indexName : string) : unit = ExceptionHelper(indexService.OpenIndex(indexName))
            member this.Search(query : SearchQuery) : SearchResults = ExceptionHelper(searchService.Search(query))
            member this.SearchWithFlatResults(query : SearchQuery) : Collections.Generic.List<Collections.Generic.Dictionary<string, string>> = 
                failwith "Not implemented yet"
            member this.UpdateIndex(index : Index) : unit = ExceptionHelper(indexService.UpdateIndex(index))
