module PoolTests
open FsUnit
open Fuchu
open FlexSearch.Core
open FlexSearch.Core.Pool
open System
open System.Threading

type SimpleItem() = 
    inherit PooledObject()
    member val Name = "TestObject" with get, set

type SimpleItem1() = 
    inherit PooledObject()
    member val Name1 = "TestObject" with get, set

[<Tests>]
let objectPoolTests =
    let factory() = new SimpleItem(Name = Guid.NewGuid().ToString())
    let factory1() = new SimpleItem1(Name1 = Guid.NewGuid().ToString())
    let itemCount = 10
    let pool = new ObjectPool<SimpleItem>(factory, itemCount)

    testList "Object pool tests" [
        testCase (sprintf "Pool Item count should be %i" itemCount) <| fun _ ->
            pool.Total() |> should equal itemCount
        
        testCase "Taking two successive Pool Items will result in creation of new pool item" <| fun _ ->
            let testPool = new ObjectPool<SimpleItem>(factory, 1)
            let item1 = testPool.Acquire()
            let item2 = testPool.Acquire()            
            testPool.Total() |> should equal 2
            item1.Release()
            item2.Release()

        testList "Initialization cases" [
            let item = ref Unchecked.defaultof<SimpleItem> 
            yield testCase "Acquiring 1 item should reduce the count by 1" <| fun _ ->
                item := pool.Acquire()
                pool.Available() |> should equal (itemCount - 1)
            
            yield testCase "Releasing the item should increment the count by 1" <| fun _ ->
                item.Value.Release()
                item := Unchecked.defaultof<SimpleItem>
                pool.Available() |> should equal (itemCount)
            
            yield testCase "Unreleased items automatically go back to the pool when Dispose is called" <| fun _ ->
                let item = pool.Acquire()
                pool.Available() |> should equal (itemCount - 1)
                (item :> IDisposable).Dispose()
                GC.WaitForPendingFinalizers()
                pool.Available() |> should equal (itemCount)

            yield testCase "Use construct can be used to release item automatically" <| fun _ ->
                let test =
                    use item = pool.Acquire()
                    pool.Available() |> should equal (itemCount - 1)
                
                pool.Available() |> should equal (itemCount)
        ]

        testList "Multiple pools of different type" [
            let testPool1 = new ObjectPool<SimpleItem>(factory, 1)
            let testPool2 = new ObjectPool<SimpleItem1>(factory1, 1)
            let item1 = testPool1.Acquire()
            let item2 = testPool2.Acquire() 
            
            yield testCase "Pool should have 1 item" <| fun _ ->           
                testPool1.Total() |> should equal 1
            
            yield testCase "Pool should have 1 item" <| fun _ ->           
                testPool2.Total() |> should equal 1

            yield testCase "Pool should have 0 item available" <| fun _ ->           
                testPool1.Available() |> should equal 0

            yield testCase "Pool should have 0 item available" <| fun _ ->           
                testPool2.Available() |> should equal 0

            yield testCase "After release Pool should have 1 item available" <| fun _ ->           
                item1.Release()
                testPool1.Total() |> should equal 1
            
            yield testCase "After release Pool should have 1 item available" <| fun _ ->           
                item2.Release()
                testPool2.Total() |> should equal 1
        ]

        testList "Multithreaded tests" [
            let testPool = new ObjectPool<SimpleItem>(factory, 1)

            yield testCase "Pool should have 100 items after being accessed 100 times from multiple threads" <| fun _ ->           
                let result = 
                    Async.Parallel [ for i in 1..100 -> async { return testPool.Acquire() } ]
                    |> Async.RunSynchronously
                    
                testPool.Total() |> should equal 100
                result |> Array.iter(fun x -> x.Release())

            yield testCase "Pool should have 100 items available as items will be returned to the pool after call to release" <| fun _ ->           
                testPool.Available() |> should equal 100
        ]
    ]
    
