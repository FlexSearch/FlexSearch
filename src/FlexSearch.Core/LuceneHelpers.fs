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
open FlexLucene.Analysis.Core
open FlexLucene.Analysis.Miscellaneous
open FlexLucene.Analysis.Standard
open FlexLucene.Analysis.Tokenattributes
open FlexLucene.Analysis.Util
open FlexLucene.Document
open FlexLucene.Index
open FlexLucene.Queries
open FlexLucene.Queryparser.Classic
open FlexLucene.Queryparser.Flexible
open FlexLucene.Search
open FlexLucene.Search.Highlight
open FlexLucene.Search.Postingshighlight
open FlexSearch.Core
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Linq
open java.io
open java.util

/// Determines whether it's an infinite value and if it's minimum or maximum
type Infinite = 
    | MinInfinite
    | MaxInfinite
    | NoInfinite

[<AutoOpenAttribute>]
module JavaHelpers = 
    // These are needed to satisfy certain Lucene query requirements
    let inline GetJavaDouble(value : Double) = java.lang.Double(value)
    let inline GetJavaInt(value : int) = java.lang.Integer(value)
    let inline GetJavaLong(value : int64) = java.lang.Long(value)
    let JavaLongMax = java.lang.Long(java.lang.Long.MAX_VALUE)
    let JavaLongMin = java.lang.Long(java.lang.Long.MIN_VALUE)
    let JavaDoubleMax = java.lang.Double(java.lang.Double.MAX_VALUE)
    let JavaDoubleMin = java.lang.Double(java.lang.Double.MIN_VALUE)
    let JavaIntMax = java.lang.Integer(java.lang.Integer.MAX_VALUE)
    let JavaIntMin = java.lang.Integer(java.lang.Integer.MIN_VALUE)
    
    /// Get a new Java hashmap
    let hashMap() = new HashMap()
    
    /// Put an item in the hashmap and continue
    let putC (key, value) (hashMap : HashMap) = 
        hashMap.put (key, value.ToString()) |> ignore
        hashMap
    
    /// Put an item in the hashmap
    let put (key, value) (hashMap : HashMap) = hashMap.put (key, value) |> ignore
    
    let inline getJavaDouble infinite (value : float) = 
        match infinite with
        | MaxInfinite -> JavaDoubleMax
        | MinInfinite -> JavaDoubleMin
        | _ -> java.lang.Double(value)
    
    let inline getJavaInt infinite (value : int) = 
        match infinite with
        | MaxInfinite -> JavaIntMax
        | MinInfinite -> JavaIntMin
        | _ -> java.lang.Integer(value)
    
    let inline getJavaLong infinite (value : int64) = 
        match infinite with
        | MaxInfinite -> JavaLongMax
        | MinInfinite -> JavaLongMin
        | _ -> java.lang.Long(value)

