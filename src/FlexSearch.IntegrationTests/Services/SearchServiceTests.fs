namespace FlexSearch.IntegrationTests.``Search Related``

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.TestSupport
open System.Collections.Generic
open System.Linq
open Xunit
open Xunit.Extensions

module ``Column Tests`` = 
    let testData = """
id,topic,surname,cvv2,company
1,a,jhonson,1,test1
2,c,hewitt,1,test2
3,b,Garner,1,test3
4,e,Garner,1,test4
5,d,jhonson,1,test5"""
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching with no columns specified will return no additional columns`` (index : Index, 
                                                                                   indexService : IIndexService, 
                                                                                   documentService : IDocumentService, 
                                                                                   searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(0, result.Documents.[0].Fields.Count)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching with columns specified with '*' will return all column`` (index : Index, 
                                                                              indexService : IIndexService, 
                                                                              documentService : IDocumentService, 
                                                                              searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.Columns.Add("*")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(index.Fields.Count, result.Documents.[0].Fields.Count)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching with columns specified as 'topic' will return just one column`` (index : Index, 
                                                                                     indexService : IIndexService, 
                                                                                     documentService : IDocumentService, 
                                                                                     searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.Columns.Add("topic")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(1, result.Documents.[0].Fields.Count)
        Assert.True(result.Documents.[0].Fields.ContainsKey("topic"))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching with columns specified as 'topic' & 'surname' will return just one column`` (index : Index, 
                                                                                                 indexService : IIndexService, 
                                                                                                 documentService : IDocumentService, 
                                                                                                 searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.Columns.Add("topic")
        query.Columns.Add("surname")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(2, result.Documents.[0].Fields.Count)
        Assert.True(result.Documents.[0].Fields.ContainsKey("topic"))
        Assert.True(result.Documents.[0].Fields.ContainsKey("surname"))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``SearchAsDictionarySeq will return the id column populated in Fields`` (index : Index, 
                                                                                 indexService : IIndexService, 
                                                                                 documentService : IDocumentService, 
                                                                                 searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        let (result, _, _) = GetSuccessChoice(searchService.SearchAsDictionarySeq(query))
        Assert.True(result.ToList().[0].ContainsKey(Constants.IdField))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``SearchAsDictionarySeq will return the lastmodified column populated in Fields`` (index : Index, 
                                                                                           indexService : IIndexService, 
                                                                                           documentService : IDocumentService, 
                                                                                           searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        let (result, _, _) = GetSuccessChoice(searchService.SearchAsDictionarySeq(query))
        Assert.True(result.ToList().[0].ContainsKey(Constants.LastModifiedField))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``SearchAsDictionarySeq will return the type column populated in Fields`` (index : Index, 
                                                                                   indexService : IIndexService, 
                                                                                   documentService : IDocumentService, 
                                                                                   searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        let (result, _, _) = GetSuccessChoice(searchService.SearchAsDictionarySeq(query))
        Assert.True(result.ToList().[0].ContainsKey(Constants.TypeField))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``SearchAsDictionarySeq will return the _score column populated in Fields`` (index : Index, 
                                                                                     indexService : IIndexService, 
                                                                                     documentService : IDocumentService, 
                                                                                     searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        let (result, _, _) = GetSuccessChoice(searchService.SearchAsDictionarySeq(query))
        Assert.True(result.ToList().[0].ContainsKey("_score"))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``No score will be returned if ReturnScore is set to false`` (index : Index, indexService : IIndexService, 
                                                                      documentService : IDocumentService, 
                                                                      searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.ReturnScore <- false
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<double>(0.0, result.Documents.[0].Score)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``Stored field cannot be searched`` (index : Index, indexService : IIndexService, 
                                             documentService : IDocumentService, searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "company = 'test1'")
        searchService.Search(query) |> ExpectErrorCode(STORED_FIELDS_CANNOT_BE_SEARCHED |> GenerateOperationMessage)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``Stored fields can be retrieved`` (index : Index, indexService : IIndexService, 
                                            documentService : IDocumentService, searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.Columns.Add("company")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.True(result.Documents.[0].Fields.ContainsKey("company"))
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess

module ``Paging Tests`` = 
    let testData = """
id,givenname,surname,cvv2
1,Aaron,jhonson,1
2,aron,hewitt,1
3,Airon,Garner,1
4,aroon,Garner,1
5,aronn,jhonson,1
6,aroonn,jhonson,1"""
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching for 'cvv2 = 1' with Count = 2 will return 2 records`` (index : Index, indexService : IIndexService, 
                                                                           documentService : IDocumentService, 
                                                                           searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.Count <- 2
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(2, result.Documents.Count)
        Assert.Equal<string>("1", result.Documents.[0].Id)
        Assert.Equal<string>("2", result.Documents.[1].Id)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory>]
    [<InlineAutoMockIntegrationDataAttribute(1, "2", "3")>]
    [<InlineAutoMockIntegrationDataAttribute(2, "3", "4")>]
    [<InlineAutoMockIntegrationDataAttribute(3, "4", "5")>]
    [<InlineAutoMockIntegrationDataAttribute(4, "5", "6")>]
    let ``Searching for 'cvv2 = 1' with records to return = 2 and skip = x will return 2 records`` (skip : int, 
                                                                                                    expected1 : string, 
                                                                                                    expected2 : string, 
                                                                                                    index : Index, 
                                                                                                    indexService : IIndexService, 
                                                                                                    documentService : IDocumentService, 
                                                                                                    searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.Count <- 2
        query.Skip <- skip
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(2, result.Documents.Count)
        Assert.Equal<string>(expected1, result.Documents.[0].Id)
        Assert.Equal<string>(expected2, result.Documents.[1].Id)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess

module ``Sorting Tests`` = 
    let testData = """
id,topic,surname,cvv2
1,a,jhonson,1
2,c,hewitt,1
3,b,Garner,1
4,e,Garner,1
5,d,jhonson,1"""
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching for 'cvv2 = 1' with orderby topic should return 5 records`` (index : Index, 
                                                                                 indexService : IIndexService, 
                                                                                 documentService : IDocumentService, 
                                                                                 searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "cvv2 eq '1'")
        query.OrderBy <- "topic"
        query.Columns.Add("topic")
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(5, result.Documents.Count)
        Assert.Equal<string>("a", result.Documents.[0].Fields.["topic"])
        Assert.Equal<string>("b", result.Documents.[1].Fields.["topic"])
        Assert.Equal<string>("c", result.Documents.[2].Fields.["topic"])
        Assert.Equal<string>("d", result.Documents.[3].Fields.["topic"])
        Assert.Equal<string>("e", result.Documents.[4].Fields.["topic"])
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess

module ``Highlighting Tests`` = 
    let testData = """
id,topic,abstract
1,Computer Science,Computer science (abbreviated CS or CompSci) is the scientific and practical approach to computation and its applications. It is the systematic study of the feasibility structure expression and mechanization of the methodical processes (or algorithms) that underlie the acquisition representation processing storage communication of and access to information whether such information is encoded in bits and bytes in a computer memory or transcribed in genes and protein structures in a human cell. A computer scientist specializes in the theory of computation and the design of computational systems.
2,Computer programming,Computer programming (often shortened to programming) is the comprehensive process that leads from an original formulation of a computing problem to executable programs. It involves activities such as analysis understanding and generically solving such problems resulting in an algorithm verification of requirements of the algorithm including its correctness and its resource consumption implementation (or coding) of the algorithm in a target programming language testing debugging and maintaining the source code implementation of the build system and management of derived artefacts such as machine code of computer programs.
"""
    
    [<Theory; AutoMockIntegrationData>]
    let ``Searching for abstract match 'practical approach' with orderby topic should return 1 records`` (index : Index, 
                                                                                                          indexService : IIndexService, 
                                                                                                          documentService : IDocumentService, 
                                                                                                          searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "abstract match 'practical approach'")
        query.Highlights <- new HighlightOption(new List<string>([ "abstract" ]))
        query.Highlights.FragmentsToReturn <- 1
        query.Highlights.PreTag <- "<imp>"
        query.Highlights.PostTag <- "</imp>"
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(1, result.Documents.Count)
        Assert.Equal<int>(1, result.Documents.[0].Highlights.Count)
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("practical"), 
             "The highlighted passage should contain 'practical'")
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("approach"), 
             "The highlighted passage should contain 'approach'")
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("<imp>practical</imp>"), 
             "The highlighted should contain 'practical' with in pre and post tags")
        Assert.True
            (result.Documents.[0].Highlights.[0].Contains("<imp>approach</imp>"), 
             "The highlighted should contain 'approach' with in pre and post tags")
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess

module ``Search profile Tests`` = 
    let testData = """
id,topic,surname,cvv2,givenname
1,a,jhonson,1,aron
2,c,hewitt,1,jhon
3,c,hewitt,1,jhon
4,d,hewitt,1,jhon
5,d,hewitt,1,jhon
6,b,Garner,1,joe
7,e,Garner,1,sam
8,d,jhonson,1,andrew"""
    
    // "givenname = '' AND surname = '' AND (cvv2 = '1' OR topic = '')"
    [<Theory; AutoMockIntegrationData>]
    let ``Searching with searchprofile 'test1' will return 2 record`` (index : Index, indexService : IIndexService, 
                                                                       documentService : IDocumentService, 
                                                                       searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "{givenname:'jhon',surname:'hewitt',cvv2:'1',topic:'c'}")
        query.SearchProfile <- "test1"
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(2, result.Documents.Count)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``If no value for cvv2 is passed then the default configured value of 1 will be used`` (index : Index, 
                                                                                                indexService : IIndexService, 
                                                                                                documentService : IDocumentService, 
                                                                                                searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "{givenname:'jhon',surname:'hewitt',topic:'c'}")
        query.SearchProfile <- "test1"
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(2, result.Documents.Count)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``If no value for cvv2 is passed and no value for topic is passed then topic will be ignored`` (index : Index, 
                                                                                                        indexService : IIndexService, 
                                                                                                        documentService : IDocumentService, 
                                                                                                        searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "{givenname:'jhon',surname:'hewitt'}")
        query.SearchProfile <- "test1"
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(4, result.Documents.Count)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``If no value for givenname is passed then the profile will throw error as that option is set`` (index : Index, 
                                                                                                         indexService : IIndexService, 
                                                                                                         documentService : IDocumentService, 
                                                                                                         searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "{surname:'hewitt'}")
        query.SearchProfile <- "test1"
        searchService.Search(query) |> ExpectErrorCode(MISSING_FIELD_VALUE |> GenerateOperationMessage)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
    
    [<Theory; AutoMockIntegrationData>]
    let ``If no value for surname is passed then the profile will throw error as the value is missing`` (index : Index, 
                                                                                                         indexService : IIndexService, 
                                                                                                         documentService : IDocumentService, 
                                                                                                         searchService : ISearchService) = 
        AddTestDataToIndex(index, testData, documentService, indexService)
        let query = new SearchQuery(index.IndexName, "{givenname:'jhon'}")
        query.SearchProfile <- "test1"
        searchService.Search(query) |> ExpectErrorCode(MISSING_FIELD_VALUE |> GenerateOperationMessage)
        indexService.DeleteIndex(index.IndexName) |> ExpectSuccess
