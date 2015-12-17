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
open FlexSearch.Api.Constants
open FlexSearch.Api.Models
open FlexLucene.Analysis.Synonym
open FlexLucene.Util
open Newtonsoft.Json
open System
open System.IO
open System.Linq

type Country() = 
    member val Id = Unchecked.defaultof<string> with get, set
    member val CountryName = Unchecked.defaultof<string> with get, set
    member val Exports = Unchecked.defaultof<string> with get, set
    member val Imports = Unchecked.defaultof<string> with get, set
    member val Independence = Unchecked.defaultof<string> with get, set
    member val MilitaryExpenditure = Unchecked.defaultof<string> with get, set
    member val NetMigration = Unchecked.defaultof<string> with get, set
    member val Area = Unchecked.defaultof<string> with get, set
    member val InternetUsers = Unchecked.defaultof<string> with get, set
    member val LabourForce = Unchecked.defaultof<string> with get, set
    member val Population = Unchecked.defaultof<int64> with get, set
    member val AgriProducts = Unchecked.defaultof<string> with get, set
    member val AreaComparative = Unchecked.defaultof<string> with get, set
    member val Background = Unchecked.defaultof<string> with get, set
    member val Capital = Unchecked.defaultof<string> with get, set
    member val Climate = Unchecked.defaultof<string> with get, set
    member val Economy = Unchecked.defaultof<string> with get, set
    member val GovernmentType = Unchecked.defaultof<string> with get, set
    member val MemberOf = Unchecked.defaultof<string> with get, set
    member val CountryCode = Unchecked.defaultof<string> with get, set
    member val Nationality = Unchecked.defaultof<string> with get, set
    member val Coordinates = Unchecked.defaultof<string> with get, set

