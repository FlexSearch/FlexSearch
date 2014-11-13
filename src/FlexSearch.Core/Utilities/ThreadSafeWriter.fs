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

type IThreadSafeWriter = 
    abstract WriteFile<'T> : filePath:string * content:'T -> Choice<unit, OperationMessage>
    abstract ReadFile<'T> : filePath:string -> Choice<'T, OperationMessage>
    abstract DeleteFile : filePath:string -> Choice<unit, OperationMessage>

/// <summary>
/// Thread safe file writer. Create one per folder and call it for writing to specific files
/// in that folder from multiple threads. Uses one lock per folder.
/// Note : This is not meant to be used for huge files and should be used for writing configuration
/// files.
/// </summary>
[<Sealed>]
type ThreadSafeFileWiter(formatter : IFormatter) = 
    let GetPathWithExtension(path) =
        if Path.GetExtension(path) <> Constants.SettingsFileExtension then
            path + Constants.SettingsFileExtension
        else
            path
    interface IThreadSafeWriter with
        member this.DeleteFile(filePath : string) : Choice<unit, OperationMessage> = 
            let path = GetPathWithExtension(filePath)
            if File.Exists(path) then 
                use mutex = new Mutex(false, path.Replace("\\", ""))
                File.Delete(path)
                Choice1Of2()
            else 
                // Don't care if file is no longer present
                Choice1Of2()
        
        member this.ReadFile(filePath : string) : Choice<'T, OperationMessage> = 
            let path = GetPathWithExtension(filePath)
            if File.Exists(path) then 
                try 
                    use stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    let response = formatter.DeSerialize<'T>(stream)
                    Choice1Of2(response)
                with e -> 
                    Choice2Of2(Errors.FILE_NOT_FOUND
                               |> GenerateOperationMessage
                               |> Append("filepath", path)
                               |> Append("exception", e.Message))
            else 
                Choice2Of2(Errors.FILE_NOT_FOUND
                           |> GenerateOperationMessage
                           |> Append("filepath", path))
        
        member this.WriteFile<'T>(filePath : string, content : 'T) = 
            let path = GetPathWithExtension(filePath)
            use mutex = new Mutex(false, path.Replace("\\", ""))
            Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
            try 
                mutex.WaitOne() |> ignore
                use file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)
                let byteContent = System.Text.UTF8Encoding.UTF8.GetBytes(formatter.SerializeToString(content))
                file.Write(byteContent, 0, byteContent.Length)
                mutex.ReleaseMutex()
                Choice1Of2()
            with e -> 
                mutex.ReleaseMutex()
                Choice2Of2(Errors.FILE_WRITE_ERROR
                           |> GenerateOperationMessage
                           |> Append("filepath", path)
                           |> Append("exception", e.Message))
