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
open Autofac.Integration.WebApi
open Microsoft.Owin.Hosting
open Owin
open System
open System.Net.Http.Formatting
open System.Threading
open System.Threading.Tasks
open System.Web.Http
open System.Web.Http.ExceptionHandling

type GlobalExceptionHandler() =
    interface IExceptionHandler with
        member this.HandleAsync(context: ExceptionHandlerContext, cancellationToken: CancellationToken): Task = 
            let tcs = new Task(fun _ -> ())
            failwithf ""
            tcs

[<Sealed>]
type OwinWebApiServer(container : IContainer, ?port0 : int) = 
    let port = defaultArg port0 9800
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<_>
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="appBuilder"></param>
    member this.Configuration(appBuilder : IAppBuilder) = 
        let configuration = new HttpConfiguration()
        // Attribute routing.
        configuration.MapHttpAttributeRoutes()
        configuration.Routes.MapHttpRoute("default", "{controller}") |> ignore
        configuration.IncludeErrorDetailPolicy <- IncludeErrorDetailPolicy.LocalOnly
        // Add formatters
        configuration.Formatters.Add(new BsonMediaTypeFormatter())
        configuration.Formatters.Add(new XmlMediaTypeFormatter())
        // There must be exactly one exception handler. (There is a default one that may be replaced.)
        // To make this sample easier to run in a browser, replace the default exception handler with one that sends
        // back text/plain content for all errors.
        configuration.Services.Replace(typeof<IExceptionHandler>, new GlobalExceptionHandler())

        let resolver = new AutofacWebApiDependencyResolver(container)
        // Configure Web API with the dependency resolver
        configuration.DependencyResolver <- resolver
        appBuilder.UseWebApi(configuration) |> ignore
    
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
