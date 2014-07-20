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
namespace FlexSearch.Core

open FlexSearch.Api.Message
open FlexSearch.Core
open System
open System.Linq
open System.Collections.Generic
open System

[<AutoOpen>]
module OperationMessageExtensions = 
    type OperationMessage with
        member this.Clone() = 
            new OperationMessage(DeveloperMessage = this.DeveloperMessage, UserMessage = this.UserMessage, 
                                 ErrorCode = this.ErrorCode)
        member this.WithDeveloperMessage(message : OperationMessage, developerMessage : string) = 
            let message = 
                new OperationMessage(DeveloperMessage = developerMessage, UserMessage = message.UserMessage, 
                                     ErrorCode = message.ErrorCode)
            message
    
    /// <summary>
    /// Append the given key value pair to the developer message
    /// The developer message has a format of key1='value1',key2='value2'
    /// This is specifically done to enable easy error message parsing in the user interface
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="message"></param>
    let Append (key : string, value : string) (message : OperationMessage) = 
        let message' = message.Clone()
        if (message'.DeveloperMessage.EndsWith(",") && String.IsNullOrWhiteSpace(message.DeveloperMessage) <> true) then 
            message'.DeveloperMessage <- sprintf "%s='%s'" key value
        else message'.DeveloperMessage <- sprintf ",%s='%s'" key value
        message'
    
    /// <summary>
    /// Append a key value pair to the error message
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="message"></param>
    let AppendFieldAndValue (key : string, value : string) (message : OperationMessage) = 
        let message' = message.Clone()
        if (message'.DeveloperMessage.EndsWith(",") && String.IsNullOrWhiteSpace(message.DeveloperMessage) <> true) then 
            message'.DeveloperMessage <- sprintf "Field Name='%s';Field Value='%s'" key value
        else message'.DeveloperMessage <- sprintf ",Field Name='%s';Field Value='%s'" key value
        message'

/// Contains all validators used for domain validation  
[<AutoOpen>]
module Validator = 
    /// Validation helper wrapper function
    let Validate propName (v : 'a) = (propName, v)
    
    let NotNullAndEmpty(propName : string, value : string) = 
        if System.String.IsNullOrWhiteSpace(value) <> true then Choice1Of2()
        else Choice2Of2(MessageConstants.PROPERTY_CANNOT_BE_EMPTY |> Append("FieldName", propName))
    
    let RegexMatch (pattern : string) (propName : string, value : string) = 
        let m = System.Text.RegularExpressions.Regex.Match(value, pattern)
        if m.Success then Choice1Of2()
        else 
            Choice2Of2(MessageConstants.REGEX_NOT_MATCHED
                       |> AppendFieldAndValue(propName, value)
                       |> Append("Pattern", pattern))
    
    let NotIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) <> true then Choice1Of2()
        else 
            Choice2Of2(MessageConstants.VALUE_NOT_IN
                       |> AppendFieldAndValue(propName, value)
                       |> Append("Valid Values", String.Join(",", values)))
    
    let OnlyIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) = true then Choice1Of2()
        else 
            Choice2Of2(MessageConstants.VALUE_ONLY_IN
                       |> AppendFieldAndValue(propName, value)
                       |> Append("Valid Values", String.Join(",", values)))
    
    let GreaterThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value >= range then Choice1Of2()
        else 
            Choice2Of2(MessageConstants.GREATER_THAN_EQUAL_TO
                       |> AppendFieldAndValue(propName, value.ToString())
                       |> Append("Range", range.ToString()))
    
    let GreaterThan (range : int) (propName : string, value : int) = 
        if value > range then Choice1Of2(propName, value)
        else 
            Choice2Of2(MessageConstants.GREATER_THAN
                       |> AppendFieldAndValue(propName, value.ToString())
                       |> Append("Range", range.ToString()))
    
    let LessThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value <= range then Choice1Of2()
        else 
            Choice2Of2(MessageConstants.LESS_THAN_EQUAL_TO
                       |> AppendFieldAndValue(propName, value.ToString())
                       |> Append("Range", range.ToString()))
    
    let LessThan (range : int) (propName : string, value : int) = 
        if value < range then Choice1Of2()
        else 
            Choice2Of2(MessageConstants.LESS_THAN
                       |> AppendFieldAndValue(propName, value.ToString())
                       |> Append("Range", range.ToString()))
    
    /// Wrapper around Dictionary lookup. Useful for validation in tokenizers and filters
    let inline KeyExists(key, dict : IDictionary<string, 'T>, errorMessage : OperationMessage) = 
        match dict.TryGetValue(key) with
        | (true, value) -> Choice1Of2(value)
        | _ -> 
            errorMessage.DeveloperMessage <- sprintf "Message='A required property is not defined.',Property='%s'" key
            Choice2Of2(errorMessage)
    
    /// Helper method to check if the passed key exists in the dictionary and if it does then the
    /// specified value is in the enum list
    let inline ValidateIsInList(key, param : IDictionary<string, string>, enumValues : HashSet<string>, 
                                errorMessage : OperationMessage) = 
        maybe { 
            let! value = KeyExists(key, param, errorMessage)
            match enumValues.Contains(value) with
            | true -> return! Choice1Of2(value)
            | _ -> 
                errorMessage.DeveloperMessage <- sprintf 
                                                     "Message='The specified property value is not valid.',Property='%s'" 
                                                     key
                return! Choice2Of2(errorMessage)
        }
    
    /// <summary>
    /// Find a key in a dictionary and parse the resulting value as integer
    /// </summary>
    /// <param name="key"></param>
    /// <param name="param"></param>
    /// <param name="errorMessage"></param>
    let inline ParseValueAsInteger(key, param : IDictionary<string, string>, errorMessage : OperationMessage) = 
        maybe { 
            let! value = KeyExists(key, param, errorMessage)
            match Int32.TryParse(value) with
            | (true, value) -> return! Choice1Of2(value)
            | _ -> 
                errorMessage.DeveloperMessage <- sprintf 
                                                     "Message='The specified property value is not a valid integer.',Property='%s',Value='%s'" 
                                                     key value
                return! Choice2Of2(errorMessage)
        }
    
    type System.String with
        /// <summary>
        /// Validate a given property value
        /// </summary>
        /// <param name="propertyName"></param>
        member this.ValidatePropertyValue(propertyName : string) = 
            maybe { 
                do! (propertyName, this) |> NotNullAndEmpty
                do! (propertyName, this) |> RegexMatch "^[a-z0-9_]*$"
                do! (propertyName, this) 
                    |> NotIn [| Constants.IdField; Constants.LastModifiedField; Constants.TypeField |]
            }
