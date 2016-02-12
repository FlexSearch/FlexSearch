namespace FlexSearch.Tests

open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open FsCheck
open Swensen.Unquote
open System.Collections.Generic
open System.Linq
open Fixie
open FlexSearch.Api.Model
open FlexSearch.Api
open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open System
open System.Collections.Generic
open System.Linq
open System.IO
open System.Reflection
open Swensen.Unquote
open Autofac
open System.Diagnostics
open Microsoft.Extensions.DependencyInjection

/// An object which can be used to simplify integration testing. Think
/// of it as a container for all the integration related stuff.
/// The idea is to reduce the number of asserts used in the code.
type IntegrationHelper = 
    { Index : Index
      IndexService : IIndexService
      SearchService : ISearchService
      DocumentService : IDocumentService
      JobService : IJobService
      QueueService : IQueueService }
    interface IDisposable with
        member this.Dispose() = 
            // Delete index if it exists otherwise we don't care
            this.IndexService.DeleteIndex(this.Index.IndexName) |> ignore

[<AttributeUsage(AttributeTargets.Method)>]
type IgnoreAttribute() = 
    inherit Attribute()

[<AutoOpenAttribute>]
module DataHelpers = 
    let writer = new TextWriterTraceListener(System.Console.Out)
    
    Debug.Listeners.Add(writer) |> ignore
    
    let rootFolder = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
    let testData = """
id,b1,d1,dt1,i1,i2,l1,db1,f1,et1,t1,t2,s1
1,t,20101010,20101010101010,100,1000,1000,1000,1000,aaron,aaron,hewitt,aaaaa
2,T,20111201,20111201111213,100,1000,1000,1000,1000,AAron,AAron,Garner,bbbbb
3,f,20101210,20101210070611,-100,-1000,-1000,-1000,-1000,Fred,Fred,hewitt,ccccc
4,F,20151201,20151201010159,150,1500,1500,1500,1500,fred,fred,Garner,ddddd
5,true,20101101,20101101131312,-150,-1500,-1500,-1500,-1500,aaron,aaron,johnson,eeeee
6,false,20130211,20130211070809,200,2000,2000,2000,2000,airen,airen,johnson,fffff
7,True,20111201,20111201091011,250,2500,2500,2500,2500,aaron,aaron,Garner,ggggg
8,False,20161217,20161217151618,300,3000,3000,3000,3000,ford,ford,johnson,hhhhh
9,TRUE,20111111,20101111111111,-300,-3000,-3000,-3000,-3000,erik,erik,hendrick,iiiii
10,FALSE,20100310,20100310111213,400,4000,4000,4000,4000,aren,aren,Hewitt,jjjjj
"""
    
    /// Basic test index with all field types
    let getTestIndex() = 
        let index = new Index(IndexName = Guid.NewGuid().ToString("N"))
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, 
                                                           AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- Constants.DirectoryType.MemoryMapped
        index.Fields <- [| new Field("b1", Constants.FieldType.Bool)
                           new Field("d1", Constants.FieldType.Date, AllowSort = true)
                           new Field("dt1", Constants.FieldType.DateTime, AllowSort = true)
                           new Field("i1", Constants.FieldType.Int, AllowSort = true)
                           new Field("i2", Constants.FieldType.Int, AllowSort = true)
                           new Field("l1", Constants.FieldType.Long, AllowSort = true)
                           new Field("db1", Constants.FieldType.Double, AllowSort = true)
                           new Field("f1", Constants.FieldType.Float, AllowSort = true)
                           new Field("et1", Constants.FieldType.ExactText, AllowSort = true)
                           new Field("t1", Constants.FieldType.Text)
                           new Field("t2", Constants.FieldType.Text)
                           new Field("s1", Constants.FieldType.Stored) |]
        index
    
    /// Utility method to add data to an index
    let indexTestData (testData : string, index : Index, indexService : IIndexService, 
                       documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        let linesToLoop = lines.Skip(1).ToArray()
        for line in linesToLoop do
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let document = new Document()
            document.Id <- items.[0].Trim()
            document.IndexName <- index.IndexName
            for i in 1..items.Length - 1 do
                document.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            test <@ succeeded <| documentService.AddDocument(document) @>
        test <@ succeeded <| indexService.Refresh(index.IndexName) @>
        test <@ extract <| documentService.TotalDocumentCount(index.IndexName) = lines.Count() - 1 @>
    
    let indexData (testData : string) (ih : IntegrationHelper) = 
        indexTestData (testData, ih.Index, ih.IndexService, ih.DocumentService)
    let container = Main.setupDependencies true <| Settings.T.GetDefault()
    let serverSettings = container.Resolve<Settings.T>()
    let handlerModules = container.Resolve<Dictionary<string, IHttpHandler>>()

[<AutoOpen>]
module TestHelper = 
    // Runs a test against the given result and checks if it succeeded
    let (?) r = test <@ succeeded r @>
    // ----------------------------------------------------------------------------
    // Index service wrappers
    // ----------------------------------------------------------------------------    
    let addIndex (ih : IntegrationHelper) = test <@ succeeded <| ih.IndexService.AddIndex(ih.Index) @>
    let closeIndex (ih : IntegrationHelper) = test <@ succeeded <| ih.IndexService.CloseIndex(ih.Index.IndexName) @>
    let refreshIndex (ih : IntegrationHelper) = test <@ succeeded <| ih.IndexService.Refresh(ih.Index.IndexName) @>
    let commitIndex (ih : IntegrationHelper) = test <@ succeeded <| ih.IndexService.Commit(ih.Index.IndexName) @>
    // ----------------------------------------------------------------------------
    // Document service wrappers
    // ----------------------------------------------------------------------------
    let totalDocs (ih : IntegrationHelper) = extract <| ih.DocumentService.TotalDocumentCount(ih.Index.IndexName)
    let getDoc id (ih : IntegrationHelper) = extract <| ih.DocumentService.GetDocument(ih.Index.IndexName, id)

[<AutoOpen>]
module SearchHelpers = 
    /// General search related helpers
    let getQuery (indexName, queryString) = new SearchQuery(indexName, queryString)
    
    let withName (name) (query : SearchQuery) = 
        query.QueryName <- name
        query
    
    let withColumns (columns : string []) (query : SearchQuery) = 
        query.Columns <- columns
        query
    
    let withPredefinedQuery (profileName : string) (query : SearchQuery) = 
        query.PredefinedQuery <- profileName
        query
    
    let withVariables (variables : (string * string) list) (query : SearchQuery) = 
        query.Variables <- variables.ToDictionary(fst, snd)
        query
    
    let withPredefinedQueryOverride (query : SearchQuery) = 
        query.OverridePredefinedQueryOptions <- true
        query
    
    let withHighlighting (option : HighlightOption) (query : SearchQuery) = 
        query.Highlights <- option
        query
    
    let withOrderBy (column : string) (query : SearchQuery) = 
        query.OrderBy <- column
        query
    
    let withOrderByDesc (column : string) (query : SearchQuery) = 
        query.OrderBy <- column
        query.OrderByDirection <- OrderByDirection.Descending
        query
    
    let withDistinctBy (column : string) (query : SearchQuery) = 
        query.DistinctBy <- column
        query
    
    let withNoScore (query : SearchQuery) = 
        query.ReturnScore <- false
        query
    
    let withCount (count : int) (query : SearchQuery) = 
        query.Count <- count
        query
    
    let withSkip (skip : int) (query : SearchQuery) = 
        query.Skip <- skip
        query
    
    let searchAndExtract (searchService : ISearchService) (query) = 
        let result = searchService.Search(query)
        test <@ succeeded <| result @>
        (extract <| result)
    
    let searchExtractDocList (searchService : ISearchService) (query : SearchQuery) = 
        let result = searchService.Search(query)
        test <@ succeeded <| result @>
        (extract <| result).Documents.ToList()
    
    /// Assertions
    /// Checks if the total number of fields returned by the query matched the expected
    /// count
    let assertFieldCount (expected : int) (result : SearchResults) = 
        test <@ result.Documents.[0].Fields.Count = expected @>
    
    /// Check if the total number of document returned by the query matched the expected
    /// count
    let assertReturnedDocsCount (expected : int) (result : SearchResults) = 
        test <@ result.Documents.Length = expected @>
        test <@ result.RecordsReturned = expected @>
    
    let assertFieldValue (documentNo : int) (fieldName : string) (expectedFieldValue : string) (result : SearchResults) = 
        test <@ result.Documents.Length >= documentNo + 1 @>
        test <@ result.Documents.[documentNo].Fields.[fieldName] = expectedFieldValue @>
    
    /// Check if the total number of available document returned by the query matched the expected
    /// count
    let assertAvailableDocsCount (expected : int) (result : SearchResults) = test <@ result.TotalAvailable = expected @>
    
    /// Check if the field is present in the document returned by the query
    let assertFieldPresent (expected : string) (result : SearchResults) = 
        test <@ result.Documents.[0].Fields.ContainsKey(expected) @>
    
    let getResult (queryString : string) (ih : IntegrationHelper) = 
        getQuery (ih.Index.IndexName, queryString) 
        |> withColumns [| "*" |] 
        |> searchAndExtract ih.SearchService

    /// This is a helper method to combine searching and asserting on returned document count 
    let verifyResultCount (expectedCount : int) (queryString : string) (ih : IntegrationHelper) = 
        getResult queryString ih
        |> assertReturnedDocsCount expectedCount
    
    let storedFieldCannotBeSearched (queryString : string) (ih : IntegrationHelper) = 
        let result = getQuery (ih.Index.IndexName, queryString) |> ih.SearchService.Search
        match result with
        | Fail(f) -> 
            if f.OperationMessage().ErrorCode <> "StoredFieldCannotBeSearched" then 
                failwithf "Expecting Stored field cannot be searched error."
        | _ -> failwithf "Expecting Stored field cannot be searched error."

    let verifyScore (expectedScore : int) (queryString : string) (ih : IntegrationHelper) =
        getResult queryString ih
        |> fun r -> test <@ r.BestScore = float32 expectedScore @>

    let getScore (queryString : string) (ih : IntegrationHelper) =
        getResult queryString ih
        |> fun r -> r.BestScore

