using System;
using Xunit;

namespace Dapper.Tests
{
    public class TupleTests : TestBase
    {
        [Fact]
        public void TupleStructParameter_Fails_HelpfulMessage()
        {
            try
            {
                // I can see this happening...
                connection.QuerySingle<int>("select @id", (id: 42, name: "Fred"));
                Assert.Fail();
            }
            catch (NotSupportedException ex)
            {
                ex.Message.IsEqualTo("ValueTuple should not be used for parameters - the language-level names are not available to use as parameter names, and it adds unnecessary boxing");
            }
        }

        [Fact]
        public void TupleClassParameter_Works()
        {
            connection.QuerySingle<int>("select @Item1", Tuple.Create(42, "Fred")).IsEqualTo(42);
        }

        [Fact]
        public void TupleReturnValue_Works_ByPosition()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42, 'Fred'");
            val.id.IsEqualTo(42);
            val.name.IsEqualTo("Fred");
        }

        [Fact]
        public void TupleReturnValue_TooManyColumns_Ignored()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42, 'Fred', 123");
            val.id.IsEqualTo(42);
            val.name.IsEqualTo("Fred");
        }

        [Fact]
        public void TupleReturnValue_TooFewColumns_Unmapped()
        {
            // I'm very wary of making this throw, but I can also see some sense in pointing out the oddness
            var val = connection.QuerySingle<(int id, string name, int extra)>("select 42, 'Fred'");
            val.id.IsEqualTo(42);
            val.name.IsEqualTo("Fred");
            val.extra.IsEqualTo(0);
        }

        [Fact]
        public void TupleReturnValue_Works_NamesIgnored()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42 as [Item2], 'Fred' as [Item1]");
            val.id.IsEqualTo(42);
            val.name.IsEqualTo("Fred");
        }
    }
}
