namespace FlexSearch.Tests

open FlexSearch.Tests
open FlexSearch.Tests.SearchTests
open Swensen.Unquote

type ``Operator: 'allOf' Tests``(ih : IntegrationHelper) = 
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ih |> verifyResultCount 4 "allof(et1, 'aaron')"
    override __.``Works with Id Field``() = ih |> verifyResultCount 1 "allof(_id, '1')"
    override __.``Works with TimeStamp Field``() = ih |> verifyResultCount 10 "allof(_timestamp, @IGNORE, -matchall)"
    override __.``Works with ModifyIndex Field``() = ih |> verifyResultCount 1 "allof(_modifyindex, '2')"
    override __.``Works with Int Field``() = ih |> verifyResultCount 1 "allof(i1, '-100')"
    override __.``Works with Multiple Int input``() = ih |> verifyResultCount 0 "allof(i1, '-100', '150')"
    override __.``Works with Long Field``() = ih |> verifyResultCount 1 "allof(l1, '-1000')"
    override __.``Works with Double Field``() = ih |> verifyResultCount 1 "allof(db1, '-1000')"
    override __.``Works with Float Field``() = ih |> verifyResultCount 1 "allof(f1, '-1000')"
    override __.``Works with DateTime Field``() = ih |> verifyResultCount 1 "allof(dt1, '20101010101010')"
    override __.``Works with Date Field``() = ih |> verifyResultCount 1 "allof(d1, '20101010')"
    override __.``Works with Bool Field``() = ih |> verifyResultCount 5 "allof(b1, 'T')"
    override __.``Works with Stored Field``() = ih |> storedFieldCannotBeSearched "allof(s1, '*')"
    override __.``Works with And clause``() = ih |> verifyResultCount 1 "allof(et1, 'fred') AND allof(l1, '1500')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 2 "allof(et1, 'erik') OR allof(l1, '4000')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 2 "allof(et1, 'aaron') AND NOT allof(l1, '1000')"
    override __.``Filter query``() = ("allof(et1, 'aaron')", "allof(et1, 'aaron') and allof(et1, 'aaron', -filter)")
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 3 "allof(t1, 'aaron') and (allof(t2, 'johnson') or allof(t2, 'Garner'))"
    override __.``Works with Multiple params``() = ih |> verifyResultCount 0 "allof(et1, 'aaron', 'erik')"
    override __.``Works with Constants``() = ih |> verifyResultCount 4 "allof(et1, 'aaron', @IGNORE)"
    member __.``Order of tokens does not matter``() = ih |> verifyResultCount 6 "allof(t3, 'parliamentary', 'monarchy')"
    member __.``Tokens don't have to be adjacent to each other``() = 
        ih |> verifyResultCount 2 "allof(t3, 'federal', 'democracy')"

