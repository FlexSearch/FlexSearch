module PoolTests
open FsUnit
open Fuchu
open FlexSearch.Core
open FlexSearch.Core.Pool
open System

type SimpleItem() = 
    inherit PooledObject()
    member val Name = "TestObject" with get, set

[<Tests>]
let objectPoolTests =
    let factory() = new SimpleItem()
    let itemCount = 10
    let pool = new ObjectPool<SimpleItem>(factory, itemCount)

    testList "Object pool tests" [
        testCase (sprintf "Pool Item count should be %i" itemCount) <| fun _ ->
            pool.Total |> should equal itemCount

        testList "Initialization cases" [
            let item = ref Unchecked.defaultof<SimpleItem> 
            yield testCase "Acquiring 1 item should reduce the count by 1" <| fun _ ->
                item := pool.Acquire()
                pool.Total |> should equal (itemCount - 1)
            
            yield testCase "Releasing the item should increment the count by 1" <| fun _ ->
                pool.Release(item.Value)
                item := Unchecked.defaultof<SimpleItem>
                pool.Total |> should equal (itemCount)
            
            yield testCase "Unreleased items automatically go back to the pool when Dispose is called" <| fun _ ->
                let item = pool.Acquire()
                pool.Total |> should equal (itemCount - 1)
                (item :> IDisposable).Dispose()
                GC.WaitForPendingFinalizers()
                pool.Total |> should equal (itemCount)

            yield testCase "Use construct can be used to release item automatically" <| fun _ ->
                let test =
                    use item = pool.Acquire()
                    pool.Total |> should equal (itemCount - 1)
                
                pool.Total |> should equal (itemCount)

            yield testCase "Out of scope items are automatically returned to the pool on GC" <| fun _ ->
                item := pool.Acquire()
                pool.Total |> should equal (itemCount - 1)
                item := Unchecked.defaultof<SimpleItem>
                GC.Collect()
                pool.Total |> should equal (itemCount)
        ]
    ]
    
