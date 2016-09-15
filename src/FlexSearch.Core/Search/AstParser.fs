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
open Microsoft.Extensions.ObjectPool

type Variables = Dictionary<string, string>

/// Signifies the behaviour of the clause when no value
/// is provided for searching
type ClauseMatchRule = 
    /// Default behaviour that is to thrown error when no
    /// value is provided for searching
    | ThrowError = 0
    /// Convert the clause to a match all clause which allows
    /// matching against all values
    | All = 1
    /// Convert the clause to a match none clause which matches
    /// no values
    | None = 2
    /// Use the default value for the field for searching
    | FieldDefault = 3
    /// Use the user defined value to search
    | UseDefault = 4

type ClauseProperties = 
    { MatchRule : ClauseMatchRule
      Filter : bool
      DefaultValue : string option
      Boost : float32 option
      ConstantScore : float32 option
      Switches : IEnumerable<Switch> }
    static member Create(parameters : FunctionParameter list) = 
        let mutable matchRule = ClauseMatchRule.ThrowError
        let mutable defaultValue = None
        let mutable boost = None
        let mutable constantScore = None
        let mutable filter = false
        let switches = new List<Switch>()
        // Could use List.choose here but it is a lot slower
        for p in parameters do
            match p with
            | Switch(s) -> 
                match s.Name with
                | InvariantEqual "MatchAll" -> matchRule <- ClauseMatchRule.All
                | InvariantEqual "MatchNone" -> matchRule <- ClauseMatchRule.None
                | InvariantEqual "MatchFieldDefault" -> matchRule <- ClauseMatchRule.FieldDefault
                | InvariantEqual "UseDefault" -> 
                    match s.Value with
                    | Some v -> 
                        if isBlank v then matchRule <- ClauseMatchRule.FieldDefault
                        else 
                            matchRule <- ClauseMatchRule.UseDefault
                            defaultValue <- Some v
                    | None -> matchRule <- ClauseMatchRule.FieldDefault
                | InvariantEqual "Boost" -> 
                    match s.Value with
                    | Some v -> 
                        if isNotBlank v then 
                            let boostValue = pFloat 1.0f v
                            if boostValue <> 1.0f then boost <- Some boostValue
                    | None -> ()
                | InvariantEqual "ConstantScore" -> 
                    match s.Value with
                    | Some v -> 
                        if isNotBlank v then 
                            let constantValue = pFloat 1.0f v
                            if constantValue <> 1.0f then constantScore <- Some constantValue
                    | None -> ()
                | InvariantEqual "NoScore" -> filter <- true
                | _ -> switches.Add(s)
            | _ -> ()
        { MatchRule = matchRule
          Filter = filter
          DefaultValue = defaultValue
          Boost = boost
          ConstantScore = constantScore
          Switches = switches }

/// Represents the tokens to be used while searching. These
/// are generated for each clause in a query.
/// A single clause can contain multiple segments each consisting of 
/// multiple tokens. For example : 
/// AllOf(firstname, 'seemant raj', 'roger')
/// contains two segments 'seemant raj' with two tokens and
/// 'roger' with one token. These tokens are generated using the
/// field level analyzer. The above will be represented as:
/// Segments = ['seemant', 'raj', 'roger']
/// Positions = [(0,2), (2,1)]
/// The total items in Positions represents the total number of
/// segments and each item represents the starting position of the
/// segments in the Segments array.  
type Tokens = 
    { Segments : List<string>
      Positions : List<int * int> }
    member this.Count() = this.Positions.Count

/// Used to represent a search operator like allOf etc.
[<Interface>]
type IQueryFunction = 
    abstract GetQuery : FieldSchema * Tokens * ClauseProperties -> Result<Query>
    abstract GetNumericQuery : FieldSchema * Tokens * ClauseProperties -> Result<Query>
    abstract UseAnalyzer : bool

type ReadOnlySchema = IReadOnlyDictionary<string, FieldSchema>

type QueryFunctions = Dictionary<string, IQueryFunction>

