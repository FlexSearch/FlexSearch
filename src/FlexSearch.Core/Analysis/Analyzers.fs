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
namespace FlexSearch.Core

open FlexSearch.Core
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Linq
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.charfilter
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.phonetic
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.util

// ----------------------------------------------------------------------------
// Contains all predefined analyzer. The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------

/// <summary>
/// HTML analyzer which uses Lucene character filter. Character Filters are not
/// supported by flex through configuration.
/// </summary>
[<Name("HtmlAnalyzer")>]
[<Sealed>]
type HtmlAnalyzer() = 
    inherit Analyzer()
    override this.createComponents (fieldName : string, reader : Reader) = 
        let charFilter = new HTMLStripCharFilter(reader)
        let source = new StandardTokenizer(charFilter)
        let result = new StandardFilter(source)
        new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)
    
/// <summary>
/// A phonetic analyzer using double meta-phone filter
/// </summary>
[<Name("DoubleMetaPhonePhoneticAnalyzer")>]
[<Sealed>]
type DoubleMetaPhonePhoneticAnalyzer() = 
    inherit Analyzer()
    override this.createComponents (fieldName : string, reader : Reader) = 
        let source = new StandardTokenizer(reader)
        let mutable result = new StandardFilter(source) :> TokenStream
        result <- new LowerCaseFilter(source)
        result <- new DoubleMetaphoneFilter(result, 4, false)
        new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)
    
/// <summary>
/// Wrapper around standard analyzer
/// </summary>
[<Name("StandardAnalyzer")>]
[<Sealed>]
type FlexStandardAnalyzer() = 
    inherit Analyzer()
    override this.createComponents (fieldName : string, reader : Reader) = 
        let source = new StandardTokenizer(reader)
        let mutable result = new StandardFilter(source) :> TokenStream
        result <- new LowerCaseFilter(source)
        new org.apache.lucene.analysis.Analyzer.TokenStreamComponents(source, result)
