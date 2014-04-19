// ----------------------------------------------------------------------------
// Flexsearch predefined analyzers (Analyzers.fs)
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
// Contains all predefined analyzer. The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
[<AutoOpen>]
module Analyzers =
            
    // ----------------------------------------------------------------------------
    // Html analyzer which uses lucene character filter. Character Filters are not
    // supported by flex through configuration.
    // ----------------------------------------------------------------------------                
    [<Export(typeof<Analyzer>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "HtmlAnalyzer")>]
    type HtmlAnalyzer() =
        inherit Analyzer()
        override this.createComponents(fieldName: string, reader: Reader) =
            let charFilter = new HTMLStripCharFilter(reader)
            let source = new StandardTokenizer(Constants.LuceneVersion, charFilter)
            let result = new StandardFilter(Constants.LuceneVersion, source)
            new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)


    // ----------------------------------------------------------------------------
    // A phonetic analyzer using double metaphone filter
    // ----------------------------------------------------------------------------
    [<Export(typeof<Analyzer>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "DoubleMetaPhonePhoneticAnalyzer")>]
    type DoubleMetaPhonePhoneticAnalyzer() =
        inherit Analyzer()
        override this.createComponents(fieldName: string, reader: Reader) =
            let source = new StandardTokenizer(Constants.LuceneVersion, reader)
            let mutable result = new StandardFilter(Constants.LuceneVersion, source) :> TokenStream
            result <- new LowerCaseFilter(Constants.LuceneVersion, source)
            result <- new DoubleMetaphoneFilter(result, 4, false)
            new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)


    // ----------------------------------------------------------------------------
    // Wrapper around standard analyzer
    // ----------------------------------------------------------------------------
    [<Export(typeof<Analyzer>)>]
    [<ExportMetadata("Name", "StandardAnalyzer")>]
    let FlexStandardAnalyzer: Analyzer = new StandardAnalyzer(Constants.LuceneVersion) :> Analyzer


    // ----------------------------------------------------------------------------
    // Wrapper around keyword analyzer
    // ----------------------------------------------------------------------------
    [<Export(typeof<Analyzer>)>]
    [<ExportMetadata("Name", "KeywordAnalyzer")>]
    let FlexKeywordAnalyzer: Analyzer = new KeywordAnalyzer() :> Analyzer

