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
                ValueTuple<int, int> b = (24, 13);
                b.Item1.IsEqualTo(24);
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
        public void TupleReturnValue_Works_NamesIgnored()
        {
            var val = connection.QuerySingle<(int id, string name)>("select 42 as [Item2], 'Fred' as [Item1]");
            val.id.IsEqualTo(42);
            val.name.IsEqualTo("Fred");
        }

    }
}
