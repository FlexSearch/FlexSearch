namespace FlexSearch.Server
open Autofac
open FlexSearch.Core
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.AspNet.Hosting
open Microsoft.AspNet.Hosting.Internal
open Microsoft.AspNet.Http
open Microsoft.AspNet.Builder
open Microsoft.AspNet.StaticFiles
open Microsoft.AspNet.FileProviders
open Microsoft.Extensions.PlatformAbstractions
open Microsoft.Net.Http.Server
open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Reflection
open System.Runtime.Versioning
open System.Net
open FlexSearch.Server.Extensions

module Messages =
    let accessDenied = """
Port access issue. Make sure that the running user has necessary permission to open the port. 
Use the below command to add URL reservation.
---------------------------------------------------------------------------
netsh http add urlacl url=http://+:{port}/ user=everyone listen=yes
---------------------------------------------------------------------------
"""

/// Creates a web server which supports ASP.net style initialization.
/// Pass custom server settings object in case creating a test server.
/// Otherwise the default settings will be loaded from the config.ini
/// file in production mode.
/// Note: It is advisable to create this class through WebServerBuilder 
[<Sealed>]
type WebServer(configuration : IConfiguration) =
    
    /// Create settings object from the passed configuration
    let serverSettings = new Settings.T(configuration)
    
    /// Determine if we have to start the server in test mode
    let testServer = serverSettings.GetBool("server", "testserver", false)
    
    /// Name of the HTTP server to be used
    let serverAssemblyName = 
        let name = "Microsoft.AspNet.Server." + serverSettings.Get(Settings.ServerKey, Settings.ServerType, "Kestrel")
        Logger.Log("Using server " + name, MessageKeyword.Startup, MessageLevel.Verbose)
        name
    
    /// Port on which server should start (Defaults to 9800)
    let port = serverSettings.GetInt(Settings.ServerKey, Settings.HttpPort, 9800).ToString()
        
    /// Autofac container 
    let mutable container = setupDependencies testServer serverSettings

    /// Represents all the HTTP services loaded in the domain
    let httpHandlers = 
        container.Resolve<Dictionary<string, IHttpHandler>>()
        |> generateRoutingTable

    /// Default handler to transform C# function to F#
    let handler = Func<HttpContext, Func<Task>, Task>(fun ctx _ -> Async.StartAsTask(requestProcessor ctx httpHandlers) :> Task)
    
    /// Wraps ASP.net dependencies inside the AutoFac container
    let setupContainerForAsp(services : IServiceCollection) =
        let builder = new ContainerBuilder()
        builder
        |> injectFromAspDi services
        |> fun x -> 
            x.Update(container)
        // Return an IServiceProvider to be compatible with Microsoft's DI
        container.Resolve<IServiceProvider>()

    // Use this method to add services to the container.
    // NOTE: This method name/signature cannot be changed as ASP.net expects it.
    member __.ConfigureServices(services : IServiceCollection) : IServiceProvider =
        services.AddCors() |> ignore

        // Add the instance of the Hosting environment
        services.AddSingleton<IApplicationEnvironment>(new DefaultApplicationEnvironment()) |> ignore
        services |> setupContainerForAsp

    // Use this method to configure the HTTP request pipeline.
    // NOTE: This method name/signature cannot be changed as ASP.net expects it.
    member __.Configure (app : IApplicationBuilder) = 
        let configureFileServer() =
            let fileServerOptions = new FileServerOptions()
            fileServerOptions.EnableDefaultFiles <- true
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("index.html")
            fileServerOptions.FileProvider <- new PhysicalFileProvider(Constants.WebFolder)
            fileServerOptions.StaticFileOptions.ServeUnknownFileTypes <- true
            fileServerOptions.RequestPath <- new PathString(@"/portal")
            app.UseStaticFiles("/web") 
               .UseFileServer(fileServerOptions) |> ignore

        let configureCors() =
           // TODO: Get CORS settings from the settings file
           app.UseCors(fun builder -> builder.AllowAnyOrigin()
                                          .AllowAnyHeader()
                                          .AllowAnyMethod()
                                          .AllowCredentials() |> ignore) |> ignore

        configureFileServer()
        configureCors()
   
        // This should always be the last middle-ware in the pipeline as this is
        // responsible for handling our REST requests
        app.Use(handler) |> ignore

[<Sealed>]
type WebServerBuilder(settings : Settings.T) =
    let mutable engine = Unchecked.defaultof<IWebApplication>
    let mutable server = Unchecked.defaultof<IDisposable>
    let mutable thread = Unchecked.defaultof<Task>
    
    /// Name of the HTTP server to be used
    let serverAssemblyName = 
        let name = "Microsoft.AspNet.Server." + settings.Get(Settings.ServerKey, Settings.ServerType, "Kestrel")
        Logger.Log("Using server " + name, MessageKeyword.Startup, MessageLevel.Verbose)
        name
    
    /// Port on which server should start (Defaults to 9800)
    let port = settings.GetInt(Settings.ServerKey, Settings.HttpPort, 9800).ToString()
    
    let webHostBuilder =
        let config = settings.ConfigurationSource
        let webAppBuilder = new WebApplicationBuilder()
        webAppBuilder.UseConfiguration(config)
                     .UseServerFactory(serverAssemblyName)
                     .ConfigureServices(fun s -> s.AddSingleton<IConfiguration>(config) |> ignore)
                     .UseStartup<WebServer>()
    
    /// Perform all the clean up tasks to be run just before a shutdown request is 
    /// received by the server
    let shutdown() = 
        // Get all types which implement IRequireNotificationForShutdown and issue shutdown command 
        engine.Services.GetServices<IRequireNotificationForShutdown>()
        |> Seq.toArray
        |> Array.Parallel.iter (fun x -> x.Shutdown() |> Async.RunSynchronously)
        server.Dispose()

    member __.Start() =
        let startServer() = 
            try 
                engine <- webHostBuilder.Build()
                let addresses = engine.GetAddresses()
                addresses.Add(sprintf "http://localhost:%s" port)
                server <- engine.Start()
            with 
                | :? ReflectionTypeLoadException as e -> 
                    let loaderExceptions = e.LoaderExceptions 
                                            |> Seq.map exceptionPrinter
                                            |> fun exns -> String.Join(Environment.NewLine, exns)
                    let message = sprintf "Main exception:\n%s\n\nType Loader Exceptions:\n%s" 
                                    <| exceptionPrinter e
                                    <| loaderExceptions
                    Logger.Log(message, MessageKeyword.Startup, MessageLevel.Critical);
                    reraise()
                | :? WebListenerException as e -> 
                    if e.ErrorCode = 5 then // Access Denied
                        Logger.Log("FlexSearch can only be run as an administrator when using WebListener server",
                                    MessageKeyword.Node,
                                    MessageLevel.Critical)
                    else Logger.Log(e, MessageKeyword.Startup, MessageLevel.Critical)
                    reraise()
                | e when e.InnerException |> (isNull >> not) ->
                    if e.InnerException :? HttpListenerException then
                        let innerException = e.InnerException :?> HttpListenerException
                        if innerException.ErrorCode = 5 then 
                            // Access denied error
                            Logger.Log(Messages.accessDenied, e, MessageKeyword.Node, MessageLevel.Critical)
                        else Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical)
                    else Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical)
                    reraise()
                | e -> Logger.Log(e, MessageKeyword.Node, MessageLevel.Critical); reraise()
            
        thread <- Task.Factory.StartNew(startServer, TaskCreationOptions.LongRunning)
        
    member __.Stop() = shutdown()