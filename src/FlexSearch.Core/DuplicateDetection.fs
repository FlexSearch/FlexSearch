// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.DuplicateDetection

open FlexSearch.Api
open FlexSearch.Api.Constants
open FlexSearch.Api.Models
open FlexSearch.Core
open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations.Schema
open System.Diagnostics
open System.IO
open System.Text
open System.Threading.Tasks
open System.Linq
open System.Xml.Linq
open Microsoft.VisualBasic.FileIO
open System.Runtime.Serialization

[<Sealed>]
type Session() = 
    member val Id = Guid.NewGuid().ToString()
    member val SessionId = Guid.NewGuid().ToString()
    member val IndexName = Unchecked.defaultof<string> with get, set
    member val ProfileName = Unchecked.defaultof<string> with get, set
    member val JobStartTime = Unchecked.defaultof<DateTime> with get, set
    member val JobEndTime = Unchecked.defaultof<DateTime> with get, set
    member val SelectionQuery = Unchecked.defaultof<string> with get, set
    member val DisplayFieldName = Unchecked.defaultof<string> with get, set
    member val RecordsReturned = Unchecked.defaultof<int> with get, set
    member val RecordsAvailable = Unchecked.defaultof<int> with get, set
    member val ThreadCount = Unchecked.defaultof<int> with get, set

type TargetRecord() = 
    member val TargetId = 0 with get, set
    member val TargetRecordId = "" with get, set
    member val TargetDisplayName = "" with get, set
    member val TrueDuplicate = false with get, set
    member val Quality = "0" with get, set
    member val TargetScore = 0.0f with get, set

type SourceRecord(sessionId) = 
    member val SessionId = sessionId
    member val SourceId = 0 with get, set
    member val SourceRecordId = "" with get, set
    member val SourceContent = "" with get, set
    member val SourceDisplayName = "" with get, set
    member val SourceStatus = "0" with get, set
    member val TotalDupes = 0 with get, set
    member val TargetRecords = Array.empty<TargetRecord> with get, set

[<AutoOpen>]
module DuplicateDetection = 
    let sessionId = "sessionid"
    let sourceId = "sourceid"
    let targetId = "targetid"
    let recordType = "type"
    let sessionRecordType = "session"
    let sourceRecordType = "source"
    let targetRecordType = "target"
    let sourceRecordId = "sourcerecordid"
    let sourceContent = "sourcecontent"
    let sourceDisplayName = "sourcedisplayname"
    let totalDupesFound = "totaldupesfound"
    let sourceStatus = "sourcestatus"
    let targetRecords = "targetrecords"
    let targetRecordId = "targetrecordid"
    let targetDisplayName = "targetdisplayname"
    let targetScore = "targetscore"
    let quality = "quality"
    let notes = "notes"
    let sessionProperties = "sessionproperties"
    let misc = "misc"
    let formatter = new NewtonsoftJsonFormatter() :> FlexSearch.Core.IFormatter
    
    let schema = 
        let index = new Index(IndexName = "duplicates")
        index.IndexConfiguration <- new IndexConfiguration()
        index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
        index.Active <- true
        index.Fields <- [| new Field(sessionId, Constants.FieldType.ExactText)
                           new Field(recordType, Constants.FieldType.ExactText)
                           // Source record
                           new Field(sourceId, Constants.FieldType.Int, AllowSort = true)
                           new Field(sourceRecordId, Constants.FieldType.ExactText)
                           new Field(sourceContent, Constants.FieldType.Stored)
                           new Field(sourceDisplayName, Constants.FieldType.Stored)
                           new Field(totalDupesFound, Constants.FieldType.Int)
                           new Field(sourceStatus, Constants.FieldType.Int, AllowSort = true)
                           new Field(targetRecords, Constants.FieldType.Stored)
                           new Field(notes, Constants.FieldType.Text)
                           // Session related
                           new Field(sessionProperties, Constants.FieldType.Stored)
                           new Field(misc, Constants.FieldType.Text) |]
        index
    
    let getId() = Guid.NewGuid().ToString()
    
    let writeSessionRecord (session : Session, documentService : IDocumentService) = 
        let doc = new Document(indexName = schema.IndexName, id = session.Id)
        doc.Fields.Add(sessionId, session.SessionId)
        doc.Fields.Add(recordType, sessionRecordType)
        let sessionPropertiesJson = formatter.SerializeToString(session)
        assert (sessionPropertiesJson <> "{}")
        doc.Fields.Add(sessionProperties, sessionPropertiesJson)
        documentService.AddOrUpdateDocument(doc) |> Logger.Log
    
    let writeDuplicates (sourceRecord : SourceRecord) (documentService : IDocumentService) = 
        let sourceDoc = new Document(indexName = schema.IndexName, id = getId())
        sourceDoc.Fields.Add(sessionId, sourceRecord.SessionId)
        sourceDoc.Fields.Add(sourceId, sourceRecord.SourceId.ToString())
        sourceDoc.Fields.Add(sourceRecordId, sourceRecord.SourceRecordId)
        sourceDoc.Fields.Add(sourceContent, sourceRecord.SourceContent)
        sourceDoc.Fields.Add(sourceDisplayName, sourceRecord.SourceDisplayName)
        sourceDoc.Fields.Add(sourceStatus, sourceRecord.SourceStatus)
        sourceDoc.Fields.Add(totalDupesFound, (sourceRecord.TargetRecords |> Seq.length).ToString())
        sourceDoc.Fields.Add(recordType, sourceRecordType)
        sourceDoc.Fields.Add(targetRecords, formatter.SerializeToString(sourceRecord.TargetRecords))
        documentService.AddDocument(sourceDoc)
        |> Logger.Log
        |> ignore

