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

open Autofac
open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Core
open FlexSearch.Utility
open Microsoft.Owin
open Microsoft.Owin.Hosting
open Newtonsoft.Json
open Owin
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Linq
open System.Net
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Thrift server
/// </summary>
[<Name("Http")>]
[<Sealed>]
type OwinServer(indexService : IIndexService, httpFactory : IFlexFactory<IHttpHandler>, ?port0 : int) = 
    let port = defaultArg port0 9800
    
    let httpModule = 
        let modules = httpFactory.GetAllModules()
        let result = new Dictionary<string, IHttpHandler>(StringComparer.OrdinalIgnoreCase)
        for m in modules do
            // check if the key supports more than one http verb
            if m.Key.Contains("|") then 
                let verb = m.Key.Substring(0, m.Key.IndexOf("-"))
                let verbs = verb.Split([| '|' |], StringSplitOptions.RemoveEmptyEntries)
                let value = m.Key.Substring(m.Key.IndexOf("-") + 1)
                for v in verbs do
                    result.Add(v + "-" + value, m.Value)
            else result.Add(m.Key, m.Value)
        result
    
    /// <summary>
    /// Default OWIN method to process request
    /// </summary>
    /// <param name="owin">OWIN Context</param>
    let exec (owin : IOwinContext) = 
        async { 
            let getModule lookupValue (owin : IOwinContext) = 
                match httpModule.TryGetValue(owin.Request.Method.ToLowerInvariant() + "-" + lookupValue) with
                | (true, x) -> x.Process owin
                | _ -> 
                    owin 
                    |> BAD_REQUEST(new Response<unit>(Error = (Errors.HTTP_NOT_SUPPORTED |> GenerateOperationMessage)))
            
            let matchSubModules (x : int, owin : IOwinContext) = 
                match x with
                | 3 -> getModule ("/" + owin.Request.Uri.Segments.[1] + ":id") owin
                | 4 -> 
                    getModule 
                        ("/" + owin.Request.Uri.Segments.[1] + ":id/" 
                         + HttpHelpers.RemoveTrailingSlash owin.Request.Uri.Segments.[3]) owin
                | 5 -> 
                    getModule ("/" + owin.Request.Uri.Segments.[1] + ":id/" + owin.Request.Uri.Segments.[3] + ":id") 
                        owin
                | _ -> 
                    owin 
                    |> BAD_REQUEST(new Response<unit>(Error = (Errors.HTTP_NOT_SUPPORTED |> GenerateOperationMessage)))
            
            try 
                match owin.Request.Uri.Segments.Length with
                // Server root
                | 1 -> getModule "/" owin
                // Root resource request
                | 2 -> getModule ("/" + HttpHelpers.RemoveTrailingSlash owin.Request.Uri.Segments.[1]) owin
                | x when x > 2 && x <= 5 -> 
                    // Check if the Uri is indices and perform an index exists check
                    if (String.Equals(owin.Request.Uri.Segments.[1], "indices") 
                        || String.Equals(owin.Request.Uri.Segments.[1], "indices/")) && owin.Request.Method <> "POST" then 
                        match indexService.IndexExists(RemoveTrailingSlash owin.Request.Uri.Segments.[2]) with
                        | true -> matchSubModules (x, owin)
                        | false -> 
                            owin 
                            |> NOT_FOUND
                                   (new Response<unit>(Error = (Errors.INDEX_NOT_FOUND |> GenerateOperationMessage)))
                    else matchSubModules (x, owin)
                | _ -> 
                    owin 
                    |> BAD_REQUEST(new Response<unit>(Error = (Errors.HTTP_NOT_SUPPORTED |> GenerateOperationMessage)))
            with ex -> ()
        }
    
    /// <summary>
    /// Default OWIN handler to transform C# function to F#
    /// </summary>
    let handler = Func<IOwinContext, Tasks.Task>(fun owin -> Async.StartAsTask(exec (owin)) :> Task)
    
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<_>
    member this.Configuration(app : IAppBuilder) = app.Run(handler)
    interface IServer with
        
        member this.Start() = 
            let startServer() = 
                try 
                    //netsh http add urlacl url=http://+:9800/ user=everyone listen=yes
                    let startOptions = new StartOptions(sprintf "http://+:%i/" port)
                    server <- Microsoft.Owin.Hosting.WebApp.Start(startOptions, this.Configuration)
                    Console.ReadKey() |> ignore
                with e -> printfn "%A" e
            try 
                thread <- Task.Factory.StartNew(startServer, TaskCreationOptions.LongRunning)
            with e -> ()
            ()
        
        member this.Stop() = server.Dispose()
