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
open FlexSearch.Api.Message
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
type OwinServer(indexService : IIndexService, httpFactory : IFlexFactory<HttpModuleBase>, ?port0 : int) = 
    let port = defaultArg port0 9800
    let httpModule = httpFactory.GetAllModules()
        
    /// <summary>
    /// Default OWIN method to process request
    /// </summary>
    /// <param name="owin">OWIN Context</param>
    let exec (owin : IOwinContext) = 
        async { 
            let getModule moduleName indexName (owin : IOwinContext) = 
                match httpModule.TryGetValue(moduleName) with
                | (true, x) -> 
                    match owin.Request.Method.ToUpperInvariant() with
                    | "GET" -> x.Get(indexName, owin)
                    | "POST" -> x.Post(indexName, owin)
                    | "PUT" -> x.Put(indexName, owin)
                    | "DELETE" -> x.Delete(indexName, owin)
                    | _ -> owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
                | _ -> owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
                
            let getIndexName (owin : IOwinContext) = 
                if owin.Request.Uri.Segments.[1].EndsWith("/") then 
                    owin.Request.Uri.Segments.[1].Substring(0, owin.Request.Uri.Segments.[1].Length - 1)
                else owin.Request.Uri.Segments.[1]
                
            try 
                match owin.Request.Uri.Segments.Length with
                // Server root
                | 1 -> getModule "/" "/" owin
                // Root index request
                | 2 -> 
                    let indexName = getIndexName owin
                    match indexService.IndexExists(indexName) with
                    | true -> getModule "index" indexName owin
                    | false -> 
                        // This can be an index creation request
                        if owin.Request.Method = "POST" then getModule "index" indexName owin
                        else owin |> BAD_REQUEST MessageConstants.INDEX_NOT_FOUND
                // Index module request
                | x when x > 2 && x < 5 -> 
                    let indexName = getIndexName owin
                    match indexService.IndexExists(indexName) with
                    | true -> 
                        let moduleName = 
                            if owin.Request.Uri.Segments.[2].EndsWith("/") then 
                                owin.Request.Uri.Segments.[2].Substring(0, owin.Request.Uri.Segments.[2].Length - 1)
                            else owin.Request.Uri.Segments.[2]
                        getModule moduleName indexName owin
                    | false -> owin |> BAD_REQUEST MessageConstants.INDEX_NOT_FOUND
                | _ -> owin |> BAD_REQUEST MessageConstants.HTTP_NOT_SUPPORTED
            with ex -> ()
        }
        
    /// <summary>
    /// Default OWIN handler to transform C# function to F#
    /// </summary>
    let handler = Func<IOwinContext, Tasks.Task>(fun owin -> Async.StartAsTask(exec (owin)) :> Task)
        
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<_>
    let configuration (app : IAppBuilder) = app.Run(handler)
        
    let startServer() = 
        let startOptions = new StartOptions(sprintf "http://*:%i" port)
        server <- Microsoft.Owin.Hosting.WebApp.Start(startOptions, configuration)
        Console.ReadKey() |> ignore
        
    interface IServer with
            
        member this.Start() = 
            try 
                thread <- Task.Factory.StartNew(startServer, TaskCreationOptions.LongRunning)
            with e -> ()
            ()
            
        member this.Stop() = server.Dispose()
