#r "System"
open System.IO

let generateThriftFiles(filename) =
    let p = new System.Diagnostics.Process()
    p.StartInfo.FileName <- Path.Combine(__SOURCE_DIRECTORY__ , "thrift-0.9.1.exe")
    if File.Exists(p.StartInfo.FileName) <> true then
        failwithf "Thrift compiler cannot be located: %s." p.StartInfo.FileName
    //--gen csharp:serial,hashcode,wcf,union
    p.StartInfo.Arguments <- (sprintf "--gen csharp:serial,wcf,union,async %s" filename)
    p.StartInfo.WorkingDirectory <- __SOURCE_DIRECTORY__
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.UseShellExecute <- false
    p.Start() |> ignore 
    let result = p.StandardOutput.ReadToEnd()
    printf "Generating: %s " filename
    if result = "" then
        printfn " [Success]"
    else 
        printfn " [%A]" result
    

let addDataMemberOrder() =
    for file in Directory.GetFiles(__SOURCE_DIRECTORY__ + "\gen-csharp\FlexSearch\Api", "*.cs", System.IO.SearchOption.AllDirectories) do
        printfn "%s" file
        let fileContent = File.ReadAllLines(file)
        let targetFile = new ResizeArray<string>()
        let mutable count: int = 1
        for line in fileContent do
            
            let mutable result = line
            if result.Contains("//using System.ServiceModel;") then
                result <- result.Replace("//using System.ServiceModel;", "using System.ServiceModel;")

            if result.Contains("[DataMember]") then
                result <- result.Replace("[DataMember]", "[DataMember(Order = " + count.ToString() + ")]")
                count <- count + 1

            targetFile.Add(result)
                
        File.WriteAllLines(file, targetFile.ToArray())

generateThriftFiles("Connector.thrift")
generateThriftFiles("Service.thrift")
generateThriftFiles("Api.thrift")
generateThriftFiles("Message.thrift")
generateThriftFiles("Dto.thrift")
addDataMemberOrder()
