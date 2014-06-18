// ----------------------------------------------------------------------------
// Mef container to dynamic dependency resolution (MefFactory.fs)
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

open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.ComponentModel.Composition
open System.ComponentModel.Composition.Hosting
open System.IO
open System.Linq
open System.Reflection
open org.apache.lucene.analysis
open org.apache.lucene.analysis.miscellaneous
open Autofac

// ----------------------------------------------------------------------------
// Contains MEF container and other factory implementation
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Factories = 
    
    /// <summary>
    /// Concrete implementation of IResourceLoader
    /// </summary>
    [<Sealed>]
    type ResourceLoader() = 
        interface IResourceLoader with
            
            member this.LoadResourceAsString(resourceName) = 
                let path = Helpers.GenerateAbsolutePath(".\\conf\\" + resourceName)
                Helpers.LoadFile(path)
            
            member this.LoadResourceAsList(resourceName) = 
                let path = Helpers.GenerateAbsolutePath(".\\conf\\" + resourceName)
                let readLines = System.IO.File.ReadLines(path)
                let result = new List<string>()
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false && line.StartsWith("#") = false then 
                        result.Add(line.Trim().ToLowerInvariant())
                result
            
            member this.LoadResourceAsMap(resourceName) = 
                let path = Helpers.GenerateAbsolutePath(".\\conf\\" + resourceName)
                let readLines = System.IO.File.ReadLines(path)
                let result = new List<string []>()
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false && line.StartsWith("#") = false then 
                        let lineLower = line.ToLowerInvariant()
                        let values = lineLower.Split([| ":"; "," |], System.StringSplitOptions.RemoveEmptyEntries)
                        if values.Length > 1 then result.Add(values)
                result
