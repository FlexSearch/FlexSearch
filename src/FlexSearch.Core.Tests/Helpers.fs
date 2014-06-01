namespace FlexSearch.Core.Tests
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
module Helpers = 
    let TestChoice choice expectedChoice1 = 
        match choice with
        | Choice1Of2(success) -> 
            if expectedChoice1 then Assert.Equal(1, 1)
            else Assert.Equal(1, 2)
        | Choice2Of2(error) -> 
            if expectedChoice1 then Assert.Equal(1, 2)
            else Assert.Equal(1, 1)
    
    let GetSuccessChoice(choice : Choice<'T, 'U>) = 
        match choice with
        | Choice1Of2(success) -> success
        | Choice2Of2(error) -> failwith "Expected the result to be success but received failure."
    
    let ExpectSuccess(choice : Choice<'T, OperationMessage>) = 
        match choice with
        | Choice1Of2(success) -> Assert.True(true)
        | Choice2Of2(error) -> 
            Assert.True(false, "Expected the result to be success but received failure: " + error.DeveloperMessage)
    
    let ExpectErrorCode (choice : Choice<'T, OperationMessage>) (operationMessage : OperationMessage) = 
        match choice with
        | Choice1Of2(success) -> Assert.True(1 = 2, "Expecting error but received success")
        | Choice2Of2(error) -> Assert.Equal(operationMessage.ErrorCode, error.ErrorCode)
    
    type DomainCustomization() = 
        inherit CompositeCustomization(new AutoNSubstituteCustomization(), new SupportMutableValueTypesCustomization())
    
    type AutoMockDataAttribute() = 
        inherit AutoDataAttribute((new Fixture()).Customize(new DomainCustomization()))
    
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockDataAttribute([<ParamArray>] values : Object []) = 
        inherit CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                          new AutoMockDataAttribute() :> DataAttribute |])
    
    type TestPriorityAttribute(priority : int) = 
        inherit Attribute()
        member this.Priority = priority
    
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
    
    type PrioritizedFixtureAttribute() = 
        inherit RunWithAttribute(typeof<PrioritizedFixtureAttribute>)
