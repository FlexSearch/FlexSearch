// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchTestsBase.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace FlexSearch.Tests.CSharp.Search
{
    using FlexSearch.Core;

    using NUnit.Framework;

    [TestFixture]
    public abstract class SearchTestsBase
    {
        #region Fields

        protected static Interface.IIndexService indexService;

        #endregion

        #region Public Methods and Operators

        [TestFixtureSetUp]
        public virtual void Init()
        {
            if (indexService != null)
            {
                return;
            }

            indexService = TestHelperFactory.GetDefaultIndexService();
            var settings = TestHelperFactory.GetBasicIndexSettingsForContact();
            indexService.AddIndex(settings);
            TestDataFactory.PopulateIndexWithTestData(indexService);
        }

        #endregion
    }
}