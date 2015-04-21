[<AutoOpenAttribute>]
module Helpers

open Fixie
open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open System
open System.Collections.Generic
open System.Linq
open System.Reflection
open Swensen.Unquote

[<AutoOpenAttribute>]
module DataHelpers = 
    open Autofac
    open Autofac.Extras.Attributed
    
    /// Basic test index with all field types
    let getTestIndex() = 
        let index = new Index.Dto()
        index.IndexName <- Guid.NewGuid().ToString("N")
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
        // Search profile setup
        let searchProfileQuery = 
            new SearchQuery.Dto(index.IndexName, "t1 = '' AND t2 = '' AND i1 = '1' AND et1 = ''", QueryName = "profile1")
        searchProfileQuery.MissingValueConfiguration.Add("t1", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("i1", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("et1", MissingValueOption.Ignore)
        index.SearchProfiles <- [| searchProfileQuery |]
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
    
    let container = 
        let builder = new ContainerBuilder()
        builder.RegisterModule<AttributedMetadataModule>() |> ignore
        builder.RegisterInstance(Log.logger).As<ILogService>() |> ignore
        builder |> FactoryService.registerInterfaceAssemblies<IFlexQuery>
        builder |> FactoryService.registerSingleFactoryInstance<IFlexQuery>
        builder.Build()
    
    let flexQueryFactory = container.Resolve<IFlexFactory<IFlexQuery>>()
    
    /// Autofixture customizations
    let fixtureCustomization() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        // We override Auto fixture's string generation mechanism to return this string which will be
        // used as index name
        fixture.Inject<string>(Guid.NewGuid().ToString("N")) |> ignore
        fixture.Inject<Index.Dto>(getTestIndex()) |> ignore
        let threadSafeFileWriter = new ThreadSafeFileWiter(new YamlFormatter())
        let analyzerService = new AnalyzerService(threadSafeFileWriter)
        let indexService = new IndexService(threadSafeFileWriter, analyzerService)
        let searchService = new SearchService(new FlexParser(), flexQueryFactory, indexService)
        let documentService = new DocumentService(searchService, indexService)
        let queueService = new QueueService(documentService)
        let jobService = new JobService()
        fixture.Inject<IIndexService>(indexService) |> ignore
        fixture.Inject<ISearchService>(searchService) |> ignore
        fixture.Inject<IDocumentService>(documentService) |> ignore
        fixture.Inject<IJobService>(jobService) |> ignore
        fixture.Inject<IQueueService>(queueService) |> ignore
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
        self.ClassExecution.CreateInstancePerClass().UsingFactory(fun typ -> fixtureFactory (typ)) |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore
