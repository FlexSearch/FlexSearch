module AnalysisTests

open FlexSearch.Api
open FlexSearch.Api.Message
open FlexSearch.Core
open FsUnit
open Fuchu
open System.Collections.Generic
open System.Linq

type TokenizerTestObject = 
    { Name : string
      Tokenizer : IFlexTokenizerFactory
      Input : string
      Output : string list }

type FilterTestObject = 
    { Name : string
      Filter : IFlexFilterFactory
      Parameters : IDictionary<string, string>
      Input : string
      Output : string list }

[<Tests>]
let tokenizerAnalysisTests = 
    testList "Tokenizer analysis tests" 
        [ for exp in [| { Name = "Standard Tokenizer"
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
                          Output = [ "Please,"; "email"; "john.doe@foo.com"; "by"; "03-09,"; "re:"; "m37-xq." ] } |] -> 
              testCase (sprintf "%s should tokenize the input" exp.Name) <| fun _ -> 
                  // Creating a dummy filter which won't do anything so that we can test the effect of tokenizer 
                  // in a stand alone manner
                  let filter = new Filters.PatternReplaceFilterFactory() :> IFlexFilterFactory
                  let filterParameters = new Dictionary<string, string>()
                  filterParameters.Add("pattern", "1")
                  filterParameters.Add("replacementtext", "")
                  filter.Initialize(filterParameters, new Factories.ResourceLoader())
                  let filters = new List<IFlexFilterFactory>()
                  filters.Add(filter)
                  let analyzer = new CustomAnalyzer(exp.Tokenizer, filters.ToArray())
                  SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", exp.Input) |> should equal exp.Output ]

[<Tests>]
let filterAnalysisTests = 
    testList "Filter analysis tests" 
        [ for exp in [| { Name = "Keepword filter"
                          Filter = new KeepWordsFilterFactory()
                          Parameters = dict [ ("filename", "wordlist.txt") ]
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
                          Filter = new StopFilterFactory()
                          Parameters = dict [ ("filename", "wordlist.txt") ]
                          Input = "hello world test"
                          Output = [ "test" ] }
                        { Name = "Synonym filter"
                          Filter = new SynonymFilter()
                          Parameters = dict [ ("filename", "synonymlist.txt") ]
                          Input = "easy"
                          Output = [ "easy"; "simple"; "clear" ] } |] -> 
              testList (sprintf "Given a %s" exp.Name) 
                  [ let result = ref Unchecked.defaultof<_>
                    yield testCase (sprintf "%s should filter the input" exp.Name) <| fun _ -> 
                              let filters = [| exp.Filter |]
                              filters.[0].Initialize(exp.Parameters, Helpers.resourceLoaderMock)
                              let analyzer = new CustomAnalyzer(new Tokenizers.StandardTokenizerFactory(), filters)
                              result := SearchDsl.ParseTextUsingAnalyzer(analyzer, "test", exp.Input)
                    yield testCase (sprintf "It should produce %i tokens" (exp.Output.Count())) 
                          <| fun _ -> result.contents.Count |> should equal (exp.Output.Count())
                    yield testCase "It should be equal to" <| fun _ -> result.contents |> should equal exp.Output ] ]
