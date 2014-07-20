namespace FlexSearch.Core.Tests

open FlexSearch.Core
open FlexSearch.TestSupport
open System
open System.Linq
open Xunit
open Xunit.Extensions

module ``Persistance Store Tests`` = 
    type TestClass() = 
        member val Property1 = "TestObject" with get, set
        member val Property2 = 0 with get, set
    
    let GetMemoryStore() = new SqlLitePersistanceStore(true) :> IPersistanceStore
    
    [<Theory; AutoMockData>]
    let ``Adding a new TestClass should pass`` (sut : TestClass) = ExpectSuccess(GetMemoryStore().Put("test", sut))
    
    [<Theory; AutoMockData>]
    let ``Newly added value can be retrieved`` (sut : TestClass, key : string) = 
        let memoryStore = GetMemoryStore()
        ExpectSuccess(memoryStore.Put(key, sut))
        let test = memoryStore.Get<TestClass>(key)
        let result = GetSuccessChoice test
        Assert.Equal<string>(sut.Property1, result.Property1)
        Assert.Equal(sut.Property2, result.Property2)
    
    [<Theory; AutoMockData>]
    let ``Updating a value by key is possible`` (sut : TestClass, key : string) = 
        let memoryStore = GetMemoryStore()
        ExpectSuccess(memoryStore.Put(key, sut))
        let test1 = memoryStore.Get<TestClass>(key)
        let result = GetSuccessChoice test1
        Assert.Equal<string>(sut.Property1, result.Property1)
        Assert.Equal(sut.Property2, result.Property2)
    
    [<Theory; AutoMockData>]
    let ``After adding another record of type TestClass, GetAll should return 2`` (sut : TestClass, key1 : string, 
                                                                                   key2 : string) = 
        let memoryStore = GetMemoryStore()
        ExpectSuccess(memoryStore.Put(key1, sut))
        ExpectSuccess(memoryStore.Put(key2, sut))
        let result = memoryStore.GetAll<TestClass>()
        Assert.Equal(2, result.Count())
