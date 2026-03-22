#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbValueTupleTests
    {
        [Fact]
        public void Query_ValueTuple_TwoElements()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 1 }, { "Item2", "Alice" } }
            });
            conn.Open();

            var result = conn.Query<(int, string)>("SELECT 1, 'Alice'").ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Item1);
            Assert.Equal("Alice", result[0].Item2);
        }

        [Fact]
        public void Query_ValueTuple_ThreeElements()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 42 }, { "Item2", "hello" }, { "Item3", true } }
            });
            conn.Open();

            var result = conn.Query<(int, string, bool)>("SELECT 42, 'hello', 1").ToList();

            Assert.Single(result);
            Assert.Equal(42, result[0].Item1);
            Assert.Equal("hello", result[0].Item2);
            Assert.True(result[0].Item3);
        }

        [Fact]
        public void Query_ValueTuple_MultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 1 }, { "Item2", "A" } },
                new Dictionary<string, object?> { { "Item1", 2 }, { "Item2", "B" } },
                new Dictionary<string, object?> { { "Item1", 3 }, { "Item2", "C" } },
            });
            conn.Open();

            var result = conn.Query<(int, string)>("SELECT Id, Name FROM T").ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].Item1);
            Assert.Equal(3, result[2].Item1);
        }

        [Fact]
        public void QueryFirst_ValueTuple_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 5 }, { "Item2", "five" } }
            });
            conn.Open();

            var result = conn.QueryFirst<(int, string)>("SELECT 5, 'five'");

            Assert.Equal(5, result.Item1);
            Assert.Equal("five", result.Item2);
        }

        [Fact]
        public void QuerySingle_ValueTuple_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 9 }, { "Item2", "nine" } }
            });
            conn.Open();

            var result = conn.QuerySingle<(int, string)>("SELECT 9, 'nine'");

            Assert.Equal(9, result.Item1);
        }

        [Fact]
        public void Query_ValueTuple_NullableElement()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 7 }, { "Item2", DBNull.Value } }
            });
            conn.Open();

            var result = conn.QueryFirst<(int, string?)>("SELECT 7, NULL");

            Assert.Equal(7, result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        public async Task QueryAsync_ValueTuple_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 11 }, { "Item2", "eleven" } }
            });
            conn.Open();

            var result = (await conn.QueryAsync<(int, string)>("SELECT 11, 'eleven'")).ToList();

            Assert.Single(result);
            Assert.Equal(11, result[0].Item1);
        }

        [Fact]
        public void Query_ValueTuple_FourElements()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Item1", 1 }, { "Item2", "a" }, { "Item3", 3.14 }, { "Item4", true }
                }
            });
            conn.Open();

            var result = conn.QueryFirst<(int, string, double, bool)>("SELECT ...");

            Assert.Equal(1, result.Item1);
            Assert.Equal("a", result.Item2);
            Assert.True(result.Item4);
        }
    }
}
#endif
