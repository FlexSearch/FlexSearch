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
    interface IThreadSafeWriter with
        
        member this.DeleteFile(filePath : string) : Choice<unit, OperationMessage> = 
            if File.Exists(filePath) then 
                use mutex = new Mutex(false, filePath.Replace("\\", ""))
                File.Delete(filePath)
                Choice1Of2()
            else 
                // Don't care if file is no longer present
                Choice1Of2()
        
        member this.ReadFile(filePath : string) : Choice<'T, OperationMessage> = 
            if File.Exists(filePath) then 
                try 
                    use stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    let response = formatter.DeSerialize<'T>(stream)
                    Choice1Of2(response)
                with e -> 
                    Choice2Of2(Errors.FILE_NOT_FOUND
                               |> GenerateOperationMessage
                               |> Append("filepath", filePath)
                               |> Append("exception", e.Message))
            else 
                Choice2Of2(Errors.FILE_NOT_FOUND
                           |> GenerateOperationMessage
                           |> Append("filepath", filePath))
        
        member this.WriteFile<'T>(filePath : string, content : 'T) = 
            use mutex = new Mutex(false, filePath.Replace("\\", ""))
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore
            try 
                mutex.WaitOne() |> ignore
                use file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)
                let byteContent = System.Text.UTF8Encoding.UTF8.GetBytes(formatter.SerializeToString(content))
                file.Write(byteContent, 0, byteContent.Length)
                mutex.ReleaseMutex()
                Choice1Of2()
            with e -> 
                mutex.ReleaseMutex()
                Choice2Of2(Errors.FILE_WRITE_ERROR
                           |> GenerateOperationMessage
                           |> Append("filepath", filePath)
                           |> Append("exception", e.Message))
