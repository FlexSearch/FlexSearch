namespace FlexSearch.Documention

module GenerateExamples = 
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
    open System.Text
    
    let template = """
+++
<span class="label">Example</span>
<table id="fetch-an-index-detail" class="tableblock tableblock-example">
	<tr>
		<td>
			<p class="rest-example-title">{title}</p>
		</td>
		<td>
			<a class="right button small" onClick="$('#{guid}').toggle();">Show/Hide response</a>
		</td>

	</tr>
	<tr id="{guid}" style="display:none;">
		<td colspan="2">	
			<pre class="rest-output"><code>
{code}
            </code></pre>
	    </td>
    </tr>	
</table>
+++
"""
    let Container = IntegrationTestHelpers.Container
    let exampleFolder = "F:\SkyDrive\FlexSearch Documentation\source\docs\examples"
    
    let testServer = 
        TestServer.Create(fun app -> 
            let owinServer = 
                new OwinServer(Container.Resolve<IIndexService>(), Container.Resolve<IFlexFactory<IHttpResource>>(), Container.Resolve<ILogService>())
            owinServer.Configuration(app))
    
    let document (filename : string) (title : string) (requestBuilder : RequestBuilder) = 
        let path = Path.Combine(DocumentationConf.DocumentationFolder, filename + ".adoc")
        let output = new StringBuilder()
        // print request information
        output.AppendLine(requestBuilder.RequestType + " " + requestBuilder.Uri).AppendLine("") |> ignore
        if String.IsNullOrWhiteSpace(requestBuilder.RequestBody) = false then 
            let parsedJson = JsonConvert.DeserializeObject(requestBuilder.RequestBody)
            if parsedJson <> Unchecked.defaultof<_> then 
                output.AppendLine("")
                      .AppendLine(sprintf "%s" (JsonConvert.SerializeObject(parsedJson, Formatting.Indented)))
                      .AppendLine("") |> ignore
        output.AppendLine("----------------------------------") |> ignore
        output.AppendLine
            (sprintf "HTTP 1.1 %i %s" (int32 (requestBuilder.Response.StatusCode)) 
                 (requestBuilder.Response.StatusCode.ToString())) |> ignore
        for header in requestBuilder.Response.Headers do
            output.AppendLine(header.Key + " : " + header.Value.First()) |> ignore
        let body = requestBuilder.Response.Content.ReadAsStringAsync().Result
        if String.IsNullOrWhiteSpace(body) <> true then 
            let parsedJson = JsonConvert.DeserializeObject(body)
            if parsedJson <> Unchecked.defaultof<_> then 
                output.AppendLine("")
                      .AppendLine(sprintf "%s" (JsonConvert.SerializeObject(parsedJson, Formatting.Indented))) |> ignore
        if Directory.Exists(DocumentationConf.DocumentationFolder) then 
            let outputFile = 
                template.Replace("{guid}", (Guid.NewGuid().ToString("N"))).Replace("{code}", output.ToString())
                        .Replace("{title}", title)
            File.WriteAllText(path, outputFile)
    
    let GenerateIndicesExamples() = 
        let assembly = typeof<FlexSearch.IntegrationTests.Rest.Dummy>.Assembly
        for typ in assembly.GetTypes().Where(fun x -> x.IsPublic && x.IsClass) do
            let methods = typ.GetMethods()
            for m in methods.Where
                         (fun x -> x.IsDefined(typeof<FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute>)) do
                printfn "Method Name: %s" m.Name
                try 
                    let paramCount = m.GetParameters().Count()
                    let param1 = testServer
                    let param2 = Guid.NewGuid()
                    
                    let parameters = 
                        if paramCount = 2 then 
                            [| param1 :> obj
                               param2 :> obj |]
                        else [| param1 :> obj |]
                    
                    let result = m.Invoke(null, parameters) :?> RequestBuilder
                    let attributeValue = 
                        m.GetCustomAttributes(typeof<FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute>, false)
                         .First() :?> FlexSearch.TestSupport.UnitTestAttributes.ExampleAttribute
                    
                    let title = 
                        if String.IsNullOrWhiteSpace(attributeValue.Title) then m.Name
                        else attributeValue.Title
                    document attributeValue.FileName title result
                    ()
                with e -> printfn "%s" e.Message
