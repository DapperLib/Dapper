using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Dapper.Tests;

public class QueryRowTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void QuerySingle_ErrorPathDoesNotExplicitlyCancelCommand(int rowCount)
    {
        using var connection = new TrackingConnection();
        string sql = rowCount == 0 ? "select 1 where 0 = 1" : "select 1 union all select 2";

        Assert.Throws<InvalidOperationException>(() => connection.QuerySingle<int>(sql));

        var command = Assert.IsType<TrackingCommand>(connection.LastCommand);
        Assert.False(command.CancelCalled);
        Assert.True(command.IsDisposed);
        Assert.True(command.LastReader?.IsClosed);
    }

    private sealed class TrackingConnection : IDbConnection
    {
        private readonly SqliteConnection _inner = new("Data Source=:memory:");

        public TrackingConnection() => _inner.Open();

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
        public string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value; }
#pragma warning restore CS8767

        public int ConnectionTimeout => _inner.ConnectionTimeout;
        public string Database => _inner.Database;
        public ConnectionState State => _inner.State;
        public TrackingCommand? LastCommand { get; private set; }

        public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public void Close() => _inner.Close();
        public IDbCommand CreateCommand() => LastCommand = new TrackingCommand(_inner.CreateCommand());
        public void Dispose() => _inner.Dispose();
        public void Open() => _inner.Open();
    }

    private sealed class TrackingCommand(IDbCommand inner) : IDbCommand
    {
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
        public string CommandText { get => inner.CommandText; set => inner.CommandText = value; }
#pragma warning restore CS8767

        public int CommandTimeout { get => inner.CommandTimeout; set => inner.CommandTimeout = value; }
        public CommandType CommandType { get => inner.CommandType; set => inner.CommandType = value; }
        public IDbConnection? Connection { get => inner.Connection; set => inner.Connection = value; }
        public IDataParameterCollection Parameters => inner.Parameters;
        public IDbTransaction? Transaction { get => inner.Transaction; set => inner.Transaction = value; }
        public UpdateRowSource UpdatedRowSource { get => inner.UpdatedRowSource; set => inner.UpdatedRowSource = value; }
        public bool CancelCalled { get; private set; }
        public bool IsDisposed { get; private set; }
        public IDataReader? LastReader { get; private set; }

        public void Cancel() => CancelCalled = true;
        public IDbDataParameter CreateParameter() => inner.CreateParameter();

        public void Dispose()
        {
            IsDisposed = true;
            inner.Dispose();
        }

        public int ExecuteNonQuery() => inner.ExecuteNonQuery();
        public IDataReader ExecuteReader() => ExecuteReader(CommandBehavior.Default);
        public IDataReader ExecuteReader(CommandBehavior behavior) => LastReader = inner.ExecuteReader(behavior);
        public object? ExecuteScalar() => inner.ExecuteScalar();
        public void Prepare() => inner.Prepare();
    }
}
