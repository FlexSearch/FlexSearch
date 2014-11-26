// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Api

/// <summary>
/// Dummy holder for generating attribute related documentation
/// </summary>
type private AttributeDocumentation = 
    /// <summary>
    /// The collection should have at least %i item.
    /// </summary>
    | MinimumItems = 1
    /// <summary>
    /// The property should follow the following naming convention:
    /// + Should match the Regex pattern : ^[a-z0-9_]*$.
    /// + Should not be same as a reserved field name like Id, Timestamp.
    /// </summary>
    | PropertyName = 2
    /// <summary>
    /// The value of the property should be greater than %i.
    /// </summary>
    | GreaterThanOrEqual = 3
