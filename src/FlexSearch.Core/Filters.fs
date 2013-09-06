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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
            member this.Create(ts: TokenStream) =
                new StandardFilter(Constants.LuceneVersion, ts) :> TokenStream


    // ----------------------------------------------------------------------------
    // BeiderMorse Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "BeiderMorseFilter")>]
    type BeiderMorseFilterFactory() =
        let phoneticEngine = new PhoneticEngine(NameType.GENERIC, RuleType.APPROX, true)
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                let fileName =
                    match parameters.TryGetValue("filename") with
                    | (true, a) -> a
                    | _ -> failwithf "message='Filename' property is required by the Keepword filter."
                
                if (File.Exists(Constants.ConfFolder.Value + fileName) = false) then
                    failwithf "message='Filename' specifed in the Keepword filter configuration does not exist in the conf folder.; filename=%s" fileName

                let readLines = System.IO.File.ReadLines(Constants.ConfFolder.Value + fileName)
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false then
                        keepWords.Add(line.Trim())
                true
                           
            member this.Create(ts: TokenStream) =
                new KeepWordFilter(Constants.LuceneVersion, ts, keepWords) :> TokenStream
                

    // ----------------------------------------------------------------------------
    // KeepWordsFilter Filter 
    // ----------------------------------------------------------------------------    
    [<Export(typeof<IFlexFilterFactory>)>]
    [<PartCreationPolicy(CreationPolicy.NonShared)>]
    [<ExportMetadata("Name", "LengthFilter")>]
    type LengthFilterFactory() =
        let mutable min = 0
        let mutable max = 0
        interface IFlexFilterFactory with
            member this.Initialize(parameters: Dictionary<string,string>) =
                min <-
                    match parameters.TryGetValue("min") with
                    | (true, a) -> 
                        match System.Int32.TryParse(a) with
                        | (true, b) -> b
                        | _ -> failwithf "message=For Length filter 'min' property should be an integer." 
                    | _ -> failwithf "message='min' property is required by the Length filter."
                
                max <-
                    match parameters.TryGetValue("max") with
                    | (true, a) -> 
                        match System.Int32.TryParse(a) with
                        | (true, b) -> b
                        | _ -> failwithf "message=For Length filter'min' property should be an integer." 
                    | _ -> failwithf "message='min' property is required by the Length filter."
                
                true
                           
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                pattern <-
                    match parameters.TryGetValue("pattern") with
                    | (true, a) -> Pattern.compile(a)
                    | _ -> failwithf "message='pattern' value is required by Pattern Replace filter."
                
                replaceText <-
                    match parameters.TryGetValue("replacementtext") with
                    | (true, a) -> a
                    | _ -> failwithf "message='replacementtext' value is required by Pattern Replace filter."

                true

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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                let fileName =
                    match parameters.TryGetValue("filename") with
                    | (true, a) -> a
                    | _ -> failwithf "message='Filename' property is required by the stop word filter."
                
                if (File.Exists(Constants.ConfFolder.Value + fileName) = false) then
                    failwithf "message='Filename' specifed in the stop word filter configuration does not exist in the conf folder.; filename=%s" fileName

                let readLines = System.IO.File.ReadLines(Constants.ConfFolder.Value + fileName)
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false then
                        stopWords.Add(line.Trim())
                true
                           
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                let fileName =
                    match parameters.TryGetValue("filename") with
                    | (true, a) -> a
                    | _ -> failwithf "message='Filename' property is required by the Synonym filter."
                
                if (File.Exists(Constants.ConfFolder.Value + fileName) = false) then
                    failwithf "message='Filename' specifed in the Synonym filter configuration does not exist in the conf folder.; filename=%s" fileName

                let readLines = System.IO.File.ReadLines(Constants.ConfFolder.Value + fileName)
                let builder = new SynonymMap.Builder(false)
                for line in readLines do
                    if System.String.IsNullOrWhiteSpace(line) = false then
                        let lineLower = line.ToLowerInvariant()
                        let values = lineLower.Split([|":"; ","|], System.StringSplitOptions.RemoveEmptyEntries)
                        if values.Length > 1 then
                            for i = 1 to values.Length - 1 do
                                builder.add(new CharsRef(values.[0]), new CharsRef(values.[i]), true)

                map <- builder.build()
                true
                           
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
            member this.Initialize(parameters: Dictionary<string,string>) =
                true
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