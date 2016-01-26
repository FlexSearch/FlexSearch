// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Server

open System
open System.IO
open FlexSearch.Core
open FlexSearch.Core.Helpers
open Newtonsoft.Json
open Microsoft.Extensions.Configuration

module HomepageGenerator =
    let getDirName dir = (new DirectoryInfo(dir)).Name

    let getBasePath (moduleName : string) = sprintf "apps/%s" moduleName

    let private getInfoJson (moduleFolder : string) =
        let path = moduleFolder +/ "info.json"
        if not <| File.Exists path then fail <| FileNotFound(path)
        else 
            let configBuilder = new ConfigurationBuilder()
            let conf = configBuilder.AddJsonFile(path).Build() 
            conf.Item "basePath" <- getBasePath <| getDirName moduleFolder
            ok conf

    let private injectConfiguration (info : IConfigurationRoot) (confKey : string)  (moduleFolder : string) (cardTemplate : string) =
        let conf = info.Get<string>(key = confKey)
        if isNull conf then fail <| FileReadError(moduleFolder, sprintf "Couldn't find the '%s' property in the info.json file" confKey)
        else cardTemplate.Replace("{{" + confKey + "}}", conf) |> ok

    let private getCardForModule (cardTemplate : string) (moduleFolder : string) =
        getInfoJson moduleFolder
        >>= fun info -> [ "title"; "description"; "version"; "url"; "imgSrc"; "basePath" ]
                        |> Seq.fold (fun acc key -> acc >>= injectConfiguration info key moduleFolder) 
                                     (ok cardTemplate)

    let private getCardTemplate (srcFolder : string) = 
        let path = srcFolder +/ "cardTemplate.html"
        if not <| File.Exists path then fail <| FileNotFound(path)
        else File.ReadAllText path |> ok

    let private getHomeHtml (srcFolder : string) = 
        let path = srcFolder +/ "home.html"
        if not <| File.Exists path then fail <| FileNotFound(path)
        else File.ReadAllText path |> ok

    let private groupInBatchesOf batchSize (source : 'T seq)=
        source
        |> Seq.mapi (fun i c -> (i, c))
        |> Seq.groupBy (fun (i, c) -> i / batchSize)
        |> Seq.map (fun (_,group) -> group |> Seq.map snd)

    let injectCards () =
        getCardTemplate WebFolder
        // Populate the cards
        >>= fun cardTemplate -> loopDir (WebFolder +/ "apps")
                                // Get each card according to the configuration
                                |> Seq.map (getCardForModule cardTemplate)
                                // convert "Result<string> list" into "string list Result"
                                |> Seq.fold (fun acc card -> acc >>= fun cards -> match card with
                                                                                  | Ok c -> c :: cards |> ok
                                                                                  | Fail(e) -> fail e) 
                                            (ok [])
        // Get the Home page template
        >>= fun cards -> getHomeHtml WebFolder >>= fun homeHtml -> (homeHtml, cards) |> ok
        // Inject the cards into the homepage
        >>= fun (homeHtml, cards) -> cards 
                                     |> groupInBatchesOf 3
                                     // wrap 3 cards in a 'row'
                                     |> Seq.fold (fun acc group -> acc + (sprintf "<div class='row'>\n%s\n</div>" <| String.Join("\n", group))) ""
                                     // inject the HTML rows in the homepage HTML
                                     |> fun cardsHtml -> homeHtml.Replace("<!-- {{inject-cards}} -->", cardsHtml)
                                     |> ok

    let injectCss (homeHtml : string) =
        loopDir (WebFolder +/ "apps")
        |> Seq.head
        // Get the Css references
        |> fun firstAppFolder -> 
            let appName = getDirName firstAppFolder
            loopFiles <| firstAppFolder +/ "styles"
            |> Seq.map Path.GetFileName
            |> Seq.map (fun css -> sprintf "<link rel='stylesheet' href='apps/%s/styles/%s'>" appName css)
        // Inject them into the HTML
        |> fun cssRefs -> 
            let toInject = String.Join("\n", cssRefs)
            homeHtml.Replace("<!-- {{inject-css}} -->", toInject)
        |> ok

    let buildHomePage() =
        injectCards()
        >>= injectCss
        >>= fun fullHomeHtml -> File.WriteAllText(WebFolder +/ "home.html", fullHomeHtml) |> ok

