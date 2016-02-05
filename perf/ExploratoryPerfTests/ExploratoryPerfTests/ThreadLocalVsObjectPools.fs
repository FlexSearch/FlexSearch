module ThreadLocalVsObjectPools

open PerfUtil
open System.Threading
open System.Threading.Tasks
open System

type TestClass() = 
    member val TestString : string = "test" with get, set
    member val TestInt : int = 100 with get, set

let getParallelOptions() = 
    let parallelOptions = new ParallelOptions()
    parallelOptions.MaxDegreeOfParallelism <- Environment.ProcessorCount * 2
    parallelOptions

let ThreadLocalPerformance(iters) = 
    let store = new ThreadLocal<TestClass>((fun _ -> new TestClass()), true)
    Parallel.For(0, iters, getParallelOptions(), 
                 fun _ -> 
                     let a = store.Value.TestString = "test"
                     store.Value.TestInt <- store.Value.TestInt + 1
                     ())
    |> ignore

open Microsoft.Extensions.ObjectPool

let ObjectPoolPerformance(iters) = 
    let pool = new DefaultObjectPool<TestClass>(new DefaultPooledObjectPolicy<TestClass>()) :> ObjectPool<TestClass>
    Parallel.For(0, iters, getParallelOptions(), 
                 fun _ -> 
                     let instance = pool.Get()
                     let a = instance.TestString = "test"
                     instance.TestInt <- instance.TestInt + 1
                     pool.Return(instance)
                     ())
    |> ignore

let Test() = 
    let iters = pown 10 6
    printfn "ThreadLocal : \n %A" 
    <| Benchmark.Run((fun _ -> ThreadLocalPerformance(iters)), repeat = 20, warmup = false)
    printfn "ObjectPool : \n %A" <| Benchmark.Run((fun _ -> ObjectPoolPerformance(iters)), repeat = 20, warmup = false)
