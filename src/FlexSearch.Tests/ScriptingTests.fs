module ScriptTests

open FlexSearch.Api.Model
open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic

type PreSearchScriptTests() = 
    member __.``Script should compile``() = 
        let scriptSrc = """
void Execute(SearchQuery query){
	string value = String.Empty;
    var fields = query.Variables;
	var queryString = query.QueryString;
    if (fields.TryGetValue("test", out value)) {	
		fields["test"] = "test1";
	}
}
"""
        let (_, sut) = Compiler.compileScript (scriptSrc, "test", ScriptType.PreSearch) |> extract
        let testDict = new Dictionary<string, string>()
        testDict.Add("test", "test0")
        match sut with
        | PreSearchScript(computedDelegate) -> 
            let result = computedDelegate.Invoke(new SearchQuery(Variables = testDict))
            test <@ testDict.["test"] = "test1" @>
        | _ -> failwithf "Wrong Script Type returned"

type ComputedScriptTests() = 
    member __.``Script should compile``() = 
        let scriptSrc = """
string Execute(string indexName, string fieldName, IReadOnlyDictionary<string, string> fields, string[] parameters){
	string value = String.Empty;
    if (fields.TryGetValue("test", out value)) {	
		value = "test1";
	}
    return value;
}
"""
        let (_, sut) = Compiler.compileScript(scriptSrc, "test", ScriptType.Computed) |> extract
        let testDict = new Dictionary<string, string>()
        testDict.Add("test", "test0")
        match sut with
        | ComputedScript(computedDelegate) -> 
            let result = computedDelegate.Invoke("test", "test", testDict, Array.empty)
            test <@ result = "test1" @>
        | _ -> failwithf "Wrong Script Type returned"
