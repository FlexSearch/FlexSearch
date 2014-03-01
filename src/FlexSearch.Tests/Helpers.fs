module Helpers

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open System
open System.Collections.Generic
open System.Linq
open System.Threading

let pluginContainer = PluginContainer(false).Value
let factoryCollection = new FactoryCollection(pluginContainer) :> IFactoryCollection
let settingBuilder = SettingsBuilder.SettingsBuilder factoryCollection (new Validator.IndexValidator(factoryCollection))
let persistanceStore = new PersistanceStore("", true)
let searchService = new SearchService(GetQueryModules(factoryCollection), getParserPool (2)) :> ISearchService
let indexService = 
    new IndexService(settingBuilder, persistanceStore, new VersioningCacheStore(), searchService) :> IIndexService

let GetBasicIndexSettingsForContact() = 
    let index = new Index()
    index.IndexName <- Guid.NewGuid().ToString("N")
    index.Online <- true
    index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
    index.Fields.Add("gender", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("title", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("givenname", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("middleinitial", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("surname", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("streetaddress", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("city", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("state", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("zipcode", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("country", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("countryfull", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("emailaddress", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("username", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("password", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("cctype", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("ccnumber", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("occupation", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("cvv2", new FieldProperties(FieldType = FieldType.Int))
    index.Fields.Add("nationalid", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("ups", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("company", new FieldProperties(FieldType = FieldType.Text))
    index.Fields.Add("pounds", new FieldProperties(FieldType = FieldType.Double))
    index.Fields.Add("centimeters", new FieldProperties(FieldType = FieldType.Int))
    index.Fields.Add("guid", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("latitude", new FieldProperties(FieldType = FieldType.Double))
    index.Fields.Add("longitude", new FieldProperties(FieldType = FieldType.Double))
    index.Fields.Add("importdate", new FieldProperties(FieldType = FieldType.Date))
    index.Fields.Add("timestamp", new FieldProperties(FieldType = FieldType.DateTime))
    index.Fields.Add("topic", new FieldProperties(FieldType = FieldType.ExactText))
    index.Fields.Add("abstract", new FieldProperties(FieldType = FieldType.Text))
    // Computed fields
    index.Fields.Add("fullname", new FieldProperties(FieldType = FieldType.Text, ScriptName = "fullname"))
    index.Scripts.Add
        ("fullname", 
         new ScriptProperties("""return fields["givenname"] + " " + fields["surname"];""", ScriptType.ComputedField))
    index

/// <summary>
/// Utility method to add data to an index
/// </summary>
/// <param name="indexService"></param>
/// <param name="index"></param>
/// <param name="testData"></param>
let AddTestDataToIndex(indexService : IIndexService, index : Index, testData : string) = 
    let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
    let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
    for line in lines.Skip(1) do
        let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        let indexDocument = new Document()
        indexDocument.Id <- items.[0]
        indexDocument.Index <- index.IndexName
        for i in 1..items.Length - 1 do
            indexDocument.Fields.Add(headers.[i], items.[i])
        indexService.PerformCommand(index.IndexName, Create(indexDocument.Id, indexDocument.Fields)) |> ignore
    indexService.PerformCommand(index.IndexName, IndexCommand.Commit) |> ignore
    Thread.Sleep(100)

let expectedFailureMessage (message : OperationMessage) (f : Choice<_, OperationMessage>) = 
    match f with
    | Choice1Of2(_) -> Assert.AreEqual(1, 2, sprintf "Expected an error of type: %i." message.ErrorCode)
    | Choice2Of2(error) -> 
        if message.ErrorCode = error.ErrorCode then Assert.AreEqual(1, 1)
        else Assert.AreEqual(1, 2, sprintf "Expected an error of type: %i." message.ErrorCode)

let expectedSuccessMessage (message : 'T) (f : Choice<'T, _>) = 
    match f with
    | Choice1Of2(a) -> Assert.AreEqual(message, a)
    | Choice2Of2(error) -> Assert.AreEqual(1, 2, "Expected Choice1Of1 but received Choice1Of2.")

let getResult (f : Choice<SearchResults, OperationMessage>) = 
    match f with
    | Choice1Of2(a) -> a
    | Choice2Of2(b) -> failtest b.DeveloperMessage

let resourceLoaderMock = 
    { new IResourceLoader with
          member this.LoadResourceAsString str = "hello"
          member this.LoadResourceAsList str = ([| "hello"; "world" |].ToList())
          member this.LoadResourceAsMap str = 
              let result = new List<string []>()
              result.Add([| "easy"; "simple"; "clear" |])
              result }
