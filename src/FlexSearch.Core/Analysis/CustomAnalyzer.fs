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

open FlexSearch.Core.Interface
open FlexSearch.Core

open java.io
open java.util

open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.charfilter
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.phonetic
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.util

open System.Collections.Generic
open System.ComponentModel.Composition
open System.Linq

// ----------------------------------------------------------------------------
// Custom analyzer which can take any combination of filters
// ----------------------------------------------------------------------------
[<Sealed>]
type CustomAnalyzer(tokenizerFactory: IFlexTokenizerFactory, filterFactories: IFlexFilterFactory[]) =
    inherit Analyzer()
    do
        if filterFactories.Count() = 0 then
            failwithf "Atleast 1 filter must be specified"

    override this.createComponents(fieldName: string, reader: Reader) =
        let source = tokenizerFactory.Create(reader)
        let mutable result = filterFactories.First().Create(source)
        for filterFactory in filterFactories.Skip(1) do
            result <- filterFactory.Create(result)
        new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)
            

