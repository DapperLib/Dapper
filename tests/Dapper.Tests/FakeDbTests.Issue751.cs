#if !NET481
using System.Collections.Generic;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue751Tests
    {
        [Fact]
        public void IsConsumedIsTrueWhenReaderHasNoResultSets()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            using var grid = conn.QueryMultiple("exec NoResultSets");

            Assert.True(grid.IsConsumed);
        }

        [Fact]
        public void IsConsumedIsFalseWhenEmptyResultPrecedesRealResultSet()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueMultiResultReader(new[]
            {
                Enumerable.Empty<Dictionary<string, object?>>(),
                new[] { new Dictionary<string, object?> { ["Value"] = 7 } }
            });
            conn.Open();

            using var grid = conn.QueryMultiple("insert x; select 7");

            Assert.False(grid.IsConsumed);
            Assert.Equal(7, grid.Read<int>().Single());
            Assert.True(grid.IsConsumed);
        }

        [Fact]
        public void IsConsumedIsFalseForNonEmptyFirstResultAndTrueAfterLast()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueMultiResultReader(new[]
            {
                new[] { new Dictionary<string, object?> { ["Value"] = 1 } },
                new[] { new Dictionary<string, object?> { ["Value"] = 2 } }
            });
            conn.Open();

            using var grid = conn.QueryMultiple("select 1; select 2");

            Assert.False(grid.IsConsumed);
            Assert.Equal(1, grid.Read<int>().Single());
            Assert.False(grid.IsConsumed);
            Assert.Equal(2, grid.Read<int>().Single());
            Assert.True(grid.IsConsumed);
        }
    }
}
#endif
