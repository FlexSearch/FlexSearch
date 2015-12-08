// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open System
open System.Reflection
open System.Runtime.Versioning
open Microsoft.Extensions.PlatformAbstractions
open System.Collections.Concurrent

type HostApplicationEnvironment(appBase : string, 
                                targetFramework: FrameworkName,
                                configuration : string,
                                assembly : Assembly) =
    let assemblyName = assembly.GetName()
    let store = new ConcurrentDictionary<string,obj>(StringComparer.Ordinal)
    interface IApplicationEnvironment with
        member __.ApplicationName = assemblyName.Name
        member __.ApplicationVersion = assemblyName.Version.ToString()
        member __.GetData(name) = match store.TryGetValue(name) with
                                  | true, value -> value
                                  | _ -> null
        member __.SetData(name, value) = store.AddOrUpdate(name, value, fun k v -> v) |> ignore
        member __.ApplicationBasePath = appBase
        member __.RuntimeFramework = targetFramework
        member __.Configuration = configuration

module Extensions =
    open Microsoft.AspNet.Http.Features
    open Microsoft.Net.Http.Server
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.Logging

    type Microsoft.AspNet.Builder.IApplicationBuilder with
        member app.UseAuthenticationSchemes(authenticationSchemes) =
            let listener = app.ServerFeatures.Get<WebListener>()
            if listener |> isNotNull then
                listener.AuthenticationManager.AuthenticationSchemes <- authenticationSchemes
            app
    
    type Microsoft.Extensions.DependencyInjection.IServiceCollection with
        member services.AddFrameworkLogging(confCheck : unit -> bool) =
            if confCheck () then
                let logging = services 
                              |> Seq.find (fun x -> x.ServiceType = typedefof<ILoggerFactory>)
                              |> fun x -> x.ImplementationInstance
                              :?> ILoggerFactory
                logging.MinimumLevel <- LogLevel.Verbose
                logging.AddEventLog() |> ignore
            services