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

//[<AutoOpen>]
//module QueryFunctionHelpers = 
//    // Convert the parameters to the integers.
//    // We aren't expecting arrays
//    let rec convertToInts typeName source queryFunctionTypes parameters =          
//        match parameters with
//        | [] -> ok []
//        | SingleValue(v) :: rest -> convertToInts typeName source queryFunctionTypes rest
//                                    >>= (fun vs -> Some v :: vs |> ok)
//        | ValueList(_) :: rest -> 
//            fail <| FunctionParamTypeMismatch(
//                typeName,
//                "Single values, functions or fieldnames",
//                "Value list")
//        | Constant.Function(n,ps) :: rest -> 
//            convertToInts typeName source queryFunctionTypes rest >>= (fun vs -> 
//                handleFunctionValue n (ps |> Seq.toList) queryFunctionTypes source
//                >>= (fun v -> v :: vs |> ok))
//        | SearchProfileField(name) :: rest ->
//            match source with
//            | Some(sourceDict) -> match sourceDict.TryGetValue(name) with
//                                    | true, v -> convertToInts typeName source queryFunctionTypes rest
//                                                >>= (fun vs -> Some v :: vs |> ok)
//                                    | _ -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
//            | None -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
//
//    // Convert the single parameter to a string
//    let getSingleStringParam typeName source queryFunctionTypes parameters =
//        match parameters with
//        | [] -> ok <| Some ""
//        | [SingleValue(v)] -> ok <| Some v
//        | [ValueList(_)] -> fail <| FunctionParamTypeMismatch(
//                                typeName,
//                                "Single values, functions or fieldnames",
//                                "Value list")
//        | [Constant.Function(n,ps)] -> 
//            handleFunctionValue n (ps |> Seq.toList) queryFunctionTypes source
//        | [SearchProfileField(name)] ->
//            match source with
//            | Some(sourceDict) -> match sourceDict.TryGetValue(name) with
//                                    | true, v -> ok <| Some v
//                                    | _ -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
//            | None -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
//        | _ -> fail <| NumberOfFunctionParametersMismatch(typeName, 1, parameters.Length)
//
//    let private getStringParam typeName source queryFunctionTypes parameters accumulatedResult =  
//        match parameters with
//        | [] -> fail <| NotEnoughParameters typeName
//        | SingleValue(v) :: rest -> ok (Some v :: accumulatedResult, rest)
//        | ValueList(_) :: rest -> fail <| FunctionParamTypeMismatch(
//                                    typeName,
//                                    "Single values, functions or fieldnames",
//                                    "Value list")
//        | Constant.Function(n,ps) :: rest -> 
//            handleFunctionValue n (ps |> Seq.toList) queryFunctionTypes source
//            >>= fun result -> ok (result :: accumulatedResult,rest)
//        | SearchProfileField(name) :: rest ->
//            match source with
//            | Some(sourceDict) -> match sourceDict.TryGetValue(name) with
//                                    | true, v -> ok (Some v :: accumulatedResult, rest)
//                                    | _ -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
//            | None -> fail <| FieldNamesNotSupportedOutsideSearchProfile(typeName,name)
//
//    // Applies the given folding function to the list of integers, after processing them 
//    // from a list of function parameter types
//    let doForNumericParameters parameters typeName source queryFunctionTypes (optionHandler : string option -> string) doFunction =
//        parameters
//        |> convertToInts typeName source queryFunctionTypes
//        >>= (fun stringParams -> stringParams
//                                 |> Seq.map optionHandler
//                                 |> Seq.map Convert.ToDouble
//                                 // Calculate the sum
//                                 |> doFunction
//                                 // Bring back to string type
//                                 |> (fun x -> x.ToString()) |> ok)
//
//    // Applies the given function to the string value, after processing it from a list of 
//    // of function parameter types
//    let doForSingleParameter parameters typeName source queryFunctionTypes optionHandler doFunction =
//        parameters
//        |> getSingleStringParam typeName source queryFunctionTypes
//        >>= (optionHandler >> doFunction >> ok)
//
//    let getFirstNStringParams paramCount typeName source queryFunctionTypes (parameters : Constant list) =
//        if parameters.Length <> paramCount then 
//            fail <| NumberOfFunctionParametersMismatch(typeName, paramCount, parameters.Length)
//        else
//            let computeParam = getStringParam typeName source queryFunctionTypes
//            
//            parameters
//            // Get the first N parameters
//            |> Seq.take paramCount
//            // Compute the value of each parameter
//            |> Seq.fold 
//                (fun acc param -> acc 
//                                  >>= computeParam [param] 
//                                  >>= (fst >> ok)) 
//                (ok [])
//            // Bring the list back in the original order
//            >>= (List.rev >> ok)
//            
//
//    let getSomeValue (value : 'a option) = match value with
//                                           | Some(v) -> v
//                                           | None -> Unchecked.defaultof<'a>
//
//    let getSomeString str = match str with
//                            | Some(s) -> s
//                            | None -> ""

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
                         

//[<Name("isblank"); Sealed>]
//type IsBlankFunc() =
//    interface IFlexQueryFunction with
//        member __.GetVariableResult(_,_,_,_,_,_) = 
//            fail <| VariableFunctionNotSupported (__.GetType() |> getTypeNameFromAttribute) 
//        member __.GetConstantResult(parameters, queryFunctionTypes, source) = 
//            try
//                let typeName = "isblank"
//
//                if parameters.Length <> 2 then 
//                    fail <| NumberOfFunctionParametersMismatch(typeName, 2, parameters.Length)
//                else
//                    let getDefaultValue() : Result<string option> = 
//                        match parameters.[1] with
//                        | SearchProfileField(fn) -> 
//                            if fn = "IGNORE" then ok None
//                            else getFirstNStringParams 1 typeName source queryFunctionTypes [SearchProfileField(fn)]
//                                 >>= (Seq.head >> ok)
//                        | x -> getFirstNStringParams 1 typeName source queryFunctionTypes [x]
//                               >>= (Seq.head >> ok)
//
//                    // The first parameter of the function must be a field name. If the field name
//                    // cannot be retrieved, then use the default value
//                    match parameters.[0] with
//                    | SearchProfileField(fn) -> match source with 
//                                                | Some (src) -> 
//                                                    match fn |> getFieldFromSource src with
//                                                    | Ok(value) -> ok <| Some value
//                                                    | Fail(_) -> getDefaultValue()
//                                                | None -> fail <| ExpectingSearchProfile("If first parameter of isblank function is a field, then a search profile query is needed")
//                    | x -> fail <| FunctionParamTypeMismatch(typeName, "Search profile field", sprintf "%A" x)
//            with | e -> fail <| FunctionExecutionError(__.GetType() |> getTypeNameFromAttribute, e)

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