using System;
using Xunit;
using Cassandra;
using appbox.Models;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace appbox.Store.Tests
{
    public class CqlStoreTests
    {
        public CqlStoreTests()
        {
        }

        [Fact]
        public void BuilderTest()
        {
            var cluster = Cluster.Builder().AddContactPoints("10.211.55.3").Build();
            Assert.True(cluster != null);
        }
    }
}