/// AnyOf tests can be used by other query types also as the generalized case
/// of most of the queries is same as anyOf. We will simply derive the tests for
/// these query types from 'anyOf'
type AnyOfTestsBase(ih : IntegrationHelper, operator : string, supportsNumericTypes : bool) = 
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ih |> verifyResultCount 4 (sprintf "%s(et1, 'aaron')" operator)
    override __.``Works with Id Field``() = ih |> verifyResultCount 2 (sprintf "%s(_id, '1', '2')" operator)
    
    override __.``Works with TimeStamp Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(_timestamp, '*')" operator)
        else ih |> verifyResultCount 10 (sprintf "%s(_timestamp, @IGNORE, -matchall)" operator)
    
    override __.``Works with ModifyIndex Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(_modifyindex, '2')" operator)
        else ih |> verifyResultCount 1 (sprintf "%s(_modifyindex, '2')" operator)
    
    override __.``Works with Int Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(i1, '-100')" operator)
        else ih |> verifyResultCount 1 (sprintf "%s(i1, '-100')" operator)
    
    override __.``Works with Multiple Int input``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(i1, '-100', '150')" operator)
        else ih |> verifyResultCount 2 (sprintf "%s(i1, '-100', '150')" operator)
    
    override __.``Works with Long Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(l1, '-1000', '4000')" operator)
        else ih |> verifyResultCount 2 (sprintf "%s(l1, '-1000', '4000')" operator)
    
    override __.``Works with Double Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(db1, '-1000')" operator)
        else ih |> verifyResultCount 1 (sprintf "%s(db1, '-1000')" operator)
    
    override __.``Works with Float Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(f1, '-1000')" operator)
        else ih |> verifyResultCount 1 (sprintf "%s(f1, '-1000')" operator)
    
    override __.``Works with DateTime Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(dt1, '20101010101010')" operator)
        else ih |> verifyResultCount 1 (sprintf "%s(dt1, '20101010101010')" operator)
    
    override __.``Works with Date Field``() = 
        if not supportsNumericTypes then ih |> fieldTypeNotSupported (sprintf "%s(d1, '20101010')" operator)
        else ih |> verifyResultCount 1 (sprintf "%s(d1, '20101010')" operator)
    
    override __.``Works with Bool Field``() = ih |> verifyResultCount 5 (sprintf "%s(b1, 'T')" operator)
    override __.``Works with Stored Field``() = ih |> storedFieldCannotBeSearched (sprintf "%s(s1, '*')" operator)
    override __.``Works with And clause``() = 
        ih |> verifyResultCount 2 (sprintf "%s(et1, 'fred') AND anyOf(l1, '1500', '-1000')" operator)
    override __.``Works with Or clause``() = 
        ih |> verifyResultCount 2 (sprintf "%s(et1, 'erik') OR anyOf(l1, '4000')" operator)
    override __.``Works with Not clause``() = 
        ih |> verifyResultCount 2 (sprintf "%s(et1, 'aaron') AND NOT anyOf(l1, '1000')" operator)
    override __.``Filter query``() = 
        ((sprintf "%s(et1, 'aaron')" operator), 
         (sprintf "%s(et1, 'aaron') and %s(et1, 'aaron', -filter)" operator operator))
    override __.``Works with AndOr clause``() = 
        ih 
        |> verifyResultCount 3 
               (sprintf "%s(t1, 'aaron') and (%s(t2, 'johnson') or %s(t2, 'Garner'))" operator operator operator)
    override __.``Works with Multiple params``() = 
        ih |> verifyResultCount 5 (sprintf "%s(et1, 'aaron', 'erik')" operator)
    override __.``Works with Constants``() = ih |> verifyResultCount 4 (sprintf "%s(et1, 'aaron', @IGNORE)" operator)

type ``Operator: 'anyOf' Tests``(ih : IntegrationHelper, defaultOperator : string) = 
    inherit AnyOfTestsBase(ih, "anyOf", true)
    member __.``Order of tokens does not matter``() = ih |> verifyResultCount 7 "anyof(t3, 'parliamentary', 'monarchy')"
    member __.``Tokens don't have to be adjacent to each other``() = 
        ih |> verifyResultCount 3 "anyof(t3, 'federal', 'democracy')"

type ``Operator: 'anyOf' and 'allOf' additional Tests``(ih : IntegrationHelper) = 
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    do ih |> indexData testData
    member __.``Searching for multiple words will create a new query which will search all the words but not in specific order``() = 
        ih |> verifyResultCount 1 "allof(t1, 'CompSci abbreviated approach')"
    member __.``Searching for multiple words will create a new query which will search all the words using AND style construct but not in specific order``() = 
        ih |> verifyResultCount 0 "allof(t1, 'CompSci abbreviated approach undefinedword')"
    member __.``Setting 'clausetype' in condition properties can override the default clause construction from AND style to OR``() = 
        ih |> verifyResultCount 1 "anyof(t1, 'CompSci abbreviated approach undefinedword')"

