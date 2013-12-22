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

module HttpHelpers =
    open System
    open System.Net
    open System.Text
    open System.Threading
    open FlexSearch.Core
    open FlexSearch
    open FlexSearch.Api
    open Newtonsoft.Json
    open System.Collections.Concurrent
    open ProtoBuf
    open System.IO
    open Newtonsoft.Json

    type HttpResponseError =
        {
            DeveloperMessage : string
            UserMessage : string
            ErrorCode : int
        }

    
    /// Helper method to serialize cluster messages
    let protoSerialize (message: 'a) =
        use stream = new MemoryStream()
        Serializer.Serialize(stream, message)
        stream.GetBuffer()
    

    /// Helper method to deserialize cluster messages
    let protoDeserialize<'T> (message: byte[]) =
        use stream = new MemoryStream(message)
        Serializer.Deserialize<'T>(stream)
     

    /// Write http response
    let writeResponse (statusCode: System.Net.HttpStatusCode) (res: obj) (request: HttpListenerRequest) (response: HttpListenerResponse) =
        let matchType format res =
            match format with
            | "text/json"
            | "application/json" ->
                response.ContentType <- "text/json" 
                let result = JsonConvert.SerializeObject(res)
                Some(Encoding.UTF8.GetBytes(result))
            | "application/x-protobuf" 
            | "application/octet-stream" ->
                response.ContentType <- "application/x-protobuf"
                Some(protoSerialize(res))
            | _ -> None

        let result =
            if request.AcceptTypes = null then
                matchType request.ContentType res
            else
                matchType request.AcceptTypes.[0] res
        
        response.StatusCode <- int statusCode
        match result with
        | None -> 
            response.StatusCode <- int HttpStatusCode.NotAcceptable
        | Some(x) -> 
            response.ContentLength64 <- int64 x.Length 
            response.OutputStream.Write(x, 0, x.Length)   
        
        response.Close()


    /// Write http response
    let getRequestBody<'T> (request: HttpListenerRequest) =
        if request.HasEntityBody then
            match request.ContentType with
            | "text/json"
            | "application/json" -> 
                let body =
                    use reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding)
                    reader.ReadToEnd()
                try
                    let result = JsonConvert.DeserializeObject<'T>(body)
                    Choice1Of2(result)
                with
                    | ex -> 
                        Choice2Of2({ DeveloperMessage = ex.Message; UserMessage = "The server is unable to parse the request body."; ErrorCode = 1004})
            | "application/x-protobuf" 
            | "application/octet-stream" -> 
                try
                    Choice1Of2(ProtoBuf.Serializer.Deserialize<'T>(request.InputStream))
                with
                    | ex -> Choice2Of2({ DeveloperMessage = ex.Message; UserMessage = "The server is unable to parse the request body."; ErrorCode = 1004})
            | _ -> Choice2Of2({ DeveloperMessage = "Unsupported content-type."; UserMessage = "Unsupported content-type."; ErrorCode = 1004})
        else
            Choice2Of2({ DeveloperMessage = "No body defined."; UserMessage = "No body defined."; ErrorCode = 1004})
              
                

    let (|GET|_|) (value: string) (request: System.Net.HttpListenerRequest) = 
        if request.HttpMethod = "GET" && request.Url.Segments.Length >= 2 && (value = "*" || request.Url.Segments.[2] = value) then               
            Some(value)
        else
            None

    let (|POST|_|) (value) (request: System.Net.HttpListenerRequest) = 
        if request.HttpMethod = "POST" && request.Url.Segments.Length >= 2 && (value = "*" || request.Url.Segments.[2] = value) then               
            Some(value)
        else
            None

    let (|PUT|_|) (value) (request: System.Net.HttpListenerRequest) = 
        if request.HttpMethod = "PUT" && request.Url.Segments.Length >= 2 && (value = "*" || request.Url.Segments.[2] = value) then               
            Some(value)
        else
            None

    let (|DELETE|_|) (value) (request: System.Net.HttpListenerRequest) = 
        if request.HttpMethod = "DELETE" && request.Url.Segments.Length >= 2 && (value = "*" || request.Url.Segments.[2] = value) then               
            Some(value)
        else
            None

    let OK (value : obj) (request: System.Net.HttpListenerRequest) (response: System.Net.HttpListenerResponse) =
        writeResponse HttpStatusCode.OK value request response

    let BAD_REQUEST (error : HttpResponseError) (request: System.Net.HttpListenerRequest) (response: System.Net.HttpListenerResponse) =
        writeResponse HttpStatusCode.OK error request response
        
            
    let indexShouldBeOffline =
        {
            DeveloperMessage = "Index should be made offline before attempting to update index settings."
            UserMessage = "Index should be made offline before attempting the operation."
            ErrorCode = 1001
        }

    let indexDoesNotExist =
        {
            DeveloperMessage = "The requested index does not exist."
            UserMessage = "The requested index does not exist."
            ErrorCode = 1001
        } 

    let indexAlreadyExist =
        {
            DeveloperMessage = "The requested index already exist."
            UserMessage = "The requested index already exists."
            ErrorCode = 1001
        } 