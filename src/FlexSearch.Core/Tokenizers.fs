// ----------------------------------------------------------------------------
// Flexsearch predefined tokenizers (Tokenizers.fs)
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
namespace FlexSearch.Analysis
// ----------------------------------------------------------------------------

open FlexSearch.Core

open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util

open java.io
open java.util

open System.ComponentModel.Composition
open System.Collections.Generic

// ----------------------------------------------------------------------------
// Contains all predefined tokenizers. The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
module Tokenizers =

    // ----------------------------------------------------------------------------
    // Keyword Tokenizer
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexTokenizerFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "KeywordTokenizer")>]
    type KeywordTokenizerFactory() =
        interface IFlexTokenizerFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(reader: Reader) =
                new KeywordTokenizer(reader) :> Tokenizer


    // ----------------------------------------------------------------------------
    // Standard Tokenizer
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexTokenizerFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "StandardTokenizer")>]
    type StandardTokenizerFactory() =
        interface IFlexTokenizerFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(reader: Reader) =
                new StandardTokenizer(Constants.LuceneVersion ,reader) :> Tokenizer


    // ----------------------------------------------------------------------------
    // Lowercase Tokenizer
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexTokenizerFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "LowercaseTokenizer")>]
    type LowercaseTokenizerFactory() =
        interface IFlexTokenizerFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(reader: Reader) =
                new LowerCaseTokenizer(Constants.LuceneVersion ,reader) :> Tokenizer


    // ----------------------------------------------------------------------------
    // Letter Tokenizer
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexTokenizerFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "LetterTokenizer")>]
    type LetterTokenizerFactory() =
        interface IFlexTokenizerFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(reader: Reader) =
                new LetterTokenizer(Constants.LuceneVersion ,reader) :> Tokenizer


    // ----------------------------------------------------------------------------
    // Whitespace Tokenizer
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexTokenizerFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "WhitespaceTokenizer")>]
    type WhitespaceTokenizerFactory() =
        interface IFlexTokenizerFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(reader: Reader) =
                new WhitespaceTokenizer(Constants.LuceneVersion ,reader) :> Tokenizer


    // ----------------------------------------------------------------------------
    // UAX29URLEmail Tokenizer
    // ---------------------------------------------------------------------------- 
    [<Export(typeof<IFlexTokenizerFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "UAX29URLEmailTokenizer")>]
    type UAX29URLEmailTokenizerFactory() =
        interface IFlexTokenizerFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(reader: Reader) =
                new UAX29URLEmailTokenizer(Constants.LuceneVersion ,reader) :> Tokenizer