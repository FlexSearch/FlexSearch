module OrmTests
open FlexSearch.Core
open Swensen.Unquote
open System.Collections.Generic
open System

type MappingType1() =
    member val Property1 = defString with get, set
    member val Property2 = defString with get, set

type MappingType2() =
    member val DateTimeField = DateTime.Now with get, set
    
type SimpleMappingTests() =
    member __.``Generated doc should contain 2 fields``() =
        let testInstance = new MappingType1(Property1 = "Property1", Property2 = "Property2")
        let sut = Orm.createDocFromObj "testindexname" "testid" testInstance
        test <@ sut.Id = "testid" @>
        test <@ sut.IndexName = "testindexname" @>
        test <@ sut.Fields.Count = 2 @>
        test <@ sut.Fields.ContainsKey("Property1") @>
        test <@ sut.Fields.ContainsKey("Property2") @>

    member __.``DateTime will be automatically converted to FlexSearch format``() =
        let testInstance = new MappingType2()
        let sut = Orm.createDocFromObj "testindexname" "testid" testInstance
        test <@ sut.Id = "testid" @>
        test <@ sut.IndexName = "testindexname" @>
        test <@ sut.Fields.Count = 1 @>
        test <@ sut.Fields.ContainsKey("DateTimeField") @>
        test <@ sut.Fields.["DateTimeField"] = dateToFlexFormat(testInstance.DateTimeField).ToString() @>
