namespace FlexSearch.Core

open System

/// Represents the result of a computation.
type Result<'TSuccess, 'TMessage> = 
    /// Represents the result of a successful computation.
    | Ok of 'TSuccess
    /// Represents the result of a failed computation.
    | Fail of 'TMessage

type IFreezable = 
    abstract Freeze : unit -> unit

[<AbstractClassAttribute>]
type ValidatableBase() = 
    let mutable isFrozen = false
    abstract Validate : unit -> Result<unit, Error>
    interface IFreezable with
        member __.Freeze() = isFrozen <- true

[<AutoOpen>]
module Operators = 
    /// Wraps a value in a Success
    let inline ok<'a, 'b> (x : 'a) : Result<'a, 'b> = Ok(x)
    
    /// Wraps a message in a Failure
    let inline fail<'a, 'b> (msg : 'b) : Result<'a, 'b> = Fail msg
    
    /// Returns true if the result was not successful.
    let inline failed result = 
        match result with
        | Fail _ -> true
        | _ -> false
    
    /// Takes a Result and maps it with fSuccess if it is a Success otherwise it maps it with fFailure.
    let inline either fSuccess fFailure trialResult = 
        match trialResult with
        | Ok(x) -> fSuccess (x)
        | Fail(msgs) -> fFailure (msgs)
    
    /// If the given result is a Success the wrapped value will be returned. 
    ///Otherwise the function throws an exception with Failure message of the result.
    let inline returnOrFail result = 
        let inline raiseExn msgs = 
            msgs
            |> Seq.map (sprintf "%O")
            |> String.concat (Environment.NewLine + "\t")
            |> failwith
        either fst raiseExn result
    
    /// Appends the given messages with the messages in the given result.
    let inline mergeMessages msgs result = 
        let inline fSuccess (x, msgs2) = Ok(x, msgs @ msgs2)
        let inline fFailure errs = Fail(errs @ msgs)
        either fSuccess fFailure result
    
    /// If the result is a Success it executes the given function on the value.
    /// Otherwise the exisiting failure is propagated.
    let inline bind f result = 
        let inline fSuccess (x) = f x
        let inline fFailure (msg) = Fail msg
        either fSuccess fFailure result
    
    /// If the result is a Success it executes the given function on the value. 
    /// Otherwise the exisiting failure is propagated.
    /// This is the infix operator version of ErrorHandling.bind
    let inline (>>=) result f = bind f result
    
    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    let inline apply wrappedFunction result = 
        match wrappedFunction, result with
        | Ok(f, msgs1), Ok(x, msgs2) -> Ok(f x, msgs1 @ msgs2)
        | Fail errs, Ok(_, msgs) -> Fail(errs @ msgs)
        | Ok(_, msgs), Fail errs -> Fail(errs @ msgs)
        | Fail errs1, Fail errs2 -> Fail(errs1 @ errs2)
    
    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    /// This is the infix operator version of ErrorHandling.apply
    let inline (<*>) wrappedFunction result = apply wrappedFunction result
    
    /// Lifts a function into a Result container and applies it on the given result.
    let inline lift f result = apply (ok f) result
    
    /// Lifts a function into a Result and applies it on the given result.
    /// This is the infix operator version of ErrorHandling.lift
    let inline (<!>) f result = lift f result
    
    /// If the result is a Success it executes the given success function on the value and the messages.
    /// If the result is a Failure it executes the given failure function on the messages.
    /// Result is propagated unchanged.
    let inline eitherTee fSuccess fFailure result = 
        let inline tee f x = 
            f x
            x
        tee (either fSuccess fFailure) result
    
    /// If the result is a Success it executes the given function on the value and the messages.
    /// Result is propagated unchanged.
    let inline successTee f result = eitherTee f ignore result
    
    /// If the result is a Failure it executes the given function on the messages.
    /// Result is propagated unchanged.
    let inline failureTee f result = eitherTee ignore f result
    
    /// Converts an option into a Result.
    let inline failIfNone message result = 
        match result with
        | Some x -> ok x
        | None -> fail message
    
    /// Builder type for error handling computation expressions.
    type ErrorHandlingBuilder() = 
        member __.Zero() = ok()
        member __.Bind(m, f) = bind f m
        member __.Return(x) = ok x
        member __.ReturnFrom(x) = x
    
    /// Wraps computations in an error handling computation expression.
    let trial = ErrorHandlingBuilder()
    
    [<AutoOpenAttribute>]
    module Validators = 
        let gt fieldName limit input = 
            if input > limit then ok()
            else fail (GreaterThan(fieldName, limit.ToString(), input.ToString()))
        
        let gte fieldName limit input = 
            if input >= limit then ok()
            else fail (GreaterThanEqual(fieldName, limit.ToString(), input.ToString()))
        
        let lessThan fieldName limit input = 
            if input < limit then ok()
            else fail (LessThan(fieldName, limit.ToString(), input.ToString()))
        
        let lessThanEqual fieldName limit input = 
            if fieldName <= limit then ok()
            else fail (LessThanEqual(fieldName, limit.ToString(), input.ToString()))
        
        let notEmpty fieldName input = 
            if not (String.IsNullOrWhiteSpace(fieldName)) then ok()
            else fail (NotEmpty(fieldName))
        
        let regexMatch fieldName regexExpr input = 
            let m = System.Text.RegularExpressions.Regex.Match(input, regexExpr)
            if m.Success then ok()
            else fail (RegexMatch(fieldName, regexExpr))
        
        let propertyNameRegex fieldName input = regexMatch fieldName "^[a-z0-9_]*$" input
        
        let invalidPropertyName fieldName input = 
            if String.Equals(input, Constants.IdField) || String.Equals(input, Constants.LastModifiedField) 
               || String.Equals(input, Constants.TypeField) then fail (InvalidPropertyName(fieldName))
            else ok()
        
        let propertyNameValidator fieldName input = 
            notEmpty fieldName input >>= fun _ -> propertyNameRegex fieldName input 
            >>= fun _ -> invalidPropertyName fieldName input
        
        let seqValidator (input : seq<ValidatableBase>) = 
            let res = 
                input
                |> Seq.map (fun x -> x.Validate())
                |> Seq.filter (fun x -> 
                       match x with
                       | Ok(_) -> false
                       | _ -> true)
                |> Seq.toArray
            if res.Length = 0 then ok()
            else res.[0]
