namespace FlexSearch.Server

open FlexSearch.Core
open Microsoft.Extensions.DependencyInjection
open System
open System.Threading
open System.IO

/// Used by windows service (top shelf) to start and stop windows service.
[<Sealed>]
type NodeService(serverSettings : Settings.T, testServer : bool) = 
    let mutable httpServer = Unchecked.defaultof<IServer>
    
    /// Perform all the clean up tasks to be run just before a shutdown request is 
    /// received by the server
    let shutdown() = 
        // Get all types which implement IRequireNotificationForShutdown and issue shutdown command
        match httpServer.Services with
        | Some(container) -> 
            container.GetServices<IRequireNotificationForShutdown>()
            |> Seq.toArray
            |> Array.Parallel.iter (fun x -> x.Shutdown() |> Async.RunSynchronously)
        | _ -> 
            Logger.Log
                ("Couldn't get access to the web server's service provider", MessageKeyword.Default, 
                 MessageLevel.Warning)

        httpServer.Stop()
    
    // do 
    // Increase the HTTP.SYS backlog queue from the default of 1000 to 65535.
    // To verify that this works, run `netsh http show servicestate`.
    //            if testServer <> true then MaximizeThreads() |> ignore
    member __.Start() = 
        try 
            httpServer <- new WebServer(setupDependencies testServer, serverSettings)
            httpServer.Start()
        with e -> printfn "%A" e
    
    member __.Stop() = shutdown()