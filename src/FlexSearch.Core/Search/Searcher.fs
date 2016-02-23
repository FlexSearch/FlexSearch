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

open FlexSearch.Api
open FlexSearch.Api.Model
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Search
open FlexLucene.Search.Highlight
open FlexSearch.Core
open System
open System.Collections.Generic
open System.Linq
open System.ComponentModel.Composition

/// This module is responsible for the Lucene query execution. It
/// is responsible for generating search results and contains all 
// the search time filters. 
[<AutoOpen>]
module Searcher = 
    /// Simple wrapper of objects which are passed around
    /// in a group
    type SearcherState = 
        { IndexWriter : IndexWriter
          SearchQuery : SearchQuery
          IndexSearchers : RealTimeSearcher []
          Query : Query }
        member this.GetField(fieldName) = this.IndexWriter.Settings.Fields.TryGetValue(fieldName)
        
        member this.GetDoc(shardNo, docNo) = 
            assert (shardNo <= this.IndexSearchers.Length)
            this.IndexSearchers.[shardNo].IndexSearcher.Doc(docNo)
        
        interface IDisposable with
            member this.Dispose() = 
                if isNotNull this.IndexSearchers then this.IndexSearchers |> Array.iter (fun i -> i.DisposeManaged())
    
    /// Returns a document from the index
    let getDocument (document : LuceneDocument) (s : SearcherState) = 
        let fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        
        let getValue (field : FieldSchema) = 
            let value = document.Get(field.SchemaName)
            if notNull value then 
                if value = Constants.StringDefaultValue && s.SearchQuery.ReturnEmptyStringForNull then 
                    fields.Add(field.FieldName, String.Empty)
                else fields.Add(field.FieldName, value)
        match s.SearchQuery.Columns with
        // Return no other columns when nothing is passed
        | _ when s.SearchQuery.Columns.Length = 0 -> ()
        // Return all columns when *
        | _ when s.SearchQuery.Columns.First() = "*" -> 
            for field in s.IndexWriter.Settings.Fields do
                if [ IdField.Name; TimeStampField.Name; ModifyIndexField.Name; StateField.Name ] 
                   |> Seq.contains field.FieldName then ()
                else getValue (field)
        // Return only the requested columns
        | _ -> 
            for fieldName in s.SearchQuery.Columns do
                match s.GetField(fieldName) with
                | (true, field) -> getValue (field)
                | _ -> ()
        fields
    
    /// Returns the Sort type for the query
    let inline getSort (s : SearcherState) = 
        let sortOrder = 
            match s.SearchQuery.OrderByDirection with
            | Constants.OrderByDirection.Ascending -> false
            | _ -> true
        match s.SearchQuery.OrderBy with
        | null -> Sort.RELEVANCE
        | _ -> 
            match s.GetField(s.SearchQuery.OrderBy) with
            | (true, field) -> 
                if field |> FieldSchema.hasDocValues then 
                    new Sort(new SortField(field.SchemaName, field.FieldType.SortFieldType, sortOrder))
                else Sort.RELEVANCE
            | _ -> Sort.RELEVANCE
    
    /// Returns the highlight related information along with the field schema
    let getHighlightOptions (s : SearcherState) = 
        if notNull s.SearchQuery.Highlights then 
            match s.SearchQuery.Highlights.HighlightedFields with
            | x when x.Length = 1 -> 
                match s.GetField(x.First()) with
                | (true, field) -> 
                    let htmlFormatter = 
                        new SimpleHTMLFormatter(s.SearchQuery.Highlights.PreTag, s.SearchQuery.Highlights.PostTag)
                    Some(field, new Highlighter(htmlFormatter, new QueryScorer(s.Query)))
                | _ -> None
            | _ -> None
        else None
    
    /// Returns a highlighter created with the passed highlighter options
    let inline getHighlighter (document : LuceneDocument, shardIndex, doc) 
               (highlighterOptions : option<FieldSchema * Highlighter>) (s : SearcherState) = 
        if highlighterOptions.IsSome then 
            let (field, highlighter) = highlighterOptions.Value
            let text = document.Get(field.SchemaName)
            if isNotNull text then 
                let tokenStream = 
                    TokenSources.GetAnyTokenStream
                        (s.IndexSearchers.[shardIndex].IndexReader, doc, field.SchemaName, 
                         s.IndexWriter.Settings.SearchAnalyzer)
                let frags = 
                    highlighter.GetBestTextFragments
                        (tokenStream, text, false, s.SearchQuery.Highlights.FragmentsToReturn)
                frags
                |> Array.filter (fun frag -> notNull (frag) && frag.GetScore() > float32 (0.0))
                |> Array.map (fun frag -> frag.ToString())
            else [||]
        else [||]
    
    /// Returns the settings for distinct by filter
    let inline getDistinctBy (s : SearcherState) = 
        if isNotBlank s.SearchQuery.DistinctBy then 
            match s.GetField(s.SearchQuery.DistinctBy) with
            | true, field -> 
                match field |> FieldSchema.isTokenized with
                | false -> Some(field, new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                | true -> None
            | _ -> None
        else None
    
    /// Filter which filters out all non distinct results for a given field
    let inline distinctByFilter (document : LuceneDocument) (distinctBy : option<FieldSchema * HashSet<string>>) 
               (s : SearcherState) = 
        match distinctBy with
        | Some(field, hashSet) -> 
            let distinctByValue = document.Get(s.IndexWriter.GetSchemaName(field.FieldName))
            if notNull distinctByValue && hashSet.Add(distinctByValue) then Some(document)
            else None
        | None -> Some(document)
    
    /// Filter which removed all the docs which are lower than the cutoff percentage
    let inline cutOffFilter (hit : ScoreDoc) (cutOff) (document : LuceneDocument option) = 
        match cutOff with
        | Some(cutOffValue, maxScore) -> 
            if (hit.Score / maxScore * 100.0f >= cutOffValue) then document
            else None
        | None -> document
    
    /// Generate a FlexSearch document from a Lucene document
    let inline processDocument (hit : ScoreDoc, document : LuceneDocument, highlighterOptions) (s : SearcherState) = 
        let timeStamp = 
            let t = document.Get(s.IndexWriter.GetSchemaName(TimeStampField.Name))
            if isNull t then 0L
            else int64 t
        
        let fields = s |> getDocument document
        let resultDoc = new Model.Document()
        resultDoc.Id <- document.Get(s.IndexWriter.GetSchemaName(IdField.Name))
        resultDoc.IndexName <- s.IndexWriter.Settings.IndexName
        resultDoc.TimeStamp <- timeStamp
        resultDoc.Fields <- fields
        resultDoc.Score <- if s.SearchQuery.ReturnScore then float (hit.Score)
                           else 0.0
        resultDoc.Highlights <- s |> getHighlighter (document, hit.ShardIndex, hit.Doc) highlighterOptions
        resultDoc
    
    /// Main search method responsible for searching across shards
    let search (indexWriter : IndexWriter, query : Query, searchQuery : SearchQuery) = 
        (!>) "Input Query:%s \nGenerated Query : %s" (searchQuery.QueryString) (query.ToString())
        let state = 
            { IndexWriter = indexWriter
              Query = query
              SearchQuery = searchQuery
              IndexSearchers = indexWriter |> IndexWriter.getRealTimeSearchers }
        
        // Each thread only works on a separate part of the array and as no parts are shared across
        // multiple threads the below variables are thread safe. The cost of using blocking collection vs. 
        // array per search is high
        let topDocsCollection : TopFieldDocs array = Array.zeroCreate state.IndexSearchers.Length
        let sort = state |> getSort
        let distinctBy = state |> getDistinctBy
        
        let count = 
            match searchQuery.Count with
            | 0 -> 10 + searchQuery.Skip
            | _ -> searchQuery.Count + searchQuery.Skip
        
        let searchShard (x : ShardWriter) = 
            // This is to enable proper sorting
            let topFieldCollector = TopFieldCollector.Create(sort, count, null, true, true, true)
            state.IndexSearchers.[x.ShardNo].IndexSearcher.Search(query, topFieldCollector)
            topDocsCollection.[x.ShardNo] <- topFieldCollector.TopDocs()
        
        indexWriter.ShardWriters |> Array.Parallel.iter searchShard
        let totalDocs = TopDocs.Merge(sort, count, topDocsCollection)
        let hits = totalDocs.ScoreDocs
        let recordsReturned = totalDocs.ScoreDocs.Count() - searchQuery.Skip
        let totalAvailable = totalDocs.TotalHits
        
        let cutOff = 
            match searchQuery.CutOff with
            | 0.0 -> None
            | cutOffValue -> Some(float32 <| cutOffValue, totalDocs.GetMaxScore())
        
        let highlighterOptions = state |> getHighlightOptions
        
        // Start composing the search results
        let results = 
            seq { 
                for i = searchQuery.Skip to hits.Length - 1 do
                    let hit = hits.[i]
                    let document = state.GetDoc(hit.ShardIndex, hit.Doc)
                    
                    let result = 
                        state
                        |> distinctByFilter document distinctBy
                        |> cutOffFilter hit cutOff
                    match result with
                    | Some(doc) -> yield state |> processDocument (hit, document, highlighterOptions)
                    | None -> ()
                (state :> IDisposable).Dispose()
            }
        new SearchResults(RecordsReturned = recordsReturned, BestScore = totalDocs.GetMaxScore(), 
                          TotalAvailable = totalAvailable, Documents = results.ToArray())
