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
namespace FlexSearch.Core
open IniParser
open System
open System.Text

/// This module provides abstraction for settings to be used across the 
/// project. Both internal and third party plugins can rely on this
/// abstraction to load system level settings in a consistent manner.
[<RequireQualifiedAccess; AutoOpen>]
module Settings =
    let defaultSettingsFilePath = Constants.ConfFolder +/ "settings.ini"
    
    [<Literal>]
    let ServerKey = "Server"

    [<Literal>]
    let HttpPort = "HttpPort"

    /// Create settings from the path 
    let create(path : string) =
        let parser = new FileIniDataParser()
        parser.Parser.Configuration.CaseInsensitive <- true
        parser.Parser.Configuration.SkipInvalidLines <- true
        parser.Parser.Configuration.ThrowExceptionsOnError <- false
        let source = parser.ReadFile(path)
        if parser.Parser.HasError then
            let sb = new StringBuilder()
            parser.Parser.Errors 
            |> Seq.iter (exceptionPrinter >> sb.AppendLine >> ignore)
            fail <| UnableToParseConfig(path,  sb.ToString())
        else
            ok <| source

    type T(source : Model.IniData) =
        member  __.Get(section : string, key : string, defaultValue : string) =
            match source.[section].[key] with
            | null -> defaultValue
            | v -> v

        /// Get key value as int from a section 
        member  __.GetInt(section : string, key : string, defaultValue) =
            source.[section].[key] |> pInt defaultValue

        /// Get key value as long from a section
        member  __.GetLong(section : string, key : string, defaultValue) =
            source.[section].[key] |> pLong defaultValue

        /// Get key value as double from a section
        member  __.GetDouble(section : string, key : string, defaultValue) =
            source.[section].[key] |> pDouble defaultValue

        /// Get key value as bool from a section
        member  __.GetBool(section : string, key : string, defaultValue) =
            source.[section].[key] |> pBool defaultValue

        /// Get an absolute path from a section. If the path starts with . then
        /// it will be automatically converted to an absolute path
        member __.GetPath(section : string, key : string, defaultValue) =
            let path = 
                match source.[section].[key] with
                | null -> defaultValue 
                | v -> v
            Helpers.GenerateAbsolutePath path

        static member GetDefault() =
            let parser = new FileIniDataParser()
            parser.Parser.Configuration.CaseInsensitive <- true
            parser.Parser.Configuration.SkipInvalidLines <- true
            parser.Parser.Configuration.ThrowExceptionsOnError <- false
            let source = parser.Parser.Parse("")
            new T(source)
                
