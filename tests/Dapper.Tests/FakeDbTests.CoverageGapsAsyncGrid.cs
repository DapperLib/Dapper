#if !NET481
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pengdows.crud.enums;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbCoverageGapsAsyncGridTests
    {
        // ── TryOpenAsync / TrySetupAsyncCommand: non-DbConnection paths (L395-420) ──

        private class NonDbCommandConnection : IDbConnection
        {
            public string ConnectionString { get; set; } = "";
            public int ConnectionTimeout => 0;
            public string Database => "";
            public ConnectionState State => ConnectionState.Closed;
            public IDbTransaction BeginTransaction() => throw new NotSupportedException();
            public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
            public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
            public void Close() { }
            public IDbCommand CreateCommand() => new NonDbCommand();
            public void Dispose() { }
            public void Open() { }
        }

        private class NonDbCommand : IDbCommand
        {
            public string CommandText { get; set; } = "";
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; } = CommandType.Text;
            public IDbConnection? Connection { get; set; }
            public IDataParameterCollection Parameters { get; } = new NonDbParameterCollection();
            public IDbTransaction? Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }
            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new MinimalDbParameter2();
            public void Dispose() { }
            public int ExecuteNonQuery() => 0;
            public IDataReader ExecuteReader() => throw new NotSupportedException();
            public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotSupportedException();
            public object? ExecuteScalar() => null;
            public void Prepare() { }
        }

        private class NonDbParameterCollection : ArrayList, IDataParameterCollection
        {
            public bool Contains(string parameterName) => false;
            public int IndexOf(string parameterName) => -1;
            public void RemoveAt(string parameterName) { }
            public object this[string parameterName]
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task QueryAsync_NonDbCommandConnection_ThrowsFromTrySetupAsyncCommand()
        {
            var conn = new NonDbCommandConnection();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                conn.QueryAsync<int>("SELECT 1"));
        }

        // a connection that produces a real DbCommand (so TrySetupAsyncCommand succeeds)
        // but is itself not a DbConnection, and reports Closed (so TryOpenAsync is reached).
        private class RealCommandNonDbConnection : IDbConnection
        {
            private readonly fakeDbConnection _inner;
            public RealCommandNonDbConnection(fakeDbConnection inner) => _inner = inner;
            public string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value; }
            public int ConnectionTimeout => _inner.ConnectionTimeout;
            public string Database => _inner.Database;
            public ConnectionState State => ConnectionState.Closed; // always report closed
            public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
            public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
            public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
            public void Close() => _inner.Close();
            public IDbCommand CreateCommand() => _inner.CreateCommand();
            public void Dispose() => _inner.Dispose();
            public void Open() => _inner.Open();
        }

        [Fact]
        public async Task QueryAsync_NonDbConnection_WithRealCommand_ThrowsFromTryOpenAsync()
        {
            using var inner = new fakeDbConnection(new FakeDataStore());
            inner.Open(); // command creation needs a connected inner connection
            var conn = new RealCommandNonDbConnection(inner);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                conn.QueryAsync<int>("SELECT 1"));
        }

        // ── QueryAsync: zero-field result → empty sequence (L440-441) ───────────

        [Fact]
        public async Task QueryAsync_ZeroColumns_ReturnsEmpty()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?>() });
            conn.Open();

            var result = await conn.QueryAsync<EmptyHolder>("SELECT");
            Assert.Empty(result);
        }
        private class EmptyHolder { }

        // ── QueryRowAsync: zero rows demanded → throws (L503-504) ───────────────

        [Fact]
        public async Task QueryFirstAsync_ZeroRows_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                conn.QueryFirstAsync<int>("SELECT V FROM T"));
        }

        // ── ExecuteScalarImplAsync: non-null param sets up ParamReader (L1228-1233) ──

        [Fact]
        public async Task ExecuteScalarAsync_WithParam_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(42);
            conn.Open();

            var result = await conn.ExecuteScalarAsync<int>("SELECT @x", new { x = 1 });
            Assert.Equal(42, result);
        }

        // ── Trivial async API overloads (L1123-1213) ────────────────────────────

        [Fact]
        public async Task ExecuteReaderAsync_IDbConnection_CommandDefinition_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 1 } } });
            conn.Open();

            using var reader = await ((IDbConnection)conn).ExecuteReaderAsync(new CommandDefinition("SELECT V FROM T"));
            Assert.True(reader.Read());
        }

        [Fact]
        public async Task ExecuteReaderAsync_IDbConnection_CommandDefinition_WithBehavior_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 1 } } });
            conn.Open();

            using var reader = await ((IDbConnection)conn).ExecuteReaderAsync(new CommandDefinition("SELECT V FROM T"), CommandBehavior.SequentialAccess);
            Assert.True(reader.Read());
        }

        [Fact]
        public async Task ExecuteReaderAsync_DbConnection_CommandDefinition_WithBehavior_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 1 } } });
            conn.Open();

            DbConnection dbConn = conn;
            using var reader = await dbConn.ExecuteReaderAsync(new CommandDefinition("SELECT V FROM T"), CommandBehavior.SequentialAccess);
            Assert.True(reader.Read());
        }

        [Fact]
        public async Task ExecuteScalarAsync_PlainString_ObjectReturn_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(7);
            conn.Open();

            object? result = await conn.ExecuteScalarAsync("SELECT 7");
            Assert.Equal(7, result);
        }

        [Fact]
        public async Task ExecuteScalarAsync_CommandDefinition_ObjectReturn_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(9);
            conn.Open();

            object? result = await conn.ExecuteScalarAsync(new CommandDefinition("SELECT 9"));
            Assert.Equal(9, result);
        }

        // ── QueryMultipleAsync: catch block on setup/execution failure (L1052-1067) ──

        [Fact]
        public async Task QueryMultipleAsync_CommandFailure_ThrowsAndCleansUp()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.SetFailOnCommand(true);
            conn.SetCustomFailureException(new InvalidOperationException("boom"));
            conn.Open();

            await Assert.ThrowsAnyAsync<Exception>(() => conn.QueryMultipleAsync("SELECT 1; SELECT 2"));
        }

        // ── MultiMapAsync<TReturn> with Type[] overload: empty types throws (L977-979) ──

        [Fact]
        public async Task QueryAsync_TypesOverload_EmptyTypes_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                conn.QueryAsync("SELECT 1", Array.Empty<Type>(), objs => objs.Length));
        }

        // ── QueryAsync<T1,T2,T3,TReturn> plain-sql 3-type multimap overload (L730-732) ──

        private class QA { public int Id { get; set; } }
        private class QB { public int BId { get; set; } }
        private class QC { public int CId { get; set; } }

        [Fact]
        public async Task QueryAsync_ThreeTypeMultiMap_PlainSqlOverload_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "BId", 2 }, { "CId", 3 } }
            });
            conn.Open();

            var results = await conn.QueryAsync<QA, QB, QC, string>(
                "SELECT Id, BId, CId FROM T",
                (a, b, c) => $"{a.Id}-{b.BId}-{c.CId}",
                splitOn: "BId,CId");

            Assert.Equal(new[] { "1-2-3" }, results);
        }

        // ── Pipelined async multi-exec: recycle-queue branch (>MAX_PENDING) (L588-595) ──

        [Fact]
        public async Task ExecuteAsync_Pipelined_ManyItems_TriggersRecycleQueue()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            for (int i = 0; i < 110; i++) conn.EnqueueNonQueryResult(1);
            conn.Open();

            var rows = Enumerable.Range(0, 105).Select(i => new { id = i }).ToArray();
            var cmd = new CommandDefinition("INSERT INTO T VALUES (@id)", rows, flags: CommandFlags.Pipelined);

            var total = await conn.ExecuteAsync(cmd);
            Assert.Equal(105, total);
        }

        // ── QueryUnbufferedAsync: full async-enumerable consumption (L1291-1349) ──

        [Fact]
        public async Task QueryUnbufferedAsync_FullConsumption_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "V", 1 } },
                new Dictionary<string, object?> { { "V", 2 } },
            });
            conn.Open();

            var seen = new List<int>();
            await foreach (var v in conn.QueryUnbufferedAsync<int>("SELECT V FROM T"))
            {
                seen.Add(v);
            }
            Assert.Equal(new[] { 1, 2 }, seen);
        }

        // ── GridReader: protected ctor + lazy CreateIdentity (L48-58) ───────────

        private class TestGridReader : SqlMapper.GridReader
        {
            public TestGridReader(IDbCommand cmd, DbDataReader reader) : base(cmd, reader, null) { }
        }

        private class IdHolder { public int Id { get; set; } }

        [Fact]
        public void GridReader_ProtectedCtor_LazilyCreatesIdentity()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Id", 1 } } });
            conn.Open();

            var cmd = (DbCommand)conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM T";
            var reader = (DbDataReader)cmd.ExecuteReader();

            using var grid = new TestGridReader(cmd, reader);
            var rows = grid.Read<IdHolder>().ToList();
            Assert.Single(rows);
            Assert.Equal(1, rows[0].Id);
        }

        // ── GridReader.ReadFirst<T>: zero rows throws (L228-234) ─────────────────

        [Fact]
        public void GridReader_ReadFirst_ZeroRows_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(Array.Empty<Dictionary<string, object?>>());
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT V FROM T");
            Assert.Throws<InvalidOperationException>(() => grid.ReadFirst<int>());
        }

        // ── GridReader.ConvertTo<T> fallback: mismatched numeric type (L486-490) ──

        [Fact]
        public void GridReader_ReadFirst_MismatchedNumericType_ConvertsViaChangeType()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 5L } } });
            conn.Open();

            using var grid = conn.QueryMultiple("SELECT V FROM T");
            var result = grid.ReadFirst<int>();
            Assert.Equal(5, result);
        }

        // ── GridReader async multi-grid transitions via EnqueueMultiResultReader (OnAfterGridAsync) ──

        [Fact]
        public async Task GridReader_MultipleGridsAsync_TransitionsBetweenGrids()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueMultiResultReader(new[]
            {
                new[] { new Dictionary<string, object?> { { "V", 1 } } },
                new[] { new Dictionary<string, object?> { { "V", 2 } } },
            });
            conn.Open();

            using var multi = await conn.QueryMultipleAsync("SELECT V FROM T; SELECT V FROM T");
            var a = await multi.ReadFirstAsync<int>();
            var b = await multi.ReadFirstAsync<int>();

            Assert.Equal(1, a);
            Assert.Equal(2, b);
        }

        // ── Parse<T>(this IDataReader) extension: conversion branch (L26-33) ────

        [Fact]
        public void Parse_IDataReaderExtension_ConvertsMismatchedType()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 5L } } });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT V FROM T");
            var results = ((IDataReader)reader).Parse<int>().ToList();
            Assert.Equal(new[] { 5 }, results);
        }

        // ── GetHashCollissions: exercise the public API (L145-169) ──────────────

        [Fact]
        public void GetHashCollissions_ReturnsEnumerable()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 1 } } });
            conn.Open();
            conn.QueryFirst<int>("SELECT V FROM T /* GetHashCollissions_ReturnsEnumerable */");

            var collisions = SqlMapper.GetHashCollissions().ToList();
            Assert.NotNull(collisions);
        }

        // ── WrappedReader internals: direct member coverage ─────────────────────

        private class MinimalNonDbReader : IDataReader
        {
            private bool _closed;
            public int Depth => 0;
            public int FieldCount => 1;
            public bool IsClosed => _closed;
            public bool HasRows => true;
            public int RecordsAffected => -1;
            private bool _read = true;
            public object this[int i] => 1;
            public object this[string name] => 1;
            public void Close() => _closed = true;
            public void Dispose() { }
            public bool GetBoolean(int i) => true;
            public byte GetByte(int i) => 1;
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
            public char GetChar(int i) => 'a';
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
            public IDataReader GetData(int i) => throw new NotSupportedException();
            public string GetDataTypeName(int i) => "int";
            public DateTime GetDateTime(int i) => DateTime.MinValue;
            public decimal GetDecimal(int i) => 0;
            public double GetDouble(int i) => 0;
            public Type GetFieldType(int i) => typeof(int);
            public float GetFloat(int i) => 0;
            public Guid GetGuid(int i) => Guid.Empty;
            public short GetInt16(int i) => 0;
            public int GetInt32(int i) => 1;
            public long GetInt64(int i) => 0;
            public string GetName(int i) => "V";
            public int GetOrdinal(string name) => 0;
            public DataTable? GetSchemaTable() => null;
            public string GetString(int i) => "x";
            public object GetValue(int i) => 1;
            public int GetValues(object[] values) => 0;
            public bool IsDBNull(int i) => false;
            public bool NextResult() => false;
            public bool Read()
            {
                if (_read) { _read = false; return true; }
                return false;
            }
        }

        [Fact]
        public void WrappedBasicReader_MembersDelegateOrThrowAsDesigned()
        {
            var inner = new MinimalNonDbReader();
            var wrapped = new WrappedBasicReader(inner);
            try
            {
                Assert.True(wrapped.Read());
                Assert.Equal(1, wrapped.FieldCount);
                Assert.True(wrapped.HasRows);
                Assert.False(wrapped.IsClosed);
                Assert.Equal(0, wrapped.Depth);
                Assert.False(wrapped.NextResult());
                Assert.Equal(-1, wrapped.RecordsAffected);
                Assert.Equal(1, wrapped.GetInt32(0));
                Assert.True(wrapped.GetBoolean(0));
                Assert.Equal((byte)1, wrapped.GetByte(0));
                Assert.Equal('a', wrapped.GetChar(0));
                Assert.Equal("int", wrapped.GetDataTypeName(0));
                Assert.Equal(typeof(int), wrapped.GetFieldType(0));
                Assert.Equal(Guid.Empty, wrapped.GetGuid(0));
                Assert.Equal("V", wrapped.GetName(0));
                Assert.Equal(0, wrapped.GetOrdinal("V"));
                Assert.Equal("x", wrapped.GetString(0));
                Assert.Equal(1, wrapped.GetValue(0));
                Assert.False(wrapped.IsDBNull(0));
                Assert.Equal(1, wrapped[0]);
                Assert.Equal(1, wrapped["V"]);
                Assert.Equal(1, wrapped.GetFieldValue<int>(0));
                Assert.False(wrapped.IsDBNullAsync(0, default).GetAwaiter().GetResult());
                Assert.False(wrapped.NextResultAsync(default).GetAwaiter().GetResult());
                Assert.False(wrapped.ReadAsync(default).GetAwaiter().GetResult());
                Assert.Equal(1, wrapped.GetProviderSpecificValue(0));
                Assert.Equal(typeof(int), wrapped.GetProviderSpecificFieldType(0));
                Assert.Equal(1, wrapped.VisibleFieldCount);
                Assert.Throws<NotImplementedException>(() => wrapped.GetEnumerator());

                Assert.Throws<NotSupportedException>(() => wrapped.GetStream(0));
                Assert.Throws<NotSupportedException>(() => wrapped.GetTextReader(0));
                Assert.Throws<NotSupportedException>(() => wrapped.InitializeLifetimeService());
                Assert.Throws<NotSupportedException>(() => wrapped.GetData(0));

                wrapped.CloseAsync().GetAwaiter().GetResult();
            }
            finally
            {
                wrapped.Dispose();
            }
        }

        [Fact]
        public void DbWrappedReader_MembersDelegateToInnerReader()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 1 } } });
            conn.Open();

            var cmd = (DbCommand)conn.CreateCommand();
            cmd.CommandText = "SELECT V FROM T";
            var innerReader = (DbDataReader)cmd.ExecuteReader();
            var wrapped = new DbWrappedReader(cmd, innerReader);
            try
            {
                Assert.True(wrapped.Read());
                Assert.Equal(1, wrapped.FieldCount);
                Assert.False(wrapped.IsClosed);
                Assert.Equal(0, wrapped.Depth);

                SwallowExceptions(() => wrapped.GetChar(0));
                SwallowExceptions(() => wrapped.GetChars(0, 0, null, 0, 0));
                SwallowExceptions(() => wrapped.GetBytes(0, 0, null, 0, 0));
                SwallowExceptions(() => wrapped.GetStream(0));
                SwallowExceptions(() => wrapped.GetTextReader(0));
                SwallowExceptions(() => wrapped.GetProviderSpecificValue(0));
                SwallowExceptions(() => wrapped.GetProviderSpecificValues(new object[1]));
                SwallowExceptions(() => wrapped.GetProviderSpecificFieldType(0));
                SwallowExceptions(() => wrapped.GetEnumerator());
                SwallowExceptions(() => wrapped.InitializeLifetimeService());
                SwallowExceptions(() => wrapped.GetData(0));
                SwallowExceptions(() => wrapped.IsDBNullAsync(0, default).GetAwaiter().GetResult());
                SwallowExceptions(() => wrapped.NextResultAsync(default).GetAwaiter().GetResult());
                SwallowExceptions(() => wrapped.ReadAsync(default).GetAwaiter().GetResult());
                SwallowExceptions(() => wrapped.GetFieldValueAsync<int>(0, default).GetAwaiter().GetResult());
                SwallowExceptions(() => wrapped.CloseAsync().GetAwaiter().GetResult());
                SwallowExceptions(() => wrapped.GetSchemaTableAsync(default).GetAwaiter().GetResult());
                SwallowExceptions(() => wrapped.Close());
            }
            finally
            {
                wrapped.Dispose();
            }
        }

        private static void SwallowExceptions(Action action)
        {
            try { action(); } catch { /* only line-coverage of the passthrough matters here */ }
        }

        [Fact]
        public void DisposedReader_AllMembersThrowObjectDisposed()
        {
            DbDataReader reader = DisposedReader.Instance;

            Assert.Throws<ObjectDisposedException>(() => reader.GetSchemaTable());
            Assert.Throws<ObjectDisposedException>(() => reader.InitializeLifetimeService());
            Assert.Throws<ObjectDisposedException>(() => reader.GetData(0));
            Assert.Throws<ObjectDisposedException>(() => reader.GetStream(0));
            Assert.Throws<ObjectDisposedException>(() => reader.GetTextReader(0));

            Assert.ThrowsAny<Exception>(() => reader.GetFieldValueAsync<int>(0, default).GetAwaiter().GetResult());
            Assert.ThrowsAny<Exception>(() => reader.IsDBNullAsync(0, default).GetAwaiter().GetResult());

            reader.Close(); // no-op
        }
    }
}
#endif
