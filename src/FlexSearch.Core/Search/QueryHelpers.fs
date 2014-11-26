// ----------------------------------------------------------------------------
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

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Linq
open java.io
open java.util
open org.apache.lucene.analysis
open org.apache.lucene.analysis.core
open org.apache.lucene.analysis.miscellaneous
open org.apache.lucene.analysis.standard
open org.apache.lucene.analysis.tokenattributes
open org.apache.lucene.analysis.util
open org.apache.lucene.index
open org.apache.lucene.queries
open org.apache.lucene.queryparser.classic
open org.apache.lucene.queryparser.flexible
open org.apache.lucene.search
open org.apache.lucene.search.highlight
open org.apache.lucene.search.postingshighlight

[<AutoOpen>]
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

[<AutoOpen>]
module QueryHelpers = 
    // Check if the passed field is numeric field
    let inline IsNumericField(flexField : FlexField) = 
        match flexField.FieldType with
        | FlexDate | FlexDateTime | FlexInt | FlexDouble | FlexLong -> true
        | _ -> false
    
    // Get a search query parser associated with the field 
    let inline GetSearchAnalyzer(flexField : FlexField) = 
        match flexField.FieldType with
        | FlexCustom(a, b, c) -> Some(a)
        | FlexHighlight(a, _) -> Some(a)
        | FlexText(a, _) -> Some(a)
        | FlexExactText(a) -> Some(a)
        | FlexBool(a) -> Some(a)
        | FlexDate | FlexDateTime | FlexInt | FlexDouble | FlexStored | FlexLong -> None
    
    // Find terms associated with the search string
    let inline GetTerms(flexField : FlexField, value) = 
        match GetSearchAnalyzer(flexField) with
        | Some(a) -> ParseTextUsingAnalyzer(a, flexField.SchemaName, value)
        | None -> new List<string>([ value ])
    
    let GetKeyValue(value : string) = 
        if (value.Contains(":")) then 
            Some(value.Substring(0, value.IndexOf(":")), value.Substring(value.IndexOf(":") + 1))
        else None
    
    let inline GetParametersAsDict(arr : string array, skip : int) = 
        let parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        arr 
        |> Array.iteri 
               (fun i x -> 
               if i >= skip && x.Contains(":") then 
                   parameters.Add(x.Substring(0, x.IndexOf(":")), x.Substring(x.IndexOf(":") + 1)))
        parameters
    
    let NumericTermQuery(flexIndexField, value) = 
        match flexIndexField.FieldType with
        | FlexDate | FlexDateTime | FlexLong -> 
            match Int64.TryParse(value) with
            | (true, val1) -> 
                Choice1Of2
                    (NumericRangeQuery.newLongRange 
                         (flexIndexField.SchemaName, GetJavaLong(val1), GetJavaLong(val1), true, true) :> Query)
            | _ -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))
        | FlexInt -> 
            match Int32.TryParse(value) with
            | (true, val1) -> 
                Choice1Of2
                    (NumericRangeQuery.newIntRange 
                         (flexIndexField.SchemaName, GetJavaInt(val1), GetJavaInt(val1), true, true) :> Query)
            | _ -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))
        | FlexDouble -> 
            match Double.TryParse(value) with
            | (true, val1) -> 
                Choice1Of2
                    (NumericRangeQuery.newDoubleRange 
                         (flexIndexField.SchemaName, GetJavaDouble(val1), GetJavaDouble(val1), true, true) :> Query)
            | _ -> 
                Choice2Of2(Errors.DATA_CANNOT_BE_PARSED
                           |> GenerateOperationMessage
                           |> Append("Field Name", flexIndexField.FieldName))
        | _ -> 
            Choice2Of2(Errors.QUERY_OPERATOR_FIELD_TYPE_NOT_SUPPORTED
                       |> GenerateOperationMessage
                       |> Append("Field Name", flexIndexField.FieldName))
