// ----------------------------------------------------------------------------
// FlexSearch settings (Settings.fs)
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Utility
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Threading
open System.Xml
open System.Xml.Linq
open System.IO

type IThreadSafeWriter = 
    abstract WriteToFile : filePath:string * content:string -> Choice<unit, OperationMessage>

/// <summary>
/// Thread safe file writer. Create one per folder and call it for writing to specific files
/// in that folder from multiple threads. Uses one lock per folder.
/// Note : This is not meant to be used for huge files and should be used for writing configuration
/// files.
/// </summary>
[<Sealed>]
type ThreadSafeFileWiter() = 
    interface IThreadSafeWriter with
        member this.WriteToFile(filePath : string, content : string) = 
            use mutex = new Mutex(false, filePath.Replace("\\", ""))
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore
            try 
                mutex.WaitOne() |> ignore
                use file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)
                let byteContent = System.Text.UTF8Encoding.UTF8.GetBytes(content)
                file.Write(byteContent, 0, byteContent.Length)
                mutex.ReleaseMutex()
                Choice1Of2()
            with e -> 
                mutex.ReleaseMutex()
                Choice2Of2(e.Message |> GenerateOperationMessage)