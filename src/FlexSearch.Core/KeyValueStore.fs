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
open FlexSearch.Api.Types
open SQLite.Net.Attributes
open SQLite.Net.Interop
open SQLite.Net.Platform.Win32
open System.Linq
open Newtonsoft.Json

module Store =

    type KeyValue() =       
        [<PrimaryKey>]
        member val Key = "" with get, set
        member val Value = "" with get, set

    type KeyValueStore() =
        let path = Constants.ConfFolder.Value + "\settings.config"
        let db = new SQLite.Net.SQLiteConnection(new SQLitePlatformWin32(), path, false)

        interface Interface.IKeyValueStore with
            
            member this.GetIndexSetting value =
                let result = db.Table<KeyValue>() |> Seq.tryFind(fun x -> x.Key = value)
                match result with
                | Some(a) -> Some(JsonConvert.DeserializeObject<Index>(a.Value))
                | _ -> None
          
            member this.DeleteIndexSetting value =
                db.Delete<KeyValue>(value) |> ignore
                
            member this.UpdateIndexSetting value =
                db.Update(value) |> ignore

            member this.GetAllIndexSettings value =
                let values = db.Table<KeyValue>()

            