type DuplicateDetectionRequestExtension() = 
    inherit DuplicateDetectionRequest()
    
    /// Helper field to determine if the session uses file based input
    member val FileBasedSession = false with get, set
    member val NextId = new AtomicLong(0L)

    interface IExtraValidation with
        member this.ExtraValidation() = 
            let result = 
                this.IndexName
                |> notBlank "IndexName"
                >>= fun _ -> this.ProfileName |> notBlank "ProfileName"
                >>= fun _ -> 
                    if this.FileName |> isNotBlank then 
                        this.FileBasedSession <- true
                        okUnit
                    else if this.SelectionQuery |> isNotBlank then okUnit
                    else 
                        fail 
                        <| GenericError
                               ("Either one of the field 'FileName or 'SelectionQuery' is required", 
                                new ResizeArray<KeyValuePair<string, string>>())

            match result with
            | Ok() -> true
            | Fail(msg) -> let opmsg = msg.OperationMessage()
                           this.ErrorDescription <- opmsg.Message
                           false
                               

/// Represents the datasources used by a duplicate detection
type DataSource = 
    { AvailableRecords : int
      ReturnedRecords : int
      Records : seq<Dictionary<string, string>> }

//type DuplicateDetectionReportRequest() = 
//    inherit DtoBase()
//    member val SourceFileName = defString with get, set
//    member val ProfileName = defString with get, set
//    member val IndexName = defString with get, set
//    member val QueryString = defString with get, set
//    member val SelectionQuery = defString with get, set
//    member val CutOff = defDouble with get, set
//    override this.Validate() = 
//        this.IndexName |> notBlank "IndexName"
//        >>= fun _ -> this.ProfileName |> notBlank "ProfileName"
//        >>= fun _ -> 
//            if this.SourceFileName |> isNotBlank
//               || this.SelectionQuery |> isNotBlank 
//            then okUnit
//            else fail <| GenericError
//                       ("Either one of the field 'SourceFileName or 'SelectionQuery' is required", 
//                        new ResizeArray<KeyValuePair<string, string>>())
[<Sealed>]
[<Name("POST-/indices/:id/duplicatedetection/:id")>]
type DuplicateDetectionHandler(indexService : IIndexService, documentService : IDocumentService, searchService : ISearchService) = 
    inherit HttpHandlerBase<DuplicateDetectionRequestExtension, Guid>()
    
    do 
        if not <| indexService.IndexExists schema.IndexName then 
            match indexService.AddIndex schema with
            | Ok(_) -> ()
            | Fail(error) -> Logger.Log error
    
    let duplicateRecordCheck (req : DuplicateDetectionRequestExtension, record : Dictionary<string, string>, session : Session, profileQuery : SearchQuery) = 
        let query = 
            new SearchQuery(session.IndexName, String.Empty, SearchProfile = session.ProfileName, 
                            Columns = [| session.DisplayFieldName |], ReturnFlatResult = true, ReturnScore = true)
        // Search for the records that match the current one using the search profile
        match searchService.Search(query, record) with
        | Ok(results) when results.Meta.RecordsReturned > 1 -> 
            !>"Duplicate Found"
            let header = 
                new SourceRecord(session.SessionId, SourceDisplayName = record.[session.DisplayFieldName])
            // Set the content in case of a file based session as there is no other way to
            // retrieve the source record
            if req.FileBasedSession then
                header.SourceContent <- formatter.SerializeToString(record)
            else
                header.SourceRecordId <- record.[MetaFields.IdField]

            let docs = results |> toFlatResults
            // Map each document to a TargetRecord
            header.TargetRecords <- docs.Documents
                                    |> Seq.filter (fun r -> r.[MetaFields.IdField] <> header.SourceRecordId)
                                    |> Seq.filter (fun r -> 
                                        // If duplicate detection is used with DistinctBy then do a second pass filtering to ensure
                                        // that the source record is not picked up. This can happen when source record is not the 
                                        // best match for the search profile
                                        if isNotBlank profileQuery.DistinctBy then
                                            r.[profileQuery.DistinctBy] <> record.[profileQuery.DistinctBy]
                                        else true
                                    )
                                    |> Seq.mapi 
                                           (fun i result -> 
                                           new TargetRecord(TargetId = i + 1, 
                                                            TargetDisplayName = result.[session.DisplayFieldName], 
                                                            TargetScore = float32 result.[MetaFields.Score] 
                                                                          / docs.Meta.BestScore * 100.0f, 
                                                            TargetRecordId = result.[MetaFields.IdField]))
                                    |> Seq.toArray
            // We can have less actual documents returned by the search because of
            // filtering being ran further down the search stream. Therefore it is 
            // worth checking once again that we actually have target records
            if header.TargetRecords
               |> Array.length
               > 0 then 

                // Only assign the id after the record is confirmed to have a duplicate otherwise 
                // the DuplicateCount will be wrong
                header.SourceId <- (int <| req.NextId.Increment())
                // Store the duplicates in Lucene
                documentService |> writeDuplicates header
        | Fail(error) -> Logger.Log error
        | _ -> ()
    
    /// Returns a data source based on a csv file
    let getFileDataSource (request : DuplicateDetectionRequestExtension) = 
        !> "Reading file %s" request.FileName
        try
            let reader = new TextFieldParser(request.FileName)
            reader.TextFieldType <- FieldType.Delimited
            reader.SetDelimiters([| "," |])
            reader.TrimWhiteSpace <- true
            let headers = reader.ReadFields()
        
            let data = 
                seq { 
                    while not reader.EndOfData do
                        yield reader.ReadFields()
                              |> Seq.zip headers
                              |> (fun x -> x.ToDictionary(fst, snd))
                }
                // Get the list of records so that we don't execute the sequence twice
                |> Seq.toList
        
            { AvailableRecords = data.Length
              ReturnedRecords = data.Length
              Records = data }
        with e -> 
            !> "Error parsing CSV: %s" e.Message
            Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Error)
            { AvailableRecords = -1
              ReturnedRecords = -1
              Records = Array.empty }
    
    /// Returns a search query based data source
    let getSearchQueryDataSource (request : DuplicateDetectionRequestExtension) = 
        let mainQuery = 
            new SearchQuery(request.IndexName, request.SelectionQuery, Count = int request.MaxRecordsToScan, 
                            ReturnFlatResult = true, Columns = [| "*" |])
        let result = searchService.Search(mainQuery)
        match result with
        | Ok(result) -> 
            (!>) "Main Query Records Returned:%i" result.Meta.RecordsReturned
            let records = result |> toFlatResults
            
            let d = 
                seq { 
                    for record in records.Documents do
                        yield record
                }
            { AvailableRecords = result.Meta.TotalAvailable
              ReturnedRecords = result.Meta.RecordsReturned
              Records = d }
        | Fail(err) -> 
            Logger.Log(err)
            { AvailableRecords = -1
              ReturnedRecords = -1
              Records = Array.empty }
    
    let getDataSource (req : DuplicateDetectionRequestExtension) = 
        if isNotBlank req.FileName then getFileDataSource (req)
        else getSearchQueryDataSource (req)
    
    let performDuplicateDetection (jobId, indexWriter : IndexWriter.T, req : DuplicateDetectionRequestExtension, profileQuery : SearchQuery) = 
        let session = 
            new Session(IndexName = req.IndexName, ProfileName = req.ProfileName, DisplayFieldName = req.DisplayName, 
                        JobStartTime = DateTime.Now, ThreadCount = req.ThreadCount, 
                        SelectionQuery = if isNotBlank req.SelectionQuery then req.SelectionQuery
                                         else req.FileName)
        
        let dataSource = getDataSource (req)
        // Update session record with search results
        session.RecordsReturned <- dataSource.ReturnedRecords
        session.RecordsAvailable <- dataSource.AvailableRecords
        writeSessionRecord (session, documentService) |> ignore
        // Create Duplicate/Source records
        try 
            let parallelOptions = new ParallelOptions(MaxDegreeOfParallelism = session.ThreadCount)
            Parallel.ForEach(dataSource.Records, parallelOptions, 
                             fun record loopState -> 
                                 match (loopState.IsStopped, req.NextId.Value) with
                                 | (true, _) -> ()
                                 | (_, nextId) when nextId >= int64 req.DuplicatesCount -> loopState.Stop()
                                 | _ -> duplicateRecordCheck (req, record, session, profileQuery))
            |> ignore
        with :? AggregateException as e -> Logger.Log(e, MessageKeyword.Plugin, MessageLevel.Warning)
        // Update session record with job end time
        session.JobEndTime <- DateTime.Now
        writeSessionRecord (session, documentService) |> ignore
    
    let requestProcessor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (jobId, indexWriter, request, profileQuery) = inbox.Receive()
                    performDuplicateDetection (jobId, indexWriter, request, profileQuery) |> ignore
                    return! loop()
                }
            loop())
    
    let processRequest indexName (body : DuplicateDetectionRequestExtension) = 
        maybe { 
            let! writer = indexService.IsIndexOnline(indexName)
            match indexService.IsIndexOnline(indexName) with
            | Ok(writer) -> 
                match writer.Settings.SearchProfiles.TryGetValue(body.ProfileName) with
                | true, profileQuery -> 
                    let jobId = Guid.NewGuid()
                    requestProcessor.Post(jobId, writer, body, snd profileQuery)
                    return jobId
                | _ -> return! fail <| UnknownSearchProfile(indexName, body.ProfileName)
            | Fail(error) -> return! fail error
        }
    
    override __.Process(request, body) = 
        body.Value.IndexName <- request.ResId.Value
        body.Value.ProfileName <- request.SubResId.Value
        SomeResponse(processRequest request.ResId.Value body.Value, Ok, BadRequest)
