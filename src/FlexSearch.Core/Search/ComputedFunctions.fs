// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open System
open SearchQueryHelpers

// ----------------------------------------------------------------------------
// Computed Functions
// These functions are executed by the FlexSearch server before it goes to 
// Lucene search.
// ---------------------------------------------------------------------------- 
[<Name("add"); Sealed>]
type AddFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments
            |> getArgumentsAsNumbers __
            >>= (Seq.sum >> printDouble >> Some >> ok)

[<Name("multiply"); Sealed>]
type MultiplyFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments
            |> getArgumentsAsNumbers __
            >>= (Seq.fold (*) 1.0 >> printDouble >> Some >> ok)

[<Name("max"); Sealed>]
type MaxFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkAtLeastNPopulatedArguments 1 __
            >>= fun _ -> 
                arguments 
                |> getArgumentsAsNumbers __
                >>= (Seq.max >> printDouble >> Some >> ok)

[<Name("min"); Sealed>]
type MinFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkAtLeastNPopulatedArguments 1 __
            >>= fun _ -> 
                arguments 
                |> getArgumentsAsNumbers __
                >>= (Seq.min >> printDouble >> Some >> ok)

[<Name("avg"); Sealed>]
type AvgFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkAtLeastNPopulatedArguments 1 __
            >>= fun _ -> 
                arguments 
                |> getArgumentsAsNumbers __
                >>= (Seq.average >> printDouble >> Some >> ok)

[<Name("len"); Sealed>]
type LenFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkItHasNPopulatedArguments 1 __
            >>= fun _ -> arguments.[0].Value.Length 
                         |> (string >> Some >> ok)
                

[<Name("upper"); Sealed>]
type UpperFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkItHasNPopulatedArguments 1 __
            >>= fun _ -> arguments.[0].Value.ToUpper()
                         |> (Some >> ok)

[<Name("lower"); Sealed>]
type LowerFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkItHasNPopulatedArguments 1 __
            >>= fun _ -> arguments.[0].Value.ToLower()
                         |> (Some >> ok)

[<Name("substr"); Sealed>]
type SubstrFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkItHasNPopulatedArguments 3 __
            // Get start parameter
            >>= fun _ -> arguments.[1].Value |> getInt __ 2 
            // Get length parameter
            >+= (arguments.[2].Value |> getInt __ 3)
            // Compute substring
            >>= fun (offset, length) ->
                fun _ -> arguments.[0].Value.Substring(offset, length)
                         |> (Some >> ok)
                |> tryExec __
          
[<Name("startswith"); Sealed>]
type StartsWithFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkItHasNArguments 2 __
            >>= fun _ -> 
                match arguments.[0] with
                | Some(input) -> if arguments.[1].IsSome 
                                 then input.StartsWith(arguments.[1].Value)
                                      |> fun r -> r.ToString().ToLower() 
                                      |> Some
                                 else None
                                 |> ok
                | None -> fail <| ArgumentNotSupplied("startswith", 1)

[<Name("endswith"); Sealed>]
type EndsWithFunc() =
    interface IComputedFunction with
        member __.GetQuery(arguments : ComputedValues) = 
            arguments |> checkItHasNArguments 2 __
            >>= fun _ -> 
                match arguments.[0] with
                | Some(input) -> if arguments.[1].IsSome 
                                 then input.EndsWith(arguments.[1].Value) 
                                      |> fun r -> r.ToString().ToLower() 
                                      |> Some
                                 else None
                                 |> ok
                | None -> fail <| ArgumentNotSupplied("endswith", 1)
                         
//[<Name("endswith"); Sealed>]
//type EndsWithFunc() = 
//    interface IFlexQueryFunction with
//        member __.GetVariableResult(flexField,fieldFunction,_,constant,_, queryFunctionTypes) = 
//            try
//                let typeName() = __.GetType() |> getTypeNameFromAttribute
//
//                // Only LHS values are supported with this function
//                match constant with
//                | Some(_) -> fail <| RhsValueNotSupported(typeName())
//                | None -> 
//                    // Compute the endsWith value
//                    match fieldFunction with
//                    | FieldFunction(_,_,parameters) ->
//                        getFirstNStringParams 1 (typeName()) None queryFunctionTypes (parameters |> Seq.toList)
//                        >>= (Seq.head >> getSomeString >> ok)
//
//                    // Build the "LIKE" query
//                    >>= fun endsWithValue ->
//                            let flexQuery = new FlexWildcardQuery() :> IFlexQuery
//                            flexQuery.GetQuery(flexField, [| "*" + endsWithValue |], None)
//                    
//            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)
//        member __.GetConstantResult(parameters, queryFunctionTypes, source) = 
//            try
//                let typeName() = __.GetType() |> getTypeNameFromAttribute
//
//                if parameters.Length <> 2 then
//                    fail <| NumberOfFunctionParametersMismatch(typeName(), 2, parameters.Length)
//                else
//                    getFirstNStringParams 2 (typeName()) source queryFunctionTypes parameters
//                    >>= fun parsedParams ->
//                        // The first parameter is the full string
//                        // The second parameter is the end of the string
//                        let stringParams = parsedParams |> List.map getSomeString
//                        
//                        // Return 'true' if the first parameter string ends with the second one
//                        // Return 'false' otherwise
//                        stringParams.[0]
//                            .EndsWith(stringParams.[1])
//                            .ToString().ToLower()
//                        |> (Some >> ok)
//            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)
//
//[<Name("startswith"); Sealed>]
//type StartsWithFunc() = 
//    interface IFlexQueryFunction with
//        member __.GetVariableResult(flexField,fieldFunction,_,constant,_, queryFunctionTypes) = 
//            try
//                let typeName() = __.GetType() |> getTypeNameFromAttribute
//
//                // Only LHS values are supported with this function
//                match constant with
//                | Some(_) -> fail <| RhsValueNotSupported(typeName())
//                | None -> 
//                    // Compute the endsWith value
//                    match fieldFunction with
//                    | FieldFunction(_,_,parameters) ->
//                        getFirstNStringParams 1 (typeName()) None queryFunctionTypes (parameters |> Seq.toList)
//                        >>= (Seq.head >> getSomeString >> ok)
//
//                    // Build the "LIKE" query
//                    >>= fun endsWithValue ->
//                            let flexQuery = new FlexWildcardQuery() :> IFlexQuery
//                            flexQuery.GetQuery(flexField, [| endsWithValue + "*" |], None)
//                    
//            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)
//        member __.GetConstantResult(parameters, queryFunctionTypes, source) = 
//            try
//                let typeName() = __.GetType() |> getTypeNameFromAttribute
//
//                if parameters.Length <> 2 then
//                    fail <| NumberOfFunctionParametersMismatch(typeName(), 2, parameters.Length)
//                else
//                    getFirstNStringParams 2 (typeName()) source queryFunctionTypes parameters
//                    >>= fun parsedParams ->
//                        // The first parameter is the full string
//                        // The second parameter is the end of the string
//                        let stringParams = parsedParams |> List.map getSomeString
//                        
//                        // Return 'true' if the first parameter string starts with the second one
//                        // Return 'false' otherwise
//                        stringParams.[0]
//                            .StartsWith(stringParams.[1])
//                            .ToString().ToLower()
//                        |> (Some >> ok)
//            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)