[<AutoOpenAttribute>]
module QueryHelpers = 
    open FlexLucene.Search
    
    type String with
        
        /// Get term for the given field
        member this.Term(fld : string) = new Term(fld, this)
    
    /// Get term for the given fieldname and value
    let inline getTerm (fieldName : string) (text : string) = new Term(fieldName, text)
        
    // Find terms associated with the search string
    let inline getTerms (flexField : Field.T, values : string[]) = 
        let result = new List<string>()
        for value in values do
            match Field.getSearchAnalyzer (flexField) with
            | Some(a) -> result.AddRange(parseTextUsingAnalyzer (a, flexField.SchemaName, value))
            | None -> result.Add(value)
        result

    // ----------------------------------------------------------------------------
    // Queries
    // ----------------------------------------------------------------------------
    let inline getMatchAllDocsQuery() = new MatchAllDocsQuery() :> Query
    let inline getBooleanQuery() = new BooleanQuery()
    let inline getTermQuery fieldName text = new TermQuery(getTerm fieldName text) :> Query
    let inline getFuzzyQuery fieldName slop prefixLength text = 
        new FuzzyQuery((getTerm fieldName text), slop, prefixLength) :> Query
    let inline getPhraseQuery() = new PhraseQuery()
    let inline getWildCardQuery fieldName text = new WildcardQuery(getTerm fieldName text) :> Query
    let inline getRegexpQuery fieldName text = new RegexpQuery(getTerm fieldName text) :> Query
    
    // ----------------------------------------------------------------------------
    // Clauses
    // ----------------------------------------------------------------------------
    let inline getBooleanClause (parameters : Dictionary<string, string> option) = 
        match parameters with
        | Some(p) -> 
            match p.TryGetValue("clausetype") with
            | true, b -> 
                match b with
                | InvariantEqual "or" -> BooleanClauseOccur.SHOULD
                | _ -> BooleanClauseOccur.MUST
            | _ -> BooleanClauseOccur.MUST
        | _ -> BooleanClauseOccur.MUST
    
    let inline addBooleanClause inheritedQuery occur (baseQuery : BooleanQuery) = 
        baseQuery.Add(new BooleanClause(inheritedQuery, occur))
        baseQuery
    
    let inline addMustClause inheritedQuery (baseQuery : BooleanQuery) = 
        baseQuery.Add(new BooleanClause(inheritedQuery, BooleanClauseOccur.MUST))
        baseQuery
    
    let inline addMustNotClause inheritedQuery (baseQuery : BooleanQuery) = 
        baseQuery.Add(new BooleanClause(inheritedQuery, BooleanClauseOccur.MUST_NOT))
        baseQuery
    
    let inline addShouldClause inheritedQuery (baseQuery : BooleanQuery) = 
        baseQuery.Add(new BooleanClause(inheritedQuery, BooleanClauseOccur.SHOULD))
        baseQuery
    
    // ----------------------------------------------------------------------------
    // Query generators
    // ----------------------------------------------------------------------------
    let getBoolQueryFromTerms boolClauseType innerQueryProvider terms = 
        let boolQuery = getBooleanQuery()
        terms |> Seq.iter (fun term -> 
                     let innerQuery = innerQueryProvider term
                     boolQuery
                     |> addBooleanClause innerQuery boolClauseType
                     |> ignore)
        boolQuery :> Query
    
    let zeroOneOrManyQuery terms innerQueryProvider boolClause = 
        match terms |> Seq.length with
        | 0 -> getMatchAllDocsQuery()
        | 1 -> innerQueryProvider (terms |> Seq.head)
        | _ -> getBoolQueryFromTerms boolClause innerQueryProvider terms
        |> ok
    
    // ------------------------
    // Range Queries
    // ------------------------
    let getRangeQuery value (includeLower, includeUpper) (infiniteMin, infiniteMax) (fIdxFld : Field.T) = 
        match FieldType.isNumericField fIdxFld.FieldType with
        | true -> 
            match fIdxFld.FieldType with
            | FieldType.Date | FieldType.DateTime | FieldType.Long -> 
                match Int64.TryParse(value) with
                | true, value' -> 
                    NumericRangeQuery.NewLongRange
                        (fIdxFld.SchemaName, value' |> getJavaLong infiniteMin, value' |> getJavaLong infiniteMax, 
                         includeLower, includeUpper) :> Query |> ok
                | _ -> fail <| DataCannotBeParsed(fIdxFld.FieldName, "Long, Date, DateTime")
            | FieldType.Int -> 
                match Int32.TryParse(value) with
                | true, value' -> 
                    NumericRangeQuery.NewIntRange
                        (fIdxFld.SchemaName, value' |> getJavaInt infiniteMin, value' |> getJavaInt infiniteMax, 
                         includeLower, includeUpper) :> Query |> ok
                | _ -> fail <| DataCannotBeParsed(fIdxFld.FieldName, "Integer")
            | FieldType.Double -> 
                match Double.TryParse(value) with
                | true, value' -> 
                    NumericRangeQuery.NewDoubleRange
                        (fIdxFld.SchemaName, value' |> getJavaDouble infiniteMin, value' |> getJavaDouble infiniteMax, 
                         includeLower, includeUpper) :> Query |> ok
                | _ -> fail <| DataCannotBeParsed(fIdxFld.FieldName, "Double")
            | _ -> fail <| DataCannotBeParsed(fIdxFld.FieldName, "Long, Date, DateTime, Integer, Double")
        | false -> fail <| ExpectingNumericData fIdxFld.FieldName
