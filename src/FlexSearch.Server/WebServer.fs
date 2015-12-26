namespace FlexSearch.Server

open FlexSearch.Core
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNet.Hosting
open Microsoft.AspNet.Hosting.Internal
open Microsoft.AspNet.Http
open Microsoft.AspNet.Builder
open Microsoft.AspNet.StaticFiles
open Microsoft.AspNet.FileProviders
open Microsoft.Extensions.PlatformAbstractions
open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Reflection
open System.Runtime.Versioning
open System.Net
open FlexSearch.Server.Extensions


type IServer = 
    abstract Start : unit -> unit
    abstract Stop : unit -> unit
    // This member is exposed to access the services so that they can be gracefully shut down
    abstract member Services : IServiceProvider option

[<Sealed>]
type WebServer(dependencySetup : Settings.T -> IServiceCollection -> IServiceProvider, serverSettings: Settings.T) = 
    let port = serverSettings.GetInt(Settings.ServerKey, Settings.HttpPort, 9800).ToString()
    let _httpHandlers = new Dictionary<string, IHttpHandler>()
    let mutable engine = Unchecked.defaultof<IHostingEngine>
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<_>
    let serverAssemblyName = "Microsoft.AspNet.Server.Kestrel"

    let accessDenied = """
Port access issue. Make sure that the running user has necessary permission to open the port. 
Use the below command to add URL reservation.
---------------------------------------------------------------------------
netsh http add urlacl url=http://+:{port}/ user=everyone listen=yes
---------------------------------------------------------------------------
"""
    
    /// Default OWIN handler to transform C# function to F#
    let handler = Func<HttpContext, Func<Task>, Task>(fun ctx _ -> Async.StartAsTask(requestProcessor ctx _httpHandlers) :> Task)
    

    let configuration (app : IApplicationBuilder) = 
        let fileServerOptions = new FileServerOptions()
        fileServerOptions.EnableDefaultFiles <- true
        fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("index.html")
        fileServerOptions.FileProvider <- new PhysicalFileProvider(Constants.WebFolder)
        fileServerOptions.StaticFileOptions.ServeUnknownFileTypes <- true
        fileServerOptions.RequestPath <- new PathString(@"/portal")
        
        app.UseStaticFiles("/web") 
           .UseFileServer(fileServerOptions) 
           .UseCors(fun builder -> builder.AllowAnyOrigin()
                                          .AllowAnyHeader()
                                          .AllowAnyMethod()
                                          .AllowCredentials() |> ignore)
           // This should always be the last middleware in the pipeline as this is
           // resposible for handling our REST requests
           .Use(handler)
        |> ignore

    let _webHostBuilder =
        let configureAutofacServices (services : IServiceCollection) : IServiceProvider =
            services |> dependencySetup serverSettings

        let setupServices (services : IServiceCollection) = 
            services.AddCors()
                    .AddFrameworkLogging(fun () -> serverSettings.ConfigurationSource.Item "Server:FrameworkLogging" = "true")
            |> ignore

            let conf = let c = Environment.GetEnvironmentVariable("TARGET_CONFIGURATION")
                       if isNull c then "Debug" else c

            let appEnv = new HostApplicationEnvironment(AppContext.BaseDirectory,
                                                        new FrameworkName(".NETFramework,Version=v4.5"),
                                                        conf,
                                                        Assembly.Load(serverAssemblyName))

            // Add the instance of the Hosting environment
            services.AddInstance<IApplicationEnvironment>(appEnv) |> ignore

        // Set the port number
        // This is a hacky way of doing it. This is the key that AspNet.Hosting module is using to set the
        // port number. If any programmatic way of doing it comes up in the future (apart from using the
        // --server.urls parameter in dnx.exe), please use it
        serverSettings.ConfigurationSource.Item "HTTP_PLATFORM_PORT" <- port

        (new WebHostBuilder(serverSettings.ConfigurationSource))
            .UseServer(serverAssemblyName)
            .UseStartup(configuration, configureAutofacServices)
            .UseServices(fun s -> setupServices s)

    // This member is exposed to instantiate the Test Server
    member __.GetWebHostBuilder() = _webHostBuilder

    interface IServer with
        
        member this.Start() = 
            let startServer() = 
                try 
                    engine <- _webHostBuilder.Build()

                    // Resolve the Http Handlers
                    engine.ApplicationServices.GetService<Dictionary<string, IHttpHandler>>()
                    |> generateRoutingTable
                    |> Seq.iter (fun kv -> _httpHandlers.Add(kv.Key, kv.Value))

                    // Start the server
                    server <- engine.Start()

                    //netsh http add urlacl url=http://+:9800/ user=everyone listen=yes
                with 
                    | :? ReflectionTypeLoadException as e -> 
                        let loaderExceptions = e.LoaderExceptions 
                                               |> Seq.map exceptionPrinter
                                               |> fun exns -> String.Join(Environment.NewLine, exns)
                        let message = sprintf "Main exception:\n%s\n\nType Loader Exceptions:\n%s" 
                                      <| exceptionPrinter e
                                      <| loaderExceptions
                        Logger.Log(message, MessageKeyword.Startup, MessageLevel.Critical)
                    | e when e.InnerException |> (isNull >> not) ->
                        if e.InnerException :? HttpListenerException then
                            let innerException = e.InnerException :?> HttpListenerException
                            if innerException.ErrorCode = 5 then 
                                // Access denied error
                                Logger.Log(accessDenied, e, MessageKeyword.Node, MessageLevel.Critical)
                            else Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical)
                        else Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical)
                    | e -> Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical)
            try 
                thread <- Task.Factory.StartNew(startServer, TaskCreationOptions.LongRunning)
            with e -> Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical)
        
        member __.Stop() = server.Dispose()

        member __.Services = if engine |> isNull then None
                             else engine.ApplicationServices |> Some