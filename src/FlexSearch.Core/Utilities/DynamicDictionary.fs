// ----------------------------------------------------------------------------
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
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.Linq
open System.Dynamic

/// <summary>
/// Dynamic dictionary to allow easier code when using scripting
/// </summary>
[<Sealed>]
type DynamicDictionary (source: Dictionary<string, string>) =
    inherit DynamicObject()

    /// <summary>
    /// The method which is called when we try to access a value from the dictionary
    /// </summary>
    /// <param name="binder"></param>
    /// <param name="result"></param>
    override this.TryGetMember(binder:GetMemberBinder, result) =
        // Converting the property name to lowercase 
        // so that property names become case-insensitive. 
        // Set the result and make sure it never throws
        result <-
            match source.TryGetValue(binder.Name.ToLowerInvariant()) with
            | true, value -> value
            | _ -> ""

        true

    /// <summary>
    /// The method which is called when we try to set an explicit value
    /// </summary>
    /// <param name="binder"></param>
    /// <param name="result"></param>
    override this.TrySetMember(binder:SetMemberBinder, result) = 
        failwithf "Dynamic dictionary does not support explicit setting of variables"

