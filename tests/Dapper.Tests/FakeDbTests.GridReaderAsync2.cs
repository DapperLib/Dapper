#if !NET481
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional GridReader async tests covering dynamic overloads, Type-based overloads,
    /// and unbuffered async paths.
    /// </summary>
    public class FakeDbGridReaderAsync2Tests
    {
        private class Item { public int Id { get; set; } public string? Name { get; set; } }

        // ── Dynamic async read methods ────────────────────────────────

        [Fact]
        public async Task GridReader_ReadFirstAsync_Dynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            dynamic row = await multi.ReadFirstAsync();

            Assert.Equal(1, (int)row.Id);
        }

        [Fact]
        public async Task GridReader_ReadFirstOrDefaultAsync_Dynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 2 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            dynamic? row = await multi.ReadFirstOrDefaultAsync();

            Assert.NotNull(row);
            Assert.Equal(2, (int)row!.Id);
        }

        [Fact]
        public async Task GridReader_ReadFirstOrDefaultAsync_Dynamic_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var row = await multi.ReadFirstOrDefaultAsync();

            Assert.Null(row);
        }

        [Fact]
        public async Task GridReader_ReadSingleAsync_Dynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            dynamic row = await multi.ReadSingleAsync();

            Assert.Equal(3, (int)row.Id);
        }

        [Fact]
        public async Task GridReader_ReadSingleOrDefaultAsync_Dynamic_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var row = await multi.ReadSingleOrDefaultAsync();

            Assert.Null(row);
        }

        // ── Type-based async read methods ─────────────────────────────

        [Fact]
        public async Task GridReader_ReadAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var results = (await multi.ReadAsync(typeof(Item))).ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal("Alice", ((Item)results[0]).Name);
        }

        [Fact]
        public async Task GridReader_ReadFirstAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Charlie" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = await multi.ReadFirstAsync(typeof(Item));

            Assert.Equal(5, ((Item)row).Id);
        }

        [Fact]
        public async Task GridReader_ReadFirstOrDefaultAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 6 }, { "Name", "Dave" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = await multi.ReadFirstOrDefaultAsync(typeof(Item));

            Assert.NotNull(row);
            Assert.Equal(6, ((Item)row!).Id);
        }

        [Fact]
        public async Task GridReader_ReadFirstOrDefaultAsync_ByType_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = await multi.ReadFirstOrDefaultAsync(typeof(Item));

            Assert.Null(row);
        }

        [Fact]
        public async Task GridReader_ReadSingleAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Eve" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = await multi.ReadSingleAsync(typeof(Item));

            Assert.Equal(7, ((Item)row).Id);
        }

        [Fact]
        public async Task GridReader_ReadSingleOrDefaultAsync_ByType_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var row = await multi.ReadSingleOrDefaultAsync(typeof(Item));

            Assert.Null(row);
        }

        // ── ReadAsync<T>(buffered: false) — triggers unbuffered deferred path ──

        [Fact]
        public async Task GridReader_ReadAsync_Unbuffered_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            var results = (await multi.ReadAsync<Item>(buffered: false)).ToList();

            Assert.Equal(2, results.Count);
        }

        // ── ReadUnbufferedAsync() — dynamic variant ───────────────────

        [Fact]
        public async Task GridReader_ReadUnbufferedAsync_Dynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 99 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var results = new List<dynamic>();
            await foreach (var item in multi.ReadUnbufferedAsync())
            {
                results.Add(item);
            }

            Assert.Single(results);
            Assert.Equal(99, (int)results[0].Id);
        }
    }
}
#endif
