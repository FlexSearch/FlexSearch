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

open System
open System.ComponentModel.Composition

// ----------------------------------------------------------------------------
// Contains custom attributes required
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Attributes = 
    /// <summary>
    /// Represents the lookup name for the plug-in
    /// </summary>
    [<MetadataAttribute>]
    [<Sealed>]
    type NameAttribute(name : string) = 
        inherit Attribute()
        member this.Name = name
    
    /// <summary>
    /// Represents the display name for the plug-in
    /// </summary>
    [<MetadataAttribute>]
    [<Sealed>]
    type DisplayAttribute(displayName : string) = 
        inherit Attribute()
        member this.DisplayName = displayName
    
    /// <summary>
    /// Represents the description for the plug-in
    /// </summary>
    [<MetadataAttribute>]
    [<Sealed>]
    type DescriptionAttribute(description : string) = 
        inherit Attribute()
        member this.Description = description
