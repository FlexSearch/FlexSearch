namespace FlexSearch.Core.Tests

open FlexSearch.Core
open System
open System.Threading
open Xunit

//type SimpleItem() = 
//    inherit PooledObject()
//    member val Name = "TestObject" with get, set
//
//type SimpleItem1() = 
//    inherit PooledObject()
//    member val Name1 = "TestObject" with get, set
//
//module ``Object pool tests`` = 
//    let factory() = new SimpleItem(Name = Guid.NewGuid().ToString())
//    let factory1() = new SimpleItem1(Name1 = Guid.NewGuid().ToString())
//    
//    let ``Taking two successive Pool Items will result in creation of new pool item``() = 
//        let testPool = new ObjectPool<SimpleItem>(factory, 1)
//        let item1 = testPool.Acquire()
//        let item2 = testPool.Acquire()
//        Assert.Equal(2, int (testPool.Total()))
//        item1.Release()
//        item2.Release()
//    
//    type ``Simple pool tests``() = 
//        let itemCount = 10
//        let pool = new ObjectPool<SimpleItem>(factory, itemCount)
//        let mutable item = Unchecked.defaultof<SimpleItem>
//        
//        [<Fact>]
//        let ``Pool Item count should be 10``() = Assert.Equal(itemCount, int (pool.Total()))
//        
//        [<Fact>]
//        let ``Acquiring 1 item should reduce the count by 1``() = 
//            let item = pool.Acquire()
//            Assert.Equal(itemCount - 1, pool.Available())
//        
//        [<Fact>]
//        let ``Releasing the item should increment the count by 1``() = 
//            let item = pool.Acquire()
//            Assert.Equal(itemCount - 1, pool.Available())
//            item.Release()
//            Assert.Equal(itemCount, pool.Available())
//        
//        [<Fact>]
//        let ``Unreleased items automatically go back to the pool when Dispose is called``() = 
//            let item = pool.Acquire()
//            Assert.Equal(itemCount - 1, pool.Available())
//            (item :> IDisposable).Dispose()
//            GC.WaitForPendingFinalizers()
//            Assert.Equal(itemCount, pool.Available())
//        
//        [<Fact>]
//        let ``Use construct can be used to release item automatically``() = 
//            let test = 
//                use item = pool.Acquire()
//                Assert.Equal(itemCount - 1, pool.Available())
//            Assert.Equal(itemCount, pool.Available())
//    
//    type ``Multiple pools of different type tests``() = 
//        let testPool1 = new ObjectPool<SimpleItem>(factory, 1)
//        let testPool2 = new ObjectPool<SimpleItem1>(factory1, 1)
//        let item1 = testPool1.Acquire()
//        let item2 = testPool2.Acquire()
//        
//        [<Fact>]
//        let ``Pool should have 1 item``() = Assert.Equal(1, int (testPool1.Total()))
//        
//        [<Fact>]
//        let ``Pool should have 1 item``() = Assert.Equal(1, int (testPool2.Total()))
//        
//        [<Fact>]
//        let ``Pool should have 0 item available``() = Assert.Equal(0, testPool1.Available())
//        
//        [<Fact>]
//        let ``Pool should have 0 item available``() = Assert.Equal(0, testPool2.Available())
//        
//        [<Fact>]
//        let ``After release Pool should have 1 item available``() = 
//            item1.Release()
//            Assert.Equal(1, int (testPool1.Total()))
//        
//        [<Fact>]
//        let ``After release Pool should have 1 item available``() = 
//            item1.Release()
//            Assert.Equal(1, int (testPool2.Total()))
//    
//    module ``Multithreaded tests`` = 
//        [<Fact>]
//        let ``Pool should have 100 items after being accessed 100 times from multiple threads``() = 
//            let testPool = new ObjectPool<SimpleItem>(factory, 1)
//            
//            let result = 
//                Async.Parallel [ for i in 1..100 -> async { return testPool.Acquire() } ]
//                |> Async.RunSynchronously
//            Assert.Equal(100, int (testPool.Total()))
//            result |> Array.iter (fun x -> x.Release())
//        
//        [<Fact>]
//        let ``Pool should have 100 items available as items will be returned to the pool after call to release``() = 
//            let testPool = new ObjectPool<SimpleItem>(factory, 1)
//            
//            let result = 
//                Async.Parallel [ for i in 1..100 -> async { return testPool.Acquire() } ]
//                |> Async.RunSynchronously
//            result |> Array.iter (fun x -> x.Release())
//            Assert.Equal(100, testPool.Available())
