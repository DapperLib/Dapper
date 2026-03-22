#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbBatchExecuteTests
    {
        // ── Execute with IEnumerable<T> (sync batch) ──────────────────

        [Fact]
        public void Execute_WithList_ExecutesForEach()
        {
            var items = new[]
            {
                new { id = 1, name = "Alice" },
                new { id = 2, name = "Bob" },
                new { id = 3, name = "Carol" },
            };

            using var conn = new fakeDbConnection(new FakeDataStore());
            // Enqueue one result per item
            foreach (var _ in items)
                conn.EnqueueNonQueryResult(1);
            conn.Open();

            var total = conn.Execute("INSERT INTO Users (Id, Name) VALUES (@id, @name)", items);

            Assert.Equal(3, total);
        }

        [Fact]
        public void Execute_WithEmptyList_ReturnsZero()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            var total = conn.Execute("INSERT INTO Users VALUES (@id)",
                Enumerable.Empty<object>());

            Assert.Equal(0, total);
        }

        [Fact]
        public void Execute_WithSingleItem_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var total = conn.Execute("DELETE FROM Users WHERE Id = @id",
                new[] { new { id = 42 } });

            Assert.Equal(1, total);
        }

        // ── Async batch execute ───────────────────────────────────────

        [Fact]
        public async Task ExecuteAsync_WithList_ExecutesForEach()
        {
            var items = new[]
            {
                new { id = 1 },
                new { id = 2 },
            };

            using var conn = new fakeDbConnection(new FakeDataStore());
            foreach (var _ in items)
                conn.EnqueueNonQueryResult(1);
            conn.Open();

            var total = await conn.ExecuteAsync(
                "DELETE FROM Users WHERE Id = @id", items);

            Assert.Equal(2, total);
        }

        [Fact]
        public async Task ExecuteAsync_Pipelined_WithList_Works()
        {
            var items = Enumerable.Range(1, 5)
                .Select(i => new { id = i })
                .ToList();

            using var conn = new fakeDbConnection(new FakeDataStore());
            foreach (var _ in items)
                conn.EnqueueNonQueryResult(1);
            conn.Open();

            var cmd = new CommandDefinition(
                "DELETE FROM Users WHERE Id = @id",
                items,
                flags: CommandFlags.Pipelined);

            var total = await conn.ExecuteAsync(cmd);

            Assert.Equal(5, total);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyList_ReturnsZero()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            var total = await conn.ExecuteAsync("DELETE FROM Users WHERE Id = @id",
                Enumerable.Empty<object>());

            Assert.Equal(0, total);
        }

        // ── ExecuteReaderAsync returning IDataReader (triggers CastResult) ──

        [Fact]
        public async Task ExecuteReaderAsync_ViaIDbConnection_ReturnsIDataReader()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            // Call via IDbConnection interface to get Task<IDataReader> (triggers Extensions.CastResult)
            IDbConnection iconn = conn;
            var reader = await iconn.ExecuteReaderAsync("SELECT Id, Name FROM Users");
            using (reader)
            {
                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetInt32(reader.GetOrdinal("Id")));
            }
        }

        [Fact]
        public async Task ExecuteReaderAsync_ViaIDbConnection_WithParams_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 } }
            });
            conn.Open();

            IDbConnection iconn = conn;
            var reader = await iconn.ExecuteReaderAsync(
                "SELECT Id FROM Users WHERE Id = @id",
                new { id = 5 });
            using (reader)
            {
                Assert.True(reader.Read());
            }
        }

        // ── DataTable as parameter (triggers DataTableHandler) ────────

        [Fact]
        public void Execute_WithDataTableParameter_Works()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);
            dt.Rows.Add(2);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(2);
            conn.Open();

            // Pass DataTable via DynamicParameters (triggers DataTableHandler.SetValue)
            var dp = new DynamicParameters();
            dp.Add("ids", dt, DbType.Object);
            conn.Execute("EXEC BulkInsert @ids", dp);
        }

        // ── Large list (tests padding logic) ─────────────────────────

        [Fact]
        public void Query_WithLargeListParameter_Works()
        {
            var ids = Enumerable.Range(1, 15).ToList();

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(ids.Select(i =>
                new Dictionary<string, object?> { { "Id", i }, { "Name", $"User{i}" } }
            ).ToList());
            conn.Open();

            var result = conn.Query<User>(
                "SELECT Id, Name FROM Users WHERE Id IN @ids",
                new { ids }).ToList();

            Assert.Equal(15, result.Count);
        }

        [Fact]
        public void Query_WithListParameter_OddCount_Works()
        {
            var ids = new[] { 1, 2, 3 };  // odd - may trigger padding

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(ids.Select(i =>
                new Dictionary<string, object?> { { "Id", i }, { "Name", $"U{i}" } }
            ).ToList());
            conn.Open();

            var result = conn.Query<User>(
                "SELECT Id, Name FROM Users WHERE Id IN @ids",
                new { ids }).ToList();

            Assert.Equal(3, result.Count);
        }
    }
}
#endif
