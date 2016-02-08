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

module SearchQueryHelpers =
    let getPopulatedArguments (arguments : ComputedValues) =
        arguments
        |> Array.where (fun a -> a.IsSome)
        |> Array.map (fun a -> a.Value)

    let checkItHasNArguments n (instance : 'T) (arguments : ComputedValues) =
        if arguments.Length = n then okUnit
        else 
            let functionName = instance.GetType() |> getTypeNameFromAttribute
            fail <| NumberOfFunctionParametersMismatch(functionName, n, arguments.Length)

    let checkItHasNPopulatedArguments n (instance : 'T) (arguments : ComputedValues) =
        if arguments.Length <> n 
        then
            let functionName = instance.GetType() |> getTypeNameFromAttribute 
            fail <| NumberOfFunctionParametersMismatch(functionName, n, arguments.Length)
        else arguments
             |> Seq.zip [1..arguments.Length]
             >>>= fun (idx, arg) -> if arg.IsSome then okUnit
                                    else let functionName = instance.GetType() |> getTypeNameFromAttribute
                                         fail <| ArgumentNotSupplied(functionName, idx)

    let checkAtLeastNPopulatedArguments n (instance : 'T) (arguments : ComputedValues) =
        arguments
        |> getPopulatedArguments
        |> Seq.length
        |> fun count -> if count >= n then okUnit 
                        else
                            let functionName = instance.GetType() |> getTypeNameFromAttribute 
                            fail <| ExpectedAtLeastNParamsMismatch(functionName, n, count)

    let byDefault defaultValue (value : 'T option) =
        match value with
        | Some(v) -> v
        | None -> defaultValue

    let extractDigits (str : string) =
        str |> Seq.where (fun c -> Char.IsDigit c)
            |> String.Concat
            |> fun s -> if String.IsNullOrEmpty s then None 
                        else Some(Int32.Parse s)

    

