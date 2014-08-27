// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Common

[<Sealed>]
type ValidationBuilder() = 
    
    let bind v f = 
        match v with
        | Choice1Of2(x) -> f x
        | Choice2Of2(s) -> Choice2Of2(s)
    
    let combine a b = 
        match a, b with
        | Choice1Of2 a', Choice1Of2 b' -> Choice1Of2 b'
        | Choice2Of2 a', Choice1Of2 b' -> Choice2Of2 a'
        | Choice1Of2 a', Choice2Of2 b' -> Choice2Of2 b'
        | Choice2Of2 a', Choice2Of2 b' -> Choice2Of2 a'
    
    let zero = Choice1Of2()
    let returnFrom v = v
    let delay f = f()
    
    let tryFinally body compensation = 
        try 
            returnFrom (body())
        finally
            compensation()
    
    let using (disposable : #System.IDisposable) body = 
        let body' = fun () -> body disposable
        tryFinally body' (fun () -> disposable.Dispose())
    
    // The whileLoop operator
    let rec whileLoop pred body = 
        if pred() then bind (body()) (fun _ -> whileLoop pred body)
        else zero
    
    // The forLoop operator
    let forLoop (collection : seq<_>) func = 
        using (collection.GetEnumerator()) 
            (fun it -> whileLoop (fun () -> it.MoveNext()) (fun () -> it.Current |> func))
    member this.Bind(v, f) = bind v f
    member this.ReturnFrom v = returnFrom v
    member this.Return v = Choice1Of2(v)
    member this.Zero() = zero
    member this.Combine(a, b) = combine a b
    member this.Delay(f) = delay f
    member this.TryFinally(body, compensation) = tryFinally body compensation
    member this.For(collection : seq<_>, func) = forLoop collection func
    member this.Using(disposable : #System.IDisposable, body) = using disposable body

[<AutoOpen>]
module Maybe = 
    let maybe = new ValidationBuilder()
