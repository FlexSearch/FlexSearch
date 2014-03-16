module HttpDocumentation

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open HttpClient
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Threading

let generateDocumentation() =
    let serverSettings = GetServerSettings(ConfFolder.Value + "\\Config.json")
    let node = new NodeService(serverSettings, true)
    node.Start()