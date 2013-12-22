using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Specs.UnitTests
{
    using FlexSearch.Core;
    using FlexSearch.Specs.Helpers.SubSpec;

    using FluentAssertions;

    using Xunit;
    using Xunit.Extensions;

    public class PersistanceStoreSpec
    {
        public class TestClass
        {
            public string Property1 { get; set; }
            public int Property2 { get; set; }
        }

        [Specification]
        public void PersistanceTests()
        {
            Interface.IPersistanceStore persistanceStore = null;
            "Given an in memory store".Given(
                () =>
                {
                    persistanceStore = new Settings.PersistanceStore("", true);
                });

            "Adding a new TestClass should pass".Observation(
                () =>
                {
                    var test = new TestClass { Property1 = "test", Property2 = 1 };
                    var result = persistanceStore.Put("test", test);
                    result.Should().Be(true);
                });

            "Newly added value can be retrieved".Observation(
                () =>
                {
                    var result = persistanceStore.Get<TestClass>("test");
                    result.Value.Property1.Should().Be("test");
                    result.Value.Property2.Should().Be(1);
                });

            "Updating a value by key is possible".Observation(
                () =>
                {
                    var test = new TestClass { Property1 = "test1", Property2 = 2 };
                    persistanceStore.Put("test", test);
                    var result = persistanceStore.Get<TestClass>("test");
                    result.Value.Property1.Should().Be("test1");
                    result.Value.Property2.Should().Be(2);
                });

            "After adding another record of type TestClass, GetAll should return 2".Observation(
                () =>
                {
                    var test = new TestClass { Property1 = "test2", Property2 = 2 };
                    persistanceStore.Put("test1", test);
                    var result = persistanceStore.GetAll<TestClass>();
                    result.Count().Should().Be(2);
                });
        }
    }
}
