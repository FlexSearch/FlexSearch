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

[<Tests>]
let persistanceStoreTests = 
    testList "Simple persistance tests" 
        [ 
        let memoryStore = new Store.PersistanceStore("", true) :> IPersistanceStore
        yield testCase "Adding a new TestClass should pass" <| fun _ -> 
                let test = new TestClass(Property1 = "test", Property2 = 1)
                memoryStore.Put "test" test |> should equal true
        yield testCase "Newly added value can be retrieved" <| fun _ -> 
                let test = memoryStore.Get<TestClass>("test")
                test.Value.Property1 |> should equal "test"
                test.Value.Property2 |> should equal 1
        yield testCase "Updating a value by key is possible" <| fun _ -> 
                let test = new TestClass(Property1 = "test1", Property2 = 2)
                memoryStore.Put "test" test |> ignore
                let result = memoryStore.Get<TestClass>("test")
                result.Value.Property1 |> should equal "test1"
                result.Value.Property2 |> should equal 2
        yield testCase 
                "After adding another record of type TestClass, GetAll should return 2" <| fun _ -> 
                let test = new TestClass(Property1 = "test1", Property2 = 2)
                memoryStore.Put "test1" test |> ignore
                let result = memoryStore.GetAll<TestClass>()
                result.Count() |> should equal 2 
        ]
