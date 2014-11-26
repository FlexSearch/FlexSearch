// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Utility
open System
open System.IO
open System.Linq

[<CLIMutableAttribute>]
type ServerSettings = 
    { HttpPort : int
      DataFolder : string
      PluginFolder : string
      ConfFolder : string
      NodeName : string }
    
    /// <summary>
    /// Get default server configuration
    /// </summary>
    static member GetDefault() = 
        let setting = 
            { HttpPort = 9800
              DataFolder = Helpers.GenerateAbsolutePath("./data")
              PluginFolder = Constants.PluginFolder
              ConfFolder = Constants.ConfFolder
              NodeName = "FlexSearchNode" }
        setting
    
    /// <summary>
    /// Reads server configuration from the given file
    /// </summary>
    /// <param name="path"></param>
    static member GetFromFile(path : string, formatter : IFormatter) = 
        assert (String.IsNullOrWhiteSpace(path) <> true)
        if File.Exists(path) then 
            let fileStream = new FileStream(path, FileMode.Open)
            let parsedResult = formatter.DeSerialize<ServerSettings>(fileStream)
            
            let setting = 
                { HttpPort = parsedResult.HttpPort
                  DataFolder = Helpers.GenerateAbsolutePath(parsedResult.DataFolder)
                  PluginFolder = Constants.PluginFolder
                  ConfFolder = Constants.ConfFolder
                  NodeName = parsedResult.NodeName }
            Choice1Of2(setting)
        else Choice2Of2(Errors.UNABLE_TO_PARSE_CONFIG |> GenerateOperationMessage)
