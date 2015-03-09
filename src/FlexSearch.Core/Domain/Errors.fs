namespace FlexSearch.Core

open System.Collections.Generic

type Error = 
    // Generic Validation Errors
    | GreaterThan of fieldName : string * lowerLimit : string * value : string
    | LessThan of fieldName : string * upperLimit : string * value : string
    | GreaterThanEqual of fieldName : string * lowerLimit : string * value : string
    | LessThanEqual of fieldName : string * lowerLimit : string * value : string
    | NotEmpty of fieldName : string
    | RegexMatch of fieldName : string * regexExpr : string
    // Domain related
    | InvalidPropertyName of fieldName : string
    | AnalyzerIsMandatory of fieldName : string
    | DuplicateFieldValue of groupName : string * fieldName : string
    | ScriptNotFound of scriptName : string * fieldName : string
    // Indexing related errrors
    | IndexNotFound of indexName : string
    // Generic error to be used by plugins
    | GenericError of userMessage : string * data : ResizeArray<KeyValuePair<string, string>>
