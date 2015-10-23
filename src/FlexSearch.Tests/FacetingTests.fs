module FacetingTests

open FlexSearch.Core
open Swensen.Unquote
open System
open System.Collections.Generic
open SearchTests

open FlexLucene.Index
open FlexLucene.Facet
open FlexLucene.Facet.Sortedset
open FlexLucene.Search
open FlexLucene.Analysis.Core
open FlexLucene.Store

let getFacetQuery indexName = new FacetQuery(indexName)
let withCount count (fq : FacetQuery) = fq.Count <- count; fq
let addGroup (fieldName, value, count) (fq : FacetQuery) = 
    let group = new FacetGroup(fq.IndexName)
    group.Count <- count
    group.FieldValue <- value
    group.FieldName <- fieldName
    fq.GroupBy <- fq.GroupBy |> Array.append [| group |]
    fq
let searchAndExtractFacet (searchService : ISearchService) (fq : FacetQuery) = 
    let result = searchService.Search fq
    test <@ succeeded result @>
    extract result

type ``Faceting Tests``(index : Index, 
                        indexService : IIndexService, 
                        documentService : IDocumentService,
                        searchService : ISearchService) =
    let addFacetField fieldName (index : Index) =
        let ff = new Field(fieldName, FieldDataType.Text)
        ff.AllowFaceting <- true
        index.Fields <- index.Fields |> Array.append [| ff |]
    
    do
        let testData = """
id,category,t1
1,Fish,Salmon
2,Insects,Mosquito
3,Fish,Piranha"""

        index |> addFacetField "category"

        indexTestData(testData, index, indexService, documentService)
        test <@ succeeded <| indexService.ForceCommit(index.IndexName) @>
    
    member __.``Should be able to use faceting field for normal searches``() =
        let result = 
            getQuery (index.IndexName, "category eq 'Fish'")
            |> withColumns [| "category"; "t1" |]
            |> searchAndExtract searchService
        result |> assertReturnedDocsCount 2
        result |> assertFieldCount 2
        result |> assertFieldValue 0 "category" "Fish"
        result |> assertFieldValue 0 "t1" "Salmon"

    member __.``Should be able to do faceting searches with a facet field``() =
        let result =
            getFacetQuery index.IndexName
            |> addGroup ("category", defString, 10)
            |> searchAndExtractFacet searchService
        test <@ not << String.IsNullOrEmpty <| result @>
        printfn "%s" result
        
    member __.``Low level facet test``(analyzerService : IAnalyzerService, scriptService : IScriptService) =
        let index = new Index(IndexName = Guid.NewGuid().ToString("N"))
        index.IndexConfiguration <- new IndexConfiguration(CommitOnClose = false, AutoCommit = false, AutoRefresh = false)
        index.Active <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.MemoryMapped
        index.Fields <- [| new Field("Author", FieldDataType.Text, AllowFaceting = true)
                           new Field("Publish Year", FieldDataType.Text, AllowFaceting = true) |]

        let indexSetting = 
            withIndexName (index.IndexName, Constants.DataFolder +/ index.IndexName)
            |> withShardConfiguration (index.ShardConfiguration)
            |> withIndexConfiguration (index.IndexConfiguration)
            |> withFields (index.Fields, analyzerService.GetAnalyzer, scriptService.GetComputedScript)
            |> withSearchProfiles (index.SearchProfiles, new FlexParser())
            |> build
        
        let indexDir = new FlexLucene.Store.MMapDirectory(java.nio.file.Paths.get(Constants.DataFolder +/ index.IndexName))
            //new RAMDirectory()
        let config = new FacetsConfig()
        let indexWriter = new IndexWriter(indexDir, IndexWriterConfigBuilder.buildWithSettings indexSetting)
            //(new IndexWriterConfig(new WhitespaceAnalyzer())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE))
        let buildDoc author year =
            let doc = new FlexLucene.Document.Document()
            doc.Add(new SortedSetDocValuesFacetField("Author", author))
            doc.Add(new SortedSetDocValuesFacetField("Publish Year", year))
            indexWriter.AddDocument(config.Build(doc))

        buildDoc "Bob" "2010"
        buildDoc "Lisa" "2010"
        buildDoc "Lisa" "2012"
        buildDoc "Susan" "2012"
        buildDoc "Frank" "1999"
        
        indexWriter.Commit()
        indexWriter.Close()

        let indexReader = DirectoryReader.Open(indexDir)
        let searcher = new IndexSearcher(indexReader)
        let state = new DefaultSortedSetDocValuesReaderState(indexReader)
        let fc = new FacetsCollector()
        
        let results = FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc)

        let facets = new SortedSetDocValuesFacetCounts(state, fc)

        let results = new List<FacetResult>()
        results.Add(facets.GetTopChildren(10, "Author"))
        results.Add(facets.GetTopChildren(10, "Publish Year"))
        indexReader.Close()

        printfn "%A" results
