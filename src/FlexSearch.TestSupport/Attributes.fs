namespace FlexSearch.TestSupport

open Xunit
open FlexSearch.Api
open FlexSearch.Core
open NSubstitute
open Ploeh.AutoFixture
open Ploeh.AutoFixture.AutoNSubstitute
open Ploeh.AutoFixture.DataAnnotations
open Ploeh.AutoFixture.Xunit
open System
open System.Linq
open Xunit.Extensions
open Xunit.Sdk
open Autofac
open System.Threading
open Microsoft.Owin.Testing

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
        inherit CompositeCustomization(new AutoNSubstituteCustomization(), new SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockDataAttribute() = 
        inherit AutoDataAttribute((new Fixture()).Customize(new DomainCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit inline data attribute
    /// </summary>
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockDataAttribute([<ParamArray>] values : Object []) = 
        inherit CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                          new AutoMockDataAttribute() :> DataAttribute |])

[<AutoOpen>]
module IntegrationTestHelpers = 
    let serverSettings = new ServerSettings()
    let Container = Main.GetContainer(serverSettings, true)
    
    /// <summary>
    /// Baisc index configuration
    /// </summary>
    let MockIndexSettings() = 
        let index = new Index()
        index.IndexName <- "contact"
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add("firstname", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("lastname", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("email", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("country", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("ipaddress", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("cvv2", new FieldProperties(FieldType = FieldType.Int))
        index.Fields.Add("description", new FieldProperties(FieldType = FieldType.Highlight))
        // Computed fields
        index.Fields.Add("fullname", new FieldProperties(FieldType = FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            ("fullname", 
             
             new ScriptProperties("""return fields["firstname"] + " " + fields["lastname"];""", ScriptType.ComputedField))
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "firstname = '' AND lastname = '' AND cvv2 = '116' AND country = ''")
        searchProfileQuery.MissingValueConfiguration.Add("firstname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles.Add("test1", searchProfileQuery)
        index
    
    let GetBasicIndexSettingsForContact() = 
        let index = new Index()
        index.IndexName <- Guid.NewGuid().ToString("N")
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add("gender", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("title", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("givenname", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("middleinitial", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("surname", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("streetaddress", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("city", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("state", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("zipcode", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("country", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("countryfull", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("emailaddress", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("username", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("password", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("cctype", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("ccnumber", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("occupation", new FieldProperties(FieldType = FieldType.Text))
        index.Fields.Add("cvv2", new FieldProperties(FieldType = FieldType.Int))
        index.Fields.Add("nationalid", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("ups", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("company", new FieldProperties(FieldType = FieldType.Stored))
        index.Fields.Add("pounds", new FieldProperties(FieldType = FieldType.Double))
        index.Fields.Add("centimeters", new FieldProperties(FieldType = FieldType.Int))
        index.Fields.Add("guid", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("latitude", new FieldProperties(FieldType = FieldType.Double))
        index.Fields.Add("longitude", new FieldProperties(FieldType = FieldType.Double))
        index.Fields.Add("importdate", new FieldProperties(FieldType = FieldType.Date))
        index.Fields.Add("timestamp", new FieldProperties(FieldType = FieldType.DateTime))
        index.Fields.Add("topic", new FieldProperties(FieldType = FieldType.ExactText))
        index.Fields.Add("abstract", new FieldProperties(FieldType = FieldType.Highlight))
        // Computed fields
        index.Fields.Add("fullname", new FieldProperties(FieldType = FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            ("fullname", 
             new ScriptProperties("""return fields.givenname + " " + fields.surname;""", ScriptType.ComputedField))
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "givenname = '' AND surname = '' AND cvv2 = '1' AND topic = ''")
        searchProfileQuery.MissingValueConfiguration.Add("givenname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles.Add("test1", searchProfileQuery)
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
            let indexDocument = new Document()
            indexDocument.Id <- items.[0].Trim()
            indexDocument.Index <- index.IndexName
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            documentService.AddDocument(index.IndexName, indexDocument.Id, indexDocument.Fields) |> ExpectSuccess
        indexService.Commit(index.IndexName) |> ExpectSuccess
        indexService.Refresh(index.IndexName) |> ExpectSuccess
    
    //Thread.Sleep(200)
    //        let documents = GetSuccessChoice(documentService.GetDocuments(index.IndexName))
    //        Assert.Equal<int>(lines.Count() - 1, (documents.Count))
    /// <summary>
    /// Helper method to generate test index with supplied data
    /// </summary>
    /// <param name="testData"></param>
    let GenerateIndexWithTestData(testData : string, index : Index) = 
        AddTestDataToIndex(index, testData, Container.Resolve<IDocumentService>(), Container.Resolve<IIndexService>())
        index
    
    // Add mock contact index to our test server 
    GenerateIndexWithTestData(TestData.MockTestData, MockIndexSettings()) |> ignore
    
    /// <summary>
    /// Test setup fixture to use with Xunit IUseFixture
    /// </summary>
    type IndexFixture() = 
        member val Index = Unchecked.defaultof<_> with get, set
        
        member this.Setup(testData : string, index : Index) = 
            if this.Index = Unchecked.defaultof<_> then this.Index <- GenerateIndexWithTestData(testData, index)
        
        interface System.IDisposable with
            member this.Dispose() = ExpectSuccess(Container.Resolve<IIndexService>().DeleteIndex(this.Index.IndexName))
    
    let private VerifySearchCount (expected : int) (queryString : string) (indexName : string) = 
        let query = new SearchQuery(indexName, queryString)
        let searchService = Container.Resolve<ISearchService>()
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(expected, result.RecordsReturned)
    
    /// <summary>
    /// Base for creating all Xunit based indexing integration tests
    /// </summary>
    [<AbstractClass>]
    type IndexTestBase(testData : string, ?index0 : Index) = 
        let index = defaultArg index0 (GetBasicIndexSettingsForContact())
        member val Index = Unchecked.defaultof<_> with get, set
        member val IndexName = Unchecked.defaultof<_> with get, set
        member this.VerifySearchCount (expected : int) (queryString : string) = 
            VerifySearchCount expected queryString this.IndexName
        interface IUseFixture<IndexFixture> with
            member this.SetFixture(data) = 
                data.Setup(testData, index)
                this.Index <- data.Index
                this.IndexName <- data.Index.IndexName
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type IntegrationCustomization() = 
        interface ICustomization with
            member this.Customize(fixture : IFixture) = 
                let GetTestServer(indexService : IIndexService, httpFactory : IFlexFactory<IHttpHandler>) = 
                    let testServer = 
                        TestServer.Create(fun app -> 
                            let owinServer = new OwinServer(indexService, httpFactory)
                            owinServer.Configuration(app))
                    testServer
                fixture.Inject<IIndexService>(Container.Resolve<IIndexService>()) |> ignore
                fixture.Inject<ISearchService>(Container.Resolve<ISearchService>()) |> ignore
                fixture.Inject<IDocumentService>(Container.Resolve<IDocumentService>()) |> ignore
                fixture.Inject<IFlexFactory<IHttpHandler>>(Container.Resolve<IFlexFactory<IHttpHandler>>()) |> ignore
                fixture.Register<Index>(fun _ -> GetBasicIndexSettingsForContact()) |> ignore
                fixture.Inject<TestServer>
                    (GetTestServer(Container.Resolve<IIndexService>(), Container.Resolve<IFlexFactory<IHttpHandler>>())) 
                |> ignore
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type IntegrationDomainCustomization() = 
        inherit CompositeCustomization(new IntegrationCustomization(), new SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockIntegrationDataAttribute() = 
        inherit AutoDataAttribute((new Fixture()).Customize(new IntegrationDomainCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit in-line data attribute
    /// </summary>
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockIntegrationDataAttribute([<ParamArray>] values : Object []) = 
        inherit CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                          new AutoMockIntegrationDataAttribute() :> DataAttribute |])
