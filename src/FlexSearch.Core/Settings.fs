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

open FSharp.Data
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
module Settings =

    /// Xml setting provider for server config
    type private FlexServerSetting = JsonProvider<"""
        {
            "HttpPort" : 9800,
            "TcpPort" : 9900,
            "IsMaster" : false,
            "DataFolder" : "./data"
        }
    """
    >

    // ----------------------------------------------------------------------------
    /// Concerete implementation of ISettingsServer
    // ----------------------------------------------------------------------------

    open System.Linq
    open System.Data.SQLite
    open System.IO
    
    let private sqlCreateTable = """
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
    
    type SettingsStore(path : string) =
        let mutable settings = None
        let mutable db = None

        do
            let fileXml = Helpers.LoadFile(path)
            let parsedResult = FlexServerSetting.Parse(fileXml)          

            let setting =
                {
                    LuceneVersion = Constants.LuceneVersion
                    HttpPort = parsedResult.HttpPort
                    TcpPort = parsedResult.TcpPort
                    DataFolder = Helpers.GenerateAbsolutePath(parsedResult.DataFolder)
                    PluginFolder = Constants.PluginFolder.Value
                    ConfFolder = Constants.ConfFolder.Value
                    NodeName = ""
                    NodeRole = NodeRole.UnDefined
                    MasterNode = IPAddress.None
                }
            
            let sqlLiteDbPath = setting.ConfFolder + "//conf"
            let connectionString = sprintf "%s;Version=3;UseUTF16Encoding=True;" sqlLiteDbPath
            
            let dbConnection =
                if File.Exists(sqlLiteDbPath) then
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
               
            settings <- Some(setting)
            db <- Some(dbConnection)

        interface IPersistanceStore with
            member this.Settings = settings.Value
            
            member this.Get<'T>(key) =
                let instanceType = typeof<'T>.FullName
                if key = "" then
                    None
                else
                    let sql = sprintf "select * from keyvalue limit 1 where type=%s and key=%s" instanceType key
                    let command = new SQLiteCommand(sql, db.Value)
                    let reader = command.ExecuteReader()
                    let mutable result : 'T = Unchecked.defaultof<'T>
                    while reader.Read() do
                        result <- Newtonsoft.Json.JsonConvert.DeserializeObject<'T>(reader.GetString(2)) 
                    Some(result)


            member this.GetAll<'T>() =
                let sql = sprintf "select * from keyvalue where type=%s" typeof<'T>.FullName
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
                    let sql = sprintf "select * from keyvalue limit 1 where type='%s' and key='%s'" instanceType key
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