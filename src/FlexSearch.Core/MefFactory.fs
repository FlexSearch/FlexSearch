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
namespace FlexSearch

open FlexSearch.Core
open FlexSearch.Api.Message
open FlexSearch.Core.State
open org.apache.lucene.analysis
open org.apache.lucene.analysis.miscellaneous
open System
open System.Collections.Generic
open System.ComponentModel.Composition
open System.ComponentModel.Composition.Hosting
open System.IO
open System.Linq
open System.Reflection

// ----------------------------------------------------------------------------
// Contains mef container and other factory implementation
// ----------------------------------------------------------------------------
module Factories = 
    // Mef container which loads all the related plugins
    let PluginContainer(readPluginDirectory) = 
        lazy (let aggrCatalog = new AggregateCatalog()
              // An assembly catalog to load information about part from this assembly  
              let asmCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly())
              // A directory catalog, to load parts from dlls in the Extensions folder
              if readPluginDirectory then 
                  let dirCatalog = new DirectoryCatalog(Constants.PluginFolder.Force(), "*.dll")
                  aggrCatalog.Catalogs.Add(dirCatalog)
              aggrCatalog.Catalogs.Add(asmCatalog)
              // Create a container  
              new CompositionContainer(aggrCatalog, CompositionOptions.IsThreadSafe))
    
    // ----------------------------------------------------------------------------
    // Concerete implementation of IResourceLoader
    // ----------------------------------------------------------------------------   
    type ResourceLoader() = 
        interface IResourceLoader with
            
            member this.LoadResourceAsString(resourceName) = 
                let path = Utility.Helpers.GenerateAbsolutePath(".\\conf\\" + resourceName)
                Utility.Helpers.LoadFile(path)
            
            member this.LoadResourceAsList(resourceName) = 
                let path = Utility.Helpers.GenerateAbsolutePath(".\\conf\\" + resourceName)
                let readLines = System.IO.File.ReadLines(path)
                let result = new List<string>()
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false && line.StartsWith("#") = false then 
                        result.Add(line.Trim().ToLowerInvariant())
                result
            
            member this.LoadResourceAsMap(resourceName) = 
                let path = Utility.Helpers.GenerateAbsolutePath(".\\conf\\" + resourceName)
                let readLines = System.IO.File.ReadLines(path)
                let result = new List<string []>()
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false && line.StartsWith("#") = false then 
                        let lineLower = line.ToLowerInvariant()
                        let values = lineLower.Split([| ":"; "," |], System.StringSplitOptions.RemoveEmptyEntries)
                        if values.Length > 1 then result.Add(values)
                result
    
    // ----------------------------------------------------------------------------
    // Concerete implementation of IFlexFactory
    // ----------------------------------------------------------------------------    
    type FlexFactory<'a>(container : CompositionContainer, moduleType) as self = 
        
        [<ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)>]
        let factory : seq<ExportFactory<'a, IFlexMetaData>> = null
        
        do container.ComposeParts(self)
        interface IFlexFactory<'a> with
            
            member this.GetModuleByName(moduleName) = 
                if (System.String.IsNullOrWhiteSpace(moduleName)) then 
                    Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.MODULE_NOT_FOUND, moduleName))
                else 
                    let injectMeta = 
                        factory.FirstOrDefault
                            (fun a -> String.Equals(a.Metadata.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                    match injectMeta with
                    | null -> 
                        Choice2Of2(OperationMessage.WithPropertyName(MessageConstants.MODULE_NOT_FOUND, moduleName))
                    | _ -> Choice1Of2(injectMeta.CreateExport().Value)
            
            member this.ModuleExists(moduleName) = 
                let injectMeta = 
                    factory.FirstOrDefault
                        (fun a -> String.Equals(a.Metadata.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                match injectMeta with
                | null -> false
                | _ -> true
            
            member this.GetAllModules() = 
                let modules = new Dictionary<string, 'a>(StringComparer.OrdinalIgnoreCase)
                factory |> Seq.iter (fun x -> modules.Add(x.Metadata.Name, x.CreateExport().Value))
                modules
    
    /// Loads all the http modules
    let GetHttpModules() = 
        lazy (let httpModule = 
                  new FlexFactory<IHttpModule>(PluginContainer(false).Value, "HttpModule") :> IFlexFactory<IHttpModule>
              httpModule.GetAllModules())
    
    // ---------------------------------------------------------------------------- 
    // Concerete implementation of IFactoryCollection
    // ---------------------------------------------------------------------------- 
    type FactoryCollection(container : CompositionContainer) = 
        let filterFactory = new FlexFactory<IFlexFilterFactory>(container, "Filter") :> IFlexFactory<IFlexFilterFactory>
        let tokenizerFactory = 
            new FlexFactory<IFlexTokenizerFactory>(container, "Tokenizer") :> IFlexFactory<IFlexTokenizerFactory>
        let analyzerFactory = new FlexFactory<Analyzer>(container, "Analyzer") :> IFlexFactory<Analyzer>
        //let searchQueryFactory = new FlexFactory<IFlexQuery>(container, "Query") :> IFlexFactory<IFlexQuery>
        let computationOpertionFactory = 
            new FlexFactory<IComputationOperation>(container, "Computation Operation") :> IFlexFactory<IComputationOperation>
        //let pluginsFactory = new FlexFactory<IPlugin>(container, "Computation Operation") :> IFlexFactory<IPlugin>
        let scriptFactory = new CompilerService.ScriptFactoryCollection() :> IScriptFactoryCollection
        let resourceLoader = new ResourceLoader() :> IResourceLoader
        interface IFactoryCollection with
            member this.FilterFactory = filterFactory
            member this.TokenizerFactory = tokenizerFactory
            member this.AnalyzerFactory = analyzerFactory
            //member this.SearchQueryFactory = searchQueryFactory
            member this.ComputationOpertionFactory = computationOpertionFactory
            //member this.PluginsFactory = pluginsFactory
            member this.ScriptFactoryCollection = scriptFactory
            member this.ResourceLoader = resourceLoader
