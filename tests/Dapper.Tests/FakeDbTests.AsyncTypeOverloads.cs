#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for async overloads that take a runtime Type parameter,
    /// XElement parameter handling, DataTableHandler.Parse exception,
    /// TableValuedParameter.AddParameter, and more SqlMapper coverage.
    /// </summary>
    public class FakeDbAsyncTypeOverloadTests
    {
        private class User { public int Id { get; set; } public string? Name { get; set; } }

        // ── QueryFirstAsync(Type) ─────────────────────────────────────

        [Fact]
        public async Task QueryFirstAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var row = await conn.QueryFirstAsync(typeof(User), "SELECT Id, Name FROM T");
            Assert.Equal(1, ((User)row).Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } }
            });
            conn.Open();

            var row = await conn.QueryFirstOrDefaultAsync(typeof(User), "SELECT Id, Name FROM T");
            Assert.NotNull(row);
            Assert.Equal(2, ((User)row!).Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_ByType_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var row = await conn.QueryFirstOrDefaultAsync(typeof(User), "SELECT Id, Name FROM T");
            Assert.Null(row);
        }

        [Fact]
        public async Task QuerySingleAsync_ByType_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Carol" } }
            });
            conn.Open();

            var row = await conn.QuerySingleAsync(typeof(User), "SELECT Id, Name FROM T");
            Assert.Equal(3, ((User)row).Id);
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_ByType_EmptySet_ReturnsNull()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var row = await conn.QuerySingleOrDefaultAsync(typeof(User), "SELECT Id, Name FROM T");
            Assert.Null(row);
        }

        // ── XElement as parameter (covers XElementHandler.SetValue + Format) ──

        [Fact]
        public void Execute_WithXElementParam_Works()
        {
            var element = new XElement("root", new XElement("child", "value"));

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            // Passing XElement triggers XElementHandler.SetValue and Format
            conn.Execute("UPDATE T SET Xml = @xml WHERE Id = 1", new { xml = element });
        }

        [Fact]
        public void Query_WithXElementParam_Works()
        {
            var element = new XElement("filter", "value");

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            var result = conn.Query<int>("SELECT Id FROM T WHERE Xml = @xml", new { xml = element })
                .ToList();
            Assert.Single(result);
        }

        // ── DataTableHandler.Parse throws NotImplementedException ─────

        [Fact]
        public void DataTableHandler_Parse_Throws_NotImplementedException()
        {
            // DataTableHandler.Parse is not implemented and throws NotImplementedException
            // To trigger it: pass DataTable as target type in Query<DataTable>
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });
            conn.Open();

            // This should trigger DataTableHandler.Parse which throws NotImplementedException
            Assert.Throws<NotImplementedException>(() =>
                conn.Query<DataTable>("SELECT Id FROM T").ToList());
        }

        // ── TableValuedParameter.AddParameter ─────────────────────────

        [Fact]
        public void TableValuedParameter_AddParameter_Works()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);
            dt.Rows.Add(2);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            // AsTableValuedParameter() returns ICustomQueryParameter
            // When passed to Dapper, it calls AddParameter
            var tvp = dt.AsTableValuedParameter();
            var dp = new DynamicParameters();
            dp.Add("ids", tvp);
            conn.Execute("EXEC BulkInsert @ids", dp);
        }

        // ── CommandDefinition.InferCommandType — StoredProcedure path ─

        [Fact]
        public void CommandDefinition_InferCommandType_StoredProc()
        {
            // A name with no whitespace/special chars is inferred as StoredProcedure
            var cmd = new CommandDefinition("sp_GetUser"); // no whitespace -> StoredProc
            Assert.Equal(CommandType.StoredProcedure, cmd.CommandTypeDirect);
        }

        [Fact]
        public void CommandDefinition_InferCommandType_Text()
        {
            var cmd = new CommandDefinition("SELECT 1"); // has whitespace -> Text
            Assert.Equal(CommandType.Text, cmd.CommandTypeDirect);
        }

        // ── CollectCacheGarbage path ───────────────────────────────────
        // Triggered after COLLECT_PER_ITEMS (1000) distinct queries

        // ── DefaultTypeMap additional coverage ────────────────────────

        private class TypeWithField { public int Id; }

        [Fact]
        public void DefaultTypeMap_GetSettableFields_ReturnsFields()
        {
            var fields = DefaultTypeMap.GetSettableFields(typeof(TypeWithField));
            Assert.NotEmpty(fields);
        }

        [Fact]
        public void DefaultTypeMap_GetSettableProps_ReturnsProps()
        {
            var props = DefaultTypeMap.GetSettableProps(typeof(User));
            Assert.Equal(2, props.Count);
        }

        // ── Additional SqlMapper coverage: GetCachedSQLCount ──────────

        [Fact]
        public void GetCachedSQLCount_ReturnsNonNegative()
        {
            var count = SqlMapper.GetCachedSQLCount();
            Assert.True(count >= 0);
        }

        // ── ExecuteScalarAsync(CommandDefinition) with timeout ─────────

        [Fact]
        public async Task ExecuteScalarAsync_WithCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Result", 42 } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT 42", commandTimeout: 5);
            var result = await conn.ExecuteScalarAsync<int>(cmd);
            Assert.Equal(42, result);
        }

        // ── Query with CommandTimeout from Settings ────────────────────

        [Fact]
        public void Query_WithSettingsCommandTimeout_Works()
        {
            var original = SqlMapper.Settings.CommandTimeout;
            try
            {
                SqlMapper.Settings.CommandTimeout = 30;

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
                });
                conn.Open();

                // This exercises the Settings.CommandTimeout fallback in SetupCommand
                var cmd = new CommandDefinition("SELECT Id, Name FROM T"); // no explicit timeout
                var results = conn.Query<User>(cmd).ToList();
                Assert.Single(results);
            }
            finally
            {
                SqlMapper.Settings.CommandTimeout = original;
            }
        }
    }
}
#endif
