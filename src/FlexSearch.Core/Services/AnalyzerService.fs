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

open FlexSearch.Api
open FlexSearch.Api.Messages
open FlexSearch.Api.Validation
open FlexSearch.Common
open FlexSearch.Core
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Linq
open org.apache.lucene.search

[<Sealed>]
/// <summary>
/// Service wrapper around all analyzer/analysis related services
/// </summary>
type AnalyzerService(factoryService : IFactoryCollection, threadSafeWriter : IThreadSafeWriter, logger : ILogService, serverSettings : ServerSettings) = 
    let analyzerRepository = 
        new ConcurrentDictionary<string, org.apache.lucene.analysis.Analyzer>(StringComparer.OrdinalIgnoreCase)
    
    let AddOrUpdateAnalyzer(analyzerInfo : Analyzer) = 
        maybe { 
            let! instance = analyzerInfo.Build(analyzerInfo.AnalyzerName, factoryService)
            // Update the content without any comparison
            analyzerRepository.AddOrUpdate(analyzerInfo.AnalyzerName, instance, fun key oldValue -> instance) |> ignore
            do! threadSafeWriter.WriteFile
                    (Path.Combine(serverSettings.ConfFolder, "analyzers", analyzerInfo.AnalyzerName), analyzerInfo)
        }
    
    let GetAnalyzer(analyzerName : string) = 
        match analyzerRepository.TryGetValue(analyzerName) with
        | true, a -> Choice1Of2(a)
        | _ -> 
            Choice2Of2("ANALYZER_NOT_FOUND:Analyzer not found."
                       |> GenerateOperationMessage
                       |> Append("AnalyzerName", analyzerName))
    
    let DeleteAnalyzer(analyzerName : string) = 
        match analyzerRepository.TryRemove(analyzerName) with
        | true, _ -> 
            threadSafeWriter.DeleteFile(Path.Combine(serverSettings.ConfFolder, "analyzers", analyzerName)) |> ignore
            Choice1Of2()
        | _ -> 
            Choice2Of2("UNABLE_TO_REMOVE_ANALYZER:Analyzer can not be removed."
                       |> GenerateOperationMessage
                       |> Append("AnalyzerName", analyzerName))
    
    let LoadAllAnalyzers() = 
        Directory.CreateDirectory(Path.Combine(serverSettings.ConfFolder, "analyzers")) |> ignore
        // Load all custom analyzer
        for file in Directory.EnumerateFiles(Path.Combine(serverSettings.ConfFolder, "analyzers"), "*.yml") do
            match threadSafeWriter.ReadFile<Analyzer>(file) with
            | Choice1Of2(analyzerInfo) -> 
                let analyzerInstance = analyzerInfo.Build(analyzerInfo.AnalyzerName, factoryService)
                match analyzerInstance with
                | Choice1Of2(a) -> analyzerRepository.TryAdd(analyzerInfo.AnalyzerName, a) |> ignore
                | Choice2Of2(e) -> 
                    logger.ComponentInitializationFailed(analyzerInfo.AnalyzerName, "Analyzer", e.ToString())
            | Choice2Of2(e) -> logger.ComponentInitializationFailed(file, "Analyzer", e.ToString())
        // Load all out of box analyzers
        factoryService.AnalyzerFactory.GetAllModules() 
        |> Seq.iter (fun x -> analyzerRepository.TryAdd(x.Key, x.Value) |> ignore)
    
    do LoadAllAnalyzers()
    interface IAnalyzerService with
        member x.AddOrUpdateAnalyzer(analyzer) = AddOrUpdateAnalyzer(analyzer)
        member x.Analyze(analyzerName : string, input : string) = failwith "Not implemented yet"
        member x.DeleteAnalyzer(analyzerName : string) = DeleteAnalyzer(analyzerName)
        member x.GetAllAnalyzers() = Choice1Of2(analyzerRepository.Values.ToList())
        member x.GetAnalyzer(analyzerName : string) = GetAnalyzer(analyzerName)
