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
namespace FlexSearch.Core

open FlexSearch.Core
open System.Collections.Generic
open System.ComponentModel.Composition
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.util

// ----------------------------------------------------------------------------
// Contains all predefined tokenizers. The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------
/// <summary>
/// Keyword Tokenizer
/// </summary>
[<Name("KeywordTokenizer")>]
[<Sealed>]
type KeywordTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new KeywordTokenizer(reader) :> Tokenizer

/// <summary>
/// Standard Tokenizer
/// </summary>
[<Name("StandardTokenizer")>]
[<Sealed>]
type StandardTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new StandardTokenizer(Constants.LuceneVersion, reader) :> Tokenizer

/// <summary>
/// Classic Tokenizer 
/// </summary>
[<Name("ClassicTokenizer")>]
[<Sealed>]
type ClassicTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new ClassicTokenizer(Constants.LuceneVersion, reader) :> Tokenizer

/// <summary>
/// Lowercase Tokenizer
/// </summary>
[<Name("LowercaseTokenizer")>]
[<Sealed>]
type LowercaseTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new LowerCaseTokenizer(Constants.LuceneVersion, reader) :> Tokenizer

/// <summary>
/// Letter Tokenizer
/// </summary>
[<Name("LetterTokenizer")>]
[<Sealed>]
type LetterTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new LetterTokenizer(Constants.LuceneVersion, reader) :> Tokenizer

/// <summary>
/// Whitespace Tokenizer
/// </summary>
[<Name("WhitespaceTokenizer")>]
[<Sealed>]
type WhitespaceTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new WhitespaceTokenizer(Constants.LuceneVersion, reader) :> Tokenizer

/// <summary>
/// UAX29URLEmail Tokenizer
/// </summary>
[<Name("UAX29URLEmailTokenizer")>]
[<Sealed>]
type UAX29URLEmailTokenizerFactory() = 
    interface IFlexTokenizerFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(reader : Reader) = new UAX29URLEmailTokenizer(Constants.LuceneVersion, reader) :> Tokenizer
