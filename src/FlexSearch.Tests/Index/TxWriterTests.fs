namespace FlexSearch.Tests.Index
open FlexSearch.Api.Model
open FlexSearch.Api.Constants
open FlexSearch.Core
open Ploeh.AutoFixture
open Ploeh.AutoFixture.Kernel
open Swensen.Unquote
open System.IO
open System.Linq
open System.Threading
open System.Collections.Generic
open FsCheck

type TransactionWriterTests() = 
    
    let getTransactionLogEntry() = 
        let fixture = new Ploeh.AutoFixture.Fixture()
        let context = new SpecimenContext(fixture)
        let txEntry = context.Create<TransactionEntry>()
        txEntry
    
    member __.``Transaction can be added and retrieved``() = 
        let tempFile = Path.GetTempFileName()
        let writer = new TxWriter(0L, filePath = tempFile)
        let entries = Array.create 5 (getTransactionLogEntry())
        entries |> Array.iter (fun entry -> writer.AppendEntry(entry, 0L))
        let result = TxWriter.ReadLog(tempFile) |> Seq.toArray
        test <@ result.Length = 5 @>
        test <@ result.[0].Id = entries.[0].Id @>
        test <@ result.[1].Id = entries.[1].Id @>
        test <@ result.[2].Id = entries.[2].Id @>
        test <@ result.[3].Id = entries.[3].Id @>
        test <@ result.[4].Id = entries.[4].Id @>

    member __.``Random Log read write test using FSCheck``() =
        let tempFile = Path.GetTempFileName()
        let writer = new TxWriter(0L, filePath = tempFile)
        let entries = Gen.listOf Arb.generate<TransactionEntry> |> Gen.eval 1000 (Random.StdGen(20000, 100))
        entries |> List.iter (fun entry -> writer.AppendEntry(entry, 0L))
        let result = TxWriter.ReadLog(tempFile) |> Seq.toList
        test <@ entries.Length = result.Length @>
        for i = 0 to entries.Length - 1 do
            let entryId = entries.[i].Id
            let resultId = result.[i].Id
            test <@ entryId = resultId @>
            test <@ entries.[i].ModifyIndex = result.[i].ModifyIndex @>
            test <@ entries.[i].Operation = result.[i].Operation @>
            let entryKeys = entries.[i].Data.Keys.ToArray()
            let resultKeys = result.[i].Data.Keys.ToArray()
            test <@ entryKeys = resultKeys @>
            let entryValues = entries.[i].Data.Values.ToArray()
            let resultValues = result.[i].Data.Values.ToArray()
            test <@ entryValues = resultValues @>