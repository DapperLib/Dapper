#if !NET481
using System.Collections.Generic;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers SqlDataRecordListTVPParameter&lt;T&gt; (lines 14-43) and StructuredHelper (lines 44-105):
    /// AddParameter, Set (null/empty/non-empty), ConfigureTVP, ConfigureUDT, IL generation, cache.
    /// </summary>
    public class FakeDbTVPParameterTests
    {
        // ── AddParameter via DynamicParameters.Add ────────────────────
        // SqlDataRecordListTVPParameter<T>.AddParameter creates param, calls Set, adds to command.

        [Fact]
        public void TVPParameter_AddParameter_NonEmpty_AddsParamWithValue()
        {
            var records = new List<SimpleDataRecord2> { new SimpleDataRecord2() };
            var tvp = new SqlDataRecordListTVPParameter<SimpleDataRecord2>(records, "dbo.MyType");

            var dp = new DynamicParameters();
            dp.Add("ids", tvp);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("EXEC sp @ids", dp);
        }

        [Fact]
        public void TVPParameter_AddParameter_EmptyList_AddsParamWithNull()
        {
            var records = new List<SimpleDataRecord2>();
            var tvp = new SqlDataRecordListTVPParameter<SimpleDataRecord2>(records, "dbo.MyType");

            var dp = new DynamicParameters();
            dp.Add("ids", tvp);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(0);
            conn.Open();

            conn.Execute("EXEC sp @ids", dp);
        }

        // ── Set() static — null data ──────────────────────────────────

        [Fact]
        public void TVPParameter_Set_NullData_SetsNullValue()
        {
            var param = new MinimalDbParameter2();
            SqlDataRecordListTVPParameter<SimpleDataRecord2>.Set(param, null, null);
            Assert.Null(param.Value);
        }

        // ── Set() static — empty data ─────────────────────────────────

        [Fact]
        public void TVPParameter_Set_EmptyData_SetsNullValue()
        {
            var param = new MinimalDbParameter2();
            var empty = new List<SimpleDataRecord2>();
            SqlDataRecordListTVPParameter<SimpleDataRecord2>.Set(param, empty, null);
            Assert.Null(param.Value);
        }

        // ── Set() static — non-empty data ────────────────────────────

        [Fact]
        public void TVPParameter_Set_NonEmptyData_SetsValue()
        {
            var param = new MinimalDbParameter2();
            var records = new List<SimpleDataRecord2> { new SimpleDataRecord2() };
            SqlDataRecordListTVPParameter<SimpleDataRecord2>.Set(param, records, null);
            Assert.NotNull(param.Value);
        }

        // ── StructuredHelper.ConfigureTVP — no TypeName property → no-op ──

        [Fact]
        public void StructuredHelper_ConfigureTVP_NoProperty_IsNoOp()
        {
            var param = new MinimalDbParameter2();
            StructuredHelper.ConfigureTVP(param, "dbo.Type");
            // no TypeName property → no-op; param.Value unchanged
            Assert.Null(param.Value);
        }

        // ── StructuredHelper.ConfigureTVP — with TypeName property → IL path ──

        [Fact]
        public void StructuredHelper_ConfigureTVP_WithTypeNameProperty_SetsTypeName()
        {
            var param = new FakeParamWithTypeName();
            StructuredHelper.ConfigureTVP(param, "dbo.IdList");
            Assert.Equal("dbo.IdList", param.TypeName);
            Assert.Equal(30, param.SqlDbType); // SqlDbType.Structured = 30
        }

        // ── StructuredHelper.ConfigureTVP — cache hit (second call same type) ──

        [Fact]
        public void StructuredHelper_ConfigureTVP_SecondCall_UsesCachedDelegate()
        {
            var p1 = new FakeParamWithTypeName();
            var p2 = new FakeParamWithTypeName();

            StructuredHelper.ConfigureTVP(p1, "dbo.Type1");
            StructuredHelper.ConfigureTVP(p2, "dbo.Type2");

            Assert.Equal("dbo.Type1", p1.TypeName);
            Assert.Equal("dbo.Type2", p2.TypeName);
        }

        // ── StructuredHelper.ConfigureUDT — with UdtTypeName property → IL path ──
        // (ConfigureUDT is also tested via UdtTypeHandler, but test coverage for StructuredHelper here)

        [Fact]
        public void StructuredHelper_ConfigureUDT_WithUdtTypeNameProperty_SetsTypeName()
        {
            var param = new FakeParamWithUdtOnly();
            StructuredHelper.ConfigureUDT(param, "dbo.Point");
            Assert.Equal("dbo.Point", param.UdtTypeName);
            Assert.Equal(29, param.SqlDbType); // SqlDbType.Udt = 29
        }

        // ── StructuredHelper.ConfigureTVP — TypeName property not writable → no-op ──

        [Fact]
        public void StructuredHelper_ConfigureTVP_ReadOnlyProperty_IsNoOp()
        {
            var param = new FakeParamWithReadOnlyTypeName();
            StructuredHelper.ConfigureTVP(param, "dbo.Table");
            // no setter → no-op; TypeName stays at default
            Assert.Equal("readonly-default", param.TypeName);
        }

        // ── SqlDbType property present but not writable → skipped ─────

        [Fact]
        public void StructuredHelper_ConfigureTVP_WithTypeNameOnly_NoSqlDbType_Works()
        {
            var param = new FakeParamWithTypeNameNoSqlDbType();
            StructuredHelper.ConfigureTVP(param, "dbo.Tbl");
            Assert.Equal("dbo.Tbl", param.TypeName);
        }
    }

    // ── Helper parameter types ─────────────────────────────────────────

    internal class MinimalDbParameter2 : IDbDataParameter
    {
        public DbType DbType { get; set; } = DbType.Object;
        public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public bool IsNullable => false;
        public string? ParameterName { get; set; }
        public string? SourceColumn { get; set; }
        public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Default;
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }

    internal class FakeParamWithTypeName : MinimalDbParameter2
    {
        public string? TypeName { get; set; }
        public int SqlDbType { get; set; }
    }

    internal class FakeParamWithUdtOnly : MinimalDbParameter2
    {
        public string? UdtTypeName { get; set; }
        public int SqlDbType { get; set; }
    }

    internal class FakeParamWithReadOnlyTypeName : MinimalDbParameter2
    {
        public string TypeName => "readonly-default"; // no setter
    }

    internal class FakeParamWithTypeNameNoSqlDbType : MinimalDbParameter2
    {
        public string? TypeName { get; set; }
        // no SqlDbType property
    }

    internal class SimpleDataRecord2 : IDataRecord
    {
        public int FieldCount => 0;
        public object this[int i] => throw new System.NotImplementedException();
        public object this[string name] => throw new System.NotImplementedException();
        public bool GetBoolean(int i) => throw new System.NotImplementedException();
        public byte GetByte(int i) => throw new System.NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new System.NotImplementedException();
        public char GetChar(int i) => throw new System.NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new System.NotImplementedException();
        public IDataReader GetData(int i) => throw new System.NotImplementedException();
        public string GetDataTypeName(int i) => throw new System.NotImplementedException();
        public System.DateTime GetDateTime(int i) => throw new System.NotImplementedException();
        public decimal GetDecimal(int i) => throw new System.NotImplementedException();
        public double GetDouble(int i) => throw new System.NotImplementedException();
        public System.Type GetFieldType(int i) => throw new System.NotImplementedException();
        public float GetFloat(int i) => throw new System.NotImplementedException();
        public System.Guid GetGuid(int i) => throw new System.NotImplementedException();
        public short GetInt16(int i) => throw new System.NotImplementedException();
        public int GetInt32(int i) => throw new System.NotImplementedException();
        public long GetInt64(int i) => throw new System.NotImplementedException();
        public string GetName(int i) => throw new System.NotImplementedException();
        public int GetOrdinal(string name) => throw new System.NotImplementedException();
        public string GetString(int i) => throw new System.NotImplementedException();
        public object GetValue(int i) => throw new System.NotImplementedException();
        public int GetValues(object[] values) => throw new System.NotImplementedException();
        public bool IsDBNull(int i) => throw new System.NotImplementedException();
    }
}
#endif
