// ----------------------------------------------------------------------------
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
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Reflection
open System.Threading
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.lucene.codecs
open org.apache.lucene.codecs.lucene42
open org.apache.lucene.document
open org.apache.lucene.index
open org.apache.lucene.search
open org.apache.lucene.store

// ----------------------------------------------------------------------------
// Contains all the flex constants and cache store definitions 
// ----------------------------------------------------------------------------
[<AutoOpen>]
[<RequireQualifiedAccess>]
module Constants = 
    // Lucene version to be used across the application
    let LuceneVersion = org.apache.lucene.util.Version.LUCENE_47
    
    [<Literal>]
    let IdField = "_id"
    
    [<Literal>]
    let LastModifiedField = "_lastmodified"
    
    [<Literal>]
    let TypeField = "_type"
    
    [<Literal>]
    let VersionField = "_version"
    
    [<Literal>]
    let DocumentField = "_document"
    
    // Flex root folder path
    let private rootFolder = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
    let private dataFolder = Path.Combine(rootFolder, "Data")
    let private confFolder = Path.Combine(rootFolder, "Conf")
    let private pluginFolder = Path.Combine(rootFolder, "Plugins")
    
    // Flex data folder
    let DataFolder = 
        Directory.CreateDirectory(dataFolder) |> ignore
        dataFolder
    
    // Flex index folder
    let ConfFolder = 
        Directory.CreateDirectory(confFolder) |> ignore
        confFolder
    
    // Flex plugins folder
    let PluginFolder = 
        Directory.CreateDirectory(pluginFolder) |> ignore
        pluginFolder
