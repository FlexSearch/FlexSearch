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
namespace FlexSearch.Connectors

open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Common
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Utility
open Microsoft.Owin
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.Data.SqlClient
open System.IO
open CsvHelper
open CsvHelper.Configuration

open System

[<Sealed>]
[<Name("POST-/indices/:id/csv")>]
type SqlHandler(queueService : IQueueService, jobService : IJobService, logger : ILogService) = 
    inherit HttpHandlerBase<unit, Guid>()
    let ProcessFile(filePath, indexName) =
        let configuration = new CsvConfiguration()
        configuration.HasHeaderRecord <- true
        
        use textReader = File.OpenText(filePath)
        let parser = new CsvParser(textReader, configuration)

        let mutable exit = false
        while exit <> true do
            let row = parser.Read()
            if row = null then
                exit <- true
            else
                let document = new FlexDocument(indexName, row.[0])
                //document.Fields.Add()
                ()
            

    let bulkRequestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop = 
                async { 
                    let! (path, jobId, isFolder) = inbox.Receive()
                    if isFolder then
                        for file in Directory.EnumerateFiles(path) do
                                
                    return! loop
                }
            loop)

    let ProcessRequest(index : string, context : IOwinContext) = 
        maybe { 
            let filePath = GetValueFromQueryString "filepath" "default" context
            let folderPath = GetValueFromQueryString "folderPath" "default" context
            if filePath = "default" && folderPath = "default" then
                return! Choice2Of2(Errors.MISSING_FIELD_VALUE
                       |> GenerateOperationMessage |> Append("Parameter" , "filePath, folderPath") |> Append("Message", "One of the following two parameters are required: filePath, folderPath."))
            if filePath <> "default" then
                if File.Exists(filePath) then
                    let jobId =Guid.NewGuid()
                    bulkRequestProcessor.Post(filePath, jobId, false)
                    return! Choice1Of2(jobId)
                else
                     return! Choice2Of2(Errors.FILE_NOT_FOUND
                       |> GenerateOperationMessage |> Append("FilePath" , filePath))
            else 
                if Directory.Exists(folderPath) then
                    let jobId =Guid.NewGuid()
                    bulkRequestProcessor.Post(folderPath, jobId, true)
                    return! Choice1Of2(jobId)
                else
                     return! Choice2Of2(Errors.FILE_NOT_FOUND
                       |> GenerateOperationMessage |> Append("FolderPath" , filePath))
           
        }

    override this.Process(index, connectionName, body, context) = 
        ((ProcessRequest(index.Value, context)), Ok, BadRequest)


