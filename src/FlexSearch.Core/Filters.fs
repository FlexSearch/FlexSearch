// ----------------------------------------------------------------------------
// Flexsearch predefined filters (Filters.fs)
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
open FlexSearch.Utility

open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.util
open org.apache.commons.codec.language.bm
open org.apache.lucene.analysis.pattern
open org.apache.lucene.analysis.phonetic
open org.apache.commons.codec.language
open org.apache.lucene.analysis.reverse
open org.apache.lucene.analysis.synonym
open org.apache.lucene.util

open java.util
open java.util.regex

open System.IO
open System.ComponentModel.Composition
open System.Collections.Generic
open System.Linq

// ----------------------------------------------------------------------------
// Contains all predefined filters. The order of this file does not matter as
// all classes defined here are dynamically discovered using MEF
// ----------------------------------------------------------------------------                        
module Filters =
    
    // ----------------------------------------------------------------------------
    // AsciiFolding Filter 
    // ----------------------------------------------------------------------------
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "AsciiFoldingFilter")>]
    type AsciiFoldingFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new ASCIIFoldingFilter(ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // Standard Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "StandardFilter")>]
    type StandardFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new StandardFilter(Constants.LuceneVersion, ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // BeiderMorse Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "BeiderMorseFilter")>]
    type BeiderMorseFilterFactory() =
        let mutable phoneticEngine = null
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = 
                let nametype =
                    match parameters.TryGetValue("nametype") with
                    | (true, a) -> 
                        match a with
                        | InvariantEqual "GENERIC" -> NameType.GENERIC
                        | InvariantEqual "ASHKENAZI" -> NameType.ASHKENAZI
                        | InvariantEqual "SEPHARDIC" -> NameType.SEPHARDIC
                        | _ -> failwithf "message=Specified nametype is invalid."
                    | _ -> NameType.GENERIC

                let ruletype =
                    match parameters.TryGetValue("ruletype") with
                    | (true, a) -> 
                        match a with
                        | InvariantEqual "APPROX" -> RuleType.APPROX
                        | InvariantEqual "EXACT" -> RuleType.EXACT
                        | _ -> failwithf "message=Specified ruletype is invalid."
                    | _ -> RuleType.EXACT
                
                phoneticEngine <- new PhoneticEngine(nametype, ruletype, true)

            member this.Create(ts: TokenStream) =
                new BeiderMorseFilter(ts, phoneticEngine) :> TokenStream


    // ----------------------------------------------------------------------------
    // Capitalization Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "CapitalizationFilter")>]
    type CapitalizationFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new CapitalizationFilter(ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // Caverphone2 Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "Caverphone2Filter")>]
    type Caverphone2FilterFactory() =
        let caverphone = new Caverphone2()
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new PhoneticFilter(ts, caverphone, false) :> TokenStream


    // ----------------------------------------------------------------------------
    // MetaphoneFilter Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "MetaphoneFilter")>]
    type MetaphoneFilterFactory() =
        let metaphone = new Metaphone()
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new PhoneticFilter(ts, metaphone, false) :> TokenStream


    // ----------------------------------------------------------------------------
    // Caverphone2 Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "DoubleMetaphoneFilter")>]
    type DoubleMetaphoneFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new DoubleMetaphoneFilter(ts, 4, false) :> TokenStream


    // ----------------------------------------------------------------------------
    // RefinedSoundex Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "RefinedSoundexFilter")>]
    type RefinedSoundexFilterFactory() =
        let refinedSoundex = new RefinedSoundex()
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new PhoneticFilter(ts, refinedSoundex, false) :> TokenStream


    // ----------------------------------------------------------------------------
    // Soundex Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "SoundexFilter")>]
    type SoundexFilterFactory() =
        let soundex = new Soundex()
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new PhoneticFilter(ts, soundex, false) :> TokenStream


    // ----------------------------------------------------------------------------
    // KeepWordsFilter Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "KeepWordsFilter")>]
    type KeepWordsFilterFactory() =
        let keepWords: CharArraySet = new CharArraySet(Constants.LuceneVersion, 100, true) 
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) =
                let fileName = Helpers.KeyExists("filename", parameters)    
                resourceLoader.LoadResourceAsList(fileName)
                |> Seq.iter(fun x -> keepWords.Add(x))
           
            member this.Create(ts: TokenStream) =
                new KeepWordFilter(Constants.LuceneVersion, ts, keepWords) :> TokenStream
                

    // ----------------------------------------------------------------------------
    // Length Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "LengthFilter")>]
    type LengthFilterFactory() =
        let mutable min = 0
        let mutable max = 0
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) =
                min <- Helpers.ParseValueAsInteger("min", parameters)    
                max <- Helpers.ParseValueAsInteger("max", parameters)
                           
            member this.Create(ts: TokenStream) =
                new LengthFilter(Constants.LuceneVersion, ts, min, max) :> TokenStream


    // ----------------------------------------------------------------------------
    // LowerCase Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "LowerCaseFilter")>]
    type LowerCaseFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new LowerCaseFilter(Constants.LuceneVersion, ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // PatternReplace Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "PatternReplaceFilter")>]
    type PatternReplaceFilterFactory() =
        let mutable pattern : Pattern = null
        let mutable replaceText : string = ""
         
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) =
                pattern <- Pattern.compile(Helpers.KeyExists("pattern", parameters))                
                replaceText <- Helpers.KeyExists("replacementtext", parameters)
                    
            member this.Create(ts: TokenStream) =
                new PatternReplaceFilter(ts, pattern, replaceText, true) :> TokenStream


    // ----------------------------------------------------------------------------
    // RemoveDuplicatesToken Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "RemoveDuplicatesTokenFilter")>]
    type RemoveDuplicatesTokenFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new RemoveDuplicatesTokenFilter(ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // ReverseString Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "ReverseStringFilter")>]
    type ReverseStringFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new ReverseStringFilter(Constants.LuceneVersion, ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // Stop Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "StopFilter")>]
    type StopFilterFactory() =
        let stopWords: CharArraySet = new CharArraySet(Constants.LuceneVersion, 100, true) 
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) =
                let fileName = Helpers.KeyExists("filename", parameters)      
                resourceLoader.LoadResourceAsList(fileName)
                |> Seq.iter(fun x -> stopWords.Add(x))
                           
            member this.Create(ts: TokenStream) =
                new StopFilter(Constants.LuceneVersion, ts, stopWords) :> TokenStream


    // ----------------------------------------------------------------------------
    // Stop Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "SynonymFilter")>]
    type SynonymFilter() =
        let mutable map: SynonymMap = null
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) =
                let fileName = Helpers.KeyExists("filename", parameters) 
                let builder = new SynonymMap.Builder(false)
                resourceLoader.LoadResourceAsMap(fileName)
                |> Seq.iter(fun x -> 
                        for value in x.Skip(1) do
                            builder.add(new CharsRef(x.[0]), new CharsRef(value), true)
                    )                         

                map <- builder.build()
                           
            member this.Create(ts: TokenStream) =
                new org.apache.lucene.analysis.synonym.SynonymFilter(ts, map, true) :> TokenStream


    // ----------------------------------------------------------------------------
    // Trim Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "TrimFilter")>]
    type TrimFilterFactory() =
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>, resourceLoader: IResourceLoader) = ()
            member this.Create(ts: TokenStream) =
                new TrimFilter(Constants.LuceneVersion, ts) :> TokenStream   
                

    // ----------------------------------------------------------------------------
    // Word Delimiter Filter 
    // ----------------------------------------------------------------------------    
//    [<Export(typeof<IFlexFilterFactory>)>]
//    [<PartCreationPolicy(CreationPolicy.NonShared)>]
//    [<ExportMetadata("Name", "WordDelimiterFilter")>]
//    type WordDelimiterFilterFactory() =
//        interface IFlexFilterFactory with
//            member this.Initialize(parameters: Dictionary<string,string>) =
//                true
//            member this.Create(ts: TokenStream) =
//                new WordDelimiterFilter(ts) :> TokenStream    