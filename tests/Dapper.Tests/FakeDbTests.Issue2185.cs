#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests;

public sealed class FakeDbIssue2185Tests
{
    [Fact]
    public async Task ExecuteReaderAsyncDisposeAsyncDisposesCommand()
    {
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[] { new Dictionary<string, object?> { ["Value"] = 1 } });
        connection.Open();

        await using (var reader = await connection.ExecuteReaderAsync("select 1"))
        {
            Assert.True(await reader.ReadAsync());
        }

        Assert.NotNull(connection.LastCreatedCommand);
        Assert.True(connection.LastCreatedCommand!.WasDisposed);
    }

    [Fact]
    public async Task DisposeAsyncUsesAsyncDisposalOfDbCommand()
    {
        using var connection = new fakeDbConnection(new FakeDataStore());
        connection.EnqueueReaderResult(new[] { new Dictionary<string, object?> { ["Value"] = 1 } });
        connection.Open();
        var cmd = new RecordingCommand(connection.CreateCommand());
        var inner = await cmd.InnerCommand.ExecuteReaderAsync();

        await using (new DbWrappedReader(cmd, inner)) { }

        Assert.Equal(new[] { "DisposeAsync" }, cmd.Calls);
    }

    [Fact]
    public async Task DisposeAsyncStillDisposesCommandWhenReaderDisposalThrows()
    {
        var cmd = new RecordingCommand(new fakeDbConnection(new FakeDataStore()).CreateCommand());
        var wrapped = new DbWrappedReader(cmd, new ThrowingReader());

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await wrapped.DisposeAsync());

        Assert.Equal(new[] { "DisposeAsync" }, cmd.Calls);
    }

    private sealed class ThrowingReader : DbDataReader
    {
        public override System.Threading.Tasks.ValueTask DisposeAsync() => throw new InvalidOperationException();
        public override int Depth => 0;
        public override int FieldCount => 0;
        public override bool HasRows => false;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override object this[int ordinal] => throw new NotSupportedException();
        public override object this[string name] => throw new NotSupportedException();
        public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
        public override byte GetByte(int ordinal) => throw new NotSupportedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override char GetChar(int ordinal) => throw new NotSupportedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override string GetDataTypeName(int ordinal) => throw new NotSupportedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
        public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
        public override double GetDouble(int ordinal) => throw new NotSupportedException();
        public override System.Collections.IEnumerator GetEnumerator() => throw new NotSupportedException();
        public override Type GetFieldType(int ordinal) => throw new NotSupportedException();
        public override float GetFloat(int ordinal) => throw new NotSupportedException();
        public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
        public override short GetInt16(int ordinal) => throw new NotSupportedException();
        public override int GetInt32(int ordinal) => throw new NotSupportedException();
        public override long GetInt64(int ordinal) => throw new NotSupportedException();
        public override string GetName(int ordinal) => throw new NotSupportedException();
        public override int GetOrdinal(string name) => throw new NotSupportedException();
        public override string GetString(int ordinal) => throw new NotSupportedException();
        public override object GetValue(int ordinal) => throw new NotSupportedException();
        public override int GetValues(object[] values) => throw new NotSupportedException();
        public override bool IsDBNull(int ordinal) => throw new NotSupportedException();
        public override bool NextResult() => false;
        public override bool Read() => false;
    }

    // Delegating command that records which disposal path was taken.
    private sealed class RecordingCommand : DbCommand
    {
        public RecordingCommand(DbCommand inner) => InnerCommand = inner;
        public DbCommand InnerCommand { get; }
        public List<string> Calls { get; } = new();

        public override System.Threading.Tasks.ValueTask DisposeAsync()
        {
            Calls.Add("DisposeAsync");
            return InnerCommand.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Calls.Add("Dispose");
            InnerCommand.Dispose();
            base.Dispose(disposing);
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get => InnerCommand.CommandText; set => InnerCommand.CommandText = value; }
        public override int CommandTimeout { get => InnerCommand.CommandTimeout; set => InnerCommand.CommandTimeout = value; }
        public override CommandType CommandType { get => InnerCommand.CommandType; set => InnerCommand.CommandType = value; }
        public override bool DesignTimeVisible { get => InnerCommand.DesignTimeVisible; set => InnerCommand.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => InnerCommand.UpdatedRowSource; set => InnerCommand.UpdatedRowSource = value; }
        protected override DbConnection? DbConnection { get => InnerCommand.Connection; set => InnerCommand.Connection = value; }
        protected override DbParameterCollection DbParameterCollection => InnerCommand.Parameters;
        protected override DbTransaction? DbTransaction { get => InnerCommand.Transaction; set => InnerCommand.Transaction = value; }
        public override void Cancel() => InnerCommand.Cancel();
        public override int ExecuteNonQuery() => InnerCommand.ExecuteNonQuery();
        public override object? ExecuteScalar() => InnerCommand.ExecuteScalar();
        public override void Prepare() => InnerCommand.Prepare();
        protected override DbParameter CreateDbParameter() => InnerCommand.CreateParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => InnerCommand.ExecuteReader(behavior);
    }
}
#endif
