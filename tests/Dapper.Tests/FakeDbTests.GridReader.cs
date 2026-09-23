#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbGridReaderTests
    {
        // Enqueue two result sets: first for Users, second for Products
        private static fakeDbConnection TwoResultSetConnection()
        {
            var conn = new fakeDbConnection(new FakeDataStore());
            // First result set
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            // Second result set (used by NextResult)
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "ProductId", 10 }, { "ProductName", "Widget" } },
            });
            conn.Open();
            return conn;
        }

        [Fact]
        public void QueryMultiple_ReadFirst_ReturnsUsers()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...; SELECT ...");
            var users = grid.Read<User>().ToList();

            Assert.Equal(2, users.Count);
            Assert.Equal("Alice", users[0].Name);
            Assert.Equal("Bob", users[1].Name);
        }

        [Fact]
        public void QueryMultiple_ReadDynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Eve" } }
            });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            var rows = grid.Read().ToList();

            Assert.Single(rows);
            Assert.Equal(5, (int)rows[0].Id);
        }

        [Fact]
        public void QueryMultiple_ReadFirst_ReturnsFirst()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            var first = grid.ReadFirst<User>();

            Assert.Equal(1, first.Id);
        }

        [Fact]
        public void QueryMultiple_ReadFirstOrDefault_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            var result = grid.ReadFirstOrDefault<User>();

            Assert.Null(result);
        }

        [Fact]
        public void QueryMultiple_ReadSingle_ThrowsOnMultiple()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            Assert.Throws<InvalidOperationException>(() => grid.ReadSingle<User>());
        }

        [Fact]
        public void QueryMultiple_ReadSingle_ReturnsRow_WhenExactlyOne()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Solo" } }
            });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            var row = grid.ReadSingle<User>();

            Assert.Equal(7, row.Id);
        }

        [Fact]
        public void QueryMultiple_ReadSingleOrDefault_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            var result = grid.ReadSingleOrDefault<User>();

            Assert.Null(result);
        }

        [Fact]
        public void QueryMultiple_ReadFirst_ThrowsOnEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            Assert.Throws<InvalidOperationException>(() => grid.ReadFirst<User>());
        }

        [Fact]
        public void QueryMultiple_ReadUnbuffered_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } },
            });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT ...");
            var result = grid.Read<User>(buffered: false).ToList();

            Assert.Equal(2, result.Count);
        }

        // ── async variants ────────────────────────────────────────────

        [Fact]
        public async Task QueryMultiple_ReadAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var grid = await conn.QueryMultipleAsync("SELECT ...");
            var users = (await grid.ReadAsync<User>()).ToList();

            Assert.Equal(2, users.Count);
        }

        [Fact]
        public async Task QueryMultiple_ReadFirstAsync_ReturnsFirst()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Three" } },
                new Dictionary<string, object?> { { "Id", 4 }, { "Name", "Four" } },
            });
            conn.Open();

            using var grid = await conn.QueryMultipleAsync("SELECT ...");
            var first = await grid.ReadFirstAsync<User>();

            Assert.Equal(3, first.Id);
        }

        [Fact]
        public async Task QueryMultiple_ReadFirstOrDefaultAsync_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var grid = await conn.QueryMultipleAsync("SELECT ...");
            var result = await grid.ReadFirstOrDefaultAsync<User>();

            Assert.Null(result);
        }

        [Fact]
        public async Task QueryMultiple_ReadSingleAsync_ReturnsRow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 9 }, { "Name", "Nine" } }
            });
            conn.Open();

            using var grid = await conn.QueryMultipleAsync("SELECT ...");
            var result = await grid.ReadSingleAsync<User>();

            Assert.Equal(9, result.Id);
        }

        [Fact]
        public async Task QueryMultiple_ReadSingleOrDefaultAsync_ReturnsNull_WhenEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var grid = await conn.QueryMultipleAsync("SELECT ...");
            var result = await grid.ReadSingleOrDefaultAsync<User>();

            Assert.Null(result);
        }

        [Fact]
        public async Task QueryMultiple_ReadDynamicAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            using var grid = await conn.QueryMultipleAsync("SELECT ...");
            var rows = (await grid.ReadAsync()).ToList();

            Assert.Single(rows);
        }

        [Fact]
        public void QueryMultiple_Dispose_DoesNotThrow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } }
            });
            conn.Open();

            var grid = conn.QueryMultiple("SELECT ...");
            grid.Dispose(); // Should not throw even without reading
        }
    }
}
#endif
