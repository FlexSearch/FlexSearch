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

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.FileProviders

open System
open System.Text

/// This module provides abstraction for settings to be used across the 
/// project. Both internal and third party plug-ins can rely on this
/// abstraction to load system level settings in a consistent manner.
[<RequireQualifiedAccess; AutoOpen>]
module Settings = 
    let defaultSettingsFilePath = Constants.ConfFolder +/ "settings.ini"
    
    [<Literal>]
    let ServerKey = "Server"
    
    [<Literal>]
    let SecurityKey = "Security"

    [<Literal>]
    let HttpPort = "HttpPort"

    [<Literal>]
    let ServerType = "ServerType"

    [<Literal>]
    let UseHttps = "UseHttps"

    [<Literal>]
    let HttpsCertificatePath = "HttpsCertificatePath"

    [<Literal>]
    let HttpsCertificatePassword = "HttpsCertificatePassword"
    
    /// Create settings from the path 
    let create (path : string) = 
        let configBuilder = new ConfigurationBuilder()
        // We have to set the base path to use the indicated directory because
        // FileProvider implementation doesn't allow for absolute paths when getting a file.
        configBuilder.SetBasePath(IO.Path.GetDirectoryName path)
                     .AddIniFile(IO.Path.GetFileName path, false)
                     .AddEnvironmentVariables("FS_")
                     |> ignore
        
        try 
            ok <| configBuilder.Build()
        with e -> fail <| UnableToParseConfig(path, exceptionPrinter e)
    
    type T(source : IConfiguration) = 
        
        member val ConfigurationSource = source

        member __.Get(section : string, key : string, defaultValue : string) = 
            match source.[sprintf "%s:%s" section key] with
            | null -> defaultValue
            | v -> v
        
        /// Get key value as int from a section 
        member __.GetInt(section : string, key : string, defaultValue) = 
            source.[sprintf "%s:%s" section key] |> pInt defaultValue
        
        /// Get key value as long from a section
        member __.GetLong(section : string, key : string, defaultValue) = 
            source.[sprintf "%s:%s" section key] |> pLong defaultValue
        
        /// Get key value as double from a section
        member __.GetDouble(section : string, key : string, defaultValue) = 
            source.[sprintf "%s:%s" section key] |> pDouble defaultValue
        
        /// Get key value as bool from a section
        member __.GetBool(section : string, key : string, defaultValue) = 
            source.[sprintf "%s:%s" section key] |> pBool defaultValue
        
        /// Get an absolute path from a section. If the path starts with . then
        /// it will be automatically converted to an absolute path
        member __.GetPath(section : string, key : string, defaultValue) = 
            let path = 
                match source.[sprintf "%s:%s" section key] with
                | null -> defaultValue
                | v -> v
            Helpers.generateAbsolutePath path
        
        /// Gets the Base64 UTF8 encoded password from the environment variable
        /// and decodes it.
        member __.GetPassword(environmentVariableName) =
            try
                source.GetValue<string>(environmentVariableName)
                |> Convert.FromBase64String
                |> Encoding.UTF8.GetString
            with _ -> 
                failwithf "Could not parse the password from the variable %s. Make sure it is Base64 encoded." environmentVariableName

        static member GetDefault() = 
            let configBuilder = new ConfigurationBuilder()
            configBuilder.AddInMemoryCollection() |> ignore
            new T(configBuilder.Build())
