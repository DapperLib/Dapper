#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using pengdows.crud.fakeDb;
using Xunit;

#pragma warning disable CS0618, CS0619 // Obsolete internal-use-only members are intentionally exercised here

namespace Dapper.Tests
{
    // ── Direct static-API coverage: methods that are public/internal on SqlMapper
    // but obsolete-internal-use-only, exercised directly against minimal fakes
    // rather than through a full query pipeline. ──────────────────────────────
    public class FakeDbCoverageGapsCoreTests
    {
        // ── SetQueryCache / CollectCacheGarbage (L64-88) ───────────────────────

        [Fact]
        public void ManyDistinctQueries_TriggerCacheGarbageCollection()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            for (int i = 0; i < 1005; i++)
            {
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "v", i } } });
            }
            conn.Open();

            for (int i = 0; i < 1005; i++)
            {
                var result = conn.QueryFirst<int>($"SELECT {i} AS v /* q{i} */");
                Assert.Equal(i, result);
            }
        }

        // ── ShouldSetDbType (internal, direct) (L171-175) ──────────────────────

        [Fact]
        public void ShouldSetDbType_NullableOverload_FalseForEnumerableMultiParameter()
        {
            DbType? enumerableMulti = (DbType)(-1);
            Assert.False(DynamicParameters.ShouldSetDbType(enumerableMulti));
            Assert.True(DynamicParameters.ShouldSetDbType((DbType?)DbType.Int32));
        }

        [Fact]
        public void ShouldSetDbType_NonNullableOverload_FalseForEnumerableMultiParameter()
        {
            Assert.False(DynamicParameters.ShouldSetDbType((DbType)(-1)));
            Assert.True(DynamicParameters.ShouldSetDbType(DbType.Int32));
        }

        // ── FindOrAddParameter (L2108-2122) ─────────────────────────────────────

        private static IDbDataParameter InvokeFindOrAddParameter(IDataParameterCollection parameters, IDbCommand command, string name)
        {
            var method = typeof(SqlMapper).GetMethod(nameof(SqlMapper.FindOrAddParameter))!;
            return (IDbDataParameter)method.Invoke(null, new object[] { parameters, command, name })!;
        }

        // FakeParameterCollection.Contains(string) always returns false, so it can't
        // exercise the "found existing" branch of FindOrAddParameter; use a collection
        // that actually tracks parameter names by name.
        private class NameTrackingParameterCollection : System.Collections.ArrayList, IDataParameterCollection
        {
            public bool Contains(string parameterName) =>
                this.Cast<IDataParameter>().Any(p => p.ParameterName == parameterName);
            public int IndexOf(string parameterName) =>
                this.Cast<IDataParameter>().ToList().FindIndex(p => p.ParameterName == parameterName);
            public void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
            public object this[string parameterName]
            {
                get => this.Cast<IDataParameter>().First(p => p.ParameterName == parameterName);
                set => this[IndexOf(parameterName)] = value;
            }
        }

        [Fact]
        public void FindOrAddParameter_ExistingName_ReturnsExisting()
        {
            var cmd = new MinimalFakeCommand();
            var parameters = new NameTrackingParameterCollection();
            var existing = cmd.CreateParameter();
            existing.ParameterName = "p1";
            parameters.Add(existing);

            var found = InvokeFindOrAddParameter(parameters, cmd, "p1");
            Assert.Same(existing, found);
        }

        [Fact]
        public void FindOrAddParameter_MissingName_CreatesAndAdds()
        {
            var cmd = new MinimalFakeCommand();
            var parameters = new NameTrackingParameterCollection();
            var found = InvokeFindOrAddParameter(parameters, cmd, "p2");
            Assert.Equal("p2", found.ParameterName);
            Assert.Contains(found, parameters.Cast<object>());
        }

        // ── GetListPaddingExtraCount (internal, direct) (L2124-2149) ────────────

        [Theory]
        [InlineData(0, 0)]      // no padding: count <= 5
        [InlineData(-5, 0)]     // negative count
        [InlineData(17, 3)]     // padFactor=10 (count<=150): 17 % 10 = 7, need 3 more
        [InlineData(200, 0)]    // padFactor=50 (count<=750): 200 % 50 = 0
        [InlineData(760, 40)]   // padFactor=100 (count<=2000): 760 % 100 = 60, need 40 more
        [InlineData(2053, 7)]   // padFactor=10 (count<=2070): 2053 % 10 = 3, need 7 more
        [InlineData(2075, 0)]   // between 2070 and 2100: no padding
        [InlineData(2200, 0)]   // padFactor=200 (>2100): 2200 % 200 = 0
        [InlineData(2250, 150)] // padFactor=200 (>2100): 2250 % 200 = 50, need 150 more
        public void GetListPaddingExtraCount_VariousBands(int count, int expected)
        {
            var method = typeof(SqlMapper).GetMethod("GetListPaddingExtraCount", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (int)method.Invoke(null, new object[] { count })!;
            Assert.Equal(expected, result);
        }

        // ── PackListParameters: Arrays-capable connection (L2169-2175) ──────────

        private class NpgsqlConnection : IDbConnection
        {
            public string ConnectionString { get; set; } = "";
            public int ConnectionTimeout => 0;
            public string Database => "";
            public ConnectionState State => ConnectionState.Closed;
            public IDbTransaction BeginTransaction() => throw new NotSupportedException();
            public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
            public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
            public void Close() { }
            public IDbCommand CreateCommand() => throw new NotSupportedException();
            public void Dispose() { }
            public void Open() { }
        }

        [Fact]
        public void PackListParameters_ArraysCapableConnection_UsesSingleArrayParameter()
        {
            var cmd = new MinimalFakeCommand { Connection = new NpgsqlConnection(), CommandText = "SELECT * FROM T WHERE Id = ANY(@ids)" };
            SqlMapper.PackListParameters(cmd, "ids", new List<int> { 1, 2, 3 });

            Assert.Single(cmd.Parameters.Cast<IDbDataParameter>());
            var p = (IDbDataParameter)cmd.Parameters[0]!;
            Assert.Equal("ids", p.ParameterName);
        }

        // ── PackListParameters: first item null throws (L2197-2200) ─────────────

        [Fact]
        public void PackListParameters_FirstItemNull_Throws()
        {
            var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Id IN @ids" };
            Assert.Throws<NotSupportedException>(() =>
                SqlMapper.PackListParameters(cmd, "ids", new object?[] { null, 1 }));
        }

        // ── PackListParameters: long string sets Size=-1 (L2218-2221) ───────────

        [Fact]
        public void PackListParameters_LongStringInList_SetsSizeMinusOne()
        {
            var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Name IN @names" };
            var longString = new string('x', DbString.DefaultLength + 10);
            SqlMapper.PackListParameters(cmd, "names", new List<string> { longString });

            var p = (IDbDataParameter)cmd.Parameters[0]!;
            Assert.Equal(-1, p.Size);
        }

        // ── PackListParameters: "unknown" optimize-hint, count==0 and count>0 (L2270-2320) ──

        [Fact]
        public void PackListParameters_UnknownHint_EmptyList_LeavesHintAlone()
        {
            var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Id IN @ids unknown" };
            SqlMapper.PackListParameters(cmd, "ids", Array.Empty<int>());

            Assert.Contains("unknown", cmd.CommandText);
        }

        [Fact]
        public void PackListParameters_UnknownHint_NonEmptyList_ExpandsHint()
        {
            var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Id IN @ids unknown" };
            SqlMapper.PackListParameters(cmd, "ids", new List<int> { 1, 2, 3 });

            Assert.DoesNotContain("@ids unknown", cmd.CommandText);
            Assert.Equal(3, cmd.Parameters.Count);
        }

        [Fact]
        public void PackListParameters_EmptyList_NoHint_ProducesDummyParam()
        {
            var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Id IN @ids" };
            SqlMapper.PackListParameters(cmd, "ids", Array.Empty<int>());

            var p = (IDbDataParameter)cmd.Parameters[0]!;
            Assert.Equal(DBNull.Value, p.Value);
        }

        // ── TryStringSplit: byte case, non-ICollection source, unknown-hint bypass ──

        [Fact]
        public void PackListParameters_ByteEnumerable_ViaStringSplit_NonCollectionSource()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 1;
                var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Id IN @ids" };
                IEnumerable<byte> lazy = Enumerable.Range(1, 3).Select(i => (byte)i);
                SqlMapper.PackListParameters(cmd, "ids", lazy);

                Assert.Contains("string_split", cmd.CommandText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        [Fact]
        public void PackListParameters_UnknownHint_WithStringSplitEnabled_LeavesHintAloneAndFallsBackToNormalExpansion()
        {
            var original = SqlMapper.Settings.InListStringSplitCount;
            try
            {
                SqlMapper.Settings.InListStringSplitCount = 1;
                var cmd = new MinimalFakeCommand { CommandText = "SELECT * FROM T WHERE Id IN @ids unknown" };
                SqlMapper.PackListParameters(cmd, "ids", new List<int> { 1, 2, 3 });

                // TryStringSplit bails out (leaves "unknown" hint alone, returns false),
                // so the normal expansion path takes over instead.
                Assert.Equal(3, cmd.Parameters.Count);
            }
            finally
            {
                SqlMapper.Settings.InListStringSplitCount = original;
            }
        }

        // ── LookupDbType / GetSimpleValueDeserializer: fake "System.Data.Linq.Binary" (L482-484, L2849-2852) ──

        [Fact]
        public void LookupDbType_LinqBinaryByName_ReturnsBinary()
        {
            var dbType = SqlMapper.LookupDbType(typeof(System.Data.Linq.Binary), "n/a", false, out var handler);
            Assert.Equal(DbType.Binary, dbType);
            Assert.Null(handler);
        }

        [Fact]
        public void Query_WithLinqBinaryLikeParameter_ExpandsToArrayCall()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("INSERT INTO T (Data) VALUES (@Data)", new { Data = new System.Data.Linq.Binary(new byte[] { 1, 2, 3 }) });
        }

        // ── AddTypeHandler(Type, handler) with a Nullable<T> value type (L372-385) ──

        private class Int32Handler : SqlMapper.TypeHandler<int>
        {
            public override void SetValue(IDbDataParameter parameter, int value) => parameter.Value = value;
            public override int Parse(object value) => Convert.ToInt32(value);
        }

        [Fact]
        public void AddTypeHandler_NullableValueType_RegistersBothNullableAndUnderlying()
        {
            try
            {
                SqlMapper.AddTypeHandler(typeof(int?), new Int32Handler());
                Assert.True(SqlMapper.HasTypeHandler(typeof(int?)));
                Assert.True(SqlMapper.HasTypeHandler(typeof(int)));
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        // ── SanitizeParameterValue (L2398-2420) - already largely covered; smoke test ──

        [Fact]
        public void SanitizeParameterValue_Null_ReturnsDBNull()
        {
            Assert.Equal(DBNull.Value, SqlMapper.SanitizeParameterValue(null));
        }

        // ── ThrowNullCustomQueryParameter (L4043-4044) ──────────────────────────

        [Fact]
        public void ThrowNullCustomQueryParameter_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => SqlMapper.ThrowNullCustomQueryParameter("Foo"));
        }

        // ── ConnectionStringComparer getter/setter (L4136-4140) ─────────────────

        [Fact]
        public void ConnectionStringComparer_CanBeSetAndRetrieved()
        {
            var original = SqlMapper.ConnectionStringComparer;
            try
            {
                SqlMapper.ConnectionStringComparer = StringComparer.OrdinalIgnoreCase;
                Assert.Same(StringComparer.OrdinalIgnoreCase, SqlMapper.ConnectionStringComparer);

                SqlMapper.ConnectionStringComparer = null!;
                Assert.Same(StringComparer.Ordinal, SqlMapper.ConnectionStringComparer);
            }
            finally
            {
                SqlMapper.ConnectionStringComparer = original;
            }
        }

        // ── AsTableValuedParameter<T> extension (L4187-4188) ────────────────────

        private class SimpleRecord : IDataRecord
        {
            public int FieldCount => 1;
            public object this[int i] => 1;
            public object this[string name] => 1;
            public bool GetBoolean(int i) => throw new NotSupportedException();
            public byte GetByte(int i) => throw new NotSupportedException();
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
            public char GetChar(int i) => throw new NotSupportedException();
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
            public IDataReader GetData(int i) => throw new NotSupportedException();
            public string GetDataTypeName(int i) => "int";
            public DateTime GetDateTime(int i) => throw new NotSupportedException();
            public decimal GetDecimal(int i) => throw new NotSupportedException();
            public double GetDouble(int i) => throw new NotSupportedException();
            public Type GetFieldType(int i) => typeof(int);
            public float GetFloat(int i) => throw new NotSupportedException();
            public Guid GetGuid(int i) => throw new NotSupportedException();
            public short GetInt16(int i) => throw new NotSupportedException();
            public int GetInt32(int i) => 1;
            public long GetInt64(int i) => throw new NotSupportedException();
            public string GetName(int i) => "Value";
            public int GetOrdinal(string name) => 0;
            public string GetString(int i) => throw new NotSupportedException();
            public object GetValue(int i) => 1;
            public int GetValues(object[] values) => 0;
            public bool IsDBNull(int i) => false;
        }

        [Fact]
        public void AsTableValuedParameter_IEnumerableOfIDataRecord_CreatesTvp()
        {
            var records = new[] { new SimpleRecord() };
            var tvp = records.AsTableValuedParameter("MyTableType");
            Assert.NotNull(tvp);
        }
    }
}

namespace System.Data.Linq
{
    // matches SqlMapper's string-based "System.Data.Linq.Binary" FullName check
    // without requiring the real (netfx-only) System.Data.Linq assembly.
    internal class Binary
    {
        private readonly byte[] _data;
        public Binary(byte[] data) => _data = data;
        public byte[] ToArray() => _data;
    }
}
#endif
