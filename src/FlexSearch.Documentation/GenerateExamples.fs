namespace FlexSearch.Documention

module GenerateExamples =
    open FlexSearch.IntegrationTests.``Rest webservices tests - Documents``
    open FlexSearch.IntegrationTests.``Rest webservices tests - Indices``
    open FlexSearch.IntegrationTests.``Rest webservices tests - Search``
    open Microsoft.Owin.Testing
    open Microsoft.Owin
    open FlexSearch.Core
    open FlexSearch.TestSupport
    open Autofac
    open System
    open System.IO
    open Newtonsoft.Json
    open Newtonsoft.Json.Converters
    open System.Reflection
    open System.Linq

    let Container = IntegrationTestHelpers.Container
    let exampleFolder = "F:\SkyDrive\FlexSearch Documentation\source\docs\examples"
    let testServer = TestServer.Create(fun app -> 
            let owinServer = new OwinServer(Container.Resolve<IIndexService>(), Container.Resolve<IFlexFactory<IHttpHandler>>())
            owinServer.Configuration(app)
        )

    let document (filename: string) (requestBuilder : RequestBuilder) = 
            let path = Path.Combine(DocumentationConf.DocumentationFolder, filename + ".adoc")
            let output = ResizeArray<string>()
            output.Add("""
    [source,javascript]
    ----------------------------------------------------------------------------------
            """)
            // print request information
            output.Add(requestBuilder.RequestType + " " + requestBuilder.Uri)
            output.Add("")
            if String.IsNullOrWhiteSpace(requestBuilder.RequestBody) = false then 
                output.Add(requestBuilder.RequestBody)
                output.Add("")
            output.Add("")
            output.Add(sprintf "HTTP 1.1 %i %s" (int32(requestBuilder.Response.StatusCode)) (requestBuilder.Response.StatusCode.ToString()))
            for header in requestBuilder.Response.Headers do
                output.Add(header.Key + " : " + header.Value.First()) 
        
            let body = requestBuilder.Response.Content.ReadAsStringAsync().Result
            if String.IsNullOrWhiteSpace(body) <> true then
                let parsedJson = JsonConvert.DeserializeObject(body)
                if parsedJson <> Unchecked.defaultof<_> then 
                    output.Add("")
                    output.Add(sprintf "%s" (JsonConvert.SerializeObject(parsedJson, Formatting.Indented)))
            output.Add("----------------------------------------------------------------------------------")
            if Directory.Exists(DocumentationConf.DocumentationFolder) then 
                File.WriteAllLines(path, output)

    let GenerateIndicesExamples() = 
        let assembly = typeof<FlexSearch.IntegrationTests.``Rest webservices tests - Indices``.Dummy>.Assembly
        for typ in assembly.GetTypes().Where(fun x -> x.IsPublic && x.IsClass) do
            let methods = typ.GetMethods()
            for m in methods.Where(fun x -> x.IsDefined(typeof<FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute>)) do
                printfn "Method Name: %s" m.Name
        


