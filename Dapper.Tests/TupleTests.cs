using System;
using Xunit;

namespace Dapper.Tests
{
    public class TupleTests : TestBase
    {
        [Fact]
        public void TupleStructParameter_Fails_HelpfulMessage()
        {
            var ex = Assert.Throws<NotSupportedException>(() => connection.QuerySingle<int>("select @id", (id: 42, name: "Fred")));
            Assert.Equal("ValueTuple should not be used for parameters - the language-level names are not available to use as parameter names, and it adds unnecessary boxing", ex.Message);
        }

        [Fact]
        public void TupleClassParameter_Works()
        {
            Assert.Equal(42, connection.QuerySingle<int>("select @Item1", Tuple.Create(42, "Fred")));
        }

        [Fact]
        public void TupleReturnValue_Works_ByPosition()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42, 'Fred'");
            Assert.Equal(42, val.id);
            Assert.Equal("Fred", val.name);
        }

        [Fact]
        public void TupleReturnValue_TooManyColumns_Ignored()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42, 'Fred', 123");
            Assert.Equal(42, val.id);
            Assert.Equal("Fred", val.name);
        }

        [Fact]
        public void TupleReturnValue_TooFewColumns_Unmapped()
        {
            // I'm very wary of making this throw, but I can also see some sense in pointing out the oddness
            var val = connection.QuerySingle<(int id, string name, int extra)>("select 42, 'Fred'");
            Assert.Equal(42, val.id);
            Assert.Equal("Fred", val.name);
            Assert.Equal(0, val.extra);
        }

        [Fact]
        public void TupleReturnValue_Works_NamesIgnored()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42 as [Item2], 'Fred' as [Item1]");
            Assert.Equal(42, val.id);
            Assert.Equal("Fred", val.name);
        }
    }
}
