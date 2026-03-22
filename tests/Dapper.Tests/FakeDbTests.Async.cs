#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbAsyncTests
    {
        [Fact]
        public async Task QueryAsync_MapsColumnsToProperties()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var result = (await conn.QueryAsync<User>("SELECT Id, Name FROM Users")).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public async Task QueryAsync_ReturnsMultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            var result = (await conn.QueryAsync<User>("SELECT Id, Name FROM Users")).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task QueryFirstAsync_ReturnsFirstRow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "First" } },
                new Dictionary<string, object?> { { "Id", 6 }, { "Name", "Second" } },
            });
            conn.Open();

            var result = await conn.QueryFirstAsync<User>("SELECT Id, Name FROM Users");

            Assert.Equal(5, result.Id);
        }

        [Fact]
        public async Task QueryFirstAsync_ThrowsOnEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                conn.QueryFirstAsync<User>("SELECT Id, Name FROM Users"));
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var result = await conn.QueryFirstOrDefaultAsync<User>("SELECT Id, Name FROM Users");

            Assert.Null(result);
        }

        [Fact]
        public async Task QuerySingleAsync_ReturnsRow_WhenExactlyOne()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Solo" } }
            });
            conn.Open();

            var result = await conn.QuerySingleAsync<User>("SELECT Id, Name FROM Users WHERE Id = 7");

            Assert.Equal(7, result.Id);
        }

        [Fact]
        public async Task QuerySingleAsync_ThrowsOnMultipleRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                conn.QuerySingleAsync<User>("SELECT Id, Name FROM Users"));
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var result = await conn.QuerySingleOrDefaultAsync<User>("SELECT Id, Name FROM Users");

            Assert.Null(result);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsAffectedRowCount()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(5);
            conn.Open();

            var rows = await conn.ExecuteAsync("DELETE FROM Users");

            Assert.Equal(5, rows);
        }

        [Fact]
        public async Task ExecuteScalarAsync_ReturnsPreloadedValue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(99L);
            conn.Open();

            var count = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Users");

            Assert.Equal(99L, count);
        }
    }
}
#endif
