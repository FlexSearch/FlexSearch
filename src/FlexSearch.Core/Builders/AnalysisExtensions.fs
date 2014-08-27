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

[<AutoOpen>]
module AnalysisExtensions = 
    open FlexSearch.Api
    open FlexSearch.Core
    open FlexSearch.Utility
    open System.Collections.Generic
    open System
    open org.apache.lucene.analysis

    // ----------------------------------------------------------------------------
    // FlexSearch related validation helpers
    // ----------------------------------------------------------------------------
    let private MustGenerateFilterInstance (factoryCollection : IFactoryCollection) 
        (propName : string, value : FlexSearch.Api.TokenFilter) = 
        match factoryCollection.FilterFactory.GetModuleByName(value.FilterName) with
        | Choice1Of2(instance) -> instance.Initialize(value.Parameters)
        | _ -> Choice2Of2(Errors.FILTER_NOT_FOUND |> GenerateOperationMessage |> Append("Filter Name", propName))
    
    let private MustGenerateTokenizerInstance (factoryCollection : IFactoryCollection) 
        (propName : string, value : FlexSearch.Api.Tokenizer) = 
        match factoryCollection.TokenizerFactory.GetModuleByName(value.TokenizerName) with
        | Choice1Of2(instance) -> instance.Initialize(value.Parameters)
        | _ -> Choice2Of2(Errors.TOKENIZER_NOT_FOUND  |> GenerateOperationMessage |> Append("Tokenizer Name", propName))
    
//    type open FlexSearch.Api.TokenFilter with
//        
//        /// <summary>
//        /// Filter validator which checks both the input parameters and naming convention
//        /// </summary>
//        /// <param name="factoryCollection"></param>
//        member this.Validate(factoryCollection : IFactoryCollection) = ()
////            maybe { 
//////                do! this.FilterName.ValidatePropertyValue("FilterName")
//////                do! ("FilterName", this) |> MustGenerateFilterInstance factoryCollection
////            }
//        
//        /// <summary>
//        /// Build a FilterFactory from TokenFilter
//        /// </summary>
//        /// <param name="factoryCollection"></param>
//        member this.Build(factoryCollection : IFactoryCollection) = ()
////            maybe { 
////                do! this.Validate(factoryCollection)
////                let! filterFactory = factoryCollection.FilterFactory.GetModuleByName(this.FilterName)
////                do! filterFactory.Initialize(this.Parameters)
////                return filterFactory
////            }
//        
//        static member Build(filters : List<open FlexSearch.Api.TokenFilter>, factoryCollection : IFactoryCollection) = 
////            maybe { 
////                let result = new List<IFlexFilterFactory>()
////                for filter in filters do
////                    let! filterFactory = filter.Build(factoryCollection)
////                    result.Add(filterFactory)
////                return result
////            }
//    
//    type open FlexSearch.Api.Tokenizer with
//        
//        /// <summary>
//        /// Tokenizer validator which checks both the input parameters and naming convention
//        /// </summary>
//        /// <param name="factoryCollection"></param>
//        member this.Validate(factoryCollection : IFactoryCollection) = 
//            maybe { 
//                do! this.TokenizerName.ValidatePropertyValue("TokenizerName")
//                do! ("TokenizerName", this) |> MustGenerateTokenizerInstance factoryCollection
//            }
//        
//        /// <summary>
//        /// Build a TokenizerFactory from Tokenizer
//        /// </summary>
//        /// <param name="factoryCollection"></param>
//        member this.Build(factoryCollection : IFactoryCollection) = 
//            maybe { 
//                do! this.Validate(factoryCollection)
//                let! tokenizerFactory = factoryCollection.TokenizerFactory.GetModuleByName(this.TokenizerName)
//                do! tokenizerFactory.Initialize(this.Parameters)
//                return tokenizerFactory
//            }
//    
//    type open FlexSearch.Api.AnalyzerProperties with
//        
//        /// <summary>
//        /// Tokenizer validator which checks both the input parameters and naming convention
//        /// </summary>
//        /// <param name="factoryCollection"></param>
//        member this.Validate(analyzerName : string, factoryCollection : IFactoryCollection) = 
//            maybe { 
//                //do! this.Tokenizer.Validate(factoryCollection)
//                if this.Filters.Count = 0 then 
//                    return! Choice2Of2
//                                (Errors.ATLEAST_ONE_FILTER_REQUIRED |> Append("Analyzer Name", analyzerName))
//                for filter in this.Filters do
//                    do! filter.Validate(factoryCollection)
//            }
//        
//        /// <summary>
//        /// Return an analyzer from analyzer properties
//        /// </summary>
//        /// <param name="analyzerName"></param>
//        /// <param name="factoryCollection"></param>
//        member this.Build(analyzerName : string, factoryCollection : IFactoryCollection) = 
//            maybe { 
//                do! this.Validate(analyzerName, factoryCollection)
//                let! tokenizerFactory = this.Tokenizer.Build(factoryCollection)
//                let! filters = open FlexSearch.Api.TokenFilter.Build(this.Filters, factoryCollection)
//                return (new CustomAnalyzer(tokenizerFactory, filters.ToArray()) :> org.apache.lucene.analysis.Analyzer)
//            }
//        
//        /// <summary>
//        /// Build a dictionary of analyzers from analyzer properties
//        /// </summary>
//        /// <param name="analyzersDict"></param>
//        /// <param name="factoryCollection"></param>
//        static member Build(analyzersDict : Dictionary<string, open FlexSearch.Api.AnalyzerProperties>, 
//                            factoryCollection : IFactoryCollection) = 
//            maybe { 
//                let result = new Dictionary<string, Analyzer>(StringComparer.OrdinalIgnoreCase)
//                for analyzer in analyzersDict do
//                    do! analyzer.Key.ValidatePropertyValue("AnalyzerName")
//                    do! analyzer.Value.Validate(analyzer.Key, factoryCollection)
//                    let! analyzerObject = analyzer.Value.Build(analyzer.Key, factoryCollection)
//                    result.Add(analyzer.Key, analyzerObject)
//                return result
//            }
