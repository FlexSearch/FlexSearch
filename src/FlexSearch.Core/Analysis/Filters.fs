// ----------------------------------------------------------------------------
// FlexSearch predefined filters (Filters.fs)
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
open FlexSearch.Common
open FlexSearch.Api
open FlexSearch.Utility
open System.Collections.Generic
open System.ComponentModel.Composition
open System.IO
open System.Linq
open java.util
open java.util.regex
open org.apache.commons.codec.language
open org.apache.commons.codec.language.bm
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.pattern
open org.apache.lucene.analysis.phonetic
open org.apache.lucene.analysis.reverse
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.synonym
open org.apache.lucene.analysis.util
open org.apache.lucene.util
open FlexSearch.Api.Validation

// ----------------------------------------------------------------------------
// Contains all predefined filters. The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------                        
/// <summary>
/// AsciiFolding Filter
/// </summary>
[<Name("AsciiFoldingFilter")>]
[<Sealed>]
type AsciiFoldingFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters : IDictionary<string, string>) = Choice1Of2()
        member this.Create(ts : TokenStream) = new ASCIIFoldingFilter(ts) :> TokenStream

/// <summary>
/// Standard Filter
/// </summary>
[<Name("StandardFilter")>]
[<Sealed>]
type StandardFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters : IDictionary<string, string>) = Choice1Of2()
        member this.Create(ts : TokenStream) = new StandardFilter(ts) :> TokenStream

/// <summary>
/// BeiderMorse Filter
/// </summary>
[<Name("BeiderMorseFilter")>]
[<Sealed>]
type BeiderMorseFilterFactory() = 
    let mutable phoneticEngine = null
    interface IFlexFilterFactory with
        
        member this.Initialize(parameters : IDictionary<string, string>) = 
            maybe { 
                let! nametype = match parameters.TryGetValue("nametype") with
                                | (true, a) -> 
                                    match a with
                                    | InvariantEqual "GENERIC" -> Choice1Of2(NameType.GENERIC)
                                    | InvariantEqual "ASHKENAZI" -> Choice1Of2(NameType.ASHKENAZI)
                                    | InvariantEqual "SEPHARDIC" -> Choice1Of2(NameType.SEPHARDIC)
                                    | _ -> 
                                        Choice2Of2(Errors.FILTER_CANNOT_BE_INITIALIZED
                                                   |> GenerateOperationMessage
                                                   |> Append("Filter Type", "BeiderMorseFilter")
                                                   |> Append("Message", "Specified 'nameType' property is invalid."))
                                | _ -> Choice1Of2(NameType.GENERIC)
                let! ruletype = match parameters.TryGetValue("ruletype") with
                                | (true, a) -> 
                                    match a with
                                    | InvariantEqual "APPROX" -> Choice1Of2(RuleType.APPROX)
                                    | InvariantEqual "EXACT" -> Choice1Of2(RuleType.EXACT)
                                    | _ -> 
                                        Choice2Of2(Errors.FILTER_CANNOT_BE_INITIALIZED
                                                   |> GenerateOperationMessage
                                                   |> Append("Filter Type", "BeiderMorseFilter")
                                                   |> Append("Message", "Specified 'ruleType' property is invalid."))
                                | _ -> Choice1Of2(RuleType.EXACT)
                return! Choice1Of2(phoneticEngine <- new PhoneticEngine(nametype, ruletype, true))
            }
        
        member this.Create(ts : TokenStream) = new BeiderMorseFilter(ts, phoneticEngine) :> TokenStream

/// <summary>
/// Capitalization Filter
/// </summary>
[<Name("CapitalizationFilter")>]
[<Sealed>]
type CapitalizationFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new CapitalizationFilter(ts) :> TokenStream

/// <summary>
/// Caverphone2 Filter
/// </summary>
[<Name("Caverphone2Filter")>]
[<Sealed>]
type Caverphone2FilterFactory() = 
    let caverphone = new Caverphone2()
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new PhoneticFilter(ts, caverphone, false) :> TokenStream

/// <summary>
/// MetaphoneFilter Filter
/// </summary>
[<Name("MetaphoneFilter")>]
[<Sealed>]
type MetaphoneFilterFactory() = 
    let metaphone = new Metaphone()
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new PhoneticFilter(ts, metaphone, false) :> TokenStream

/// <summary>
/// Caverphone2 Filter 
/// </summary>
[<Name("DoubleMetaphoneFilter")>]
[<Sealed>]
type DoubleMetaphoneFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new DoubleMetaphoneFilter(ts, 4, false) :> TokenStream

/// <summary>
/// RefinedSoundex Filter 
/// </summary>
[<Name("RefinedSoundexFilter")>]
[<Sealed>]
type RefinedSoundexFilterFactory() = 
    let refinedSoundex = new RefinedSoundex()
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new PhoneticFilter(ts, refinedSoundex, false) :> TokenStream

/// <summary>
/// Soundex Filter
/// </summary>
[<Name("SoundexFilter")>]
[<Sealed>]
type SoundexFilterFactory() = 
    let soundex = new Soundex()
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new PhoneticFilter(ts, soundex, false) :> TokenStream

