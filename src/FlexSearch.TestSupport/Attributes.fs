namespace FlexSearch.TestSupport

open Autofac
open FlexSearch.Api
open FlexSearch.Client
open FlexSearch.Core
open Microsoft.Owin.Testing
open NSubstitute
open Ploeh.AutoFixture.AutoNSubstitute
open Ploeh.AutoFixture.DataAnnotations
open Ploeh.AutoFixture.Xunit
open System
open System.Linq
open System.Net.Http
open System.Threading
open Xunit
open Xunit.Extensions
open Xunit.Sdk

[<AutoOpen>]
module UnitTestAttributes = 
    /// <summary>
    /// Represents the lookup name for the plug-in
    /// </summary>
    [<Sealed>]
    [<System.AttributeUsage(System.AttributeTargets.Method)>]
    type ExampleAttribute(fileName : string, title : string) = 
        inherit Attribute()
        member this.FileName = fileName
        member this.Title = title
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type DomainCustomization() = 
        inherit Ploeh.AutoFixture.CompositeCustomization(new AutoNSubstituteCustomization(), 
                                                         new Ploeh.AutoFixture.SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockDataAttribute() = 
        inherit Ploeh.AutoFixture.Xunit.AutoDataAttribute((new Ploeh.AutoFixture.Fixture())
            .Customize(new DomainCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit inline data attribute
    /// </summary>
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockDataAttribute([<ParamArray>] values : Object []) = 
        inherit Ploeh.AutoFixture.Xunit.CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                                                  new AutoMockDataAttribute() :> DataAttribute |])

[<AutoOpen>]
module IntegrationTestHelpers = 
    open Ploeh.AutoFixture
    open System.Net
    
    let serverSettings = ServerSettings.GetDefault()
    let logger = FlexSearch.Logging.LogService.GetLogger(true)
    let Container = Main.GetContainer(serverSettings, logger, true)
    
    // Start demo index
    let startDemoIndex = 
        match Container.Resolve<FlexSearch.Core.Services.DemoIndexService>().Setup() with
        | Choice1Of2(s) -> ()
        | Choice2Of2(e) -> failwithf "Unable to start demo index: %A" e
    
    /// <summary>
    /// Basic test index with all field types
    /// </summary>
    let GetTestIndex() = 
        let index = new Index()
        index.IndexName <- Guid.NewGuid().ToString("N")
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add(new Field("b1", FieldType.Bool))
        index.Fields.Add(new Field("b2", FieldType.Bool))
        index.Fields.Add(new Field("d1", FieldType.Date))
        index.Fields.Add(new Field("dt1", FieldType.DateTime))
        index.Fields.Add(new Field("db1", FieldType.Double))
        index.Fields.Add(new Field("et1", FieldType.ExactText))
        index.Fields.Add(new Field("h1", FieldType.Highlight))
        index.Fields.Add(new Field("i1", FieldType.Int))
        index.Fields.Add(new Field("l1", FieldType.Long))
        index.Fields.Add(new Field("t1", FieldType.Text))
        index.Fields.Add(new Field("t2", FieldType.Text))
        index.Fields.Add(new Field("s1", FieldType.Stored))
        // Search profile setup
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "t1 = '' AND t2 = '' AND i1 = '1' AND et1 = ''", QueryName = "profile1")
        searchProfileQuery.MissingValueConfiguration.Add("t1", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("i1", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("et1", MissingValueOption.Ignore)
        index.SearchProfiles.Add(searchProfileQuery)
        index
    
    /// <summary>
    /// Utility method to add data to an index
    /// </summary>
    /// <param name="indexService"></param>
    /// <param name="index"></param>
    /// <param name="testData"></param>
    let IndexData (testData : string) (index : Index) = 
        let indexService = Container.Resolve<IIndexService>()
        let documentService = Container.Resolve<IDocumentService>()
        indexService.AddIndex(index) |> ExpectSuccess
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        for line in lines.Skip(1) do
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let indexDocument = new FlexDocument()
            indexDocument.Id <- items.[0].Trim()
            indexDocument.IndexName <- index.IndexName
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            documentService.AddDocument(indexDocument) |> ExpectSuccess
        indexService.Commit(index.IndexName) |> ExpectSuccess
        indexService.Refresh(index.IndexName) |> ExpectSuccess
        (index, index.IndexName)
    
    /// <summary>
    /// Test setup fixture to use with X-unit IUseFixture
    /// </summary>
    type IndexFixture() = 
        
        [<VolatileFieldAttribute>]
        let mutable index = Unchecked.defaultof<Index>
        
        let mutable counter = 0
        let l = new Object()
        
        member this.Setup(testData : string) = 
            // Implement double locking
            if index = Unchecked.defaultof<Index> then 
                lock l (fun _ -> 
                    if index = Unchecked.defaultof<Index> then 
                        let i0 = GetTestIndex()
                        i0
                        |> IndexData(testData)
                        |> ignore
                        if counter = 1 then failwithf "Locking failed. Data already initialized."
                        counter <- counter + 1
                        index <- i0)
            index
        
        interface System.IDisposable with
            member this.Dispose() = 
                if index <> Unchecked.defaultof<Index> then 
                    Container.Resolve<IIndexService>().DeleteIndex(index.IndexName) |> ignore
    
    /// <summary>
    /// Base for creating all X-unit based indexing integration tests
    /// </summary>
    [<AbstractClass>]
    type IndexTestBase() = 
        let mutable index = Unchecked.defaultof<Index>
        let mutable indexName = Unchecked.defaultof<string>
        
        let CheckErrorCode (operationMessage : OperationMessage) (choice : Choice<'T, OperationMessage>) = 
            match choice with
            | Choice1Of2(success) -> Assert.True(1 = 2, sprintf "Expecting error but received success: %A" success)
            | Choice2Of2(error) -> 
                Assert.True
                    (operationMessage.ErrorCode = error.ErrorCode, 
                     sprintf "Expecting error with code:%s but received error with code: %s" operationMessage.ErrorCode 
                         error.ErrorCode)
        
        member this.SearchService = Container.Resolve<ISearchService>()
        member this.IndexService = Container.Resolve<IIndexService>()
        member this.IndexName = indexName
        member this.Index = index
        member val TestData = Unchecked.defaultof<string> with get, set
        
        // Helper assertions
        /// <summary>
        /// Returns success if Choice1 is present
        /// </summary>
        /// <param name="choice"></param>
        member this.ExpectSuccess(choice : Choice<'T, OperationMessage>) = 
            match choice with
            | Choice1Of2(success) -> success
            | Choice2Of2(error) -> failwithf "Expected the result to be success but received failure: %A" error
        
        // Helper assertions
        /// <summary>
        /// Returns failure if Choice2 is present
        /// </summary>
        /// <param name="choice"></param>
        member this.ExpectFailure(choice : Choice<'T, OperationMessage>) = 
            match choice with
            | Choice1Of2(success) -> failwithf "Expected the result to be failure but received success: %A" success
            | Choice2Of2(error) -> error
        
        member this.ExpectErrorCode (messageCode : string) (operationMessage : OperationMessage) = 
            let om = messageCode |> GenerateOperationMessage
            Assert.True
                (operationMessage.ErrorCode = om.ErrorCode, 
                 sprintf "Expecting error with code:%s but received error with code: %s" om.ErrorCode 
                     operationMessage.ErrorCode)
            sprintf "%A" operationMessage
        
        // Search query related builders
        member this.Query(queryString : string) = new SearchQuery(indexName, queryString)
        
        member this.WithNoScore(query : SearchQuery) = 
            query.ReturnScore <- false
            query
        
        member this.AddColumns (columns : string array) (query : SearchQuery) = 
            columns |> Array.iter (fun x -> query.Columns.Add(x))
            query
        
        member this.WithSearchHighlighting (option : HighlightOption) (query : SearchQuery) = 
            query.Highlights <- option
            query
        
        member this.WithSearchProfile (profileName : string) (query : SearchQuery) = 
            query.SearchProfile <- profileName
            query
        
        member this.WithCount (count : int) (query : SearchQuery) = 
            query.Count <- count
            query
        
        member this.OrderBy (orderBy : string) (query : SearchQuery) = 
            query.OrderBy <- orderBy
            query
        
        member this.SearchResults(query : SearchQuery) = this.SearchService.Search(query)
        member this.SearchFlatResults(query : SearchQuery) = this.SearchService.SearchAsDictionarySeq(query)
        
        member this.Search(query : SearchQuery) = 
            let result = GetSuccessChoice(this.SearchService.Search(query))
            result
        
        member this.Search(queryString : string) = 
            let query = new SearchQuery(indexName, queryString)
            this.Search(query)
        
        member this.VerifySearchCount(queryString : string, expected : int) = 
            let result = this.Search(queryString)
            Assert.Equal<int>(expected, result.RecordsReturned)
        
        interface IUseFixture<IndexFixture> with
            member this.SetFixture(data) = 
                index <- data.Setup(this.TestData)
                indexName <- index.IndexName
    
    /// <summary>
    /// Basic index configuration
    /// </summary>
    let MockIndexSettings() = 
        let index = new Index()
        index.IndexName <- "contact"
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add(new Field("firstname", FieldType.Text))
        index.Fields.Add(new Field("lastname", FieldType.Text))
        index.Fields.Add(new Field("email", FieldType.ExactText))
        index.Fields.Add(new Field("country", FieldType.Text))
        index.Fields.Add(new Field("ipaddress", FieldType.ExactText))
        index.Fields.Add(new Field("cvv2", FieldType.Int))
        index.Fields.Add(new Field("description", FieldType.Highlight))
        // Computed fields
        index.Fields.Add(new Field("fullname", FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            (new Script("fullname", """return fields.firstname + " " + fields.lastname;""", ScriptType.ComputedField))
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "firstname = '' AND lastname = '' AND cvv2 = '116' AND country = ''", 
                            QueryName = "test1")
        searchProfileQuery.MissingValueConfiguration.Add("firstname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles.Add(searchProfileQuery)
        index
    
    let GetBasicIndexSettingsForContact() = 
        let index = new Index()
        index.IndexName <- Guid.NewGuid().ToString("N")
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add(new Field("gender", FieldType.ExactText))
        index.Fields.Add(new Field("title", FieldType.ExactText))
        index.Fields.Add(new Field("givenname", FieldType.Text))
        index.Fields.Add(new Field("middleinitial", FieldType.Text))
        index.Fields.Add(new Field("surname", FieldType.Text))
        index.Fields.Add(new Field("streetaddress", FieldType.Text))
        index.Fields.Add(new Field("city", FieldType.ExactText))
        index.Fields.Add(new Field("state", FieldType.ExactText))
        index.Fields.Add(new Field("zipcode", FieldType.ExactText))
        index.Fields.Add(new Field("country", FieldType.ExactText))
        index.Fields.Add(new Field("countryfull", FieldType.ExactText))
        index.Fields.Add(new Field("emailaddress", FieldType.ExactText))
        index.Fields.Add(new Field("username", FieldType.ExactText))
        index.Fields.Add(new Field("password", FieldType.ExactText))
        index.Fields.Add(new Field("cctype", FieldType.ExactText))
        index.Fields.Add(new Field("ccnumber", FieldType.ExactText))
        index.Fields.Add(new Field("occupation", FieldType.Text))
        index.Fields.Add(new Field("cvv2", FieldType.Int))
        index.Fields.Add(new Field("nationalid", FieldType.ExactText))
        index.Fields.Add(new Field("ups", FieldType.ExactText))
        index.Fields.Add(new Field("company", FieldType.Stored))
        index.Fields.Add(new Field("pounds", FieldType.Double))
        index.Fields.Add(new Field("centimeters", FieldType.Int))
        index.Fields.Add(new Field("guid", FieldType.ExactText))
        index.Fields.Add(new Field("latitude", FieldType.Double))
        index.Fields.Add(new Field("longitude", FieldType.Double))
        index.Fields.Add(new Field("importdate", FieldType.Date))
        index.Fields.Add(new Field("timestamp", FieldType.DateTime))
        index.Fields.Add(new Field("topic", FieldType.ExactText))
        index.Fields.Add(new Field("abstract", FieldType.Highlight))
        // Computed fields
        index.Fields.Add(new Field("fullname", FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            (new Script("fullname", """return fields.givenname + " " + fields.surname;""", ScriptType.ComputedField))
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "givenname = '' AND surname = '' AND cvv2 = '1' AND topic = ''", 
                            QueryName = "test1")
        searchProfileQuery.MissingValueConfiguration.Add("givenname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles.Add(searchProfileQuery)
        index
    
    /// <summary>
    /// Utility method to add data to an index
    /// </summary>
    /// <param name="indexService"></param>
    /// <param name="index"></param>
    /// <param name="testData"></param>
    let AddTestDataToIndex(index : Index, testData : string, documentService : IDocumentService, 
                           indexService : IIndexService) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        for line in lines.Skip(1) do
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let indexDocument = new FlexDocument()
            indexDocument.Id <- items.[0].Trim()
            indexDocument.IndexName <- index.IndexName
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            documentService.AddDocument(indexDocument) |> ExpectSuccess
        indexService.Commit(index.IndexName) |> ExpectSuccess
        indexService.Refresh(index.IndexName) |> ExpectSuccess
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type IntegrationCustomization(useCountryIndex) = 
        interface Ploeh.AutoFixture.ICustomization with
            member this.Customize(fixture : Ploeh.AutoFixture.IFixture) = 
                let GetTestServer(indexService : IIndexService, httpFactory : IFlexFactory<IHttpResource>) = 
                    let testServer = 
                        TestServer.Create(fun app -> 
                            let owinServer = 
                                new OwinServer(indexService, httpFactory, FlexSearch.Logging.LogService.GetLogger(true))
                            owinServer.Configuration(app))
                    testServer
                // We override Auto fixture's string generation mechanism to return this string which will be
                // used as index name
                fixture.Inject<string>(Guid.NewGuid().ToString("N")) |> ignore
                fixture.Inject<IIndexService>(Container.Resolve<IIndexService>()) |> ignore
                fixture.Inject<ISearchService>(Container.Resolve<ISearchService>()) |> ignore
                fixture.Inject<IDocumentService>(Container.Resolve<IDocumentService>()) |> ignore
                fixture.Inject<IFlexFactory<IHttpResource>>(Container.Resolve<IFlexFactory<IHttpResource>>()) |> ignore
                let index = 
                    if useCountryIndex then 
                        Container.Resolve<FlexSearch.Core.Services.DemoIndexService>().GetDemoIndex()
                    else MockIndexSettings()
                index.IndexName <- Guid.NewGuid().ToString("N")
                fixture.Register<Index>(fun _ -> index) |> ignore
                let testServer = 
                    GetTestServer(Container.Resolve<IIndexService>(), Container.Resolve<IFlexFactory<IHttpResource>>())
                let loggingHandler = new LoggingHandler(testServer.Handler)
                let httpClient = new HttpClient(loggingHandler)
                let flexClient = new FlexClient(httpClient)
                fixture.Inject<TestServer>(testServer) |> ignore
                fixture.Inject<LoggingHandler>(loggingHandler)
                fixture.Inject<IFlexClient>(flexClient) |> ignore
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type IntegrationDomainCustomization() = 
        inherit Ploeh.AutoFixture.CompositeCustomization(new IntegrationCustomization(false), 
                                                         new Ploeh.AutoFixture.SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type RestCustomization() = 
        inherit Ploeh.AutoFixture.CompositeCustomization(new IntegrationCustomization(true), 
                                                         new Ploeh.AutoFixture.SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type RestDataAttribute() = 
        inherit Ploeh.AutoFixture.Xunit.AutoDataAttribute((new Ploeh.AutoFixture.Fixture())
            .Customize(new RestCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockIntegrationDataAttribute() = 
        inherit Ploeh.AutoFixture.Xunit.AutoDataAttribute((new Ploeh.AutoFixture.Fixture())
            .Customize(new IntegrationDomainCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit in-line data attribute
    /// </summary>
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockIntegrationDataAttribute([<ParamArray>] values : Object []) = 
        inherit Ploeh.AutoFixture.Xunit.CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                                                  
                                                                  new AutoMockIntegrationDataAttribute() :> DataAttribute |])
