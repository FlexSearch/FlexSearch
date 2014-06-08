namespace FlexSearch.IntegrationTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FlexSearch.TestSupport
open FsUnit
open Fuchu
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

module ``Search Query Tests`` = 
    let PhraseMatchTestData = """
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
"""
    
    type ``Phrase Match Tests``() = 
        inherit IndexTestBase(PhraseMatchTestData)
        
        [<Fact>]
        member this.``Searching for 'practical approach' with a slop of 1 will return 1 result``() = 
            "abstract match 'practical approach' {slop:'1'}" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'practical approach' with a default slop of 1 will return 1 result``() = 
            "abstract match 'practical approach'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'approach practical' will not return anything as the order matters``() = 
            "abstract match 'approach practical'" |> this.VerifySearchCount 0
        
        [<Fact>]
        member this.``Searching for 'approach computation' with a slop of 2 will return 1 result``() = 
            "abstract match 'approach computation' {slop:'2'}" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'comprehensive process leads' with a slop of 1 will return 1 result``() = 
            "abstract match 'comprehensive process leads' {slop:'1'}" |> this.VerifySearchCount 1
    
    let TermMatchTestData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,23
2,aaron,hewitt,32
3,Fred,Garner,44
4,aaron,Garner,43
5,fred,jhonson,332"""
    
    type ``Term Match Tests``() = 
        inherit IndexTestBase(TermMatchTestData)
        
        [<Fact>]
        member this.``Searching for 'id eq 1' should return 1 records``() = "_id eq '1'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for int field 'cvv2 eq 44' should return 1 records``() = 
            "cvv2 eq '44'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'aaron' should return 3 records``() = 
            "givenname eq 'aaron'" |> this.VerifySearchCount 3
        
        [<Fact>]
        member this.``Searching for 'aaron' & 'jhonson' should return 1 record``() = 
            "givenname eq 'aaron' and surname eq 'jhonson'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for givenname eq 'aaron' and (surname eq 'jhonson' or surname eq 'Garner') should return 2 record``() = 
            "givenname eq 'aaron' and (surname eq 'jhonson' or surname eq 'Garner')" |> this.VerifySearchCount 2
        
        [<Fact>]
        member this.``Searching for 'id = 1' should return 1 records``() = "_id = '1'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for int field 'cvv2 = 44' should return 1 records``() = 
            "cvv2 = '44'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for givenname = 'aaron' should return 3 records``() = 
            "givenname = 'aaron'" |> this.VerifySearchCount 3
        
        [<Fact>]
        member this.``Searching for givenname = 'aaron' and surname = 'jhonson' should return 1 record``() = 
            "givenname = 'aaron' and surname = 'jhonson'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for givenname 'aaron' & surname 'jhonson or Garner' should return 2 record``() = 
            "givenname = 'aaron' and (surname = 'jhonson' or surname = 'Garner')" |> this.VerifySearchCount 2
    
    let TermMatchComplexTestData = """
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artifacts such as machine code of computer programs.
    """
    
    type ``Term Match Complex Tests``() = 
        inherit IndexTestBase(TermMatchComplexTestData)
        
        [<Fact>]
        member this.``Searching for multiple words will create a new query which will search all the words but not in specific order``() = 
            "abstract eq 'CompSci abbreviated approach'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for multiple words will create a new query which will search all the words using AND style construct but not in specific order``() = 
            "abstract eq 'CompSci abbreviated approach undefinedword'" |> this.VerifySearchCount 0
        
        [<Fact>]
        member this.``Setting 'clausetype' in condition properties can override the default clause construction from AND style to OR``() = 
            "abstract eq 'CompSci abbreviated approach undefinedword' {clausetype:'or'}" |> this.VerifySearchCount 1
    
    let FuzzyWildCardMatchTestData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,23
2,aron,hewitt,32
3,Airon,Garner,44
4,aroon,Garner,43
5,aronn,jhonson,332
6,aroonn,jhonson,332
7,boat,,jhonson,332
8,moat,jhonson,332
"""
    
    type ``Fuzzy WildCard Match Tests``() = 
        inherit IndexTestBase(FuzzyWildCardMatchTestData)
        
        [<Fact>]
        member this.``Searching for 'givenname = aron' with default slop of 1 should return 5 records``() = 
            "givenname fuzzy 'aron'" |> this.VerifySearchCount 5
        
        [<Fact>]
        member this.``Searching for 'givenname = aron' with specified slop of 1 should return 5 records``() = 
            "givenname fuzzy 'aron' {slop:'1'}" |> this.VerifySearchCount 5
        
        [<Fact>]
        member this.``Searching for 'givenname = aron' with slop of 2 should return 6 records``() = 
            "givenname fuzzy 'aron'  {slop:'2'}" |> this.VerifySearchCount 6
        
        [<Fact>]
        member this.``Searching for 'givenname ~= aron' with default slop of 1 should return 5 records``() = 
            "givenname ~= 'aron'" |> this.VerifySearchCount 5
        
        [<Fact>]
        member this.``Searching for 'givenname ~= aron' with specified slop of 1 should return 5 records``() = 
            "givenname ~= 'aron' {slop:'1'}" |> this.VerifySearchCount 5
        
        [<Fact>]
        member this.``Searching for 'givenname ~= aron' with slop of 2 should return 6 records``() = 
            "givenname ~= 'aron'  {slop:'2'}" |> this.VerifySearchCount 6
        
        [<Fact>]
        member this.``Searching for 'givenname = aron?' should return 1 records``() = 
            "givenname like 'aron?'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'givenname = aron*' should return 2 records``() = 
            "givenname like 'aron*'" |> this.VerifySearchCount 2
        
        [<Fact>]
        member this.``Searching for 'givenname = ar?n' should return 1 records``() = 
            "givenname like 'ar?n'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'givenname %= aron?' should return 1 records``() = 
            "givenname %= 'aron?'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'givenname %= aron*' should return 2 records``() = 
            "givenname %= 'aron*'" |> this.VerifySearchCount 2
        
        [<Fact>]
        member this.``Searching for 'givenname %= ar?n' should return 1 records``() = 
            "givenname %= 'ar?n'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'givenname = AR?N' should return 1 records as matching is case in-sensitive even though like bypasses analysis``() = 
            "givenname %= 'AR?N'" |> this.VerifySearchCount 1
        
        [<Fact>]
        member this.``Searching for 'givenname = [mb]oat' should return 2 records``() = 
            "givenname regex '[mb]oat'" |> this.VerifySearchCount 2
    
    let RangeQueryTestsData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,1
