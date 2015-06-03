[<AutoOpenAttribute>]
module Helpers

open Fixie
open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open System
open System.Collections.Generic
open System.Linq
open System.IO
open System.Reflection
open Microsoft.Owin.Testing
open Swensen.Unquote

/// <summary>
/// Represents the lookup name for the plug-in
/// </summary>
[<Sealed>]
[<System.AttributeUsage(System.AttributeTargets.Method)>]
type ExampleAttribute(fileName : string, title : string) = 
    inherit Attribute()
    member this.FileName = fileName
    member this.Title = title

[<AttributeUsage(AttributeTargets.Method)>]
type IgnoreAttribute() = inherit Attribute()

[<AutoOpenAttribute>]
module DataHelpers = 
    open Autofac
    open Autofac.Extras.Attributed
    open System.Diagnostics
    open Client
    

    let writer = new TextWriterTraceListener(System.Console.Out)
    Debug.Listeners.Add(writer) |> ignore

    let rootFolder = AppDomain.CurrentDomain.SetupInformation.ApplicationBase

    /// Basic test index with all field types
    let getTestIndex() = 
        let index = new Index.Dto(IndexName = Guid.NewGuid().ToString("N"))
        index.IndexConfiguration <- new IndexConfiguration.Dto(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Dto.MemoryMapped
        index.Fields <- [| new Field.Dto("b1", FieldType.Dto.Bool)
                           new Field.Dto("b2", FieldType.Dto.Bool)
                           new Field.Dto("d1", FieldType.Dto.Date)
                           new Field.Dto("dt1", FieldType.Dto.DateTime)
                           new Field.Dto("db1", FieldType.Dto.Double)
                           new Field.Dto("et1", FieldType.Dto.ExactText)
                           new Field.Dto("h1", FieldType.Dto.Highlight)
                           new Field.Dto("i1", FieldType.Dto.Int)
                           new Field.Dto("l1", FieldType.Dto.Long)
                           new Field.Dto("t1", FieldType.Dto.Text)
                           new Field.Dto("t2", FieldType.Dto.Text)
                           new Field.Dto("s1", FieldType.Dto.Stored) |]
        index

    /// Utility method to add data to an index
    let indexTestData (testData : string, index : Index.Dto, indexService : IIndexService, 
                       documentService : IDocumentService) = 
        test <@ succeeded <| indexService.AddIndex(index) @>
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        let linesToLoop = lines.Skip(1).ToArray()
        for line in linesToLoop do
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let document = new Document.Dto()
            document.Id <- items.[0].Trim()
            document.IndexName <- index.IndexName
            for i in 1..items.Length - 1 do
                document.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            test <@ succeeded <| documentService.AddDocument(document) @>
        test <@ succeeded <| indexService.Refresh(index.IndexName) @>

    let container = Main.getContainer (ServerSettings.T.GetDefault(), true)
    let serverSettings = container.Resolve<ServerSettings.T>()
    let handlerModules = container.Resolve<IFlexFactory<IHttpHandler>>().GetAllModules()
        
    // Create a single instance of the OWIN server that will be shared across all tests
    let owinServer() = 
        TestServer.Create(fun app -> 
                            let owinServer = 
                                new OwinServer(generateRoutingTable handlerModules, serverSettings.HttpPort)
                            owinServer.Configuration(app))

    /// <summary>
    /// Basic index configuration
    /// </summary>
    let mockIndexSettings = 
        let index = new Index.Dto()
        index.IndexName <- "contact"
        index.IndexConfiguration <- new IndexConfiguration.Dto(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Dto.Ram
        index.Fields <- 
         [| new Field.Dto("firstname", FieldType.Dto.Text)
            new Field.Dto("lastname", FieldType.Dto.Text)
            new Field.Dto("email", FieldType.Dto.ExactText)
            new Field.Dto("country", FieldType.Dto.Text)
            new Field.Dto("ipaddress", FieldType.Dto.ExactText)
            new Field.Dto("cvv2", FieldType.Dto.Int)
            new Field.Dto("description", FieldType.Dto.Highlight)
            new Field.Dto("fullname", FieldType.Dto.Text, ScriptName = "fullname") |]
        index.Scripts <- 
            [| new Script.Dto( ScriptName = "fullname", Source = """return fields.firstname + " " + fields.lastname;""", ScriptType = ScriptType.Dto.ComputedField) |]
        
        let client = new FlexClient(owinServer().HttpClient)
        client.AddIndex(index).Result |> snd =? System.Net.HttpStatusCode.Created

        index

    let createDemoIndex = 
        // Make sure the demo index folder is empty
        let folder = ServerSettings.T.GetDefault().DataFolder + "/country"
        if Directory.Exists(folder) then Directory.Delete(folder, true)
        // Create the demo index
        let client = new FlexClient(owinServer().HttpClient)
        client.SetupDemo().Result |> snd =? System.Net.HttpStatusCode.OK

    let demoIndexData = container.Resolve<DemoIndexService>().DemoData().Value 

    /// Autofixture customizations
    let fixtureCustomization() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        // We override Auto fixture's string generation mechanism to return this string which will be
        // used as index name
        fixture.Register<String>(fun _ -> Guid.NewGuid().ToString("N"))
        fixture.Register<Index.Dto>(fun _ -> getTestIndex()) |> ignore
        fixture.Inject<IIndexService>(container.Resolve<IIndexService>()) |> ignore
        fixture.Inject<ISearchService>(container.Resolve<ISearchService>()) |> ignore
        fixture.Inject<IDocumentService>(container.Resolve<IDocumentService>()) |> ignore
        fixture.Inject<IJobService>(container.Resolve<IJobService>()) |> ignore
        fixture.Inject<IQueueService>(container.Resolve<IQueueService>()) |> ignore
        fixture.Register<FlexClient>(fun _ -> 
            let server = owinServer()
            fixture.Inject<LoggingHandler>(new LoggingHandler(server.Handler))
            new FlexClient(server.HttpClient))
        fixture.Inject<Country list>(demoIndexData)
        fixture
     
[<AutoOpenAttribute>]
module ResponseHelpers = 
    let rSucceeded (r : ResponseContext<_>) = 
        match r with
        | SuccessResponse(_) -> true
        | SomeResponse(Choice1Of2(_), _, _) -> true
        | _ -> false

// ----------------------------------------------------------------------------
// Convention Section for Fixie
// ----------------------------------------------------------------------------
/// Custom attribute to create parameterised test
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type InlineDataAttribute([<System.ParamArrayAttribute>] parameters : obj []) = 
    inherit Attribute()
    member val Parameters = parameters

type InputParameterSource() = 
    interface ParameterSource with
        member __.GetParameters(methodInfo : MethodInfo) = 
            // Check if the method contains inline data attribute. If not then use AutoFixture
            // to generate input value
            let customAttribute = methodInfo.GetCustomAttributes<InlineDataAttribute>(true)
            if customAttribute.Any() then customAttribute.Select(fun input -> input.Parameters)
            else 
                let fixture = fixtureCustomization()
                let create (builder : ISpecimenBuilder, typ : Type) = (new SpecimenContext(builder)).Resolve(typ)
                let parameterTypes = methodInfo.GetParameters().Select(fun x -> x.ParameterType)
                let parameterValues = parameterTypes.Select(fun x -> create (fixture, x)).ToArray()
                seq { yield parameterValues }

type SingleInstancePerClassConvention() as self = 
    inherit Convention()
    
    let fixtureFactory (typ : Type) = 
        let fixture = fixtureCustomization()
        (new SpecimenContext(fixture)).Resolve(typ)
    
    do 
        self.Classes.NameEndsWith([| "Tests"; "Test"; "test"; "tests" |]) |> ignore
        // Temporarily ignore parametric tests because Fixie doesn't handle them in VS 2015
        // Comment out this line if you want to also execute ignored tests
        self.Methods.Where(fun m -> m.HasOrInherits<IgnoreAttribute>() |> not) |> ignore
        self.ClassExecution.CreateInstancePerClass().UsingFactory(fun typ -> fixtureFactory (typ)) |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore
