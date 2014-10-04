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
namespace FlexSearch.Core.Services

open FlexSearch.Api
open FlexSearch.Common
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Data
open System.IO
open System.Linq

/// <summary>
/// Resource service class which is responsible for loading all resources
/// By default resources are loaded from Conf folder. 
/// </summary>
[<Sealed>]
type ResourceService(writer : IThreadSafeWriter, settings : ServerSettings) = 
    interface IResourceService with
        
        member x.DeleteResource<'T>(resourceName : string) : Choice<unit, OperationMessage> = 
            let typeName = typeof<'T>.Name
            writer.DeleteFile(Path.Combine(settings.ConfFolder, typeName, resourceName))
        
        member x.GetResource<'T>(resourceName : string) : Choice<'T, OperationMessage> = 
            let typeName = typeof<'T>.Name
            writer.ReadFile(Path.Combine(settings.ConfFolder, typeName, resourceName))
        
        member x.UpdateResource<'T>(resourceName : string, resource : 'T) : Choice<unit, OperationMessage> = 
            let typeName = typeof<'T>.Name
            writer.WriteFile<'T>(Path.Combine(settings.ConfFolder, typeName, resourceName), resource)
