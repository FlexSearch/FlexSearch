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
open Xunit.Extensions

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

    let ExpectSuccess (choice : Choice<'T, OperationMessage>)  = 
        match choice with
        | Choice1Of2(success) -> 
            Assert.True(true)    
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
    type InlineAutoMockDataAttribute([<ParamArray>] values: Object[]) = 
        inherit CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                          new AutoMockDataAttribute() :> DataAttribute |])
