namespace FlexSearch.Tests.CSharp.Integration
{
    using System;

    using FlexSearch.Api.Index;
    using FlexSearch.Api.Types;

    using NUnit.Framework;

    using ServiceStack.ServiceClient.Web;

    [TestFixture]
    public class IndexCreationTests
    {
        #region Fields

        private JsonServiceClient client;

        #endregion

        #region Public Methods and Operators

        [Test]
        public void Create_index_with_custom_analyzer()
        {
            var indexName = Guid.NewGuid().ToString("N");
            Index index = new Index { IndexName = indexName };

            index.Fields.Add(
                "firstname",
                new IndexFieldProperties { FieldType = FieldType.Text, IndexAnalyzer = "firstnameanalyzer" });

            var analyzerProperties = new AnalyzerProperties();
            analyzerProperties.Filters.Add(new Filter { FilterName = "standardfilter" });
            analyzerProperties.Filters.Add(new Filter { FilterName = "lowercasefilter" });
            index.Analyzers.Add("firstnameanalyzer", analyzerProperties);
            var response = this.client.Send<CreateIndexResponse>(new CreateIndex { Index = index });
            Assert.IsNull(response.ResponseStatus);
            this.client.Send<DestroyIndexResponse>(new DestroyIndex { IndexName = indexName });
        }

        [Test]
        public void Create_index_with_dynamic_fields()
        {
            var indexName = Guid.NewGuid().ToString("N");
            Index index = new Index { IndexName = indexName };

            FieldDictionary fields = new FieldDictionary();
            fields.Add("firstname", new IndexFieldProperties { FieldType = FieldType.Text });
            fields.Add("lastname", new IndexFieldProperties { FieldType = FieldType.Text });
            fields.Add("fullname", new IndexFieldProperties { FieldType = FieldType.Text, ScriptName = "fullname" });

            index.Scripts.Add(
                "fullname",
                new ScriptProperties
                {
                    ScriptOption = ScriptOption.SingleLine,
                    ScriptType = ScriptType.ComputedField,
                    ScriptSource = "fields[\"firstname\"] + \" \" + fields[\"lastname\"]"
                });

            index.Fields = fields;
            var response = this.client.Send<CreateIndexResponse>(new CreateIndex { Index = index });
            Assert.IsNull(response.ResponseStatus);
            this.client.Send<DestroyIndexResponse>(new DestroyIndex { IndexName = indexName });
        }

        [Test]
        public void Create_simple_index()
        {
            var indexName = Guid.NewGuid().ToString("N");
            Index index = new Index { IndexName = indexName };

            FieldDictionary fields = new FieldDictionary();
            fields.Add("firstname", new IndexFieldProperties { FieldType = FieldType.Text });
            fields.Add("lastname", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields = fields;
            var response = this.client.Send<CreateIndexResponse>(new CreateIndex { Index = index });
            Assert.IsNull(response.ResponseStatus);
            this.client.Send<DestroyIndexResponse>(new DestroyIndex { IndexName = indexName });
        }

        [TestFixtureSetUp]
        public void Setup()
        {
            TestHelperFactory.StartTestServer();
            this.client = new JsonServiceClient("http://SEEMANT-PC:9800/");
        }

        #endregion
    }
}