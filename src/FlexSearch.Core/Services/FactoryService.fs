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
namespace FlexSearch.Core.Services

open Autofac
open Autofac.Features.Metadata
open FlexSearch.Api
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
// Contains container and other factory implementation
// ----------------------------------------------------------------------------
[<AutoOpen>]
module FactoryService = 
    /// <summary>
    /// Register all the interface assemblies
    /// </summary>
    /// <param name="builder"></param>
    let RegisterInterfaceAssemblies<'T>(builder : ContainerBuilder) = 
        builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies())
               .Where(fun t -> t.GetInterfaces().Any(fun i -> i.IsAssignableFrom(typeof<'T>))).AsImplementedInterfaces() 
        |> ignore
    
    /// <summary>
    /// Register all the abstract class assemblies
    /// </summary>
    /// <param name="builder"></param>
    let RegisterAbstractClassAssemblies<'T>(builder : ContainerBuilder) = 
        builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(fun t -> t.BaseType = typeof<'T>).As<'T>
            () |> ignore
    
    let RegisterSingleInstance<'T, 'U>(builder : ContainerBuilder) = 
        builder.RegisterType<'T>().As<'U>().SingleInstance() |> ignore
    
    /// <summary>
    /// Factory implementation
    /// </summary>
    [<Sealed>]
    type FlexFactory<'T>(container : ILifetimeScope) = 
        
        /// Returns a module by name.
        /// Choice1of3 -> instance of T
        /// Choice2of3 -> meta-data of T
        /// Choice3of3 -> error
        let getModuleByName (moduleName, metaOnly) = 
            if (System.String.IsNullOrWhiteSpace(moduleName)) then 
                Choice3Of3(Errors.MODULE_NOT_FOUND
                           |> GenerateOperationMessage
                           |> Append("Module Name", moduleName))
            else 
                // We cannot use a global instance of factory as it will cache the instances. We
                // need a new instance of T per request 
                let factory = container.Resolve<IEnumerable<Meta<Lazy<'T>>>>()
                let injectMeta = 
                    factory.FirstOrDefault
                        (fun a -> 
                        a.Metadata.Keys.Contains("Name") 
                        && String.Equals(a.Metadata.["Name"].ToString(), moduleName, StringComparison.OrdinalIgnoreCase))
                match injectMeta with
                | null -> 
                    Choice3Of3(Errors.MODULE_NOT_FOUND
                               |> GenerateOperationMessage
                               |> Append("Module Name", moduleName))
                | _ -> 
                    if metaOnly then Choice2Of3(injectMeta.Metadata)
                    else Choice1Of3(injectMeta.Value.Value)
        
        interface IFlexFactory<'T> with
            
            member this.GetModuleByName(moduleName) = 
                match getModuleByName (moduleName, false) with
                | Choice1Of3(x) -> Choice1Of2(x)
                | _ -> 
                    Choice2Of2(Errors.MODULE_NOT_FOUND
                               |> GenerateOperationMessage
                               |> Append("Module Name", moduleName))
            
            member this.GetMetaData(moduleName) = 
                match getModuleByName (moduleName, true) with
                | Choice2Of3(x) -> Choice1Of2(x)
                | _ -> 
                    Choice2Of2(Errors.MODULE_NOT_FOUND
                               |> GenerateOperationMessage
                               |> Append("Module Name", moduleName))
            
            member this.ModuleExists(moduleName) = 
                match getModuleByName (moduleName, true) with
                | Choice1Of3(_) -> false
                | _ -> true
            
            member this.GetAllModules() = 
                let modules = new Dictionary<string, 'T>(StringComparer.OrdinalIgnoreCase)
                let factory = container.Resolve<IEnumerable<Meta<Lazy<'T>>>>()
                factory 
                |> Seq.iter 
                       (fun x -> 
                       if x.Metadata.ContainsKey("Name") then modules.Add(x.Metadata.["Name"].ToString(), x.Value.Value))
                modules
    
    /// <summary>
    /// Concrete implementation of IFactoryCollection
    /// </summary>
    [<Sealed>]
    type FactoryCollection(filterFactory, tokenizerFactory, analyzerFactory, searchQueryFactory, importHandlerFactory) = 
        interface IFactoryCollection with
            member this.FilterFactory = filterFactory
            member this.TokenizerFactory = tokenizerFactory
            member this.AnalyzerFactory = analyzerFactory
            member this.SearchQueryFactory = searchQueryFactory
            member this.ImportHandlerFactory = importHandlerFactory
    
    let RegisterSingleFactoryInstance<'T>(builder : ContainerBuilder) = 
        builder.RegisterType<FlexFactory<'T>>().As<IFlexFactory<'T>>().SingleInstance() |> ignore
