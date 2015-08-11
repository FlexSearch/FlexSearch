// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexLucene.Analysis
open FlexLucene.Analysis.Standard
open FlexLucene.Analysis.Synonym
open FlexLucene.Analysis.Util
open System.Collections.Generic
open java.io

/// Represents a general purpose custom analyzer
/// This is similar to Lucene's custom analyzer but
/// provides more control over generation and error 
/// handling. In future this can be used to load
/// .net based analyzers and also to handle any
/// special cases.
[<Sealed>]
type FlexAnalyzer(tokenizerFactory : TokenizerFactory, filterFactories : TokenFilterFactory []) = 
    inherit Analyzer()
    override __.createComponents (_) = 
        let tk = tokenizerFactory.Create()
        let mutable ts = tk :> TokenStream
        for filter in filterFactories do
            ts <- filter.create (ts)
        new Analyzer.TokenStreamComponents(tk, ts)

type FlexSynonymFilterFactory(args) as self = 
    inherit TokenFilterFactory(args)
    let ignoreCase = self.getBoolean (args, "ignoreCase", true)
    let synonyms = self.Require(args, "synonyms")
    let expand = self.getBoolean (args, "expand", true)
    let dedupe = self.getBoolean (args, "dedupe", true)
    let mutable map = Unchecked.defaultof<_>
    
    do 
        let parser = new SolrSynonymParser(dedupe, expand, new StandardAnalyzer())
        let filePath = ResourcesFolder +/ synonyms
        if System.IO.File.Exists(filePath) then 
            let file = new FileReader(filePath)
            parser.Parse(file)
            map <- parser.build()
    
    override __.create (input) = new SynonymFilter(input, map, ignoreCase) :> TokenStream

[<AutoOpen>]
module Analysis = 
    open FlexLucene.Analysis.Custom
    
    let availableTokenizers = TokenizerFactory.AvailableTokenizers()
    let availableFilters = TokenFilterFactory.AvailableTokenFilters()
    
    // Load all required resources from the Resource folder
    let resourcePath = 
        let file = new java.io.File(ResourcesFolder +/ "tmp")
        file.toPath().getParent()
    
    /// Builds a Tokenizer Factory for a given Tokenizer
    let buildTokenizerFactory (analyzerName, dto : FlexSearch.Core.Tokenizer) = 
        match availableTokenizers.contains (dto.TokenizerName) with
        | true -> 
            try 
                ok <| TokenizerFactory.ForName(dto.TokenizerName, dictToMap dto.Parameters)
            with e ->
                UnableToInitializeTokenizer(analyzerName, dto.TokenizerName, e.Message, exceptionPrinter e)
                |> fail
                |> Logger.Log

        | false -> 
            TokenizerNotFound(analyzerName, dto.TokenizerName)
            |> fail
            |> Logger.Log
        
    /// Builds a Tokenizer Factory for a given Tokenizer
    let buildTokenFilterFactory (analyzerName, dto : FlexSearch.Core.TokenFilter) = 
        match availableFilters.contains (dto.FilterName) with
        | true -> 
            try 
                /// Handle any special cases first
                match dto.FilterName with
                | InvariantEqual "synonym" -> 
                    ok <| (new FlexSynonymFilterFactory(dictToMap dto.Parameters) :> TokenFilterFactory)
                | _ -> ok <| TokenFilterFactory.ForName(dto.FilterName, dictToMap dto.Parameters)
            with e -> 
                UnableToInitializeFilter(analyzerName, dto.FilterName, e.Message, exceptionPrinter e)
                |> fail
                |> Logger.Log
        | false -> 
            FilterNotFound(analyzerName, dto.FilterName)
            |> fail
            |> Logger.Log
             
    let applyResourceLoader (loader : ResourceLoader, factory : obj) = 
        let instance = castAs<ResourceLoaderAware> (factory)
        if notNull instance then instance.inform (loader)
    
    /// Builds a FlexAnalyzer from the Analyzer Dto
    let buildFromAnalyzerDto (dto : FlexSearch.Core.Analyzer) = 
        maybe { 
            do! dto.Validate()
            let loader = new FilesystemResourceLoader(resourcePath)
            let! tokenizer = buildTokenizerFactory (dto.AnalyzerName, dto.Tokenizer)
            applyResourceLoader (loader, tokenizer)
            let filters = new ResizeArray<TokenFilterFactory>()
            for filter in dto.Filters do
                let! instance = buildTokenFilterFactory (dto.AnalyzerName, filter)
                applyResourceLoader (loader, instance)
                filters.Add(instance)
            return new FlexAnalyzer(tokenizer, filters.ToArray()) :> Analyzer
        }
    
    /// Build a Lucene Analyzer from FlexSearch Analyzer DTO
    let buildUsingLuceneBuilder (def : FlexSearch.Core.Analyzer) = 
        // Load all required resources from the Resource folder
        let file = new java.io.File(ResourcesFolder +/ "tmp")
        let builder = CustomAnalyzer.Builder(file.toPath().getParent())
        try 
            builder.withTokenizer (def.Tokenizer.TokenizerName, dictToMap (def.Tokenizer.Parameters)) |> ignore
            def.Filters |> Seq.iter (fun f -> builder.addTokenFilter (f.FilterName, dictToMap (f.Parameters)) |> ignore)
            ok (builder.build() :> Analyzer)
        with ex -> 
            AnalyzerBuilder(def.AnalyzerName, ex.Message, exceptionPrinter (ex))
            |> fail 
            |> Logger.Log
    
    let flexCharTermAttribute = 
        lazy java.lang.Class.forName 
                 (typeof<FlexLucene.Analysis.Tokenattributes.CharTermAttribute>.AssemblyQualifiedName)
    
    /// Utility function to get tokens from the search string based upon the passed analyzer
    /// This will enable us to avoid using the Lucene query parser
    /// We cannot use simple white space based token generation as it really depends 
    /// upon the analyzer used
    let inline parseTextUsingAnalyzer (analyzer : FlexLucene.Analysis.Analyzer, fieldName, queryText) = 
        let tokens = new List<string>()
        let source : TokenStream = analyzer.TokenStream(fieldName, new StringReader(queryText))
        // Get the CharTermAttribute from the TokenStream
        let termAtt = source.AddAttribute(flexCharTermAttribute.Value)
        try 
            try 
                source.Reset()
                while source.incrementToken() do
                    tokens.Add(termAtt.ToString())
                source.End()
            with _ -> ()
        finally
            source.Close()
        tokens
