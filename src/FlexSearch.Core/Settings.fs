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
                <TcpPort>9900</TcpPort>
                <Role>Data</Role>
                <DataFolder>c:\</DataFolder>
            </Node>
            <Cluster>
                <MasterNode>10.80.105.1</MasterNode>
            </Cluster>
        </Settings>
    """
    >

    // ----------------------------------------------------------------------------
    /// Concerete implementation of ISettingsServer
    // ----------------------------------------------------------------------------

    open SharpRepository
    open SharpRepository.Repository.Caching
    open SharpRepository.Repository
    open SharpRepository.XmlRepository

    type SettingsStore(path : string) =
        let mutable settings = None
        let mutable nodeRepository : IRepository<Node> option = None
        let mutable indexRepository : IRepository<Index> option = None

        do
            let fileXml = Helpers.LoadFile(path)
            let parsedResult = FlexServerSetting.Parse(fileXml)          
            
            // Validate node name
            Validator.validate "Node->Name" parsedResult.Node.Name 
            |> Validator.notNullAndEmpty 
            |> Validator.regexMatch "^[a-z0-9]*$" 
            |> ignore

            let nodeRole =
                match NodeRole.TryParse(parsedResult.Node.Role) with
                | (true, res) -> res
                | _ -> failwithf "Invalid Node->Role:%s" parsedResult.Node.Role  
            
            let masterNode =
                match IPAddress.TryParse(parsedResult.Cluster.MasterNode) with
                | (true, address) -> address
                | _ -> failwithf "Cluster->MasterNode ip address is  not in valid format: %s" parsedResult.Cluster.MasterNode

            let setting =
                {
                    LuceneVersion = Constants.LuceneVersion
                    HttpPort = parsedResult.Node.HttpPort
                    TcpPort = parsedResult.Node.TcpPort
                    DataFolder = Helpers.GenerateAbsolutePath(parsedResult.Node.DataFolder)
                    PluginFolder = Constants.PluginFolder.Value
                    ConfFolder = Constants.ConfFolder.Value
                    NodeName = parsedResult.Node.Name
                    NodeRole = nodeRole
                    MasterNode = masterNode
                }
            
            nodeRepository <- Some(new XmlRepository<Node>(setting.ConfFolder, new StandardCachingStrategy<Node>(new InMemoryCachingProvider())) :> IRepository<Node>) 
            indexRepository <- Some(new XmlRepository<Index>(setting.ConfFolder, new StandardCachingStrategy<Index>(new InMemoryCachingProvider())) :> IRepository<Index>)

            settings <- Some(setting)

        interface IPersistanceStore with
            member this.Settings = settings.Value
            member this.Indices = indexRepository.Value
            member this.Nodes = nodeRepository.Value

