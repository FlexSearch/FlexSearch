[<AutoOpenAttribute>]
module Helpers

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

[<AttributeUsage(AttributeTargets.Method)>]
type IgnoreAttribute() = inherit Attribute()


[<AutoOpenAttribute>]
module DataHelpers = 
    open Autofac
    open System.Diagnostics
    open Microsoft.Extensions.DependencyInjection

    let writer = new TextWriterTraceListener(System.Console.Out)
    Debug.Listeners.Add(writer) |> ignore

    let rootFolder = AppDomain.CurrentDomain.SetupInformation.ApplicationBase

    /// Basic test index with all field types
    let getTestIndex() = 
        let index = new Index(IndexName = Guid.NewGuid().ToString("N"))
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- Constants.DirectoryType.MemoryMapped
        index.Fields <- [| new Field("b1", Constants.FieldType.Bool)
                           new Field("b2", Constants.FieldType.Bool)
                           new Field("d1", Constants.FieldType.Date)
                           new Field("dt1", Constants.FieldType.DateTime)
                           new Field("db1", Constants.FieldType.Double)
                           new Field("et1", Constants.FieldType.ExactText, AllowSort = true)
                           new Field("h1", Constants.FieldType.Text)
                           new Field("i1", Constants.FieldType.Int)
                           new Field("i2", Constants.FieldType.Int, AllowSort = true)
                           new Field("l1", Constants.FieldType.Long)
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

    let container = Main.setupDependencies true <| Settings.T.GetDefault()
    let serverSettings = container.Resolve<Settings.T>()
    let handlerModules = container.Resolve<Dictionary<string, IHttpHandler>>()
    
    /// <summary>
    /// Basic index configuration
    /// </summary>
    let mockIndexSettings = 
        let index = new Index()
        index.IndexName <- "contact"
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- Constants.DirectoryType.Ram
        index.Fields <- 
         [| new Field("firstname", Constants.FieldType.Text)
            new Field("lastname", Constants.FieldType.Text)
            new Field("email", Constants.FieldType.ExactText)
            new Field("country", Constants.FieldType.Text)
            new Field("ipaddress", Constants.FieldType.ExactText)
            new Field("cvv2", Constants.FieldType.Int)
            new Field("description", Constants.FieldType.Text)
            new Field("fullname", Constants.FieldType.Text) |]
        
        let indexService = container.Resolve<IIndexService>()
        test <@ indexService.AddIndex(index) |> succeeded @>
        index

    /// Autofixture customizations
    let fixtureCustomization() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        // We override Auto fixture's string generation mechanism to return this string which will be
        // used as index name
        fixture.Register<String>(fun _ -> Guid.NewGuid().ToString("N"))
        fixture.Register<Index>(fun _ -> getTestIndex()) |> ignore
        fixture.Inject<IIndexService>(container.Resolve<IIndexService>()) |> ignore
        fixture.Inject<ISearchService>(container.Resolve<ISearchService>()) |> ignore
        fixture.Inject<IDocumentService>(container.Resolve<IDocumentService>()) |> ignore
        fixture.Inject<IJobService>(container.Resolve<IJobService>()) |> ignore
        fixture.Inject<IQueueService>(container.Resolve<IQueueService>()) |> ignore
        fixture.Inject<Dictionary<string, IComputedFunction>>(container.Resolve<Dictionary<string, IComputedFunction>>()) |> ignore
        fixture
     
[<AutoOpenAttribute>]
module ResponseHelpers = 
    let rSucceeded (r : ResponseContext<_>) = 
        match r with
        | SuccessResponse(_) -> true
        | SomeResponse(Ok(_), _, _) -> true
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
        //self.Methods.Where(fun m -> m.HasOrInherits<IgnoreAttribute>() |> not) |> ignore
        self.ClassExecution.CreateInstancePerClass().UsingFactory(fun typ -> fixtureFactory (typ)) |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore


// Runs a test against the given result and checks if it succeeded
let (?) r = test <@ succeeded r @>
