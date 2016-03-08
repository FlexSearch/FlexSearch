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

open FlexLucene.Search
open System

module SearchQueryHelpers =
    let byDefault defaultValue (value : 'T option) =
        match value with
        | Some(v) -> v
        | None -> defaultValue

    let extractDigits (str : string) =
        str |> Seq.where (fun c -> Char.IsDigit c)
            |> String.Concat
            |> fun s -> if String.IsNullOrEmpty s then None 
                        else Some(Int32.Parse s)

    let getNumeric instance argumentNumber (str : string) = 
        match Double.TryParse str with
        | true, value -> ok value
        | _ -> let functionName = instance.GetType() |> getTypeNameFromAttribute
               fail <| ExpectingNumericData(sprintf "\nFunction name: %s\nArgument number: %d\nActual data: %s"
                                                    functionName
                                                    argumentNumber
                                                    str)

    let getInt instance argumentNumber (str : string) = 
        match Int32.TryParse str with
        | true, value -> ok value
        | _ -> let functionName = instance.GetType() |> getTypeNameFromAttribute
               fail <| ExpectingIntegerData(sprintf "\nFunction name: %s\nArgument number: %d\nActual data: %s"
                                                    functionName
                                                    argumentNumber
                                                    str)
            
    let printDouble (d : double) = d.ToString("F")

    let tryExec instance (func : unit -> Result<'T>) =
        try func()
        with e -> let functionName = instance.GetType() |> getTypeNameFromAttribute
                  fail <| FunctionExecutionError(functionName, e)

    
