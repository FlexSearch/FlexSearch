// ----------------------------------------------------------------------------
// Validators (Validator.fs)
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open FluentValidation
open FluentValidation.Results
open FlexSearch.Api.Types
open FlexSearch.Core
open System

// ----------------------------------------------------------------------------
// Contains all validators used for domain validation 
// ----------------------------------------------------------------------------
module Validator =
    
    type ValidationResult =
        {
             IsValid        :   bool
             PropertyName   :   string
             ErrorMessage   :   string
             ErrorCode      :   string     
        }

    type IValidate<'T> =
        abstract Validate   :   'T ->  ValidationResult                  

    //let stringNotNullValidator s =

    type PropertyNameValidator =
        inherit AbstractValidator<string>
        new (propertyName: string) =
            base.RuleFor(fun x -> x)
                .NotNull().NotEmpty()
                .Matches("^[a-z0-9]*$")
                .Must(fun x -> String.Equals(x, "id") <> true && String.Equals(x, "lastmodified") <> true && String.Equals(x, "type") <> true)
                .WithName(propertyName)
                .WithMessage(
                    "Property name does not satisfy the required naming convention: not empty, must match regex expression ^[a-z0-9]*$ and cannot be 'id', 'type' and 'lastmodified'.") |> ignore
            {}
            


    type TokenizerValidator =
        inherit AbstractValidator<Tokenizer>
        new () =
            base.RuleFor(fun x -> x.TokenizerName).SetValidator(new PropertyNameValidator("TokenizerName"))
            {}

    type AnalyzerValidator =
        inherit AbstractValidator<AnalyzerProperties>
        new () = 
            base.RuleFor(fun x -> x.Filters)
                .Must(fun x -> x.Count > 0)
                .WithMessage("Atleast one filter should be specified.").Ignore
            {}
