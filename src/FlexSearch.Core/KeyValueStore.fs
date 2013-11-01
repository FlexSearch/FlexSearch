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
open System.Reactive.Subjects
open System

module Store =
    type WriteMsg = WriteMsg of bool * string
    let indexPath = Constants.ConfFolder.Value + "\index.config"
    let keyValPath = Constants.ConfFolder.Value + "\keys.config"

    type KeyValueStore() =
        let store = new Subject<WriteMsg>()
        
        let sub = store.Subscribe(fun x ->
            match x with
            | WriteMsg(isIndex, value) ->
                if isIndex then
                    File.WriteAllText(indexPath, value)
                else
                    File.WriteAllText(keyValPath, value)
            )

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
        
        let mutable disposed = false;
        let cleanup(disposing:bool) = 
            if not disposed then
                disposed <- true
            if disposing then
                sub.Dispose()
        
        override self.Finalize() = 
            cleanup(false)

        interface IDisposable with
            member this.Dispose() =
                cleanup(true)
                GC.SuppressFinalize(this)
        
        interface Interface.IKeyValueStore with 
            member this.GetIndexSetting value =
                match indexDict.TryGetValue(value) with
                | (true, a) -> Some(a)
                | _ -> None
          
            member this.DeleteIndexSetting value =
                indexDict.TryRemove(value) |> ignore
                store.OnNext(WriteMsg(true, JsonConvert.SerializeObject(indexDict)))
                                
            member this.UpdateIndexSetting value =
                match indexDict.TryGetValue(value.IndexName) with
                | (true, a) -> 
                    indexDict.TryUpdate(value.IndexName, value, a) |> ignore
                    store.OnNext(WriteMsg(true, JsonConvert.SerializeObject(indexDict)))
                | _ -> 
                    indexDict.TryAdd(value.IndexName, value) |> ignore
                    store.OnNext(WriteMsg(true, JsonConvert.SerializeObject(indexDict)))
                
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
                | _ -> 
                    keyDict.TryAdd(key, JsonConvert.SerializeObject(value)) |> ignore
                store.OnNext(WriteMsg(false, JsonConvert.SerializeObject(keyDict)))

            member this.DeleteItem<'T> value =
                keyDict.TryRemove(value) |> ignore
                store.OnNext(WriteMsg(false, JsonConvert.SerializeObject(keyDict)))
            