type ``Operator: 'phraseMatch' Tests``(ih : IntegrationHelper) = 
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ih |> verifyResultCount 4 "phraseMatch(et1, 'aaron')"
    override __.``Works with Id Field``() = ih |> verifyResultCount 2 "phraseMatch(_id, '1', '2')"
    override __.``Works with TimeStamp Field``() = ih |> fieldTypeNotSupported "phraseMatch(_timestamp, '*')"
    override __.``Works with ModifyIndex Field``() = ih |> fieldTypeNotSupported "phraseMatch(_modifyindex, '*')"
    override __.``Works with Int Field``() = ih |> fieldTypeNotSupported "phraseMatch(i1, '*')"
    override __.``Works with Multiple Int input``() = ih |> fieldTypeNotSupported "phraseMatch(i1, '*')"
    override __.``Works with Long Field``() = ih |> fieldTypeNotSupported "phraseMatch(l1, '*')"
    override __.``Works with Double Field``() = ih |> fieldTypeNotSupported "phraseMatch(db1, '*')"
    override __.``Works with Float Field``() = ih |> fieldTypeNotSupported "phraseMatch(f1, '*')"
    override __.``Works with DateTime Field``() = ih |> fieldTypeNotSupported "phraseMatch(dt1, '*')"
    override __.``Works with Date Field``() = ih |> fieldTypeNotSupported "phraseMatch(d1, '*')"
    override __.``Works with Bool Field``() = ih |> verifyResultCount 5 "phraseMatch(b1, 'T')"
    override __.``Works with Stored Field``() = ih |> storedFieldCannotBeSearched "phraseMatch(s1, '*')"
    override __.``Works with And clause``() = 
        ih |> verifyResultCount 2 "phraseMatch(et1, 'fred') AND anyOf(l1, '1500', '-1000')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 2 "phraseMatch(et1, 'erik') OR anyof(l1, '4000')"
    override __.``Works with Not clause``() = 
        ih |> verifyResultCount 2 "phraseMatch(et1, 'aaron') AND NOT anyof(l1, '1000')"
    override __.``Filter query``() = 
        ("phraseMatch(et1, 'aaron')", "phraseMatch(et1, 'aaron') and phraseMatch(et1, 'aaron', -filter)")
    override __.``Works with AndOr clause``() = 
        ih 
        |> verifyResultCount 3 "phraseMatch(t1, 'aaron') and (phraseMatch(t2, 'johnson') or phraseMatch(t2, 'Garner'))"
    override __.``Works with Multiple params``() = ih |> verifyResultCount 5 "phraseMatch(et1, 'aaron', 'erik')"
    override __.``Works with Constants``() = ih |> verifyResultCount 4 "phraseMatch(et1, 'aaron', @IGNORE)"
    member __.``Default slop of 1 will always find adjacent terms``() = 
        ih |> verifyResultCount 3 "phraseMatch(t3, 'parliamentary democracy')"
    member __.``Default slop of 1 will always find adjacent terms - Case 2``() = 
        ih |> verifyResultCount 1 "phraseMatch(t3, 'monarchy parliamentary')"
    member __.``Slop of 2 will allow terms to be upto 2 words apart``() = 
        ih |> verifyResultCount 3 "phraseMatch(t3, 'parliamentary democracy', -slop '2')"
    member __.``Slop of 2 will allow terms to be upto 2 words apart - Case 2``() = 
        ih |> verifyResultCount 2 "phraseMatch(t3, 'federal democracy', -slop '2')"
    member __.``Slop of 4 will allow terms to be upto 4 words apart - Case 3``() = 
        ih |> verifyResultCount 6 "phraseMatch(t3, 'parliamentary monarchy', -slop '4')"
    member __.``Slop of 2 will allow terms to interchange position``() = 
        ih |> verifyResultCount 3 "phraseMatch(t3, 'parliamentary monarchy', -slop '2')"
    member __.``MultiPhrase switch will allow matching at the same position``() = 
        // The below should match both phrases containing 'parliamentary democracy' and 'parliamentary system'
        ih |> verifyResultCount 4 "phraseMatch(t3, 'parliamentary', 'democracy system', -multiphrase)"
    member __.``MultiPhrase switch will allow matching at the same position - Case 2``() = 
        // The below should match both phrases containing 'parliamentary democracy', 'parliamentary system'
        // and 'parliamentary constitutional'
        ih |> verifyResultCount 5 "phraseMatch(t3, 'parliamentary', 'democracy system constitutional', -multiphrase)"
    member __.``MultiPhrase switch will allow matching at the same position - Case 3``() = 
        // The below should match both phrases containing 'parliamentary monarchy' and 'constitutional monarchy'
        ih |> verifyResultCount 5 "phraseMatch(t3, 'constitutional parliamentary', 'monarchy', -multiphrase)"

