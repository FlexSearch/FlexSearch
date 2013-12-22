namespace FlexSearch.Specs.UnitTests
{
    using System;

    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    public class IndexCreationSpec
    {
        //#region Public Methods and Operators

        //[Thesis]
        //[IntegrationAutoFixture]
        //public void IndexCreationBasicTests(Interface.IIndexService indexService)
        //{
        //    "Given an index service, then".Given(() => { });
        //    "it is not possible to close an closed index".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName };
        //            indexService.AddIndex(index);
        //            Action action = () => indexService.CloseIndex(indexName);
        //            action.ShouldThrow<Exception>();
        //            indexService.DeleteIndex(indexName);
        //        });

        //    "it can not create the same index twice".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName };
        //            indexService.AddIndex(index);
        //            Action action = () => indexService.AddIndex(index);
        //            action.ShouldThrow<Exception>();
        //            indexService.DeleteIndex(indexName);
        //        });

        //    "it can not open an already open index".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName, Online = true };
        //            indexService.AddIndex(index);
        //            Action action = () => indexService.OpenIndex(index.IndexName);
        //            action.ShouldThrow<Exception>();
        //            indexService.DeleteIndex(indexName);
        //        });

        //    "Newly created index should be offline".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName, Online = false };
        //            indexService.AddIndex(index);
        //            IndexState status = indexService.IndexStatus(indexName);
        //            status.Should().Be(IndexState.Offline);
        //            indexService.DeleteIndex(indexName);
        //        });

        //    "Newly created index should be online".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName, Online = true };
        //            indexService.AddIndex(index);
        //            IndexState status = indexService.IndexStatus(indexName);
        //            status.Should().Be(IndexState.Online);
        //            indexService.DeleteIndex(indexName);
        //        });

        //    "Offline index can be made online".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName, Online = false };
        //            indexService.AddIndex(index);
        //            indexService.OpenIndex(indexName);
        //            IndexState status = indexService.IndexStatus(indexName);
        //            status.Should().Be(IndexState.Online);
        //            indexService.DeleteIndex(indexName);
        //        });

        //    "Online index can be made offline".Then(
        //        () =>
        //        {
        //            string indexName = Guid.NewGuid().ToString("N");
        //            var index = new Index { IndexName = indexName, Online = true };
        //            indexService.AddIndex(index);
        //            indexService.CloseIndex(indexName);
        //            IndexState status = indexService.IndexStatus(indexName);
        //            status.Should().Be(IndexState.Offline);
        //            indexService.DeleteIndex(indexName);
        //        });
        //}

        //#endregion
    }
}