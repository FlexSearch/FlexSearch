namespace FlexSearch.Core.Tests

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open NSubstitute
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

module ``Analysis tests`` = 
    type TokenizerTestObject = 
        { Name : string
          Tokenizer : IFlexTokenizerFactory
          Input : string
          Output : string list }
    
    let tokenizerTestCases = 
        [| { Name = "Standard Tokenizer"
             Tokenizer = new StandardTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "Please"; "email"; "john.doe"; "foo.com"; "by"; "03"; "09"; "re"; "m37"; "xq" ] }
           { Name = "Classic Tokenizer"
             Tokenizer = new ClassicTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "Please"; "email"; "john.doe@foo.com"; "by"; "03-09"; "re"; "m37-xq" ] }
           { Name = "UAX29URLEmail Tokenizer"
             Tokenizer = new UAX29URLEmailTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "Please"; "email"; "john.doe@foo.com"; "by"; "03"; "09"; "re"; "m37"; "xq" ] }
           { Name = "Keyword Tokenizer"
             Tokenizer = new KeywordTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "Please, email john.doe@foo.com by 03-09, re: m37-xq." ] }
           { Name = "Lowercase Tokenizer"
             Tokenizer = new LowercaseTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "please"; "email"; "john"; "doe"; "foo"; "com"; "by"; "re"; "m"; "xq" ] }
           { Name = "Letter Tokenizer"
             Tokenizer = new LetterTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "Please"; "email"; "john"; "doe"; "foo"; "com"; "by"; "re"; "m"; "xq" ] }
           { Name = "Whitespace Tokenizer"
             Tokenizer = new WhitespaceTokenizerFactory()
             Input = "Please, email john.doe@foo.com by 03-09, re: m37-xq."
             Output = [ "Please,"; "email"; "john.doe@foo.com"; "by"; "03-09,"; "re:"; "m37-xq." ] } |]
    
    [<Theory>]
    [<InlineAutoMockDataAttribute(0)>]
    [<InlineAutoMockDataAttribute(1)>]
    [<InlineAutoMockDataAttribute(2)>]
    [<InlineAutoMockDataAttribute(3)>]
    [<InlineAutoMockDataAttribute(4)>]
    [<InlineAutoMockDataAttribute(5)>]
    [<InlineAutoMockDataAttribute(6)>]
    let ``Tokenizer analysis test`` (caseNumber : int, resourceLoader : IResourceService) = 
        // Creating a dummy filter which won't do anything so that we can test the effect of tokenizer 
        // in a stand alone manner
        let filter = new PatternReplaceFilterFactory() :> IFlexFilterFactory
        let filterParameters = new Dictionary<string, string>()
        filterParameters.Add("pattern", "1")
        filterParameters.Add("replacementtext", "")
        filter.Initialize(filterParameters) |> ignore
        let filters = new List<IFlexFilterFactory>()
        filters.Add(filter)
        let exp = tokenizerTestCases.[caseNumber]
        let analyzer = new CustomAnalyzer(exp.Tokenizer, filters.ToArray())
        let output = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", exp.Input)
        Assert.Equal<List<string>>(exp.Output.ToList(), output)
    
    type FilterTestObject = 
        { Name : string
          Filter : IFlexFilterFactory
          Parameters : IDictionary<string, string>
          Input : string
          Output : string list }
    
    let mapList = 
        let map = new MapList()
        map.Words.Add("easy", [| "simple"; "clear" |].ToList())
        map
    
    let resourceLoaderMock = Substitute.For<IResourceService>()
    
    resourceLoaderMock.GetResource<FilterList>(Arg.Any())
        .Returns(Choice1Of2(new FilterList([| "hello"; "world" |].ToList()))) |> ignore
    resourceLoaderMock.GetResource<MapList>(Arg.Any()).Returns(Choice1Of2(mapList)) |> ignore
    
    let FilterTestCases = 
        [| { Name = "Keepword filter"
             Filter = new KeepWordsFilterFactory(resourceLoaderMock)
             Parameters = dict [ ("resourceName", "wordlist") ]
             Input = "hello world test"
             Output = [ "hello"; "world" ] }
           { Name = "Standard filter"
             Filter = new StandardFilterFactory()
             Parameters = dict []
             Input = "Bob's I.O.U."
             Output = [ "Bob's"; "I.O.U" ] }
           { Name = "Lowercase filter"
             Filter = new LowerCaseFilterFactory()
             Parameters = dict []
             Input = "Bob's I.O.U."
             Output = [ "bob's"; "i.o.u" ] }
           { Name = "Length filter"
             Filter = new LengthFilterFactory()
             Parameters = 
                 dict [ ("min", "3")
                        ("max", "7") ]
             Input = "turn right at Albuquerque"
             Output = [ "turn"; "right" ] }
           { Name = "PatternReplace filter"
             Filter = new PatternReplaceFilterFactory()
             Parameters = 
                 dict [ ("pattern", "cat")
                        ("replacementtext", "dog") ]
             Input = "cat concatenate catycat"
             Output = [ "dog"; "condogenate"; "dogydog" ] }
           { Name = "ReverseString filter"
             Filter = new ReverseStringFilterFactory()
             Parameters = dict []
             Input = "hello how are you"
             Output = [ "olleh"; "woh"; "era"; "uoy" ] }
           { Name = "StopWord filter"
             Filter = new StopFilterFactory(resourceLoaderMock)
             Parameters = dict [ ("resourceName", "wordlist") ]
             Input = "hello world test"
             Output = [ "test" ] }
           { Name = "Synonym filter"
             Filter = new SynonymFilter(resourceLoaderMock)
             Parameters = dict [ ("resourceName", "synonymlist") ]
             Input = "easy"
             Output = [ "easy"; "simple"; "clear" ] } |]
    
    [<Theory>]
    [<InlineAutoMockDataAttribute(0)>]
    [<InlineAutoMockDataAttribute(1)>]
    [<InlineAutoMockDataAttribute(2)>]
    [<InlineAutoMockDataAttribute(3)>]
    [<InlineAutoMockDataAttribute(4)>]
    [<InlineAutoMockDataAttribute(5)>]
    [<InlineAutoMockDataAttribute(6)>]
    [<InlineAutoMockDataAttribute(7)>]
    let ``Filter analysis test`` (caseNumber : int) = 
        let exp = FilterTestCases.[caseNumber]
        let filters = [| exp.Filter |]
        filters.[0].Initialize(exp.Parameters) |> ignore
        let analyzer = new CustomAnalyzer(new StandardTokenizerFactory(), filters)
        let output = SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", exp.Input)
        Assert.Equal<List<string>>(exp.Output.ToList(), output)
