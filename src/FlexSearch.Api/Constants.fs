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
namespace FlexSearch.Api

/// Contains all the flex constants and cache store definitions 
[<AutoOpen>]
[<RequireQualifiedAccess>]
module Constants = 
    [<Literal>]
    let IdField = "_id"
    
    [<Literal>]
    let LastModifiedField = "_lastmodified"
    
    [<Literal>]
    let TypeField = "_type"
    
    [<Literal>]
    let VersionField = "_version"
    
    [<Literal>]
    let DocumentField = "_document"
    
    [<Literal>]
    let DotNetFrameWork = "4.5.1"

    [<Literal>]
    let StandardAnalyzer = "standardanalyzer"
