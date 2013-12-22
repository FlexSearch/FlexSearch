// ----------------------------------------------------------------------------
// Flexsearch settings (Settings.fs)
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

open FlexSearch.Utility
open FlexSearch.Api
open FlexSearch.Core

open System
open System.Net
open System.Collections.Generic
open System.Xml
open System.Xml.Linq


// ----------------------------------------------------------------------------
/// Top level settings parse function   
// ----------------------------------------------------------------------------   
module Store =

    open System.Linq
    open System.Data.SQLite
    open System.IO

    /// A reusable key value persistance store build on top of sqlite
    type PersistanceStore(path : string, isMemory: bool) =
        
        let sqlCreateTable = """
            CREATE TABLE [keyvalue] (
            [id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
            [key] TEXT  NOT NULL,
            [value] TEXT  NOT NULL,
            [type] TEXT  NOT NULL,
            [timestamp] TIMESTAMP  NOT NULL
            );

            CREATE INDEX [keyvalue_idx] ON [keyvalue](
            [key]  DESC,
            [type]  DESC
            );
            """

        let mutable db = None

        do           
            let sqlLiteDbPath = path
            let connectionString = 
                if isMemory then
                    "Data Source=:memory:;Version=3;New=True;UseUTF16Encoding=True;"
                else
                    sprintf "Data Source=%s;Version=3;UseUTF16Encoding=True;" sqlLiteDbPath

            let dbConnection =
                if isMemory then
                    let dbConnection = new SQLiteConnection(connectionString)
                    dbConnection.Open()
                    let command = new SQLiteCommand(sqlCreateTable, dbConnection)
                    command.ExecuteNonQuery() |> ignore
                    dbConnection   
                elif File.Exists(sqlLiteDbPath) then
                    let dbConnection = new SQLiteConnection(connectionString)
                    dbConnection.Open()
                    dbConnection
                else
                    SQLiteConnection.CreateFile(sqlLiteDbPath)
                    let dbConnection = new SQLiteConnection(connectionString)
                    dbConnection.Open()
                    let command = new SQLiteCommand(sqlCreateTable, dbConnection)
                    command.ExecuteNonQuery() |> ignore
                    dbConnection
               
            db <- Some(dbConnection)
        
        member private this.get<'T>(key) = 
            let instanceType = typeof<'T>.FullName
            if key = "" then
                None
            else
                let sql = sprintf "select * from keyvalue where type = '%s' and key = '%s' limit 1" instanceType key
                let command = new SQLiteCommand(sql, db.Value)
                let reader = command.ExecuteReader()
                let mutable result : 'T = Unchecked.defaultof<'T>
                while reader.Read() do
                    result <- Newtonsoft.Json.JsonConvert.DeserializeObject<'T>(reader.GetString(2)) 
                Some(result)

  
        interface IPersistanceStore with
            member this.Get<'T>(key) = this.get<'T>(key)

            member this.GetAll<'T>() =
                let sql = sprintf "select * from keyvalue where type= '%s'" typeof<'T>.FullName
                let command = new SQLiteCommand(sql, db.Value)
                let reader = command.ExecuteReader()
                let mutable result : List<'T> = new List<'T>()
                while reader.Read() do
                    result.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<'T>(reader.GetString(2)))
                result :> IEnumerable<'T>

            member this.Put<'T> (key) (instance : 'T) =
                let instanceType = typeof<'T>.FullName
                if key = "" then
                    false
                else
                    let sql = sprintf "select * from keyvalue where type='%s' and key='%s' limit 1" instanceType key
                    let command = new SQLiteCommand(sql, db.Value)
                    let reader = command.ExecuteReader()
                    let mutable result = false
                    while reader.Read() do
                        result <- true

                    let value = Newtonsoft.Json.JsonConvert.SerializeObject(instance)
                    if result then
                        // Exists so lets update it 
                        let sql = sprintf "update keyvalue SET type = '%s', key = '%s', value = '%s', timestamp = '%s' where type = '%s' and key = '%s'" instanceType key value (DateTime.Now.ToString()) instanceType key
                        let command = new SQLiteCommand(sql, db.Value)
                        command.ExecuteNonQuery() |> ignore
                    else
                        // Does not exist so create it
                        let sql = sprintf "insert into keyvalue (type, key, value, timestamp) values ('%s', '%s', '%s', '%s')" instanceType key value (DateTime.Now.ToString())
                        let command = new SQLiteCommand(sql, db.Value)
                        command.ExecuteNonQuery() |> ignore

                    true