type ``Operator: 'phraseMatch' additional Tests``(ih : IntegrationHelper) = 
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    do ih |> indexData testData
    member __.``Searching for 'practical approach' with a slop of 1 will return 1 result``() = 
        ih |> verifyResultCount 1 "phraseMatch(t1, 'practical approach', -slop '1')"
    member __.``Searching for 'practical approach' with a default slop of 1 will return 1 result``() = 
        ih |> verifyResultCount 1 "phraseMatch(t1, 'practical approach')"
    member __.``Searching for 'approach practical' will not return anything as the order matters``() = 
        ih |> verifyResultCount 0 "phraseMatch(t1, 'approach practical')"
    member __.``Searching for 'approach computation' with a slop of 2 will return 1 result``() = 
        ih |> verifyResultCount 1 "phraseMatch(t1, 'approach computation', -slop '2')"
    member __.``Searching for 'comprehensive process leads' with a slop of 1 will return 1 result``() = 
        ih |> verifyResultCount 1 "phraseMatch(t1, 'comprehensive process leads')"

type ``Operator: 'like' Tests``(ih : IntegrationHelper) = 
    inherit AnyOfTestsBase(ih, "like", false)
    member __.QueryTest1() = ih |> verifyResultCount 1 "like(t1, 'are?')"
    member __.QueryTest2() = ih |> verifyResultCount 4 "like(t1, 'aaro*')"
    member __.QueryTest3() = ih |> verifyResultCount 1 "like(t1, 'ar?n')"
    member __.``Matching is case in-sensitive``() = ih |> verifyResultCount 1 "like(t1, 'AR?N')"

type ``Operator: 'fuzzy' Tests``(ih : IntegrationHelper) = 
    inherit AnyOfTestsBase(ih, "fuzzy", false)
    member __.QueryTest1() = ih |> verifyResultCount 5 "fuzzy(t1, 'aron')"
    member __.QueryTest2() = ih |> verifyResultCount 5 "fuzzy(t1, 'aron', -slop '1')"
    member __.QueryTest3() = ih |> verifyResultCount 6 "fuzzy(t1, 'aron', -slop '2')"
    member __.``Matching is case in-sensitive``() = ih |> verifyResultCount 5 "fuzzy(t1, 'ARoN')"

type ``Operator: 'regex' Tests``(ih : IntegrationHelper) = 
    inherit AnyOfTestsBase(ih, "regex", false)
    member __.QueryTest1() = ih |> verifyResultCount 1 "regex(t1, '[fl]ord')"
    member __.``Matching is case in-sensitive``() = ih |> verifyResultCount 1 "regex(t1, '[fl]ORD')"

// ----------------------------------------------------------------------------
// Numeric Query type tests
// ----------------------------------------------------------------------------
[<AbstractClass>]
type NumericTestsBase(ih : IntegrationHelper, operator : string) = 
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ih |> fieldTypeNotSupported (sprintf "%s(et1, '*')" operator)
    override __.``Works with Id Field``() = ih |> fieldTypeNotSupported (sprintf "%s(_id, '*')" operator)
    override __.``Works with TimeStamp Field``() = 
        ih |> verifyResultCount 10 (sprintf "%s(_timestamp, @IGNORE, -matchall)" operator)
    override __.``Works with ModifyIndex Field``() = 
        ih |> verifyResultCount 10 (sprintf "%s(_modifyindex, @IGNORE, -matchall)" operator)
    override __.``Works with Int Field``() = ih |> verifyResultCount 10 (sprintf "%s(i1, @IGNORE, -matchall)" operator)
    override __.``Works with Multiple Int input``() = ()
    override __.``Works with Long Field``() = ih |> verifyResultCount 10 (sprintf "%s(l1, @IGNORE, -matchall)" operator)
    override __.``Works with Double Field``() = 
        ih |> verifyResultCount 10 (sprintf "%s(db1, @IGNORE, -matchall)" operator)
    override __.``Works with Float Field``() = 
        ih |> verifyResultCount 10 (sprintf "%s(f1, @IGNORE, -matchall)" operator)
    override __.``Works with DateTime Field``() = 
        ih |> verifyResultCount 10 (sprintf "%s(dt1, @IGNORE, -matchall)" operator)
    override __.``Works with Date Field``() = ih |> verifyResultCount 10 (sprintf "%s(d1, @IGNORE, -matchall)" operator)
    override __.``Works with Bool Field``() = ih |> fieldTypeNotSupported (sprintf "%s(b1, '*')" operator)
    override __.``Works with Stored Field``() = ih |> storedFieldCannotBeSearched (sprintf "%s(s1, '*')" operator)
    override __.``Works with Constants``() = ih |> verifyResultCount 10 (sprintf "%s(i1, @IGNORE, -matchall)" operator)

