namespace FlexSearch.TestSupport

open Xunit
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Core.Validator
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

[<AutoOpen>]
module UnitTestAttributes = 
    /// <summary>
    /// Unit test dmain customization
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
module IntegrationTestDataBuilders =
    
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
        index.Fields.Add("abstract", new FieldProperties(FieldType = FieldType.Text))
        // Computed fields
        index.Fields.Add("fullname", new FieldProperties(FieldType = FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            ("fullname", 
             new ScriptProperties("""return fields["givenname"] + " " + fields["surname"];""", ScriptType.ComputedField))
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
    let AddTestDataToIndex(index : Index, testData : string, documentService: IDocumentService, indexService: IIndexService) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        for line in lines.Skip(1) do
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let indexDocument = new Document()
            indexDocument.Id <- items.[0]
            indexDocument.Index <- index.IndexName
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i], items.[i])
            let result = documentService.AddDocument(index.IndexName, indexDocument.Id, indexDocument.Fields)
            ()
        indexService.Commit(index.IndexName) |> ignore
        Thread.Sleep(100)

[<AutoOpen>]
module IntegrationTestAttributes = 
    let serverSettings = new ServerSettings()
    let Container = Main.GetContainer(serverSettings, true)

    /// <summary>
    /// Unit test dmain customization
    /// </summary>
    type IntegrationCustomization() = 
        interface ICustomization with
            member this.Customize(fixture: IFixture) =
                fixture.Inject<IIndexService>(Container.Resolve<IIndexService>()) |> ignore
                fixture.Inject<ISearchService>(Container.Resolve<ISearchService>()) |> ignore
                fixture.Inject<IDocumentService>(Container.Resolve<IDocumentService>()) |> ignore
                fixture.Register<Index>(fun _ -> GetBasicIndexSettingsForContact()) |> ignore

    /// <summary>
    /// Unit test dmain customization
    /// </summary>
    type IntegrationDomainCustomization() = 
        inherit CompositeCustomization(new IntegrationCustomization(), new SupportMutableValueTypesCustomization())

    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockIntegrationDataAttribute() = 
        inherit AutoDataAttribute((new Fixture()).Customize(new IntegrationDomainCustomization()))
    

[<AutoOpen>]
module Attributes = 
    /// <summary>
    /// Custom Xunit attribute to signify test priority
    /// </summary>
    type TestPriorityAttribute(priority : int) = 
        inherit Attribute()
        member this.Priority = priority
    
    /// <summary>
    /// Xunit class command to represent test priority
    /// </summary>
    type PrioritizedFixtureClassCommand() = 
        let inner = new TestClassCommand()
        
        let GetPriority(meth : IMethodInfo) = 
            let priorityAttribute = meth.GetCustomAttributes(typeof<TestPriorityAttribute>).FirstOrDefault()
            if priorityAttribute = null then 0
            else priorityAttribute.GetPropertyValue<int>("Priority")
        
        interface ITestClassCommand with
            member x.ClassFinish() : exn = raise (System.NotImplementedException())
            member x.ClassStart() : exn = raise (System.NotImplementedException())
            member x.EnumerateTestCommands(testMethod : IMethodInfo) : Collections.Generic.IEnumerable<ITestCommand> = 
                raise (System.NotImplementedException())
            member x.IsTestMethod(testMethod : IMethodInfo) : bool = raise (System.NotImplementedException())
            member x.ObjectUnderTest : obj = raise (System.NotImplementedException())
            
            member x.TypeUnderTest 
                with get () = raise (System.NotImplementedException()) : ITypeInfo
                and set (v : ITypeInfo) = raise (System.NotImplementedException()) : unit
            
            member this.ChooseNextTest(testsLeftToRun : Collections.Generic.ICollection<IMethodInfo>) : int = 0
            member x.EnumerateTestMethods() : Collections.Generic.IEnumerable<IMethodInfo> = 
                query { 
                    for m in inner.EnumerateTestMethods() do
                        let p = GetPriority(m)
                        sortBy p
                        select m
                }
    
    /// <summary>
    /// Custom test priority attribute
    /// </summary>
    type PrioritizedFixtureAttribute() = 
        inherit RunWithAttribute(typeof<PrioritizedFixtureAttribute>)
