namespace FlexSearch.Documention

module GenerateExamples = 
    open Autofac
    open FlexSearch.Client
    open FlexSearch.Core
    open FlexSearch.TestSupport
    open Microsoft.Owin
    open Microsoft.Owin.Testing
    open Newtonsoft.Json
    open Newtonsoft.Json.Converters
    open Newtonsoft.Json.Serialization
    open System
    open System.Collections.Generic
    open System.IO
    open System.Linq
    open System.Net.Http
    open System.Reflection
    open System.Text
    open System.Xml.Linq
    
    let jsonSettings = new JsonSerializerSettings()
    
    jsonSettings.Converters.Add(new StringEnumConverter())
    jsonSettings.Formatting <- Formatting.Indented
    jsonSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()
    
    type Example() = 
        member val FileName = "" with get, set
        member val Title = "" with get, set
        member val Request = Unchecked.defaultof<_> with get, set
        member val RequestBody = Unchecked.defaultof<string> with get, set
        member val Response = Unchecked.defaultof<_> with get, set
        member val ResponseBody = Unchecked.defaultof<string> with get, set
        member val ResponseBodyJson = Unchecked.defaultof<_> with get, set
    
    let Container = IntegrationTestHelpers.Container
    let demoService = Container.Resolve<FlexSearch.Core.Services.DemoIndexService>()
    let exampleFolder = @"G:\Bitbucket\flexsearch-documentation\src\data\rest-examples"
    let apiFolder = @"G:\Bitbucket\flexsearch-documentation\src\data\endpoints"
    
    let testServer = 
        TestServer.Create(fun app -> 
            let owinServer = 
                new OwinServer(Container.Resolve<IIndexService>(), Container.Resolve<IFlexFactory<IHttpResource>>(), 
                               Container.Resolve<ILogService>())
            owinServer.Configuration(app))
    
    let document (examples : List<Example>) = 
        for example in examples do
            let path = Path.Combine(exampleFolder, example.FileName + ".json")
            if Directory.Exists(exampleFolder) then 
                File.WriteAllText(path, JsonConvert.SerializeObject(example, jsonSettings))
        ()
    
    let GenerateIndicesExamples() = 
        let assembly = typeof<FlexSearch.IntegrationTests.Rest.Dummy>.Assembly
        let examples = new List<Example>()
        for typ in assembly.GetTypes().Where(fun x -> x.IsPublic && x.IsClass) do
            let methods = typ.GetMethods()
            for m in methods.Where
                         (fun x -> x.IsDefined(typeof<FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute>)) do
                let example = new Example()
                printfn "Method Name: %s" m.Name
                try 
                    let testServer = testServer
                    let indexNameGuid = Guid.NewGuid()
                    let indexName = indexNameGuid.ToString("N")
                    let index = demoService.GetDemoIndex()
                    let loggingHandler = new LoggingHandler(testServer.Handler)
                    let httpClient = new HttpClient(loggingHandler)
                    let flexClient = new FlexClient(httpClient) :> IFlexClient
                    
                    let parameters = 
                        let parameters = new List<obj>()
                        for p in m.GetParameters() do
                            if p.ParameterType = typeof<String> then parameters.Add(indexName :> obj)
                            else if p.ParameterType = typeof<FlexSearch.Api.Index> then parameters.Add(index :> obj)
                            else if p.ParameterType = typeof<TestServer> then parameters.Add(testServer :> obj)
                            else if p.ParameterType = typeof<LoggingHandler> then parameters.Add(loggingHandler :> obj)
                            else if p.ParameterType = typeof<IFlexClient> then parameters.Add(flexClient :> obj)
                            else if p.ParameterType = typeof<Guid> then parameters.Add(indexNameGuid :> obj)
                            else failwithf "Unknown parameter : %s" (p.ParameterType.ToString())
                        parameters.ToArray()
                    
                    let result = m.Invoke(null, parameters)
                    let attributeValue = 
                        m.GetCustomAttributes(typeof<FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute>, false)
                         .First() :?> FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute
                    example.Title <- if String.IsNullOrWhiteSpace(attributeValue.Title) then m.Name
                                     else attributeValue.Title
                    example.FileName <- attributeValue.FileName
                    example.Request <- loggingHandler.RequestLog().HttpRequest
                    example.Response <- loggingHandler.RequestLog().HttpResponse
                    let responseBody = JsonConvert.DeserializeObject(loggingHandler.RequestLog().ResponseBody)
                    example.ResponseBody <- JsonConvert.SerializeObject(responseBody, jsonSettings)
                    example.ResponseBodyJson <- responseBody
                    let requestBody = 
                        if loggingHandler.RequestLog().RequestBody <> null then
                            JsonConvert.DeserializeObject(loggingHandler.RequestLog().RequestBody)
                        else null
                    example.RequestBody <- JsonConvert.SerializeObject(requestBody, jsonSettings)
                    examples.Add(example)
                    ()
                with e -> 
                    printfn "%s" e.Message
                    raise (e)
        document (examples)
    
    type ApiParameter() = 
        member val Name = "" with get, set
        member val Type = "" with get, set
        member val TypeDescription = "" with get, set
        member val IsRequired = false with get, set
        member val DefaultValue = "" with get, set
        member val Description = "" with get, set
    
    type ApiDocument() = 
        member val Id = "" with get, set
        member val Resource = "" with get, set
        member val Method = "" with get, set
        member val Summary = "" with get, set
        member val Description = "" with get, set
        member val Uri = "" with get, set
        member val Parameters = new List<ApiParameter>() with get, set
    
    type ResourceList() = 
        member val Resources = new List<ApiDocument>() with get, set
    
    let xn s = XName.Get(s)
    
    let GetElementValue (name) (element : XElement) = 
        let result = element.Element(xn name)
        if result = null then ""
        else result.Value.Replace("/n", "")
    
    let GetAttributeValue (name) (element : XElement) = 
        let result = element.Attribute(xn name)
        if result = null then ""
        else result.Value.Replace("\n", "")
    
    let GenerateApiDocumentation() = 
        let files = [| @"G:\GitHub\FlexSearch\build-debug\FlexSearch.Core.xml" |]
        let documents = new List<ApiDocument>()
        for file in files do
            let xml = 
                if File.Exists(file) then XElement.Load(file)
                else failwithf "Documentation file not found: %s" file
            
            let members = xml.Element(xn "members").Elements(xn "member")
            
            let apis = 
                members
                |> Seq.where 
                       (fun x -> 
                       x.Attribute(xn "name").Value.StartsWith("T") && x.Attribute(xn "name").Value.EndsWith("Handler"))
                |> Seq.iter (fun x -> 
                       let document = new ApiDocument()
                       document.Method <- x |> GetElementValue "method"
                       document.Summary <- x |> GetElementValue "summary"
                       document.Description <- x |> GetElementValue "remark"
                       document.Uri <- x |> GetElementValue "uri"
                       document.Resource <- x |> GetElementValue "resource"
                       document.Id <- x |> GetElementValue "id"
                       if String.IsNullOrWhiteSpace(document.Description) then document.Description <- document.Summary
                       // Check for parameters
                       let parameters = 
                           if x.Element(xn "parameters") <> null then 
                               x.Element(xn "parameters").Elements(xn "parameter")
                           else null
                       if parameters <> null then 
                           for p in parameters do
                               let mem = new ApiParameter()
                               mem.Name <- p |> GetAttributeValue "name"
                               mem.IsRequired <- (if (p |> GetAttributeValue "required") = "true" then true
                                                  else false)
                               mem.Description <- p.Value
                               document.Parameters.Add(mem)
                       documents.Add(document))
            for api in documents do
                let path = Path.Combine(apiFolder, api.Id + ".json")
                if Directory.Exists(apiFolder) then 
                    File.WriteAllText(path, JsonConvert.SerializeObject(api, jsonSettings))
            let groups = documents.GroupBy(fun x -> x.Resource.ToLowerInvariant())
            for group in groups do
                let resource = new ResourceList()
                for r in group.OrderBy(fun x -> x.Uri.Trim()) do
                    resource.Resources.Add(r)
                let path = Path.Combine(apiFolder, "resource-" + group.First().Resource + ".json")
                if Directory.Exists(apiFolder) then 
                    File.WriteAllText(path, JsonConvert.SerializeObject(resource, jsonSettings))
            ()
