using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

// the in-memory ADO fakes below intentionally use non-null signatures; base ADO nullability
// annotations vary across the net481/net8.0/net10.0 target frameworks
#pragma warning disable CS8765

namespace Dapper.Tests
{
    [Collection("MultiMapCachePoisoning")]
    public sealed class MultiMapCachePoisoningTests
    {
        private sealed class Foo { public int Id { get; set; } }
        private sealed class Bar { public int OtherId { get; set; } }

        private static readonly Type[] Types = { typeof(Foo), typeof(Bar) };
        private static Foo Map(object[] objs) => (Foo)objs[0];

        // Regression tests for a multi-map cache-poisoning race: the async multi-map path
        // re-derives its cache entry from a parameter-less command after setup, so if the entry is
        // evicted in between, GetCacheInfo used to rebuild a reflection reader for the parameter
        // type that bound nothing and skipped IN-expansion. Reused entries then sent every @pN
        // unbound/unexpanded -> "Incorrect syntax near '@p0'" (list token) or "Must declare the
        // scalar variable '@p0'" (scalar token). The tests hit this deterministically by purging
        // the cache when the reader executes -- the exact window the eviction races into.

        // "Incorrect syntax near '@p0'" variant: an IN-list must still be expanded.
        [Fact]
        public async Task AsyncMultiMap_ListExpansion_SurvivesCacheEvictionMidQuery()
        {
            var (executed, _) = await RunReusedQueryAfterMidQueryEviction(
                "select Id, OtherId from Whatever where Id in @ids",
                () => new Dictionary<string, object> { ["ids"] = new[] { 1, 2, 3 } });

            Assert.NotNull(executed);
            // pre-fix, the poisoned reflection reader leaves the raw token in place
            Assert.DoesNotContain("in @ids", executed, StringComparison.OrdinalIgnoreCase);
            // post-fix, the list is expanded to positional parameters
            Assert.Contains("@ids1", executed!);
        }

        // "Must declare the scalar variable '@p0'" variant: a scalar-only query must still bind its
        // parameters (pre-fix the poisoned reader binds none, so the command executes with zero).
        [Fact]
        public async Task AsyncMultiMap_ScalarParameters_SurviveCacheEvictionMidQuery()
        {
            var (_, parameterCount) = await RunReusedQueryAfterMidQueryEviction(
                "select Id, OtherId from Whatever where Id = @p0 and OtherId = @p1",
                () => new Dictionary<string, object> { ["p0"] = 1, ["p1"] = 2 });

            // post-fix both parameters are bound; pre-fix this is 0 (the "Must declare" condition)
            Assert.Equal(2, parameterCount);
        }

