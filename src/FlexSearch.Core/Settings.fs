// ----------------------------------------------------------------------------
// Flexsearch settings (Settings.fs)
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open FSharp.Data
open FlexSearch.Utility
open FlexSearch.Api
open FlexSearch.Core

open System
open System.Net
open System.Collections.Generic
open System.Xml
open System.Xml.Linq


// ----------------------------------------------------------------------------
/// Top level settings parse function   
// ----------------------------------------------------------------------------   
module Settings =

    /// Xml setting provider for server config
    type private FlexServerSetting = XmlProvider<"""
        <Settings version="1">
            <Node>
                <Name>Test</Name>
                <HttpPort>9800</HttpPort>
                <WSPort>9900</WSPort>
                <Role>Data</Role>
                <DataFolder>c:\</DataFolder>
            </Node>
            <Cluster>
                <MasterNode>10.80.105.1</MasterNode>
                <SlaveNode>10.80.105.1</SlaveNode>
            </Cluster>
            <RequestLogger Enabled="true" RollingLogCapacity="1000" WriteLogToDisk="false"/>
            <Plugins>
                <Plugin Name="test" />
                <Plugin Name="test" />
            </Plugins>
        </Settings>
    """
    >

    // ----------------------------------------------------------------------------
    /// Concerete implementation of ISettingsServer
    // ----------------------------------------------------------------------------   
    type ServerSettings(path: string) =
        let mutable dataFolder = ""
        let mutable nodeRole = NodeRole.Index
        let mutable masterNode = new IPAddress([|0uy; 0uy; 0uy; 0uy|])
        let mutable slaveNode = new IPAddress([|0uy; 0uy; 0uy; 0uy|])

        let settings = 
            let fileXml = Helpers.LoadFile(path)
            let parsedResult = FlexServerSetting.Parse(fileXml)          
            
            // Validate node name
            Validator.validate "Node->Name" parsedResult.Node.Name 
            |> Validator.notNullAndEmpty 
            |> Validator.regexMatch "^[a-z0-9]*$" 
            |> ignore

            match NodeRole.TryParse(parsedResult.Node.Role) with
            | (true, res) -> nodeRole <- res
            | _ -> failwithf "Invalid Node->Role:%s" parsedResult.Node.Role  
            
            dataFolder <- Helpers.GenerateAbsolutePath(parsedResult.Node.DataFolder)
            
            match IPAddress.TryParse(parsedResult.Cluster.MasterNode) with
            | (true, address) -> masterNode <- address
            | _ -> failwithf "Cluster->MasterNode ip address is  not in valid format: %s" parsedResult.Cluster.MasterNode

            match IPAddress.TryParse(parsedResult.Cluster.SlaveNode) with
            | (true, address) -> slaveNode <- address
            | _ -> failwithf "Cluster->SlaveNode ip address is  not in valid format: %s" parsedResult.Cluster.MasterNode

            parsedResult

        interface IServerSettings with
            member this.LuceneVersion() = Constants.LuceneVersion
            member this.HttpPort() = settings.Node.HttpPort
            member this.WSPort() = settings.Node.WspOrt
            member this.DataFolder() = dataFolder
            member this.PluginFolder() = Constants.PluginFolder.Value
            member this.ConfFolder() = Constants.ConfFolder.Value
            member this.NodeName() = settings.Node.Name
            member this.NodeType() = nodeRole
            member this.MasterNode() = settings.Cluster.MasterNode
            member this.SlaveNode() = settings.Cluster.SlaveNode     

            
            member this.PluginsToLoad() = 
                let plugins = new List<string>()
                if settings.XElement.Element(XName.Get("Plugins")) <> null then
                    for plugin in settings.Plugins.GetPlugins() do
                        plugins.Add(plugin.Name)
                plugins.ToArray()
                
            member this.LoggerProperties() = 
                (settings.RequestLogger.Enabled, settings.RequestLogger.RollingLogCapacity, settings.RequestLogger.WriteLogToDisk)