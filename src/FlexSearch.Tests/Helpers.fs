module Helpers

open Fixie
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

type FromInputAttributes() = 
    interface ParameterSource with
        member this.GetParameters(meth : MethodInfo) = 
            meth.GetCustomAttributes<InlineDataAttribute>(true).Select(fun input -> input.Parameters)

type SingleInstancePerClassConvention() as self = 
    inherit Convention()
    do 
        self.Classes.NameEndsWith([| "Tests"; "Test" |]) |> ignore
        self.ClassExecution.CreateInstancePerClass() |> ignore
        self.Parameters.Add<FromInputAttributes>() |> ignore