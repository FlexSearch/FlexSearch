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

open FlexSearch
open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Linq

/// Contains all validators used for domain validation  
module Validator = 
    /// Validation helper wrapper function
    let Validate propName (v : 'a) = (propName, v)
    
    let internal NotNullAndEmpty(propName : string, value : string) = 
        if System.String.IsNullOrWhiteSpace(value) <> true then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.PROPERTY_CANNOT_BE_EMPTY, propName))
    
    let internal RegexMatch (pattern : string) (propName : string, value : string) = 
        let m = System.Text.RegularExpressions.Regex.Match(value, pattern)
        if m.Success then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.REGEX_NOT_MATCHED, propName, pattern))
    
    let internal NotIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) <> true then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.VALUE_NOT_IN, propName, (String.Join(",", values))))
    
    let internal OnlyIn (values : string []) (propName : string, value : string) = 
        if values.Contains(value) = true then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.VALUE_ONLY_IN, propName, (String.Join(",", values))))
    
    let internal GreaterThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value >= range then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.GREATER_THAN_EQUAL_TO, propName, range.ToString()))
    
    let internal GreaterThan (range : int) (propName : string, value : int) = 
        if value > range then Choice1Of2(propName, value)
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.GREATER_THAN, propName, range.ToString()))
    
    let internal LessThanOrEqualTo (range : int) (propName : string, value : int) = 
        if value <= range then Choice1Of2()
        else 
            Choice2Of2
                (OperationMessage.WithPropertyName(MessageConstants.LESS_THAN_EQUAL_TO, propName, range.ToString()))
    
    let internal LessThan (range : int) (propName : string, value : int) = 
        if value < range then Choice1Of2()
        else Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.LESS_THAN, propName, range.ToString()))
    
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
    
    

    
