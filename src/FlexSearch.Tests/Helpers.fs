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
    
    let container = 
        let builder = new ContainerBuilder()
        builder.RegisterModule<AttributedMetadataModule>() |> ignore
        builder.RegisterInstance(Log.logger).As<ILogService>() |> ignore
        builder |> FactoryService.registerInterfaceAssemblies<IFlexQuery>
        builder |> FactoryService.registerSingleFactoryInstance<IFlexQuery>
        builder.Build()
    
    let flexQueryFactory = container.Resolve<IFlexFactory<IFlexQuery>>()
    
    /// Autofixture customizations
    let fixtureCustomization (fixture : Ploeh.AutoFixture.Fixture) = 
        // We override Auto fixture's string generation mechanism to return this string which will be
        // used as index name
        fixture.Inject<string>(Guid.NewGuid().ToString("N")) |> ignore
        let state = State.create (true)
        fixture.Inject<State.T>(state) |> ignore
        fixture.Inject<Index.Dto>(getTestIndex()) |> ignore
        let indexService = new IndexService.Service(state)
        let searchService = new SearchService.Service(state, flexQueryFactory)
        let documentService = new DocumentService.Service(searchService, state)
        fixture.Inject<IIndexService>(indexService) |> ignore
        fixture.Inject<ISearchService>(searchService) |> ignore
        fixture.Inject<IDocumentService>(documentService) |> ignore

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
                let fixture = new Ploeh.AutoFixture.Fixture()
                fixture |> fixtureCustomization
                let create (builder : ISpecimenBuilder, typ : Type) = (new SpecimenContext(builder)).Resolve(typ)
                let parameterTypes = methodInfo.GetParameters().Select(fun x -> x.ParameterType)
                let parameterValues = parameterTypes.Select(fun x -> create (fixture, x)).ToArray()
                seq { yield parameterValues }

type SingleInstancePerClassConvention() as self = 
    inherit Convention()
    do 
        self.Classes.NameEndsWith([| "Tests"; "Test"; "test"; "tests" |]) |> ignore
        self.ClassExecution.CreateInstancePerClass() |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore
