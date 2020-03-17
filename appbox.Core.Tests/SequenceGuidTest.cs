using System;
using Xunit;
using Xunit.Abstractions;

namespace appbox.Core.Tests
{
    public class SequenceGuidTest
    {

        private readonly ITestOutputHelper output;

        public SequenceGuidTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void SequenceTest()
        {
            var id1 = SequenceGuid.NewGuid();
            var id2 = SequenceGuid.NewGuid();

            Assert.True(id2.CompareTo(id1) > 0);
            output.WriteLine(id1.ToString());
            output.WriteLine(id2.ToString());
        }
    }
}
