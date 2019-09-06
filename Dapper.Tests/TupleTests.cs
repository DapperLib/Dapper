using System;
using Xunit;

namespace Dapper.Tests
{
    [Collection("TupleTests")]
    public sealed class SystemSqlClientTupleTests : TupleTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("TupleTests")]
    public sealed class MicrosoftSqlClientTupleTests : TupleTests<MicrosoftSqlClientProvider> { }
#endif
    public abstract class TupleTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
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

        [Fact]
        public void TupleReturnValue_Works_With8Elements()
        {
            // C# encodes an 8-tuple as ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>

            var val = connection.QuerySingle<(int e1, int e2, int e3, int e4, int e5, int e6, int e7, int e8)>(
                "select 1, 2, 3, 4, 5, 6, 7, 8");

            Assert.Equal(1, val.e1);
            Assert.Equal(2, val.e2);
            Assert.Equal(3, val.e3);
            Assert.Equal(4, val.e4);
            Assert.Equal(5, val.e5);
            Assert.Equal(6, val.e6);
            Assert.Equal(7, val.e7);
            Assert.Equal(8, val.e8);
        }

        [Fact]
        public void TupleReturnValue_Works_With15Elements()
        {
            // C# encodes a 15-tuple as ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>>

            var val = connection.QuerySingle<(int e1, int e2, int e3, int e4, int e5, int e6, int e7, int e8, int e9, int e10, int e11, int e12, int e13, int e14, int e15)>(
                "select 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15");

            Assert.Equal(1, val.e1);
            Assert.Equal(2, val.e2);
            Assert.Equal(3, val.e3);
            Assert.Equal(4, val.e4);
            Assert.Equal(5, val.e5);
            Assert.Equal(6, val.e6);
            Assert.Equal(7, val.e7);
            Assert.Equal(8, val.e8);
            Assert.Equal(9, val.e9);
            Assert.Equal(10, val.e10);
            Assert.Equal(11, val.e11);
            Assert.Equal(12, val.e12);
            Assert.Equal(13, val.e13);
            Assert.Equal(14, val.e14);
            Assert.Equal(15, val.e15);
        }

        [Fact]
        public void TupleReturnValue_Works_WithStringField()
        {
            var val = connection.QuerySingle<ValueTuple<string>>("select '42'");
            Assert.Equal("42", val.Item1);
        }

        [Fact]
        public void TupleReturnValue_Works_WithByteField()
        {
            var val = connection.QuerySingle<ValueTuple<byte[]>>("select 0xDEADBEEF");
            Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, val.Item1);
        }
    }
}
