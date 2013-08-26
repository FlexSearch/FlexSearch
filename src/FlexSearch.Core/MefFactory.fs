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

// ----------------------------------------------------------------------------
namespace FlexSearch
// ----------------------------------------------------------------------------

open FlexSearch.Core
open FlexSearch.Core.Index

open org.apache.lucene.analysis
open org.apache.lucene.analysis.miscellaneous

open ServiceStack.WebHost.Endpoints
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
    
    let factoryLogger = ServiceStack.Logging.LogManager.GetLogger("Factory")

    // Mef container which loads all the related plugins
    let PluginContainer(readPluginDirectory)  =
        lazy
        (
            // A catalog that can aggregate other catalogs  
            let aggrCatalog = new AggregateCatalog()

            // An assembly catalog to load information about part from this assembly  
            let asmCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

            // A directory catalog, to load parts from dlls in the Extensions folder
            if readPluginDirectory then
                let dirCatalog = new DirectoryCatalog(Constants.PluginFolder.Force(), "*.dll")
                aggrCatalog.Catalogs.Add(dirCatalog);  
                       
            aggrCatalog.Catalogs.Add(asmCatalog);

            // Create a container  
            new CompositionContainer(aggrCatalog, CompositionOptions.IsThreadSafe)
        )

    
    // ----------------------------------------------------------------------------
    // Concerete implementation of IFlexFactory
    // ----------------------------------------------------------------------------    
    type FlexFactory<'a>(container: CompositionContainer, moduleType) as self = 
        
        [<ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)>]
        let factory : seq<ExportFactory<'a, IFlexMetaData>> = null
        
        do 
            container.ComposeParts(self)
            for operation in factory do
                factoryLogger.Info(sprintf "Discovered %s module: %s" moduleType operation.Metadata.Name)
            
        interface IFlexFactory<'a> with
            member this.GetModuleByName(moduleName) = 
                if(System.String.IsNullOrWhiteSpace(moduleName)) then 
                    None
                else    
                    let injectMeta = factory.FirstOrDefault(fun a -> String.Equals(a.Metadata.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                    match injectMeta with
                    | null -> None
                    | _ -> Some(injectMeta.CreateExport().Value)

            member this.ModuleExists(moduleName) = 
                let injectMeta = factory.FirstOrDefault(fun a -> String.Equals(a.Metadata.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                match injectMeta with
                | null -> false
                | _ -> true

            member this.GetAllModules() = 
                let modules = new Dictionary<string, 'a>(StringComparer.OrdinalIgnoreCase)
                factory |>
                    Seq.iter(fun x ->
                        modules.Add(x.Metadata.Name, x.CreateExport().Value))                
                modules   
    

    // ---------------------------------------------------------------------------- 
    // Concerete implementation of IFactoryCollection
    // ---------------------------------------------------------------------------- 
    type FactoryCollection(container: CompositionContainer) =
        let filterFactory = new FlexFactory<IFlexFilterFactory>(container, "Filter") :> IFlexFactory<IFlexFilterFactory>
        let tokenizerFactory = new FlexFactory<IFlexTokenizerFactory>(container, "Tokenizer") :> IFlexFactory<IFlexTokenizerFactory>
        let analyzerFactory = new FlexFactory<Analyzer>(container, "Analyzer") :> IFlexFactory<Analyzer>
        let searchQueryFactory = new FlexFactory<IFlexQuery>(container, "Query") :> IFlexFactory<IFlexQuery>
        let computationOpertionFactory = new FlexFactory<IComputationOperation>(container, "Computation Operation") :> IFlexFactory<IComputationOperation>
        let pluginsFactory = new FlexFactory<IPlugin>(container, "Computation Operation") :> IFlexFactory<IPlugin>
        let scriptFactory = new CompilerService.ScriptFactoryCollection() :> IScriptFactoryCollection

        interface IFactoryCollection with 
            member this.FilterFactory = filterFactory
            member this.TokenizerFactory = tokenizerFactory
            member this.AnalyzerFactory = analyzerFactory
            member this.SearchQueryFactory = searchQueryFactory
            member this.ComputationOpertionFactory = computationOpertionFactory           
            member this.PluginsFactory = pluginsFactory
            member this.ScriptFactoryCollection = scriptFactory