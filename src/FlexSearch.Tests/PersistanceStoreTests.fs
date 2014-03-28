module PersistanceStoreTests

open FlexSearch.Core
open FlexSearch.Core.Store
open FsUnit
open Fuchu
open System
open System.Linq

type TestClass() = 
    member val Property1 = "TestObject" with get, set
    member val Property2 = 0 with get, set

let helper (res: Choice<'T, 'U>) expectedSuccess =
    match res with
    | Choice1Of2(a) -> 
        if expectedSuccess then 
            Assert.AreEqual(1,1, "Returned success")
        a
    | Choice2Of2(b) ->
        failtestf "Expecting success but returned failure"
        
[<Tests>]
let persistanceStoreTests = 
    testList "Simple persistence tests" 
        [ 
        let memoryStore = new Store.PersistanceStore("", true) :> IPersistanceStore
        yield testCase "Adding a new TestClass should pass" <| fun _ -> 
                let test = new TestClass(Property1 = "test", Property2 = 1)
                helper (memoryStore.Put "test" test) true
        yield testCase "Newly added value can be retrieved" <| fun _ -> 
                let test = memoryStore.Get<TestClass>("test")
                let result = helper test true
                result.Property1 |> should equal "test"
                result.Property2 |> should equal 1
        yield testCase "Updating a value by key is possible" <| fun _ -> 
                let test = new TestClass(Property1 = "test1", Property2 = 2)
                memoryStore.Put "test" test |> ignore
                let test1 = memoryStore.Get<TestClass>("test")
                let result = helper test1 true
                result.Property1 |> should equal "test1"
                result.Property2 |> should equal 2
        yield testCase 
                "After adding another record of type TestClass, GetAll should return 2" <| fun _ -> 
                let test = new TestClass(Property1 = "test1", Property2 = 2)
                memoryStore.Put "test1" test |> ignore
                let result = memoryStore.GetAll<TestClass>()
                result.Count() |> should equal 2 
        ]
