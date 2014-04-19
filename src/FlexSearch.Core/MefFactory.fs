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

// ----------------------------------------------------------------------------
// Contains MEF container and other factory implementation
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Factories = 
    /// <summary>
    /// MEF container which loads all the related plug-ins
    /// </summary>
    /// <param name="readPluginDirectory">Whether to load plug-ins from plug-in directory</param>
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
    
    /// <summary>
    /// Concrete implementation of IResourceLoader
    /// </summary>
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
    
    /// <summary>
    /// Concrete implementation of IFlexFactory
    /// </summary>
    type FlexFactory<'a>(container : CompositionContainer, moduleType) as self = 
        
        [<ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)>]
        let factory : seq<ExportFactory<'a, IFlexMetaData>> = null
        
        do 
            container.ComposeParts(self)
            factory |> Seq.iter (fun x -> Logger.MefComponentLoaded(x.Metadata.Name, moduleType))
        
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
    
    /// <summary>
    /// Loads all the HTTP modules
    /// </summary>
    let GetHttpModules() = 
        lazy (let httpModule = 
                  new FlexFactory<HttpModuleBase>(PluginContainer(false).Value, "HttpModule") :> IFlexFactory<HttpModuleBase>
              httpModule.GetAllModules())
    
    /// <summary>
    /// Get all import handler modules
    /// </summary>
    let GetImportHandlerModules() = 
        lazy (let importModules = 
                  new FlexFactory<IImportHandler>(PluginContainer(false).Value, "ImportHandlersModule") :> IFlexFactory<IImportHandler>
              importModules.GetAllModules())
    
    /// <summary>
    /// Concrete implementation of IFactoryCollection
    /// </summary>
    type FactoryCollection(container : CompositionContainer) = 
        let filterFactory = new FlexFactory<IFlexFilterFactory>(container, "Filter") :> IFlexFactory<IFlexFilterFactory>
        let tokenizerFactory = 
            new FlexFactory<IFlexTokenizerFactory>(container, "Tokenizer") :> IFlexFactory<IFlexTokenizerFactory>
        let analyzerFactory = new FlexFactory<Analyzer>(container, "Analyzer") :> IFlexFactory<Analyzer>
        let searchQueryFactory = new FlexFactory<IFlexQuery>(container, "Query") :> IFlexFactory<IFlexQuery>
        let computationOperationFactory = 
            new FlexFactory<IComputationOperation>(container, "Computation Operation") :> IFlexFactory<IComputationOperation>
        let httpModuleFactory = 
            new FlexFactory<HttpModuleBase>(container, "HTTP modules") :> IFlexFactory<HttpModuleBase>
        let importHandlerFactory = 
            new FlexFactory<IImportHandler>(container, "Import Handler") :> IFlexFactory<IImportHandler>
        let scriptFactory = new CompilerService.ScriptFactoryCollection() :> IScriptFactoryCollection
        let resourceLoader = new ResourceLoader() :> IResourceLoader
        interface IFactoryCollection with
            member this.FilterFactory = filterFactory
            member this.TokenizerFactory = tokenizerFactory
            member this.AnalyzerFactory = analyzerFactory
            member this.SearchQueryFactory = searchQueryFactory
            member this.ComputationOperationFactory = computationOperationFactory
            member this.HttpModuleFactory = httpModuleFactory
            member this.ImportHandlerFactory = importHandlerFactory
            member this.ScriptFactoryCollection = scriptFactory
            member this.ResourceLoader = resourceLoader
