namespace FlexSearch.HttpTests

open FlexSearch.Api.Api
open FlexSearch.Api.Client
open FlexSearch.Api.Constants
open FlexSearch.Api.Model
open FlexSearch.HttpTests
open System.Linq
open System.Text.RegularExpressions
open TestCommandHelpers

type HttpQueryTypeTests(s : SearchApi, serverApi : ServerApi, indexData : Country list) = 
    do serverApi.SetupDemo() |> isSuccessful
    let riceAndWheatPredicate = Predicate(fun c -> c.AgriProducts.Contains("rice") && c.AgriProducts.Contains("wheat"))
    let riceOrWheatPredicate = Predicate(fun c -> c.AgriProducts.Contains("rice") || c.AgriProducts.Contains("wheat"))
    member __.AllOfTest1() = 
        queryTest "allOf(agriproducts, 'rice') and allOf(agriproducts, 'wheat')" riceAndWheatPredicate 
            "AllOf 2 clauses with single tokens" s
    member __.AllOfTest2() = 
        queryTest "allOf(agriproducts, 'rice wheat')" riceAndWheatPredicate "AllOf single clause with a single token" s
    member __.AllOfTest3() = 
        queryTest "allOf(agriproducts, 'rice', 'wheat')" riceAndWheatPredicate "AllOf single clause with 2 tokens" s
    member __.AnyOfTest1() = 
        queryTest "anyOf(agriproducts, 'rice') OR anyOf(agriproducts, 'wheat')" riceOrWheatPredicate 
            "AnyOf 2 clauses with single tokens" s
    member __.AnyOfTest2() = 
        queryTest "anyOf(agriproducts, 'rice wheat')" riceOrWheatPredicate "AnyOf single clause with a single token" s
    member __.AnyOfTest3() = 
        queryTest "anyOf(agriproducts, 'rice', 'wheat')" riceOrWheatPredicate "AnyOf single clause with 2 tokens" s
    member __.FuzzyTest1() = queryTest "fuzzy(countryname, 'Iran')" (Expected 2) "Fuzzy with default slop of 1" s
    member __.FuzzyTest2() = queryTest "fuzzy(countryname, 'China', -slop '2')" (Expected 3) "Fuzzy with slop of 2" s
    member __.PhraseTest1() = 
        queryTest "phraseMatch(governmenttype, 'federal parliamentary democracy')" 
            (Predicate(fun x -> x.GovernmentType.Contains("federal parliamentary democracy"))) 
            "Phrase search passing multiple words as single token" s
    member __.PhraseTest2() = 
        queryTest "phraseMatch(governmenttype, 'federal', 'parliamentary', 'democracy')" 
            (Predicate
                 (fun x -> 
                 x.GovernmentType.Contains("federal") || x.GovernmentType.Contains("parliamentary") 
                 || x.GovernmentType.Contains("democracy"))) "Phrase search passing multiple words as multiple tokens" s
    member __.PhraseTest3() = 
        queryTest "phraseMatch(governmenttype, 'parliamentary monarchy', -slop '4')" (Expected 6) 
            "Phrase search with slop of 4" s
    member __.PhraseTest4() = 
        queryTest "phraseMatch(governmenttype, 'monarchy parliamentary', -slop '4')" (Expected 3) 
            "Phrase search with slop of 4" s
    member __.PhraseTest5() = 
        queryTest "phraseMatch(governmenttype, 'parliamentary', 'democracy system', -multiphrase)" 
            (Predicate
                 (fun x -> 
                 x.GovernmentType.Contains("parliamentary democracy") 
                 || x.GovernmentType.Contains("parliamentary system"))) 
            "Match both phrases containing 'parliamentary democracy' and 'parliamentary system'" s
    member __.PhraseTest6() = 
        queryTest "phraseMatch(governmenttype, 'parliamentary', 'democracy system constitutional', -multiphrase)" 
            (Predicate
                 (fun x -> 
                 x.GovernmentType.Contains("parliamentary democracy") 
                 || x.GovernmentType.Contains("parliamentary system") 
                 || x.GovernmentType.Contains("parliamentary constitutional"))) 
            "Match phrases containing 'parliamentary democracy', 'parliamentary system' and 'parliamentary constitutional'" 
            s
    member __.PhraseTest7() = 
        queryTest "phraseMatch(governmenttype, 'constitutional parliamentary', 'monarchy', -multiphrase)" 
            (Predicate
                 (fun x -> 
                 x.GovernmentType.Contains("constitutional monarchy") 
                 || x.GovernmentType.Contains("parliamentary monarchy"))) 
            "Match phrases containing 'parliamentary monarchy' and 'constitutional monarchy'" s
    member __.LikeTest1() = queryTest "like(countryname, 'uni*')" (Expected 5) "Like using '*' operator" s
    member __.LikeTest2() = 
        queryTest "like(countryname, 'unit?d')" 
            (Predicate(fun x -> Regex.Match(x.CountryName.ToLowerInvariant(), "unit[a-z]?d").Success)) 
            "Like with single character operator" s
    member __.LikeTest3() = 
        queryTest "like(countryname, '*uni*')" 
            (Predicate(fun x -> Regex.Match(x.CountryName.ToLowerInvariant(), "[a-z]?uni[a-z]?").Success)) 
            "Matching inside a word using like" s
    member __.RegexTest1() = 
        queryTest "regex(agriproducts, '[ms]ilk')" 
            (Predicate(fun x -> Regex.Match(x.AgriProducts.ToLowerInvariant(), "[ms]ilk").Success)) "Simple regex match" 
            s
    member __.MatchallTest1() = 
        queryTest "matchall(countryname, '*')" (Expected <| countryList.Count()) "Matchall to get all documents back" s
    member __.MatchnoneTest1() = 
        queryTest "matchnone(countryname, '*')" (Expected 0) "Matchnone will not match any documents" s
    member __.NumericRangeTest1() = 
        queryTest "gt(population, '1000000')" (Predicate(fun x -> x.Population > 1000000L)) "Greater than 'gt' operator" 
            s
    member __.NumericRangeTest2() = 
        queryTest "ge(population, '1000000')" (Predicate(fun x -> x.Population >= 1000000L)) 
            "Greater than or equal to 'ge' operator" s
    member __.NumericRangeTest3() = 
        queryTest "lt(population, '1000000')" (Predicate(fun x -> x.Population < 1000000L)) "Less than 'lt' operator" s
    member __.NumericRangeTest4() = 
        queryTest "le(population, '1000000')" (Predicate(fun x -> x.Population <= 1000000L)) 
            "Less than or equal to 'le' operator" s
