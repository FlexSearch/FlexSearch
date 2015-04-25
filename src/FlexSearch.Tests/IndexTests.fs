module IndexTests

open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open Swensen.Unquote
open System.IO
open System.Threading

type TransactionWriterTests() = 
    
    let getTransactionLogEntry() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        use stream = new MemoryStream()
        let context = new SpecimenContext(fixture)
        let txEntry = context.Create<TransacationLog.T>()
        TransacationLog.serializer (stream, txEntry)
        (txEntry, stream.ToArray())
    
    member __.``Transaction can be added and retrieved``() = 
        if File.Exists(DataHelpers.rootFolder +/ "0") then File.Delete(DataHelpers.rootFolder +/ "0")
        let writer = new TransacationLog.TxWriter(DataHelpers.rootFolder, 0L)
        let entries = Array.create 5 (getTransactionLogEntry())
        entries |> Array.iter (fun entry -> writer.Append(snd entry, 10L))
        let txEntries = entries |> Array.map (fun entry -> fst entry)
        // TODO: Will have to find a cleaner way than using Thread.Sleep
        // Maybe convert from MailboxProcessor to ActionBlock
        Thread.Sleep(5000)
        let result = writer.ReadLog(0L) |> Seq.toArray
        test <@ result.Length = 5 @>
        test <@ result.[0].Id = txEntries.[0].Id @>
        test <@ result.[1].Id = txEntries.[1].Id @>
        test <@ result.[2].Id = txEntries.[2].Id @>
        test <@ result.[3].Id = txEntries.[3].Id @>
        test <@ result.[4].Id = txEntries.[4].Id @>


