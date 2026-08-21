#if !NET481
using System;
using System.Data;
using System.Threading;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for CommandDefinition properties and constructor overloads.
    /// </summary>
    public class FakeDbCommandDefinitionTests
    {
        private class User { public int Id { get; set; } public string? Name { get; set; } }

        [Fact]
        public void CommandDefinition_Properties_AreReadable()
        {
            using var cts = new CancellationTokenSource();
            var cmd = new CommandDefinition(
                "SELECT 1",
                parameters: new { id = 1 },
                commandTimeout: 30,
                commandType: CommandType.Text,
                flags: CommandFlags.Buffered,
                cancellationToken: cts.Token);

            Assert.Equal("SELECT 1", cmd.CommandText);
            Assert.NotNull(cmd.Parameters);
            Assert.Equal(30, cmd.CommandTimeout);
            Assert.Equal(CommandType.Text, cmd.CommandTypeDirect);
            Assert.True(cmd.Buffered);
            Assert.False(cmd.Pipelined);
            Assert.Equal(CommandFlags.Buffered, cmd.Flags);
            Assert.Equal(cts.Token, cmd.CancellationToken);
        }

        [Fact]
        public void CommandDefinition_StoredProcedure_Type()
        {
            var cmd = new CommandDefinition("usp_GetUser",
                commandType: CommandType.StoredProcedure);

            Assert.Equal(CommandType.StoredProcedure, cmd.CommandTypeDirect);
        }

        [Fact]
        public void CommandDefinition_Pipelined_Flag()
        {
            var cmd = new CommandDefinition("SELECT 1", flags: CommandFlags.Pipelined);

            Assert.True(cmd.Pipelined);
            Assert.False(cmd.Buffered);
        }

        [Fact]
        public void CommandDefinition_NoCache_Flag()
        {
            var cmd = new CommandDefinition("SELECT 1", flags: CommandFlags.NoCache);

            Assert.Equal(CommandFlags.NoCache, cmd.Flags);
            Assert.False(cmd.Buffered);
        }

        [Fact]
        public void CommandDefinition_Transaction_IsPreserved()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();
            using var tx = conn.BeginTransaction();

            var cmd = new CommandDefinition("SELECT 1", transaction: tx);
            Assert.Equal(tx, cmd.Transaction);
        }

        [Fact]
        public void CommandDefinition_Execute_WithTimeout_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            var cmd = new CommandDefinition("DELETE FROM T WHERE Id = @id",
                new { id = 1 },
                commandTimeout: 10,
                flags: CommandFlags.Buffered);

            var result = conn.Execute(cmd);
            Assert.Equal(1, result);
        }

        [Fact]
        public void CommandDefinition_NoCache_SkipsCaching()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new System.Collections.Generic.Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } }
            });
            conn.Open();

            var cmd = new CommandDefinition("SELECT Id, Name FROM T",
                flags: CommandFlags.NoCache | CommandFlags.Buffered);

            var results = System.Linq.Enumerable.ToList(conn.Query<User>(cmd));
            Assert.Single(results);
        }
    }
}
#endif
