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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Logger =
    open FlexSearch.Logging
    open FlexSearch.Api

    let private logger = FlexLogger.Logger

    let addIndex (indexName: string) (indexDetails: Index) =
        if logger.IsEnabled() then
            logger.AddIndex(indexName, indexDetails.ToString()) 
