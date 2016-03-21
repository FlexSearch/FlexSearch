#if INTERACTIVE
// Reference the API DLL in case of testing
//#r "../../FlexSearch.Api.dll"
//#r @"D:\Bitbucket\FlexSearch\src\FlexSearch.Api\bin\Debug\FlexSearch.Api.dll"
#endif
[<AutoOpen>]
module Helpers =
    open System
    open System.Collections.Generic
    open FlexSearch.Api.Model

    type Document with
        /// Set a field value in the document
        member this.Set(fieldName : string, fieldValue : string) =
            this.Fields.[fieldName] <- fieldValue

        /// Get a field value from the document. Returns an empty string
        /// in case the value does not exist
        member this.Get(fieldName : string) =
            match this.Fields.TryGetValue(fieldName) with
            | true, value -> value
            | _ -> String.Empty