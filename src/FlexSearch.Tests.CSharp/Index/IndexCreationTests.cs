namespace FlexSearch.Tests.CSharp.Index
{
    using System;

    using FlexSearch.Api.Types;
    using FlexSearch.Core;

    using NUnit.Framework;

    [TestFixture]
    public class IndexCreationTests
    {
        #region Fields

        private Interface.IIndexService indexService;

        #endregion

        #region Public Methods and Operators

        [Test]
        public void Cannot_close_an_closed_index()
        {
            var indexName = Guid.NewGuid().ToString("N");
            var index = new Index { IndexName = indexName };
            this.indexService.AddIndex(index);
            Assert.Catch<Exception>(() => this.indexService.CloseIndex(indexName));
            this.indexService.DeleteIndex(indexName);
        }

        [Test]
        public void Cannot_create_the_same_index_twice()
        {
            var indexName = Guid.NewGuid().ToString("N");
            Index index = new Index { IndexName = indexName };
            this.indexService.AddIndex(index);
            Assert.Catch<Exception>(() => this.indexService.AddIndex(index));
            this.indexService.DeleteIndex(indexName);
        }

        [Test]
        public void Cannot_open_an_open_index()
        {
            var indexName = Guid.NewGuid().ToString("N");
            Index index = new Index { IndexName = indexName, Online = true };
            this.indexService.AddIndex(index);
            Assert.Catch<Exception>(() => this.indexService.OpenIndex(indexName));
            this.indexService.DeleteIndex(indexName);
        }

        [TestFixtureSetUp]
        public void Init()
        {
            this.indexService = TestHelperFactory.GetDefaultIndexService();
        }

        [Test]
        public void Newly_created_index_should_be_offline()
        {
            var indexName = Guid.NewGuid().ToString("N");
            var index = new Index { IndexName = indexName, Online = false };
            this.indexService.AddIndex(index);
            var status = this.indexService.IndexStatus(indexName);
            Assert.AreEqual(IndexState.Offline, status);
            this.indexService.DeleteIndex(indexName);
        }

        [Test]
        public void Newly_created_index_should_be_online()
        {
            var indexName = Guid.NewGuid().ToString("N");
            var index = new Index { IndexName = indexName, Online = true };
            this.indexService.AddIndex(index);
            var status = this.indexService.IndexStatus(indexName);
            Assert.AreEqual(IndexState.Online, status);
            this.indexService.DeleteIndex(indexName);
        }

        [Test]
        public void Offline_index_can_be_made_online()
        {
            var indexName = Guid.NewGuid().ToString("N");
            var index = new Index { IndexName = indexName, Online = false };
            this.indexService.AddIndex(index);
            this.indexService.OpenIndex(indexName);
            var status = this.indexService.IndexStatus(indexName);
            Assert.AreEqual(IndexState.Online, status);
            this.indexService.DeleteIndex(indexName);
        }

        [Test]
        public void Online_index_can_be_made_offline()
        {
            var indexName = Guid.NewGuid().ToString("N");
            var index = new Index { IndexName = indexName, Online = true };
            this.indexService.AddIndex(index);
            this.indexService.CloseIndex(indexName);
            var status = this.indexService.IndexStatus(indexName);
            Assert.AreEqual(IndexState.Offline, status);
            this.indexService.DeleteIndex(indexName);
        }

        #endregion
    }
}