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

[<AutoOpen>]
module Attributes = 
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
