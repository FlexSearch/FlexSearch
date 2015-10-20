module InfrastructureTests

open FlexSearch.Core
open Helpers
open Swensen.Unquote

type ValidatorTests() = 
    let grpName = "grpName"
    let fldName = "fldName"
    
    member __.HasDuplicateSuccessTest() = 
        let sut = [| "a"; "a"; "b" |]
        test <@ Validators.hasDuplicates grpName fldName sut = fail (DuplicateFieldValue(grpName, fldName)) @>
    
    member __.HasDuplicateFailureTest() = 
        let sut = [| "a"; "c"; "b" |]
        test <@ Validators.hasDuplicates grpName fldName sut = ok() @>
    
    member __.GreaterThanSuccessTest() = test <@ Validators.gt fldName 4 5 = ok() @>
    member __.GreaterThanFailureTest() = test <@ Validators.gt fldName 5 4 = fail (GreaterThan(fldName, "5", "4")) @>
    member __.GreaterThanEqualSuccessTest() = test <@ Validators.gte fldName 5 5 = ok() @>
    member __.GreaterThanEqualFailureTest() = 
        test <@ Validators.gte fldName 5 4 = fail (GreaterThanEqual(fldName, "5", "4")) @>
    member __.LessThanSuccessTest() = test <@ Validators.lessThan fldName 5 4 = ok() @>
    member __.LessThanFailureTest() = test <@ Validators.lessThan fldName 4 5 = fail (LessThan(fldName, "4", "5")) @>
    member __.LessThanEqualSuccessTest() = test <@ Validators.lessThanEqual fldName 5 5 = ok() @>
    member __.LessThanEqualFailureTest() = 
        test <@ Validators.lessThanEqual fldName 4 5 = fail (LessThanEqual(fldName, "4", "5")) @>
    member __.NotBlankSuccessTest() = test <@ Validators.notBlank fldName "test" = ok() @>
    member __.NotBlankFailureTest() = test <@ Validators.notBlank fldName "" = fail (NotBlank(fldName)) @>
    
    [<InlineData("test")>]
    [<InlineData("1234")>]
    [<InlineData("_test")>]
    [<Ignore>]
    member __.PropertyValidatorSuccessTests(sut : string) = 
        test <@ Validators.propertyNameValidator fldName sut = ok() @>
    
    [<InlineData("TEST")>]
    [<InlineData("Test")>]
    [<InlineData(MetaFields.IdField)>]
    [<InlineData(MetaFields.LastModifiedField)>]
    [<InlineData("<test>")>]
    [<Ignore>]
    member __.PropertyValidatorFailureTests(sut : string) = 
        test <@ Validators.propertyNameValidator fldName sut = fail (InvalidPropertyName(fldName, sut)) @>

type SeqValidatorTests() = 
    
    let implValid = 
        { new DtoBase() with
              member this.Validate() = ok() }
    
    let implInvaid = 
        { new DtoBase() with
              member this.Validate() = fail (InvalidPropertyName("test", "test")) }
    
    member __.``Test will only succeed when all elements are valid``() = 
        let sut = [| implValid; implValid; implValid |]
        test <@ Validators.seqValidator sut = ok() @>
    
    member __.``Test will fail for even a single invalid item in the seq``() = 
        let sut = [| implValid; implValid; implInvaid |]
        test <@ Validators.seqValidator sut = fail (InvalidPropertyName("test", "test")) @>
