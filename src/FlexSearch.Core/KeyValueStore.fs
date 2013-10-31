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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open System.Collections.Concurrent
open FlexSearch.Api
open System.Linq
open Newtonsoft.Json
open System.IO

module Store =
    type WriteMsg = WriteMsg of bool * string
    let indexPath = Constants.ConfFolder.Value + "\index.config"
    let keyValPath = Constants.ConfFolder.Value + "\keys.config"

    let writingAgent =
        MailboxProcessor.Start(fun inbox ->
            let rec loop n =                                
                async { 
                    let! msg = inbox.Receive()
                    match msg with
                    | WriteMsg(isIndex, value) ->
                        if isIndex then
                            File.WriteAllText(indexPath, value)
                        else
                            File.WriteAllText(keyValPath, value)

                    return! loop n 
                    }
            loop 0)

    type KeyValueStore() =
        let indexDict = 
            if File.Exists(indexPath) then
                JsonConvert.DeserializeObject<ConcurrentDictionary<string, Index>>(File.ReadAllText(indexPath))
            else
                new ConcurrentDictionary<string, Index>()

        let keyDict = 
            if File.Exists(keyValPath) then
                JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(keyValPath))
            else
                new ConcurrentDictionary<string, string>()

        interface Interface.IKeyValueStore with 
            member this.GetIndexSetting value =
                match indexDict.TryGetValue(value) with
                | (true, a) -> Some(a)
                | _ -> None
          
            member this.DeleteIndexSetting value =
                indexDict.TryRemove(value) |> ignore
                writingAgent.Post(WriteMsg(true, JsonConvert.SerializeObject(indexDict)))
                
            member this.UpdateIndexSetting value =
                match indexDict.TryGetValue(value.IndexName) with
                | (true, a) -> 
                    indexDict.TryUpdate(value.IndexName, value, a) |> ignore
                    writingAgent.Post(WriteMsg(true, JsonConvert.SerializeObject(indexDict)))
                | _ -> 
                    indexDict.TryAdd(value.IndexName, value) |> ignore
                    writingAgent.Post(WriteMsg(true, JsonConvert.SerializeObject(indexDict)))
                
            member this.GetAllIndexSettings() =
                indexDict.Values.ToList()

            member this.GetItem<'T> value =
                match keyDict.TryGetValue(value) with
                | (true, a) -> Some(JsonConvert.DeserializeObject<'T>(a))
                | _ -> None
                
            member this.UpdateItem<'T> key (value: 'T) =
                match keyDict.TryGetValue(key) with
                | (true, a) -> 
                    keyDict.TryUpdate(key, JsonConvert.SerializeObject(value), a) |> ignore
                    writingAgent.Post(WriteMsg(false, JsonConvert.SerializeObject(keyDict)))
                | _ -> 
                    keyDict.TryAdd(key, JsonConvert.SerializeObject(value)) |> ignore
                    writingAgent.Post(WriteMsg(false, JsonConvert.SerializeObject(keyDict)))

            member this.DeleteItem<'T> value =
                keyDict.TryRemove(value) |> ignore
                writingAgent.Post(WriteMsg(false, JsonConvert.SerializeObject(keyDict)))
            



