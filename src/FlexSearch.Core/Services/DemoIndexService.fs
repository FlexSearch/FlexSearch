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
namespace FlexSearch.Core.Services

open FlexSearch.Api
open FlexSearch.Common
open FlexSearch.Core
open Newtonsoft.Json
open System
open System.Collections.Generic
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
type DemoIndexService(indexService : IIndexService, documentService : IDocumentService, analyzerService : IAnalyzerService, resourceService : IResourceService, settings : ServerSettings) = 
    let indexName = "country"
    
    let AddTestDataToIndex(index : Index, testData : string) = 
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        for line in lines.Skip(1) do
            assert (lines.Count() = headers.Count())
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let indexDocument = new FlexDocument()
            indexDocument.Id <- items.[0].Trim()
            indexDocument.IndexName <- index.IndexName
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            documentService.AddDocument(indexDocument) |> ignore
        indexService.Refresh(indexName) |> ignore
    
    let GetJsonData() = lazy (JsonConvert.DeserializeObject<List<Country>>(Resources.DemoIndexData))
    
    let IndexJsonData(data : List<Country>) = 
        for record in data do
            let indexDocument = new FlexDocument()
            indexDocument.IndexName <- indexName
            indexDocument.Id <- record.Id
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
            | Choice1Of2(_) -> ()
            | Choice2Of2(e) -> 
                failwithf "%A" e
        indexService.Refresh(indexName) |> ignore
    
    let GetDemoIndexInfo() = 
        let index = new Index(IndexName = indexName)
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Online <- true
        index.Fields.Add(new Field("countryname"))
        index.Fields.Add(new Field("exports", FieldType.Long, ScriptName = "striptonumbers"))
        index.Fields.Add(new Field("imports", FieldType.Text, IndexAnalyzer = "striptonumbersanalyzer"))
        index.Fields.Add(new Field("independence", FieldType.Date))
        index.Fields.Add(new Field("militaryexpenditure", FieldType.Double))
        index.Fields.Add(new Field("netmigration", FieldType.Double))
        index.Fields.Add(new Field("area", FieldType.Int))
        index.Fields.Add(new Field("internetusers", FieldType.Long))
        index.Fields.Add(new Field("labourforce", FieldType.Long))
        index.Fields.Add(new Field("population", FieldType.Long))
        index.Fields.Add(new Field("agriproducts", FieldType.Text, IndexAnalyzer = "foodsynonymsanalyzer"))
        index.Fields.Add(new Field("areacomparative"))
        index.Fields.Add(new Field("background", FieldType.Highlight))
        index.Fields.Add(new Field("capital"))
        index.Fields.Add(new Field("climate"))
        index.Fields.Add(new Field("economy"))
        index.Fields.Add(new Field("governmenttype"))
        index.Fields.Add(new Field("memberof"))
        index.Fields.Add(new Field("countrycode", FieldType.ExactText))
        index.Fields.Add(new Field("nationality"))
        index.Fields.Add(new Field("coordinates", FieldType.ExactText))
        // Custom Script
        let source = 
            """return !System.String.IsNullOrWhiteSpace(fields.exports) ? fields.exports.Replace(" ", "").Replace("$",""): "0";"""
        let stripNumbersScript = new Script("striptonumbers", source, ScriptType.ComputedField)
        index.Scripts.Add(stripNumbersScript)
        index
    
    let CreateIndex() = 
        maybe { 
            let index = GetDemoIndexInfo()
            // Create map list resource
            let foodsynonyms = new MapList()
//            foodsynonyms.Words.Add
//                ("grain", 
//                 new List<string>([| "cereal"; "heat"; "corn"; "barley"; "oats"; "soybeans"; "pulses"; "maize"; "millet"; 
//                                     "beans"; "rice" |]))
            foodsynonyms.Words.Add("grape", new List<string>([| "fruit" |]))
            do! resourceService.UpdateResource<MapList>("foodsynonyms", foodsynonyms)
            // Custom analyzer for food synonym
            let foodsynonymsanalyzer = new Analyzer(AnalyzerName = "foodsynonymsanalyzer")
            foodsynonymsanalyzer.Tokenizer <- new Tokenizer("standardtokenizer")
            foodsynonymsanalyzer.Filters.Add(new TokenFilter("standardfilter"))
            foodsynonymsanalyzer.Filters.Add(new TokenFilter("lowercasefilter"))
            let synonymfilter = new TokenFilter("synonymfilter")
            synonymfilter.Parameters.Add("resourceName", "foodsynonyms")
            foodsynonymsanalyzer.Filters.Add(synonymfilter)
            do! analyzerService.AddOrUpdateAnalyzer(foodsynonymsanalyzer)
            // Custom analyzer for strip to numbers
            let striptonumbersanalyzer = new Analyzer(AnalyzerName = "striptonumbersanalyzer")
            striptonumbersanalyzer.Tokenizer <- new Tokenizer("keywordtokenizer")
            striptonumbersanalyzer.Filters.Add(new TokenFilter("standardfilter"))
            let regexFilter = new TokenFilter("patternreplacefilter")
            regexFilter.Parameters.Add("pattern", @"[a-z$ ]")
            regexFilter.Parameters.Add("replacementtext", "")
            striptonumbersanalyzer.Filters.Add(regexFilter)
            do! analyzerService.AddOrUpdateAnalyzer(striptonumbersanalyzer)
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
        | true -> Choice1Of2()
        | _ -> 
            match CreateIndex() with
            | Choice1Of2(_) -> Choice1Of2()
            | Choice2Of2(e) -> Choice2Of2(e)
