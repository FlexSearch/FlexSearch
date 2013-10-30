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
open FlexSearch.Analysis.Analyzers
open FlexSearch.Core
open FlexSearch.Core.Index

open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.analysis
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.store
open org.apache.lucene.facet.search
open org.apache.lucene.search

open System
open System.IO
open System.Collections.Generic
open System.Diagnostics
open System.Xml
open System.Xml.Linq


// ----------------------------------------------------------------------------
// Top level settings parse function   
// ----------------------------------------------------------------------------   
module Settings =

    // Xml setting provider for server config
    type FlexServerSetting = XmlProvider<"""
        <Settings version="1">
            <HttpPort>9800</HttpPort>
            <RequestLogger Enabled="true" RollingLogCapacity="1000" WriteLogToDisk="false"/>
            <Plugins>
                <Plugin Name="test" />
                <Plugin Name="test" />
            </Plugins>
        </Settings>
    """
    >
    

    // ----------------------------------------------------------------------------
    // Concerete implementation of settings
    // ----------------------------------------------------------------------------   
    type ServerSettings(path: string) =
        let mutable settings = None
        do
            let fileXml = Helpers.LoadFile(path)
            settings <- Some(FlexServerSetting.Parse(fileXml))
            ()
        interface IServerSettings with
            member this.LuceneVersion() = Constants.LuceneVersion
            
            member this.HttpPort() = settings.Value.HttpPort
            
            member this.DataFolder() = Constants.DataFolder.Value
            
            member this.PluginFolder() = Constants.PluginFolder.Value
            
            member this.ConfFolder() = Constants.ConfFolder.Value
            
            member this.PluginsToLoad() = 
                let plugins = new List<string>()
                if settings.Value.XElement.Element(XName.Get("Plugins")) <> null then
                    for plugin in settings.Value.Plugins.GetPlugins() do
                        plugins.Add(plugin.Name)
                plugins.ToArray()
                
            member this.LoggerProperties() = 
                (settings.Value.RequestLogger.Enabled, settings.Value.RequestLogger.RollingLogCapacity, settings.Value.RequestLogger.WriteLogToDisk)