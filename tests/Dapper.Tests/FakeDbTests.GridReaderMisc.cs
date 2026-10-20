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
    /// <summary>
    /// Covers GridReader sync dynamic overloads (ReadFirstOrDefault/ReadSingleOrDefault),
    /// async ThrowZeroRows path (ReadFirstAsync on empty), CommandDefinition with Transaction,
    /// and QueryMultiple with multiple result sets (OnAfterGrid NextResult path).
    /// </summary>
    public class FakeDbGridReaderMiscTests
    {
        private class Item { public int Id { get; set; } public string? Name { get; set; } }

        // ── Sync ReadFirstOrDefault() — dynamic, no type arg ──────────
        // Line 77 in SqlMapper.GridReader.cs

        [Fact]
        public void GridReader_ReadFirstOrDefault_Dynamic_WithValue_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            dynamic? row = multi.ReadFirstOrDefault();

            Assert.NotNull(row);
            Assert.Equal(1, (int)row!.Id);
        }

        [Fact]
        public void GridReader_ReadFirstOrDefault_Dynamic_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var row = multi.ReadFirstOrDefault();

            Assert.Null(row);
        }

        // ── Sync ReadSingleOrDefault() — dynamic, no type arg ─────────
        // Line 89 in SqlMapper.GridReader.cs

        [Fact]
        public void GridReader_ReadSingleOrDefault_Dynamic_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            var row = multi.ReadSingleOrDefault();

            Assert.Null(row);
        }

        [Fact]
        public void GridReader_ReadSingleOrDefault_Dynamic_WithValue_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 } }
            });
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id FROM T");
            dynamic? row = multi.ReadSingleOrDefault();
            Assert.NotNull(row);
            Assert.Equal(7, (int)row!.Id);
        }

        // ── ReadFirstAsync on empty throws (lines 221-223) ────────────

        [Fact]
        public async Task GridReader_ReadFirstAsync_EmptySet_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            await Assert.ThrowsAsync<InvalidOperationException>(() => multi.ReadFirstAsync<Item>());
        }

        [Fact]
        public async Task GridReader_ReadSingleAsync_EmptySet_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            await Assert.ThrowsAsync<InvalidOperationException>(() => multi.ReadSingleAsync<Item>());
        }

        // ── Sync ReadFirst on empty throws ───────────────────────────

        [Fact]
        public void GridReader_ReadFirst_EmptySet_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<InvalidOperationException>(() => multi.ReadFirst<Item>());
        }

        [Fact]
        public void GridReader_ReadSingle_EmptySet_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var multi = conn.QueryMultiple("SELECT Id, Name FROM T");
            Assert.Throws<InvalidOperationException>(() => multi.ReadSingle<Item>());
        }

        // ── CommandDefinition with Transaction (line 127) ─────────────

        [Fact]
        public void CommandDefinition_WithTransaction_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            using var tx = conn.BeginTransaction();

            conn.EnqueueNonQueryResult(1);
            var cmd = new CommandDefinition("UPDATE T SET Val = 1", transaction: tx);
            var rowsAffected = conn.Execute(cmd);

            Assert.Equal(1, rowsAffected);
        }

        // ── CommandDefinition.CommandType via reflection (line 52) ────
        // The property is [Obsolete(error)] in DEBUG, so access via reflection

        [Fact]
        public void CommandDefinition_CommandType_Accessible_ViaReflection()
        {
            var cmd = new CommandDefinition("SELECT 1", commandType: CommandType.Text);
            var prop = typeof(CommandDefinition).GetProperty("CommandType");
            if (prop is not null)
            {
                var val = prop.GetValue(cmd);
                Assert.Equal(CommandType.Text, val);
            }
        }

        // ── GridReader ConvertTo<T> type conversion path (lines 489-490) ─

        [Fact]
        public void GridReader_Read_TypeConversion_Works()
        {
            // Returning a long from DB but reading as int triggers Convert.ChangeType
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1L } } // long value
            });
            conn.Open();

            // QueryFirst<int> where DB returns long -> Convert.ChangeType path
            using var multi = conn.QueryMultiple("SELECT 1");
            var results = multi.Read<int>().ToList();
            Assert.Single(results);
            Assert.Equal(1, results[0]);
        }
    }
}
#endif
