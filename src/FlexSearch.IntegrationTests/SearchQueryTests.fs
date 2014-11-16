namespace FlexSearch.IntegrationTests.Search

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

type ``Phrase Match Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    do self.TestData <- testData
    
    [<Fact>]
    member this.``Searching for 'practical approach' with a slop of 1 will return 1 result``() = 
        this.VerifySearchCount("t1 match 'practical approach' {slop:'1'}", 1)
    
    [<Fact>]
    member this.``Searching for 'practical approach' with a default slop of 1 will return 1 result``() = 
        this.VerifySearchCount("t1 match 'practical approach'", 1)
    
    [<Fact>]
    member this.``Searching for 'approach practical' will not return anything as the order matters``() = 
        this.VerifySearchCount("t1 match 'approach practical'", 0)
    
    [<Fact>]
    member this.``Searching for 'approach computation' with a slop of 2 will return 1 result``() = 
        this.VerifySearchCount("t1 match 'approach computation' {slop:'2'}", 1)
    
    [<Fact>]
    member this.``Searching for 'comprehensive process leads' with a slop of 1 will return 1 result``() = 
        this.VerifySearchCount("t1 match 'comprehensive process leads' {slop:'1'}", 1)

type ``Term Match Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,t1,t2,i1
1,Aaron,jhonson,23
2,aaron,hewitt,32
3,Fred,Garner,44
4,aaron,Garner,43
5,fred,jhonson,332"""
    do self.TestData <- testData
    
    [<Fact>]
    member this.``Searching for 'id eq 1' should return 1 records``() = this.VerifySearchCount("_id eq '1'", 1)
    
    [<Fact>]
    member this.``Searching for int field 'i1 eq 44' should return 1 records``() = 
        this.VerifySearchCount("i1 eq '44'", 1)
    
    [<Fact>]
    member this.``Searching for 'aaron' should return 3 records``() = this.VerifySearchCount("t1 eq 'aaron'", 3)
    
    [<Fact>]
    member this.``Searching for 'aaron' & 'jhonson' should return 1 record``() = 
        this.VerifySearchCount("t1 eq 'aaron' and t2 eq 'jhonson'", 1)
    
    [<Fact>]
    member this.``Searching for t1 eq 'aaron' and (t2 eq 'jhonson' or t2 eq 'Garner') should return 2 record``() = 
        this.VerifySearchCount("t1 eq 'aaron' and (t2 eq 'jhonson' or t2 eq 'Garner')", 2)
    
    [<Fact>]
    member this.``Searching for 'id = 1' should return 1 records``() = this.VerifySearchCount("_id = '1'", 1)
    
    [<Fact>]
    member this.``Searching for int field 'i1 = 44' should return 1 records``() = this.VerifySearchCount("i1 = '44'", 1)
    
    [<Fact>]
    member this.``Searching for t1 = 'aaron' should return 3 records``() = this.VerifySearchCount("t1 = 'aaron'", 3)
    
    [<Fact>]
    member this.``Searching for t1 = 'aaron' and t2 = 'jhonson' should return 1 record``() = 
        this.VerifySearchCount("t1 = 'aaron' and t2 = 'jhonson'", 1)
    
    [<Fact>]
    member this.``Searching for t1 'aaron' & t2 'jhonson or Garner' should return 2 record``() = 
        this.VerifySearchCount("t1 = 'aaron' and (t2 = 'jhonson' or t2 = 'Garner')", 2)

type ``Term Match Complex Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,et1,t1
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    do self.TestData <- testData
    
    [<Fact>]
    member this.``Searching for multiple words will create a new query which will search all the words but not in specific order``() = 
        this.VerifySearchCount("t1 eq 'CompSci abbreviated approach'", 1)
    
    [<Fact>]
    member this.``Searching for multiple words will create a new query which will search all the words using AND style construct but not in specific order``() = 
        this.VerifySearchCount("t1 eq 'CompSci abbreviated approach undefinedword'", 0)
    
    [<Fact>]
    member this.``Setting 'clausetype' in condition properties can override the default clause construction from AND style to OR``() = 
        this.VerifySearchCount("t1 eq 'CompSci abbreviated approach undefinedword' {clausetype:'or'}", 1)

type ``Fuzzy WildCard Match Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,t1,t2,i1
1,Aaron,jhonson,23
2,aron,hewitt,32
3,Airon,Garner,44
4,aroon,Garner,43
5,aronn,jhonson,332
6,aroonn,jhonson,332
7,boat,,jhonson,332
8,moat,jhonson,332
"""
    do self.TestData <- testData
    
    [<Fact>]
    member this.``Searching for 't1 = aron' with default slop of 1 should return 5 records``() = 
        this.VerifySearchCount("t1 fuzzy 'aron'", 5)
    
    [<Fact>]
    member this.``Searching for 't1 = aron' with specified slop of 1 should return 5 records``() = 
        this.VerifySearchCount("t1 fuzzy 'aron' {slop:'1'}", 5)
    
    [<Fact>]
    member this.``Searching for 't1 = aron' with slop of 2 should return 6 records``() = 
        this.VerifySearchCount("t1 fuzzy 'aron'  {slop:'2'}", 6)
    
    [<Fact>]
    member this.``Searching for 't1 ~= aron' with default slop of 1 should return 5 records``() = 
        this.VerifySearchCount("t1 ~= 'aron'", 5)
    
    [<Fact>]
    member this.``Searching for 't1 ~= aron' with specified slop of 1 should return 5 records``() = 
        this.VerifySearchCount("t1 ~= 'aron' {slop:'1'}", 5)
    
    [<Fact>]
    member this.``Searching for 't1 ~= aron' with slop of 2 should return 6 records``() = 
        this.VerifySearchCount("t1 ~= 'aron'  {slop:'2'}", 6)
    
    [<Fact>]
    member this.``Searching for 't1 = aron?' should return 1 records``() = this.VerifySearchCount("t1 like 'aron?'", 1)
    
    [<Fact>]
    member this.``Searching for 't1 = aron*' should return 2 records``() = this.VerifySearchCount("t1 like 'aron*'", 2)
    
    [<Fact>]
    member this.``Searching for 't1 = ar?n' should return 1 records``() = this.VerifySearchCount("t1 like 'ar?n'", 1)
    
    [<Fact>]
    member this.``Searching for 't1 %= aron?' should return 1 records``() = this.VerifySearchCount("t1 %= 'aron?'", 1)
    
    [<Fact>]
    member this.``Searching for 't1 %= aron*' should return 2 records``() = this.VerifySearchCount("t1 %= 'aron*'", 2)
    
    [<Fact>]
    member this.``Searching for 't1 %= ar?n' should return 1 records``() = this.VerifySearchCount("t1 %= 'ar?n'", 1)
    
    [<Fact>]
    member this.``Searching for 't1 = AR?N' should return 1 records as matching is case in-sensitive even though like bypasses analysis``() = 
        this.VerifySearchCount("t1 %= 'AR?N'", 1)
    
    [<Fact>]
    member this.``Searching for 't1 = [mb]oat' should return 2 records``() = 
        this.VerifySearchCount("t1 regex '[mb]oat'", 2)

