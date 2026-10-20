#if !NET481
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional GridReader tests covering unbuffered async reads and more edge cases.
    /// </summary>
    public class FakeDbGridReaderUnbufferedTests
    {
        private class Item { public int Id { get; set; } public string? Name { get; set; } }
        private class Extra { public int Count { get; set; } }

        // ── ReadUnbufferedAsync ───────────────────────────────────────

        [Fact]
        public async Task GridReader_ReadUnbufferedAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");

            var results = new List<Item>();
            await foreach (var item in multi.ReadUnbufferedAsync<Item>())
            {
                results.Add(item);
            }

            Assert.Equal(2, results.Count);
            Assert.Equal("Alice", results[0].Name);
            Assert.Equal("Bob", results[1].Name);
        }

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
            await foreach (var item in multi.ReadUnbufferedAsync<dynamic>())
            {
                results.Add(item);
            }

            Assert.Single(results);
            Assert.Equal(99, (int)results[0].Id);
        }

        // ── ReadAsync (buffered) ──────────────────────────────────────

        [Fact]
        public async Task GridReader_ReadAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var results = (await multi.ReadAsync<Item>()).ToList();

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task GridReader_ReadFirstAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Dave" } },
                new Dictionary<string, object?> { { "Id", 6 }, { "Name", "Eve" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = await multi.ReadFirstAsync<Item>();

            Assert.Equal(5, item.Id);
        }

        [Fact]
        public async Task GridReader_ReadFirstOrDefaultAsync_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(System.Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = await multi.ReadFirstOrDefaultAsync<Item>();

            Assert.Null(item);
        }

        [Fact]
        public async Task GridReader_ReadSingleAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Frank" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = await multi.ReadSingleAsync<Item>();

            Assert.Equal(7, item.Id);
        }

        [Fact]
        public async Task GridReader_ReadSingleOrDefaultAsync_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(System.Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = await multi.ReadSingleOrDefaultAsync<Item>();

            Assert.Null(item);
        }

        // ── DisposeAsync ──────────────────────────────────────────────

        [Fact]
        public async Task GridReader_DisposeAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            await using var multi = conn.QueryMultiple("SELECT Id FROM T");
            // Just verify DisposeAsync doesn't throw
        }

        // ── Sync Read variants ────────────────────────────────────────

        [Fact]
        public void GridReader_ReadFirst_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 10 }, { "Name", "Grace" } },
                new Dictionary<string, object?> { { "Id", 11 }, { "Name", "Heidi" } },
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = multi.ReadFirst<Item>();

            Assert.Equal(10, item.Id);
        }

        [Fact]
        public void GridReader_ReadFirstOrDefault_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(System.Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = multi.ReadFirstOrDefault<Item>();

            Assert.Null(item);
        }

        [Fact]
        public void GridReader_ReadSingle_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 20 }, { "Name", "Ivan" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = multi.ReadSingle<Item>();

            Assert.Equal(20, item.Id);
        }

        [Fact]
        public void GridReader_ReadSingleOrDefault_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(System.Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM Items");
            var item = multi.ReadSingleOrDefault<Item>();

            Assert.Null(item);
        }

        [Fact]
        public void GridReader_ReadDynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 30 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var results = multi.Read().ToList();

            Assert.Single(results);
            Assert.Equal(30, (int)results[0].Id);
        }

        [Fact]
        public void GridReader_ReadFirstDynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 40 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var item = multi.ReadFirst();

            Assert.Equal(40, (int)item.Id);
        }

        [Fact]
        public void GridReader_ReadSingleDynamic_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 50 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var item = multi.ReadSingle();

            Assert.Equal(50, (int)item.Id);
        }

        // ── QueryMultipleAsync ────────────────────────────────────────

        [Fact]
        public async Task QueryMultipleAsync_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            await using var multi = await conn.QueryMultipleAsync("SELECT Id, Name FROM Items");
            var results = (await multi.ReadAsync<Item>()).ToList();

            Assert.Single(results);
            Assert.Equal("Alice", results[0].Name);
        }

        [Fact]
        public async Task QueryMultipleAsync_WithParams_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Dave" } }
            });
            conn.Open();

            await using var multi = await conn.QueryMultipleAsync(
                "SELECT Id, Name FROM Items WHERE Id = @id", new { id = 5 });
            var results = (await multi.ReadAsync<Item>()).ToList();

            Assert.Single(results);
        }
    }
}
#endif
