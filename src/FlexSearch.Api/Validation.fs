namespace FlexSearch.Api.Validation

open FlexSearch.Api
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.ComponentModel
open System.ComponentModel.DataAnnotations
open System.Linq
open System.Reflection

/// <summary>
///     Custom validator interface which will allow validation using Data annotation
///     Validation class.
/// </summary>
type IValidator = 
    
    /// <summary>
    ///     Validate an object using Data annotation Validator
    /// </summary>
    /// <Note>
    ///     Data annotation validator when initialized automatically
    ///     calls Validate method of IValidatableObject. So calling
    ///     validator from that validate method is not feasible as it will
    ///     result in infinite recursion.
    /// </Note>
    /// <returns></returns>
    abstract Validate : unit -> ValidationResult
    
    /// <summary>
    /// A simple wrapper around F# choices which can be easily
    /// consumed in a maybe monad
    /// </summary>
    abstract MaybeValidator : unit -> Choice<unit, OperationMessage>

[<AbstractClass>]
type ValidatableObjectBase<'T>() = 
    
    /// Cache to hold property information. This will be generated once for every
    /// subclass as this is a generic abstract class.
    static let PropertyInfoCache = new ConcurrentDictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase)
    
    static let GetPropertyInfo(propertyName) = 
        match PropertyInfoCache.TryGetValue(propertyName) with
        | (true, value) -> value
        | _ -> 
            let value = typeof<'T>.GetProperty(propertyName)
            PropertyInfoCache.TryAdd(propertyName, value) |> ignore
            value
    
    member this.Validate() = 
        let validationResults = new List<ValidationResult>()
        if Validator.TryValidateObject(this, new ValidationContext(this, null, null), validationResults, true) then 
            ValidationResult.Success
        else validationResults.First()
    
    abstract Validate : ValidationContext -> IEnumerable<ValidationResult>
    override this.Validate(validationContext) = Enumerable.Empty<ValidationResult>()
    
    interface IValidatableObject with
        // Because F# only supports explicit implementation of interface so implementation is referring back
        // to the virtual method validate
        member this.Validate(validationContext : ValidationContext) : IEnumerable<ValidationResult> = 
            this.Validate(validationContext)
    
    interface IDataErrorInfo with
        
        [<Display(AutoGenerateField = false)>]
        member x.Error : string = failwith "Not implemented yet"
        
        /// <summary>
        ///     Gets the error message for the property with the given name.
        ///     This will only be used in the UI through WPF so performance
        ///     penalty through reflection shouldn't be a major issue.
        /// </summary>
        member this.Item 
            with get (columnName : string) = 
                if String.IsNullOrEmpty(columnName) then failwithf "Invalid property name: %s" columnName
                else 
                    let errors = new List<ValidationResult>()
                    if Validator.TryValidateProperty
                           (GetPropertyInfo(columnName).GetValue(this, null), 
                            new ValidationContext(this, null, null, MemberName = columnName), errors) then ""
                    else errors.[0].ErrorMessage
    
    interface IValidator with
        
        member this.MaybeValidator() = 
            match this.Validate() with
            | null -> Choice1Of2()
            | (result) -> Choice2Of2("VALIDATION_FAILURE:" + result.ErrorMessage |> GenerateOperationMessage)
        
        member this.Validate() = this.Validate()