type ``Range Query Tests``() as self = 
    inherit IndexTestBase()
    let testData = """
id,i1
1,1
2,5
3,10
4,15
5,20
"""
    do self.TestData <- testData
    
    [<Fact>]
    member this.``Searching for records with i1 in range 1 to 20 inclusive upper & lower bound should return 5 records``() = 
        this.VerifySearchCount("i1 >= '1' and i1 <= '20'", 5)
    
    [<Fact>]
    member this.``Searching for records with cvv in range 1 to 20 exclusive upper & lower bound should return 3 records``() = 
        this.VerifySearchCount("i1 > '1' and i1 < '20'", 3)
    
    [<Fact>]
    member this.``Searching for records with cvv in range 1 to 20 inclusive upper & exclusive lower bound should return 4 records``() = 
        this.VerifySearchCount("i1 >= '1' and i1 < '20'", 4)
    
    [<Fact>]
    member this.``Searching for records with cvv in range 1 to 20 excluding upper & including lower bound should return 4 records``() = 
        this.VerifySearchCount("i1 > '1' and i1 <= '20'", 4)
    
    [<Fact>]
    member this.``Searching for records with i1 > '1' should return 4"``() = this.VerifySearchCount("i1 > '1'", 4)
    
    [<Fact>]
    member this.``Searching for records with i1 >= '1' should return 5``() = this.VerifySearchCount("i1 >= '1'", 5)
    
    [<Fact>]
    member this.``Searching for records with i1 < '20' should return 4``() = this.VerifySearchCount("i1 < '20'", 4)
    
    [<Fact>]
    member this.``Searching for records with i1 <= '20' should return 5``() = this.VerifySearchCount("i1 <= '20'", 5)