type ``Operator: 'gt' Tests``(ih : IntegrationHelper) = 
    inherit NumericTestsBase(ih, "gt")
    override __.``Filter query``() = ("gt(i2, '3000')", "gt(i2, '3000') and gt(i2, '3000', -filter)")
    override __.``Works with And clause``() = ih |> verifyResultCount 2 "gt(i2, '999') and allof(i1, '100')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 7 "gt(i2, '999') or allof(i1, '100')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 5 "gt(i2, '999') and not allof(i1, '100')"
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 3 "gt(i2, '999') and (allof(i1, '200') or allof(i1, '100'))"
    override __.``Works with Multiple params``() = 
        /// Here the 2nd parameter will be ignored
        ih |> verifyResultCount 1 "gt(i2, '3000', '4000')"

type ``Operator: 'ge' Tests``(ih : IntegrationHelper) = 
    inherit NumericTestsBase(ih, "ge")
    override __.``Filter query``() = ("ge(i2, '3000')", "ge(i2, '3000') and ge(i2, '3000', -filter)")
    override __.``Works with And clause``() = ih |> verifyResultCount 2 "ge(i2, '1000') and allof(i1, '100')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 7 "ge(i2, '1000') or allof(i1, '100')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 5 "ge(i2, '1000') and not allof(i1, '100')"
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 3 "ge(i2, '1000') and (allof(i1, '200') or allof(i1, '100'))"
    override __.``Works with Multiple params``() = 
        /// Here the 2nd parameter will be ignored
        ih |> verifyResultCount 1 "ge(i2, '3001', '4000')"

type ``Operator: 'lt' Tests``(ih : IntegrationHelper) = 
    inherit NumericTestsBase(ih, "lt")
    override __.``Filter query``() = ("lt(i2, '3000')", "lt(i2, '3000') and lt(i2, '3000', -filter)")
    override __.``Works with And clause``() = ih |> verifyResultCount 2 "lt(i2, '1001') and allof(i1, '100')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 5 "lt(i2, '1001') or allof(i1, '100')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 3 "lt(i2, '1001') and not allof(i1, '100')"
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 2 "lt(i2, '1001') and (allof(i1, '200') or allof(i1, '100'))"
    override __.``Works with Multiple params``() = 
        /// Here the 2nd parameter will be ignored
        ih |> verifyResultCount 9 "lt(i2, '3001', '4000')"

type ``Operator: 'le' Tests``(ih : IntegrationHelper) = 
    inherit NumericTestsBase(ih, "le")
    override __.``Filter query``() = ("le(i2, '3000')", "le(i2, '3000') and le(i2, '3000', -filter)")
    override __.``Works with And clause``() = ih |> verifyResultCount 2 "le(i2, '1000') and allof(i1, '100')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 5 "le(i2, '1000') or allof(i1, '100')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 3 "le(i2, '1000') and not allof(i1, '100')"
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 2 "le(i2, '1000') and (allof(i1, '200') or allof(i1, '100'))"
    override __.``Works with Multiple params``() = 
        /// Here the 2nd parameter will be ignored
        ih |> verifyResultCount 9 "le(i2, '3001', '4000')"

/// Some extra tests for checking numeric queries as the below are easier to
/// reason with due to simpler data set.
type ``Operator: Range Query Tests``(ih : IntegrationHelper) = 
    let testData = """
id,i1
1,1
2,5
3,10
4,15
5,20
"""
    do ih |> indexData testData
    member __.QueryTest1() = ih |> verifyResultCount 5 "ge(i1, '1') and le(i1, '20')"
    member __.QueryTest2() = ih |> verifyResultCount 3 "gt(i1, '1') and lt(i1, '20')"
    member __.QueryTest3() = ih |> verifyResultCount 4 "ge(i1, '1') and lt(i1, '20')"
    member __.QueryTest4() = ih |> verifyResultCount 4 "gt(i1 , '1') and le(i1 , '20')"
    member __.QueryTest5() = ih |> verifyResultCount 4 "gt(i1 , '1')"
    member __.QueryTest6() = ih |> verifyResultCount 5 "ge(i1 , '1')"
    member __.QueryTest7() = ih |> verifyResultCount 4 "lt(i1 , '20')"
    member __.QueryTest8() = ih |> verifyResultCount 5 "le(i1 , '20')"