2,aaron,hewitt,5
3,Fred,Garner,10
4,aaron,Garner,15
5,fred,jhonson,20
"""
    
    type ``Range Query Tests``() = 
        inherit IndexTestBase(RangeQueryTestsData)
        
        [<Fact>]
        member this.``Searching for records with cvv in range 1 to 20 inclusive upper & lower bound should return 5 records``() = 
            "cvv2 >= '1' and cvv2 <= '20'" |> this.VerifySearchCount 5
        
        [<Fact>]
        member this.``Searching for records with cvv in range 1 to 20 exclusive upper & lower bound should return 3 records``() = 
            "cvv2 > '1' and cvv2 < '20'" |> this.VerifySearchCount 3
        
        [<Fact>]
        member this.``Searching for records with cvv in range 1 to 20 inclusive upper & exclusive lower bound should return 4 records``() = 
            "cvv2 >= '1' and cvv2 < '20'" |> this.VerifySearchCount 4
        
        [<Fact>]
        member this.``Searching for records with cvv in range 1 to 20 excluding upper & including lower bound should return 4 records``() = 
            "cvv2 > '1' and cvv2 <= '20'" |> this.VerifySearchCount 4
        
        [<Fact>]
        member this.``Searching for records with cvv2 > '1' should return 4"``() = 
            "cvv2 > '1'" |> this.VerifySearchCount 4
        
        [<Fact>]
        member this.``Searching for records with cvv2 >= '1' should return 5``() = 
            "cvv2 >= '1'" |> this.VerifySearchCount 5
        
        [<Fact>]
        member this.``Searching for records with cvv2 < '20' should return 4``() = 
            "cvv2 < '20'" |> this.VerifySearchCount 4
        
        [<Fact>]
        member this.``Searching for records with cvv2 <= '20' should return 5``() = 
            "cvv2 <= '20'" |> this.VerifySearchCount 5
