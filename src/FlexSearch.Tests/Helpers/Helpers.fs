namespace FlexSearch.Tests

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

module AutoFixture = 
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
                let fixture = AutoFixture.fixtureCustomization()
                let create (builder : ISpecimenBuilder, typ : Type) = (new SpecimenContext(builder)).Resolve(typ)
                let parameterTypes = methodInfo.GetParameters().Select(fun x -> x.ParameterType)
                let parameterValues = parameterTypes.Select(fun x -> create (fixture, x)).ToArray()
                seq { yield parameterValues }

type SingleInstancePerClassConvention() as self = 
    inherit Convention()   
    let fixtureFactory (typ : Type) = 
        let fixture = AutoFixture.fixtureCustomization()
        (new SpecimenContext(fixture)).Resolve(typ)
    
    do 
        self.Classes.NameEndsWith([| "Tests"; "Test"; "test"; "tests" |]) |> ignore
        self.Methods.Where(fun m -> m.HasOrInherits<IgnoreAttribute>() |> not) |> ignore
        self.ClassExecution.CreateInstancePerClass().UsingFactory(fun typ -> fixtureFactory (typ)) |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore

