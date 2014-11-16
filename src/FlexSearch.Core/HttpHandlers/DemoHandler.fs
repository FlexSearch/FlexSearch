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
namespace FlexSearch.Core.HttpHandlers

open FlexSearch.Api
open FlexSearch.Common
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Core.Services
open System
open System.Collections.Generic
open System.IO
open System.Linq

/// <summary>
///  Sets up a demo index. The name of the index is `country`.
/// </summary>
/// <method>PUT</method>
/// <uri>/setupdemo</uri>
/// <resource>setup</resource>
/// <id>put-setup-demo</id>
[<Name("PUT-/setupdemo")>]
[<Sealed>]
type SetupDemoHandler(service : DemoIndexService) = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = (service.Setup(), Ok, BadRequest)