        // A GridReader grid-read calls GetCacheInfo(ForGrid(type, index), null, ...); ForGrid
        // carries the parameters-type, and for the first grid the identity is identical to a plain
        // Query<T> over the same SQL/params. On a cold cache the grid read therefore caches a
        // reader derived from a null example under that shared identity, which a later Query<T>
        // reuses -- no eviction required.
        [Fact]
        public void GridReader_DoesNotPoisonParamReaderForColocatedQuery()
        {
            const string sql = "select Id, OtherId from Whatever where Id in @ids";
            static object NewParam() => new Dictionary<string, object> { ["ids"] = new[] { 1, 2, 3 } };

            SqlMapper.PurgeQueryCache();
            try
            {
                using (var conn = new FakeConnection())
                using (var grid = conn.QueryMultiple(sql, NewParam()))
                {
                    grid.Read<Foo>();
                }

                string? executed;
                using (var conn = new FakeConnection())
                {
                    conn.Query<Foo>(sql, NewParam());
                    executed = conn.LastCommandText;
                }

                Assert.NotNull(executed);
                Assert.DoesNotContain("in @ids", executed, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("@ids1", executed!);
            }
            finally
            {
                SqlMapper.PurgeQueryCache();
            }
        }

        // Runs one "poisoning" query that evicts its own cache entry mid-flight, then a second
        // query over the same SQL, returning what the second query actually sent to the server.
        private static async Task<(string? commandText, int parameterCount)> RunReusedQueryAfterMidQueryEviction(
            string sql, Func<object> newParam)
        {
            SqlMapper.PurgeQueryCache();
            try
            {
                // Poisoning pass: evict the freshly-cached entry the instant the reader executes,
                // i.e. after parameter-setup but before the multi-map re-derivation.
                using (var poison = new FakeConnection { OnExecute = SqlMapper.PurgeQueryCache })
                {
                    await poison.QueryAsync(sql, Types, (Func<object[], Foo>)Map, newParam(), splitOn: "OtherId");
                }

                // Assertion pass: no eviction; reuses whatever the poisoning pass left cached.
                using var conn = new FakeConnection();
                await conn.QueryAsync(sql, Types, (Func<object[], Foo>)Map, newParam(), splitOn: "OtherId");
                return (conn.LastCommandText, conn.LastParameterCount);
            }
            finally
            {
                SqlMapper.PurgeQueryCache();
            }
        }

        // minimal in-memory ADO provider (no database). ConnectionString is a fixed constant so
        // both passes produce the same Dapper Identity (which keys on it) and share a cache entry.
        private sealed class FakeConnection : DbConnection
        {
            public Action? OnExecute { get; set; }
            public string? LastCommandText { get; private set; }
            public int LastParameterCount { get; private set; }
            internal void Executed(string commandText, int parameterCount)
            {
                LastCommandText = commandText;
                LastParameterCount = parameterCount;
                OnExecute?.Invoke();
            }

            public override string ConnectionString { get; set; } = "fake-poisonable";
            public override string Database => "fake";
            public override string DataSource => "fake";
            public override string ServerVersion => "1.0";
            public override ConnectionState State => ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(IsolationLevel il) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => new FakeCommand(this);
        }

        private sealed class FakeCommand : DbCommand
        {
            private readonly FakeConnection _conn;
            private readonly FakeParameterCollection _params = new();
            public FakeCommand(FakeConnection conn) { _conn = conn; DbConnection = conn; }
            public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection => _params;
            protected override DbTransaction? DbTransaction { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => 0;
            public override object? ExecuteScalar() => null;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                // capture what would hit the server (SQL + bound parameter count) + fire the hook
                _conn.Executed(CommandText, _params.Count);
                return new SchemaReader();
            }
        }

        private sealed class FakeParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; } = "";
            public override string SourceColumn { get; set; } = "";
            public override object? Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }
            public override void ResetDbType() { }
        }

        private sealed class FakeParameterCollection : DbParameterCollection
        {
            private readonly List<object> _list = new();
            public override int Count => _list.Count;
            public override object SyncRoot { get; } = new();
            public override int Add(object value) { _list.Add(value); return _list.Count - 1; }
            public override void AddRange(Array values) { foreach (var v in values) _list.Add(v); }
            public override void Clear() => _list.Clear();
            public override bool Contains(object value) => _list.Contains(value);
            public override bool Contains(string value) => _list.Any(p => ((DbParameter)p).ParameterName == value);
            public override void CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);
            public override IEnumerator GetEnumerator() => _list.GetEnumerator();
            protected override DbParameter GetParameter(int index) => (DbParameter)_list[index];
            protected override DbParameter GetParameter(string name) => (DbParameter)_list.First(p => ((DbParameter)p).ParameterName == name);
            public override int IndexOf(object value) => _list.IndexOf(value);
            public override int IndexOf(string name) => _list.FindIndex(p => ((DbParameter)p).ParameterName == name);
            public override void Insert(int index, object value) => _list.Insert(index, value);
            public override void Remove(object value) => _list.Remove(value);
            public override void RemoveAt(int index) => _list.RemoveAt(index);
            public override void RemoveAt(string name) { var i = IndexOf(name); if (i >= 0) _list.RemoveAt(i); }
            protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
            protected override void SetParameter(string name, DbParameter value) { var i = IndexOf(name); if (i >= 0) _list[i] = value; else _list.Add(value); }
        }

        // reports the column schema (so multi-map deserializers can be generated) but returns no rows
        private sealed class SchemaReader : DbDataReader
        {
            private static readonly string[] Cols = { "Id", "OtherId" };
            public override int FieldCount => Cols.Length;
            public override string GetName(int ordinal) => Cols[ordinal];
            public override Type GetFieldType(int ordinal) => typeof(int);
            public override string GetDataTypeName(int ordinal) => "int";
            public override int GetOrdinal(string name) => Array.IndexOf(Cols, name);
            public override bool Read() => false;
            public override bool NextResult() => false;
            public override bool HasRows => false;
            public override int Depth => 0;
            public override bool IsClosed => false;
            public override int RecordsAffected => 0;
            public override object this[int ordinal] => throw new NotSupportedException();
            public override object this[string name] => throw new NotSupportedException();
            public override bool GetBoolean(int ordinal) => default;
            public override byte GetByte(int ordinal) => default;
            public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
            public override char GetChar(int ordinal) => default;
            public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
            public override DateTime GetDateTime(int ordinal) => default;
            public override decimal GetDecimal(int ordinal) => default;
            public override double GetDouble(int ordinal) => default;
            public override float GetFloat(int ordinal) => default;
            public override Guid GetGuid(int ordinal) => default;
            public override short GetInt16(int ordinal) => default;
            public override int GetInt32(int ordinal) => default;
            public override long GetInt64(int ordinal) => default;
            public override string GetString(int ordinal) => "";
            public override object GetValue(int ordinal) => throw new NotSupportedException();
            public override int GetValues(object[] values) => 0;
            public override bool IsDBNull(int ordinal) => true;
            public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
        }
    }
}
