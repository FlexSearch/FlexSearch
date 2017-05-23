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
namespace FlexSearch.Server

open System.Collections.Generic
open FlexSearch.Core
open System
open System.Text

module Management = 
    open System.Management
    
    let getObj (className : string) = 
        let query = "SELECT * FROM Win32_" + className
        use mgmtObj = new ManagementObjectSearcher(query)
        mgmtObj.Get()
    
    let private infoObjects = 
        [| ("Processor", [| "Name"; "Description"; "NumberOfCores"; "NumberOfLogicalProcessors"; "MaxClockSpeed" |])
           ("OperatingSystem", [| "Caption"; "TotalVisibleMemorySize" |])
           ("ComputerSystem", [| "TotalPhysicalMemory" |])
           ("PhysicalMemory", [| "ConfiguredClockSpeed" |])
           ("DiskDrive", [| "Manufacturer"; "Model"; "Size" |]) |]
    
    /// Generates basic system info like CPU etc. It is useful to record this information
    /// along with the performance test results    
    let generateSystemInfo() = 
        let info = new Dictionary<string, Dictionary<string, string>>()
        for (className, props) in infoObjects do
            let res = new Dictionary<string, string>()
            let stmt = getObj className
            let mutable i = 0
            for s in stmt do
                for p in props do
                    let v = s.GetPropertyValue(p)
                    
                    let propName = 
                        if i = 0 then p
                        else p + i.ToString()
                    if notNull v then res.[propName] <- v.ToString()
                i <- i + 1
            info.Add(className, res)
        info
    
    /// Generate printable report of system information
    let printSystemInfo() = 
        let sb = new StringBuilder()
        for className in generateSystemInfo() do
            for prop in className.Value do
                sb.AppendLine(sprintf "%s-%s : %s" className.Key prop.Key prop.Value) |> ignore
        sb.ToString()

module Installers = 
    open System.Diagnostics
    
    let out s = printfn "[FlexSearch.Install] %s" s
    
    let toQuotedString (s : string) = 
        if System.String.IsNullOrEmpty s then s
        elif s.Chars 0 = '"' then s
        else "\"" + s + "\""
    
    // Executes a given exe along with the passed argument 
    let exec path argument showOutput = 
        let psi = new ProcessStartInfo()
        psi.FileName <- path
        psi.Arguments <- argument
        psi.WorkingDirectory <- Constants.rootFolder
        psi.RedirectStandardOutput <- not showOutput
        psi.UseShellExecute <- false
        use p = Process.Start(psi)
        p.WaitForExit()
    
    let reservePort (port : int) = 
        printfn "Reserving the port %i" port
        exec "netsh.exe" <| sprintf "http add urlacl url=http://+:%i/ user=everyone listen=yes" port
                         <| true
    
    let resetPerformanceCounters () =
        try exec "lodctr" "/r" false
        with e -> printfn "Failed to reset performance counters: %s" e.Message

    let assignKeyContainerToUser() =
        let user = sprintf "%s\\%s" Environment.UserDomainName Environment.UserName
        printfn "Assigning the '%s' RSA Key container to the user %s" Constants.RsaKeyContainerName user

        try
            let frameworkV4 = loopDir "C:\\Windows\\Microsoft.NET\\Framework"
                              |> Seq.find(fun d -> d.Contains("v4.0"))

            let aspnet_regiis = (frameworkV4 +/ "aspnet_regiis") 

            // Assign the key to the user
            exec aspnet_regiis
            <| sprintf "-pa \"%s\" \"%s\"" Constants.RsaKeyContainerName user
            <| true
        with e -> printfn "Failed to assign the key: %s" <| exceptionPrinter e

    /// Gets executed after the service is installed by TopShelf
    let afterInstall (settings : Settings.T) = 
        new Action(fun _ -> 
        reservePort (settings.GetInt(Settings.ServerKey, Settings.HttpPort, 9800)))