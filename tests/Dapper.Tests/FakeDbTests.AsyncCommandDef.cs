#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for async query overloads that take CommandDefinition (dynamic and generic),
    /// and for GetRowParser/GetRowParser(Type) with DbDataReader and IDataReader.
    /// </summary>
    public class FakeDbAsyncCommandDefTests
    {
        private class User { public int Id { get; set; } public string? Name { get; set; } }

        // ── QueryAsync(CommandDefinition) dynamic overloads ───────────

        [Fact]
        public async Task QueryAsync_Dynamic_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var results = (await conn.QueryAsync(cmd)).ToList();

            Assert.Single(results);
            Assert.Equal(1, (int)results[0].Id);
        }

        [Fact]
        public async Task QueryFirstAsync_Dynamic_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            dynamic row = await conn.QueryFirstAsync(cmd);

            Assert.Equal(2, (int)row.Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_Dynamic_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            dynamic? row = await conn.QueryFirstOrDefaultAsync(cmd);

            Assert.NotNull(row);
            Assert.Equal(3, (int)row!.Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_Dynamic_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = await conn.QueryFirstOrDefaultAsync(cmd);

            Assert.Null(row);
        }

        [Fact]
        public async Task QuerySingleAsync_Dynamic_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 4 }, { "Name", "Dave" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            dynamic row = await conn.QuerySingleAsync(cmd);

            Assert.Equal(4, (int)row.Id);
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_Dynamic_CommandDefinition_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T");
            var row = await conn.QuerySingleOrDefaultAsync(cmd);

            Assert.Null(row);
        }

        // ── QueryAsync(string, ...) dynamic overloads ─────────────────

        [Fact]
        public async Task QueryAsync_Dynamic_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 } },
                new Dictionary<string, object?> { { "Id", 6 } },
            });
            conn.Open();

            var results = (await conn.QueryAsync("SELECT Id FROM T")).ToList();

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task QueryFirstAsync_Dynamic_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 } }
            });
            conn.Open();

            dynamic row = await conn.QueryFirstAsync("SELECT Id FROM T");
            Assert.Equal(7, (int)row.Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_Dynamic_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 8 } }
            });
            conn.Open();

            dynamic? row = await conn.QueryFirstOrDefaultAsync("SELECT Id FROM T");
            Assert.NotNull(row);
        }

        [Fact]
        public async Task QuerySingleAsync_Dynamic_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 9 } }
            });
            conn.Open();

            dynamic row = await conn.QuerySingleAsync("SELECT Id FROM T");
            Assert.Equal(9, (int)row.Id);
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_Dynamic_StringSql_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 10 } }
            });
            conn.Open();

            dynamic? row = await conn.QuerySingleOrDefaultAsync("SELECT Id FROM T");
            Assert.NotNull(row);
        }

        // ── Async multimap with Type[] ─────────────────────────────────

        private class Owner { public int Id { get; set; } public string? Name { get; set; } }
        private class Pet { public int PetId { get; set; } public string? Breed { get; set; } }

        [Fact]
        public async Task QueryAsync_TypeArray_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> {
                    { "Id", 1 }, { "Name", "Alice" },
                    { "PetId", 10 }, { "Breed", "Lab" }
                }
            });
            conn.Open();

            var results = (await conn.QueryAsync<(Owner, Pet)>(
                "SELECT ...",
                new[] { typeof(Owner), typeof(Pet) },
                objs => ((Owner)objs[0], (Pet)objs[1]),
                splitOn: "PetId")).ToList();

            Assert.Single(results);
            Assert.Equal("Alice", results[0].Item1.Name);
        }

        // ── GetRowParser(IDataReader, Type) ───────────────────────────

        [Fact]
        public void IDataReader_GetRowParser_ByType_Specific_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 11 }, { "Name", "Eve" } }
            });
            conn.Open();

            using IDataReader reader = conn.ExecuteReader("SELECT Id, Name FROM Users");
            var parser = reader.GetRowParser(typeof(User));

            Assert.True(reader.Read());
            var user = (User)parser(reader);
            Assert.Equal(11, user.Id);
        }

        [Fact]
        public void DbDataReader_GetRowParser_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 12 }, { "Name", "Frank" } }
            });
            conn.Open();

            using DbDataReader dbReader = (DbDataReader)conn.ExecuteReader("SELECT Id, Name FROM Users");
            var parser = dbReader.GetRowParser(typeof(User));

            Assert.True(dbReader.Read());
            var user = (User)parser(dbReader);
            Assert.Equal(12, user.Id);
        }

        [Fact]
        public void DbDataReader_GetRowParser_ValueType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Item1", 99 } }
            });
            conn.Open();

            using DbDataReader dbReader = (DbDataReader)conn.ExecuteReader("SELECT 99");
            // GetRowParser<int> with value type takes a different code path (IsValueType branch)
            var parser = dbReader.GetRowParser<int>();

            Assert.True(dbReader.Read());
            var val = parser(dbReader);
            Assert.Equal(99, val);
        }

        // ── QueryCachePurged event ────────────────────────────────────

        [Fact]
        public void PurgeQueryCache_FiresEvent_WhenSubscribed()
        {
            bool fired = false;
            SqlMapper.QueryCachePurged += OnPurged;
            try
            {
                SqlMapper.PurgeQueryCache();
                Assert.True(fired);
            }
            finally
            {
                SqlMapper.QueryCachePurged -= OnPurged;
            }

            void OnPurged(object? sender, EventArgs e) => fired = true;
        }
    }
}
#endif
