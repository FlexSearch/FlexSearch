module ValidatorsTests

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Core.Validator
open FsUnit
open Fuchu
open System
open System.Collections.Generic

//let testChoice choice expectedChoice1 = 
//    match choice with
//    | Choice1Of2(success) -> 
//        if expectedChoice1 then Assert.AreEqual(1, 1)
//        else Assert.AreEqual(1, 2, "Was expecting Choice2 but received Choice1")
//    | Choice2Of2(error) -> 
//        if expectedChoice1 then Assert.AreEqual(1, 2, "Was expecting Choice1 but received Choice2")
//        else Assert.AreEqual(1, 1)
//
//[<Tests>]
//let propertyNameValidatorTests = 
//    testList "Property Name Validator Tests" 
//        [ testList "Property Name Value is invalid" 
//              [ for exp in [ 1, "TEST"
//                             2, "Test"
//                             3, Constants.IdField
//                             4, Constants.LastModifiedField
//                             5, Constants.TypeField
//                             6, "<test>" ] -> 
//                    testCase 
//                        (sprintf "%i When a propertyNameValue of '%s' is passed, it should not validate" (fst (exp)) 
//                             (snd (exp))) <| fun _ -> testChoice (propertyNameValidator ("test", (snd (exp)))) false ]
//          
//          testList "Property Name Value is invalid" 
//              [ for exp in [ 1, "test"
//                             2, "1234"
//                             3, "_test" ] -> 
//                    testCase 
//                        (sprintf "%i When a propertyNameValue of '%s' is passed, it should validate" (fst (exp)) 
//                             (snd (exp))) <| fun _ -> testChoice (propertyNameValidator ("test", (snd (exp)))) true ] ]
//
//[<Tests>]
//let indexFieldTests = 
//    testList "Index Field Tests" 
//        [ testList "Index Analyzer is ignored for X field types" 
//              [ for exp in [ FieldType.Bool, "dummy"
//                             FieldType.Date, "dummy"
//                             FieldType.DateTime, "dummy"
//                             FieldType.Double, "dummy"
//                             FieldType.ExactText, "dummy"
//                             FieldType.Int, "dummy"
//                             FieldType.Stored, "dummy" ] -> 
//                    testCase 
//                        (sprintf 
//                             "When a field of type '%s' is validated with 'IndexAnalyzer' value of '%s' there should be no error." 
//                             ((fst (exp)).ToString()) (snd (exp))) 
//                    <| fun _ -> 
//                        let indexProperties = new FieldProperties()
//                        indexProperties.ScriptName <- ""
//                        indexProperties.FieldType <- fst (exp)
//                        indexProperties.IndexAnalyzer <- snd (exp)
//                        testChoice 
//                            (IndexFieldValidator Helpers.factoryCollection Unchecked.defaultof<_> Unchecked.defaultof<_> 
//                                 ("test", indexProperties)) true ]
//          
//          testList "Search Analyzer is ignored for X field types" 
//              [ for exp in [ FieldType.Bool, "dummy"
//                             FieldType.Date, "dummy"
//                             FieldType.DateTime, "dummy"
//                             FieldType.Double, "dummy"
//                             FieldType.ExactText, "dummy"
//                             FieldType.Int, "dummy"
//                             FieldType.Stored, "dummy" ] -> 
//                    testCase 
//                        (sprintf 
//                             "When a field of type '%s' is validated with 'SearchAnalyzer' value of '%s' there should be no error." 
//                             ((fst (exp)).ToString()) (snd (exp))) 
//                    <| fun _ -> 
//                        let indexProperties = new FieldProperties()
//                        indexProperties.ScriptName <- ""
//                        indexProperties.FieldType <- fst (exp)
//                        indexProperties.SearchAnalyzer <- snd (exp)
//                        testChoice 
//                            (IndexFieldValidator Helpers.factoryCollection Unchecked.defaultof<_> Unchecked.defaultof<_> 
//                                 ("test", indexProperties)) true ]
//          
//          testList "Correct Index Analyzer should be specified for X field types" 
//              [ for exp in [ FieldType.Text, "dummy"
//                             FieldType.Highlight, "dummy"
//                             FieldType.Custom, "dummy" ] -> 
//                    testCase 
//                        (sprintf 
//                             "When a field of type '%s' is validated with 'IndexAnalyzer' value of '%s' there should be a error." 
//                             ((fst (exp)).ToString()) (snd (exp))) 
//                    <| fun _ -> 
//                        let indexProperties = new FieldProperties()
//                        indexProperties.ScriptName <- ""
//                        indexProperties.FieldType <- fst (exp)
//                        indexProperties.IndexAnalyzer <- snd (exp)
//                        testChoice 
//                            (IndexFieldValidator Helpers.factoryCollection 
//                                 (new Dictionary<string, AnalyzerProperties>()) Unchecked.defaultof<_> 
//                                 ("test", indexProperties)) false ]
//          
//          testList "Correct Search Analyzer should be specified for X field types" 
//              [ for exp in [ FieldType.Text, "dummy"
//                             FieldType.Highlight, "dummy"
//                             FieldType.Custom, "dummy" ] -> 
//                    testCase 
//                        (sprintf 
//                             "When a field of type '%s' is validated with 'SearchAnalyzer' value of '%s' there should be a error." 
//                             ((fst (exp)).ToString()) (snd (exp))) 
//                    <| fun _ -> 
//                        let indexProperties = new FieldProperties()
//                        indexProperties.ScriptName <- ""
//                        indexProperties.FieldType <- fst (exp)
//                        indexProperties.SearchAnalyzer <- snd (exp)
//                        testChoice 
//                            (IndexFieldValidator Helpers.factoryCollection 
//                                 (new Dictionary<string, AnalyzerProperties>()) Unchecked.defaultof<_> 
//                                 ("test", indexProperties)) false ]
//          
//          testList "Correct Search Analyzer should be specified for X field types" 
//              [ for exp in [ FieldType.Text, "standardanalyzer"
//                             FieldType.Highlight, "standardanalyzer"
//                             FieldType.Custom, "standardanalyzer" ] -> 
//                    testCase 
//                        (sprintf 
//                             "When a field of type '%s' is validated with 'SearchAnalyzer' value of '%s' there should be no error." 
//                             ((fst (exp)).ToString()) (snd (exp))) 
//                    <| fun _ -> 
//                        let indexProperties = new FieldProperties()
//                        indexProperties.ScriptName <- ""
//                        indexProperties.FieldType <- fst (exp)
//                        indexProperties.SearchAnalyzer <- snd (exp)
//                        testChoice 
//                            (IndexFieldValidator Helpers.factoryCollection 
//                                 (new Dictionary<string, AnalyzerProperties>()) Unchecked.defaultof<_> 
//                                 ("test", indexProperties)) true ]
//          
//          testList "Correct Index Analyzer should be specified for X field types" 
//              [ for exp in [ FieldType.Text, "standardanalyzer"
//                             FieldType.Highlight, "standardanalyzer"
//                             FieldType.Custom, "standardanalyzer" ] -> 
//                    testCase 
//                        (sprintf 
//                             "When a field of type '%s' is validated with 'IndexAnalyzer' value of '%s' there should be no error." 
//                             ((fst (exp)).ToString()) (snd (exp))) 
//                    <| fun _ -> 
//                        let indexProperties = new FieldProperties()
//                        indexProperties.ScriptName <- ""
//                        indexProperties.FieldType <- fst (exp)
//                        indexProperties.IndexAnalyzer <- snd (exp)
//                        testChoice 
//                            (IndexFieldValidator Helpers.factoryCollection 
//                                 (new Dictionary<string, AnalyzerProperties>()) Unchecked.defaultof<_> 
//                                 ("test", indexProperties)) true ]
//          
//          testList "Default value tests" 
//              [ let indexProperties = new FieldProperties()
//                yield testCase "standardanalyzer' should be the default 'SearchAnalyzer'" 
//                      <| fun _ -> indexProperties.SearchAnalyzer |> should equal "standardanalyzer"
//                yield testCase "standardanalyzer' should be the default 'IndexAnalyzer'" 
//                      <| fun _ -> indexProperties.IndexAnalyzer |> should equal "standardanalyzer" 
//                yield testCase "Analyze' should default to 'true'" 
//                      <| fun _ -> indexProperties.Analyze |> should equal true
//                yield testCase "Store' should default to 'true'" 
//                      <| fun _ -> indexProperties.Store |> should equal true
//                yield testCase "Index' should default to 'true'" 
//                      <| fun _ -> indexProperties.Index |> should equal true
//                yield testCase "FieldType' should default to 'Text'" 
//                      <| fun _ -> indexProperties.FieldType |> should equal FieldType.Text
//                yield testCase "TermVector' should default to 'StoreTermVectorsWithPositions'" 
//                      <| fun _ -> indexProperties.TermVector |> should equal FieldTermVector.StoreTermVectorsWithPositions] ]