/// This module is responsible for generating Lucene Queries from the AST
/// generated by the parser.
[<AutoOpen>]
module AstParser = 
    let tokenPool = 
        let factory() = 
            { Segments = new List<string>()
              Positions = new List<int * int>() }
        
        let onRelease (tokens : Tokens) = 
            tokens.Segments.Clear()
            tokens.Positions.Clear()
            true
        
        new DefaultObjectPool<Tokens>(new ObjectPoolPolicy<Tokens>(factory, onRelease))
    
    let inline queryNotFound queryName = QueryNotFound <| queryName
    let inline fieldNotFound fieldName = InvalidFieldName <| fieldName
    
    let getFieldSchema fieldName (fields : IReadOnlyDictionary<string, FieldSchema>) = 
        maybe { 
            let! field = fields |> keyExists2 (fieldName, fieldNotFound)
            do! FieldSchema.isSearchable field |> boolToResult (StoredFieldCannotBeSearched(field.FieldName))
            return field
        }
    
    // Gets a variable value from the search query.
    // Empty/blank values are considered an error
    let getVariable (name : VariableName) (variables : Variables) = 
        variables
        |> Seq.tryFind (fun kvp -> kvp.Key.ToLowerInvariant() = name.ToLowerInvariant())
        |> fun x -> if x.IsSome then x.Value.Value |> Some
                    else None
    
    /// Checks if there is a need to generate query or not? In case there are no tokens then
    /// the main query could be replaced with match all or match none queries.
    /// Also if the match rule is set to use defaults then we can inject the default value.
    let byPassMainQueryGeneration (fieldName, fieldSchema : FieldSchema, values : List<string>, 
                                   properties : ClauseProperties) = 
        if values.Count = 0 then 
            // Check if there is a configured behaviour for the missing value
            match properties.MatchRule with
            | ClauseMatchRule.All -> 
                getMatchAllDocsQuery()
                |> Some
                |> Ok
            | ClauseMatchRule.None -> 
                getMatchNoDocsQuery()
                |> Some
                |> Ok
            | ClauseMatchRule.FieldDefault -> 
                values.Add(fieldSchema.FieldType.DefaultStringValue)
                None |> Ok
            | ClauseMatchRule.UseDefault -> 
                values.Add(properties.DefaultValue.Value)
                None |> Ok
            | _ -> fail <| MissingVariableValue(fieldName)
        else None |> Ok
    
    /// Generate the leaf node clause for a given Clause predicate 
    let getClause (funcName, fieldName, parameters : FunctionParameter list) (searchQuery : SearchQuery) 
        (fields : ReadOnlySchema) (queryFuncs : QueryFunctions) = 
        maybe { 
            let! fieldSchema = fields |> getFieldSchema fieldName
            let! func = queryFuncs |> keyExists2 (funcName, queryNotFound)
            let properties = ClauseProperties.Create(parameters)
            let tokens = tokenPool.Get()
            let mutable pos = 0
            for p in parameters do
                match p with
                | Constant(c) -> 
                    if func.UseAnalyzer then fieldSchema |> FieldSchema.getTokens (c, tokens.Segments)
                    else 
                        // Makes the search case insensitive
                        tokens.Segments.Add(c.ToLowerInvariant())
                | Variable(v) -> 
                    match searchQuery.Variables |> getVariable v with
                    | Some(value) when value |> isNotBlank -> 
                        if func.UseAnalyzer then fieldSchema |> FieldSchema.getTokens (value, tokens.Segments)
                        else 
                            // Makes the search case insensitive
                            tokens.Segments.Add(value.ToLowerInvariant())
                    | _ -> ()
                | _ -> ()
                if pos <> tokens.Segments.Count then 
                    tokens.Positions.Add((pos, tokens.Segments.Count - pos))
                    pos <- tokens.Segments.Count
            // We can bypass main query if we don't have any tokens to search
            let! byPassMainQuery = byPassMainQueryGeneration (fieldName, fieldSchema, tokens.Segments, properties)
            match byPassMainQuery with
            | Some(q) -> 
                // Make sure to return the item to the pool
                tokenPool.Return(tokens)
                return q
            | None -> 
                let mutable q = 
                    if FieldSchema.isNumericField fieldSchema then func.GetNumericQuery(fieldSchema, tokens, properties)
                    else func.GetQuery(fieldSchema, tokens, properties)
                // It is safe to return the item to the pool as we have used
                // it in query generation.
                tokenPool.Return(tokens)
                match q with
                | Ok(q') -> 
                    let mutable q'' = q'
                    if properties.Boost.IsSome then q'' <- getBoostQuery (q', properties.Boost.Value)
                    if properties.ConstantScore.IsSome then 
                        q'' <- getConstantScoreQuery (q'', properties.ConstantScore.Value)
                    if properties.Filter then q'' <- getBooleanQuery() |> addFilterClause q'' :> Query
                    return q''
                | _ -> return! q
        }
    
    /// Walks over AST and generate a Lucene query from it
    let rec generateLuceneQuery (predicate : Predicate) (searchQuery : SearchQuery) (fields : ReadOnlySchema) 
            (queryFuncs : QueryFunctions) = 
        maybe { 
            match predicate with
            | NotPredicate(pr) -> 
                let! notQuery = generateLuceneQuery pr searchQuery fields queryFuncs
                return getBooleanQuery()
                       |> addMustNotClause notQuery
                       |> addMatchAllClause :> Query
            | Clause(funcName, fieldName, parameters) -> 
                return! getClause (funcName, fieldName, parameters) searchQuery fields queryFuncs
            | OrPredidate(lhs, rhs) -> 
                let! lhsQuery = generateLuceneQuery lhs searchQuery fields queryFuncs
                let! rhsQuery = generateLuceneQuery rhs searchQuery fields queryFuncs
                return getBooleanQuery()
                       |> addShouldClause lhsQuery
                       |> addShouldClause rhsQuery :> Query
            | AndPredidate(lhs, rhs) -> 
                let! lhsQuery = generateLuceneQuery lhs searchQuery fields queryFuncs
                let! rhsQuery = generateLuceneQuery rhs searchQuery fields queryFuncs
                return getBooleanQuery()
                       |> addMustClause lhsQuery
                       |> addMustClause rhsQuery :> Query
        }
    
    /// Generates a predicate using query parser. This predicate can be fed to LuceneQueryGenerator
    /// to generate Lucene query
    let getSearchPredicate (writers : IndexWriter) (sq : SearchQuery) (parser : IFlexParser) = 
        maybe { 
            // Check if preSearch script is defined. It is import to execute this script before
            do! if isNotBlank sq.PreSearchScript then 
                    match writers.Settings.Scripts.PreSearchScripts.TryGetValue(sq.PreSearchScript) with
                    | true, script -> 
                        try 
                            script.Invoke(sq)
                            okUnit
                        with e -> 
                            Logger.Log
                                ("PreSearch script execution error", e, MessageKeyword.Search, MessageLevel.Warning)
                            okUnit
                    | _ -> fail <| ScriptNotFound(sq.PreSearchScript)
                else okUnit
            if isNotBlank sq.QueryName then 
                // Search profile based
                match writers.Settings.PredefinedQueries.TryGetValue(sq.QueryName) with
                | true, p -> 
                    let (predicate, preDefinedQuery) = p
                    // This is a search profile based query. So copy over essential
                    // values from Search profile to query. Keep the search query
                    /// values if override is set to true
                    if not sq.OverridePredefinedQueryOptions then 
                        sq.Columns <- preDefinedQuery.Columns
                        sq.DistinctBy <- preDefinedQuery.DistinctBy
                        sq.Skip <- preDefinedQuery.Skip
                        sq.OrderBy <- preDefinedQuery.OrderBy
                        sq.CutOff <- preDefinedQuery.CutOff
                        sq.Count <- preDefinedQuery.Count
                        sq.Highlights <- preDefinedQuery.Highlights
                    return predicate
                | _ -> return! fail <| UnknownPredefinedQuery(sq.IndexName, sq.QueryName)
            else let! predicate = parser.Parse(sq.QueryString)
                 return predicate
        }
    
    /// Top level method responsible for generating Lucene query from the given search query. This
    /// method takes care of AST generation, query validation etc.
    let generateQuery (writer : IndexWriter) (searchQuery : SearchQuery) (parser : IFlexParser) (fields : ReadOnlySchema) 
        (queryFuncs : QueryFunctions) = 
        maybe { 
            let! predicate = getSearchPredicate writer searchQuery parser
            match predicate with
            | NotPredicate(_) -> return! fail <| PurelyNegativeQueryNotSupported
            | _ -> 
                return! generateLuceneQuery predicate searchQuery writer.Settings.Fields.ReadOnlyDictionary queryFuncs
        }
