open FlexSearch.Core
open System
open System.IO
open Topshelf

let topShelfConfiguration(conf : HostConfigurators.HostConfigurator) = 
    conf.RunAsLocalSystem() |> ignore
    conf.SetDescription("FlexSearch Server")
    conf.SetDisplayName("FlexSearch Server")
    conf.SetServiceName("FlexSearch-Server")
    conf.StartAutomatically() |> ignore
    conf.EnableServiceRecovery(fun rc -> rc.RestartService(1) |> ignore) |> ignore
    conf.Service<NodeService>(fun factory -> 
        ServiceConfiguratorExtensions.ConstructUsing(factory, fun () -> StartUp.start()) 
        |> ignore
        ServiceConfiguratorExtensions.WhenStarted(factory, fun tc -> tc.Start()) |> ignore
        ServiceConfiguratorExtensions.WhenStopped(factory, fun tc -> tc.Stop()) |> ignore)
    |> ignore
    
[<EntryPoint>]
let main argv = 
    HostFactory.Run(fun x -> topShelfConfiguration(x)) |> int
    