[<Sealed>]
/// <summary>
/// Service wrapper around all analyzer/analysis related services
/// </summary>
type DemoIndexService(indexService : IIndexService, documentService : IDocumentService, analyzerService : IAnalyzerService) = 
    let indexName = "country"
    
    let AddTestDataToIndex(index : Index, testData : string) = 
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        for line in lines.Skip(1) do
            assert (lines.Count() = headers.Count())
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let indexDocument = new Document(index.IndexName, items.[0].Trim())
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            documentService.AddDocument(indexDocument) |> ignore
        indexService.Refresh(indexName) |> ignore
    
    let GetJsonData() = lazy (JsonConvert.DeserializeObject<List<Country>>(Resources.DemoIndexData))
    
    let IndexJsonData(data : List<Country>) = 
        for record in data do
            let indexDocument = new Document(indexName, record.Id)
            indexDocument.Fields.Add("countryname", record.CountryName)
            indexDocument.Fields.Add("exports", record.Exports)
            indexDocument.Fields.Add("imports", record.Imports)
            indexDocument.Fields.Add("independence", record.Independence)
            indexDocument.Fields.Add("militaryexpenditure", record.MilitaryExpenditure)
            indexDocument.Fields.Add("netmigration", record.NetMigration)
            indexDocument.Fields.Add("area", record.Area)
            indexDocument.Fields.Add("internetusers", record.InternetUsers)
            indexDocument.Fields.Add("labourforce", record.LabourForce)
            indexDocument.Fields.Add("population", record.Population.ToString())
            indexDocument.Fields.Add("agriproducts", record.AgriProducts)
            indexDocument.Fields.Add("areacomparative", record.AreaComparative)
            indexDocument.Fields.Add("background", record.Background)
            indexDocument.Fields.Add("capital", record.Capital)
            indexDocument.Fields.Add("climate", record.Climate)
            indexDocument.Fields.Add("economy", record.Economy)
            indexDocument.Fields.Add("governmenttype", record.GovernmentType)
            indexDocument.Fields.Add("memberof", record.MemberOf)
            indexDocument.Fields.Add("countrycode", record.CountryCode)
            indexDocument.Fields.Add("nationality", record.Nationality)
            indexDocument.Fields.Add("coordinates", record.Coordinates)
            match documentService.AddDocument(indexDocument) with
            | Ok(_) -> ()
            | Fail(e) -> failwithf "%A" e
        indexService.Refresh(indexName) |> ignore
    
    let GetDemoIndexInfo() = 
        let index = new Index(IndexName = indexName)
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Active <- true
        index.Fields <- [| new Field("countryname")
                           new Field("exports", Constants.FieldType.Long)
                           new Field("imports", Constants.FieldType.Text, IndexAnalyzer = "striptonumbersanalyzer")
                           new Field("independence", Constants.FieldType.Date)
                           new Field("militaryexpenditure", Constants.FieldType.Double)
                           new Field("netmigration", Constants.FieldType.Double)
                           new Field("area", Constants.FieldType.Int)
                           new Field("internetusers", Constants.FieldType.Long)
                           new Field("labourforce", Constants.FieldType.Long)
                           new Field("population", Constants.FieldType.Long)
                           new Field("agriproducts", Constants.FieldType.Text, IndexAnalyzer = "foodsynonymsanalyzer")
                           new Field("areacomparative")
                           new Field("background", Constants.FieldType.Highlight)
                           new Field("capital")
                           new Field("climate")
                           new Field("economy")
                           new Field("governmenttype")
                           new Field("memberof")
                           new Field("countrycode", Constants.FieldType.ExactText)
                           new Field("nationality")
                           new Field("coordinates", Constants.FieldType.ExactText) |]
        index
    
    let buildSynonymFile fileName = 
        let synonymDirectory = Constants.ConfFolder + "/Resources/"
        Directory.CreateDirectory(synonymDirectory) |> ignore
        File.WriteAllText(synonymDirectory + fileName, "grape => fruit")
    
    let CreateIndex() = 
        maybe { 
            let index = GetDemoIndexInfo()
            // Custom analyzer for food synonym
            let foodsynonymsanalyzer = new Analyzer(AnalyzerName = "foodsynonymsanalyzer")
            foodsynonymsanalyzer.Tokenizer <- new Tokenizer(TokenizerName = "standard")
            let synonymfilter = new Filter(FilterName = "synonym")
            let synonymFileName = "foodsynonyms.txt"
            buildSynonymFile synonymFileName
            synonymfilter.Parameters.Add("synonyms", synonymFileName)
            foodsynonymsanalyzer.Filters <- [| new Filter(FilterName = "standard"); new Filter(FilterName = "lowercase"); synonymfilter |]
            do! analyzerService.UpdateAnalyzer(foodsynonymsanalyzer)
            // Custom analyzer for strip to numbers
            let striptonumbersanalyzer = new Analyzer(AnalyzerName = "striptonumbersanalyzer")
            striptonumbersanalyzer.Tokenizer <- new Tokenizer(TokenizerName = "keyword")
            let regexFilter = new Filter(FilterName = "patternreplace")
            regexFilter.Parameters.Add("pattern", @"[a-z$ ]")
            regexFilter.Parameters.Add("replacement", "")
            foodsynonymsanalyzer.Filters <- [| new Filter(FilterName = "standard"); regexFilter |]
            do! analyzerService.UpdateAnalyzer(striptonumbersanalyzer)
            let! createResponse = indexService.AddIndex(index)
            // Index data
            //let testData = File.ReadAllText(Path.Combine(settings.ConfFolder, "demo.csv"))
            //AddTestDataToIndex(index, testData)
            IndexJsonData(GetJsonData().Value)
        }
    
    member this.DemoData() = GetJsonData()
    member this.GetDemoIndex() = GetDemoIndexInfo()
    member this.Setup() = 
        match indexService.IndexExists("country") with
        | true -> okUnit
        | _ -> 
            match CreateIndex() with
            | Ok(_) -> okUnit
            | Fail(e) -> Fail(e)