/// <summary>
/// Length Filter
/// </summary>
[<Name("LengthFilter")>]
[<Sealed>]
type LengthFilterFactory() = 
    let mutable min = 0
    let mutable max = 0
    interface IFlexFilterFactory with
        
        member this.Initialize(parameters) = 
            maybe { 
                let! minValue = ParseValueAsInteger
                                    ("min", parameters, Errors.FILTER_CANNOT_BE_INITIALIZED |> GenerateOperationMessage)
                let! maxValue = ParseValueAsInteger
                                    ("max", parameters, Errors.FILTER_CANNOT_BE_INITIALIZED |> GenerateOperationMessage)
                min <- minValue
                max <- maxValue
                return! Choice1Of2()
            }
        
        member this.Create(ts : TokenStream) = new LengthFilter(ts, min, max) :> TokenStream

/// <summary>
/// LowerCase Filter
/// </summary>
[<Name("LowerCaseFilter")>]
[<Sealed>]
type LowerCaseFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new LowerCaseFilter(ts) :> TokenStream

/// <summary>
/// PatternReplace Filter
/// </summary>
[<Name("PatternReplaceFilter")>]
[<Sealed>]
type PatternReplaceFilterFactory() = 
    let mutable pattern : Pattern = null
    let mutable replaceText : string = ""
    interface IFlexFilterFactory with
        
        member this.Initialize(parameters) = 
            maybe { 
                let! patternValue = KeyExists
                                        ("pattern", parameters, 
                                         Errors.FILTER_CANNOT_BE_INITIALIZED |> GenerateOperationMessage)
                let! replaceTextValue = KeyExists
                                            ("replacementtext", parameters, 
                                             Errors.FILTER_CANNOT_BE_INITIALIZED |> GenerateOperationMessage)
                replaceText <- replaceTextValue
                pattern <- Pattern.compile (patternValue)
                return! Choice1Of2()
            }
        
        member this.Create(ts : TokenStream) = new PatternReplaceFilter(ts, pattern, replaceText, true) :> TokenStream

/// <summary>
/// RemoveDuplicatesToken Filter 
/// </summary>
[<Name("RemoveDuplicatesTokenFilter")>]
[<Sealed>]
type RemoveDuplicatesTokenFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new RemoveDuplicatesTokenFilter(ts) :> TokenStream

/// <summary>
/// ReverseString Filter 
/// </summary>
[<Name("ReverseStringFilter")>]
[<Sealed>]
type ReverseStringFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new ReverseStringFilter(ts) :> TokenStream

/// <summary>
/// KeepWordsFilter Filter
/// </summary>
[<Name("KeepWordsFilter")>]
[<Sealed>]
type KeepWordsFilterFactory(resourceService : IResourceService) = 
    let keepWords : CharArraySet = new CharArraySet(100, true)
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = maybe { let! fileName = KeyExists
                                                                         ("resourceName", parameters, 
                                                                          
                                                                          Errors.FILTER_CANNOT_BE_INITIALIZED 
                                                                          |> GenerateOperationMessage)
                                                     let! filterList = resourceService.GetResource<FilterList>(fileName)
                                                     filterList.Words |> Seq.iter (fun x -> keepWords.Add(x)) }
        member this.Create(ts : TokenStream) = new KeepWordFilter(ts, keepWords) :> TokenStream

/// <summary>
/// Stop Filter
/// </summary>
[<Name("StopFilter")>]
[<Sealed>]
type StopFilterFactory(resourceService : IResourceService) = 
    let stopWords : CharArraySet = new CharArraySet(100, true)
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = maybe { let! fileName = KeyExists
                                                                         ("resourceName", parameters, 
                                                                          
                                                                          Errors.FILTER_CANNOT_BE_INITIALIZED 
                                                                          |> GenerateOperationMessage)
                                                     let! filterList = resourceService.GetResource<FilterList>(fileName)
                                                     filterList.Words |> Seq.iter (fun x -> stopWords.Add(x)) }
        member this.Create(ts : TokenStream) = new StopFilter(ts, stopWords) :> TokenStream

/// <summary>
/// Stop Filter 
/// </summary>
[<Name("SynonymFilter")>]
[<Sealed>]
type SynonymFilter(resourceService : IResourceService) = 
    let mutable map : SynonymMap = null
    interface IFlexFilterFactory with
        
        member this.Initialize(parameters) = 
            maybe { 
                let! fileName = KeyExists
                                    ("resourceName", parameters, 
                                     Errors.FILTER_CANNOT_BE_INITIALIZED |> GenerateOperationMessage)
                let! mapList = resourceService.GetResource<MapList>(fileName)
                let builder = new SynonymMap.Builder(false)
                mapList.Words |> Seq.iter (fun x -> 
                                     for value in x.Value do
                                         builder.add (new CharsRef(x.Key), new CharsRef(value), true))
                map <- builder.build()
            }
        
        member this.Create(ts : TokenStream) = 
            new org.apache.lucene.analysis.synonym.SynonymFilter(ts, map, true) :> TokenStream

/// <summary>
/// Trim Filter 
/// </summary>
[<Name("TrimFilter")>]
[<Sealed>]
type TrimFilterFactory() = 
    interface IFlexFilterFactory with
        member this.Initialize(parameters) = Choice1Of2()
        member this.Create(ts : TokenStream) = new TrimFilter(ts) :> TokenStream
