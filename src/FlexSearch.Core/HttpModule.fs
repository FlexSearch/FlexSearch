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
namespace FlexSearch.Core.HttpModule

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open FlexSearch.Core.State
open Newtonsoft.Json
open ServiceStack
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.Composition
open System.Linq
open System.Net
open System.Net.Http


[<Export(typeof<HttpModule>)>]
[<PartCreationPolicy(CreationPolicy.NonShared)>]
[<ExportMetadata("Name", "index")>]
type IndexModule(state: NodeState) = 
    inherit HttpModule(state)
    
    let routes = 
        [||]
    
    override this.Routes() = routes
    override this.Get indexName request response = ()
    override this.Post indexName request  = box ""
    override this.Delete indexName request  = box ""
    override this.Put indexName request = box ""