// ----------------------------------------------------------------------------
//	System.DataAnnotation validation attributes implementations
// ----------------------------------------------------------------------------
/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type ValidateComplexAttribute() = 
    inherit ValidationAttribute()
    override this.IsValid(value, context) = 
        let errors = new List<ValidationResult>()
        if Validator.TryValidateObject(value, new ValidationContext(value, null, null), errors, true) then 
            ValidationResult.Success
        else errors.[0]

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type PropertyNameAttribute() = 
    inherit ValidationAttribute()
    override this.IsValid(value, context) = 
        let property = value :?> String
        if String.IsNullOrWhiteSpace(property) then new ValidationResult("")
        else 
            let m = System.Text.RegularExpressions.Regex.Match(property, "^[a-z0-9_]*$")
            if m.Success <> true then new ValidationResult("")
            else if (String.Equals(property, Constants.IdField) || String.Equals(property, Constants.LastModifiedField) 
                     || String.Equals(property, Constants.TypeField)) then new ValidationResult("")
            else ValidationResult.Success

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type MinimumItemsAttribute(minItems : int) = 
    inherit ValidationAttribute()
    override this.IsValid(value) = 
        let list = value :?> System.Collections.IList
        if list = null then false
        else (list.Count >= minItems)

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type GreaterThan(num : int) = 
    inherit ValidationAttribute()
    override this.IsValid(value) = 
        let res = value :?> int
        res > num

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type LessThan(num : int) = 
    inherit ValidationAttribute()
    override this.IsValid(value) = 
        let res = value :?> int
        res < num

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type GreaterThanOrEqual(num : int) = 
    inherit ValidationAttribute()
    override this.IsValid(value) = 
        let res = value :?> int
        res >= num

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type LessThanOrEqual(num : int) = 
    inherit ValidationAttribute()
    override this.IsValid(value) = 
        let res = value :?> int
        res <= num

/// <summary>
/// Validates complex object using data annotation
/// </summary>
[<Sealed>]
type ValidKeys() = 
    inherit ValidationAttribute("{0}: contains an invalid key.")
    override this.IsValid(value) = 
        let res = value :?> System.Collections.IDictionary
        let propertyName = new PropertyNameAttribute()
        let mutable success = true
        for key in res.Keys do
            success <- propertyName.IsValid(key)
        success

[<AutoOpen>]
module Helpers = 
    open FlexSearch.Common
    
    /// <summary>
    /// Helper to validate all the elements of a collection
    /// </summary>
    /// <param name="enumerableValue"></param>
    let ValidateCollection<'T>(enumerableValue : IEnumerable<'T>) = 
        assert (enumerableValue <> null)
        let results = new List<ValidationResult>()
        if enumerableValue.Any
               (fun x -> 
               Validator.TryValidateObject
                   (enumerableValue, new ValidationContext(enumerableValue, null, null), results, true) = false) then 
            results.First()
        else ValidationResult.Success
    
    /// Wrapper around Dictionary lookup. Useful for validation in tokenizers and filters
    let inline KeyExists(key, dict : IDictionary<string, 'T>, errorMessage : OperationMessage) = 
        match dict.TryGetValue(key) with
        | (true, value) -> Choice1Of2(value)
        | _ -> 
            Choice2Of2(errorMessage
                       |> Append("Reason", "A required property is not defined.")
                       |> Append("Property", key))
    
    /// Helper method to check if the passed key exists in the dictionary and if it does then the
    /// specified value is in the enum list
    let inline ValidateIsInList(key, param : IDictionary<string, string>, enumValues : HashSet<string>, 
                                errorMessage : OperationMessage) = 
        maybe { 
            let! value = KeyExists(key, param, errorMessage)
            match enumValues.Contains(value) with
            | true -> return! Choice1Of2(value)
            | _ -> 
                return! Choice2Of2(errorMessage
                                   |> Append("Reason", "The specified property value is not valid.")
                                   |> Append("Property", key))
        }
    
    /// <summary>
    /// Find a key in a dictionary and parse the resulting value as integer
    /// </summary>
    /// <param name="key"></param>
    /// <param name="param"></param>
    /// <param name="errorMessage"></param>
    let inline ParseValueAsInteger(key, param : IDictionary<string, string>, errorMessage : OperationMessage) = 
        maybe { 
            let! value = KeyExists(key, param, errorMessage)
            match Int32.TryParse(value) with
            | (true, value) -> return! Choice1Of2(value)
            | _ -> 
                return! Choice2Of2(errorMessage
                                   |> Append("Reason", "The specified property value is not a valid integer.")
                                   |> Append("Property", key)
                                   |> Append("Value", value))
        }
