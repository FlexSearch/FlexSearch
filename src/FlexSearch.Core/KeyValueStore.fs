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
open SQLite.Net.Attributes
open SQLite.Net.Interop
open SQLite.Net.Platform.Win32
open System.Linq
open Newtonsoft.Json
open System.IO

module Store =

    type KeyValue() =       
        [<PrimaryKey>]
        member val Key = "" with get, set
        member val Value = "" with get, set

    type IndexKeyValue() = 
        [<PrimaryKey>]
        member val IndexName = "" with get, set
        member val Value = Unchecked.defaultof<Index> with get, set

    type KeyValueStore() =
        let path = Constants.ConfFolder.Value + "\settings.config"
        let db = new SQLite.Net.SQLiteConnection(new SQLitePlatformWin32(), path, false)
        
        do
            if File.Exists(path) <> true then
                db.CreateTable<KeyValue>() |> ignore
                db.CreateTable<IndexKeyValue>() |> ignore

        interface Interface.IKeyValueStore with 
            member this.GetIndexSetting value =
                let result = db.Table<IndexKeyValue>() |> Seq.tryFind(fun x -> x.IndexName = value)
                match result with
                | Some(a) -> Some(a.Value)
                | _ -> None
          
            member this.DeleteIndexSetting value =
                db.Delete<IndexKeyValue>(value) |> ignore
                
            member this.UpdateIndexSetting value =
                let index = new IndexKeyValue()
                index.IndexName <- value.IndexName
                index.Value <- value
                db.Update(value) |> ignore

            member this.GetAllIndexSettings() =
                let values = db.Table<IndexKeyValue>().ToList()
                let indices = new ResizeArray<Index>()
                values |> Seq.iter( fun x -> 
                        indices.Add(x.Value)
                    )
                indices

            member this.GetItem<'T> value =
                let result = db.Table<KeyValue>() |> Seq.tryFind(fun x -> x.Key = value)
                match result with
                | Some(a) -> Some(JsonConvert.DeserializeObject<'T>(a.Value))
                | _ -> None

            member this.UpdateItem<'T> key (value: 'T) =
                let keyvalue = new KeyValue()
                keyvalue.Key <- key
                keyvalue.Value <- JsonConvert.SerializeObject(value)
                db.Update(value) |> ignore

            member this.DeleteItem value =
                db.Delete<KeyValue>(value) |> ignore
            



