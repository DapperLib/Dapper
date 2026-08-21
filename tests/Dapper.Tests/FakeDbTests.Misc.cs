#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbMiscTests
    {
        // ── PurgeQueryCache ───────────────────────────────────────────

        [Fact]
        public void PurgeQueryCache_DoesNotThrow()
        {
            SqlMapper.PurgeQueryCache();
        }

        // ── CommandDefinition flags ───────────────────────────────────

        [Fact]
        public void CommandDefinition_BufferedFalse_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...", flags: CommandFlags.None);
            var result = conn.Query<User>(cmd).ToList();
            Assert.Single(result);
        }

        [Fact]
        public void CommandDefinition_WithCommandTimeout_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var cmd = new CommandDefinition("DELETE FROM T", commandTimeout: 30);
            conn.Execute(cmd);
        }

        [Fact]
        public void CommandDefinition_Pipelined_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var cmd = new CommandDefinition("DELETE FROM T", flags: CommandFlags.Pipelined);
            conn.Execute(cmd);
        }

        [Fact]
        public void CommandDefinition_NoCache_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } } });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...", flags: CommandFlags.NoCache);
            var result = conn.Query<User>(cmd).ToList();
            Assert.Single(result);
        }

        // ── Query<T> with CommandType.StoredProcedure ─────────────────

        [Fact]
        public void Execute_StoredProcedure_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            conn.Execute("MyProc", new { id = 1 }, commandType: CommandType.StoredProcedure);
        }

        [Fact]
        public void Query_StoredProcedure_ReturnsRows()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var result = conn.Query<User>("GetUsers",
                commandType: CommandType.StoredProcedure).ToList();

            Assert.Single(result);
        }

        // ── Query returns IEnumerable<object> typed ──────────────────

        [Fact]
        public void Query_WithType_ReturnsObjects()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var result = conn.Query(typeof(User), "SELECT Id, Name FROM Users").ToList();

            Assert.Single(result);
            Assert.IsType<User>(result[0]);
        }

        // ── async versions of misc tests ──────────────────────────────

        [Fact]
        public async Task ExecuteAsync_StoredProcedure_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            await conn.ExecuteAsync("MyProc", new { id = 1 }, commandType: CommandType.StoredProcedure);
        }

        [Fact]
        public async Task QueryAsync_WithType_ReturnsObjects()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } }
            });
            conn.Open();

            var result = (await conn.QueryAsync(typeof(User), "SELECT Id, Name FROM Users")).ToList();

            Assert.Single(result);
            Assert.IsType<User>(result[0]);
        }

        [Fact]
        public async Task QueryAsync_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } } });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...", flags: CommandFlags.NoCache);
            var result = (await conn.QueryAsync<User>(cmd)).ToList();
            Assert.Single(result);
        }

        [Fact]
        public async Task QueryFirstAsync_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 3 }, { "Name", "C" } } });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var result = await conn.QueryFirstAsync<User>(cmd);
            Assert.Equal(3, result.Id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var result = await conn.QueryFirstOrDefaultAsync<User>(cmd);
            Assert.Null(result);
        }

        [Fact]
        public async Task QuerySingleAsync_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 4 }, { "Name", "D" } } });
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var result = await conn.QuerySingleAsync<User>(cmd);
            Assert.Equal(4, result.Id);
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            var cmd = new CommandDefinition("SELECT ...");
            var result = await conn.QuerySingleOrDefaultAsync<User>(cmd);
            Assert.Null(result);
        }

        [Fact]
        public async Task ExecuteScalarAsync_ViaCommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(42L);
            conn.Open();

            var cmd = new CommandDefinition("SELECT COUNT(*) FROM T");
            Assert.Equal(42L, await conn.ExecuteScalarAsync<long>(cmd));
        }

        // ── Multiple executions on same connection ────────────────────

        [Fact]
        public void MultipleQueries_OnSameConnection_Work()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 }, { "Name", "A" } } });
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 2 }, { "Name", "B" } } });
            conn.Open();

            var first = conn.QueryFirst<User>("SELECT ...");
            var second = conn.QueryFirst<User>("SELECT ...");

            Assert.Equal(1, first.Id);
            Assert.Equal(2, second.Id);
        }

        // ── Cancellation ──────────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_WithCancelledToken_RespectedOnOpen()
        {
            // Cancellation checked when Dapper tries to open a closed connection
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 } } });
            // conn is NOT opened — Dapper will try to open it

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = new CommandDefinition("SELECT ...", cancellationToken: cts.Token);
            await Assert.ThrowsAnyAsync<Exception>(() => conn.QueryAsync<User>(cmd));
        }

        [Fact]
        public async Task ExecuteAsync_WithCancelledToken_RespectedOnOpen()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            // conn is NOT opened — Dapper will try to open it

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = new CommandDefinition("DELETE FROM T", cancellationToken: cts.Token);
            await Assert.ThrowsAnyAsync<Exception>(() => conn.ExecuteAsync(cmd));
        }
    }
}
#endif
