[<AutoOpenAttribute>]
module Helpers

open Fixie
open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open System
open System.Linq
open System.Reflection

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
            if customAttribute.Any() then 
                customAttribute.Select(fun input -> input.Parameters)
            else
                let fixture = new Ploeh.AutoFixture.Fixture()
                let create (builder : ISpecimenBuilder, typ : Type) = (new SpecimenContext(builder)).Resolve(typ)
                let parameterTypes = methodInfo.GetParameters().Select(fun x -> x.ParameterType)
                let parameterValues = parameterTypes.Select(fun x -> create (fixture, x)).ToArray()
                seq { yield parameterValues }
            

type SingleInstancePerClassConvention() as self = 
    inherit Convention()
    do 
        self.Classes.NameEndsWith([| "Tests"; "Test" |]) |> ignore
        self.ClassExecution.CreateInstancePerClass() |> ignore
        self.Parameters.Add<InputParameterSource>() |> ignore
        
type SingleInstancePerClassConvention1() as self = 
    inherit Convention()
    do self.Methods.Where(fun x -> x.IsStatic && x.IsPublic) |> ignore
