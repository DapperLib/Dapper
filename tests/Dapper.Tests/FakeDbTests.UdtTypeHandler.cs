#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers UdtTypeHandler (lines 12-37) and SqlDataRecordHandler (lines 10-18)
    /// — both in the core Dapper library.
    /// </summary>
    public class FakeDbUdtTypeHandlerTests
    {
        // ── UdtTypeHandler constructor validation ─────────────────────

        [Fact]
        public void UdtTypeHandler_NullName_ThrowsArgumentException()
            => Assert.Throws<ArgumentException>(() => new SqlMapper.UdtTypeHandler(null!));

        [Fact]
        public void UdtTypeHandler_EmptyName_ThrowsArgumentException()
            => Assert.Throws<ArgumentException>(() => new SqlMapper.UdtTypeHandler(""));

        // ── ITypeHandler.Parse ────────────────────────────────────────

        [Fact]
        public void UdtTypeHandler_Parse_DBNull_ReturnsNull()
        {
            SqlMapper.ITypeHandler handler = new SqlMapper.UdtTypeHandler("dbo.Point");
            var result = handler.Parse(typeof(object), DBNull.Value);
            Assert.Null(result);
        }

        [Fact]
        public void UdtTypeHandler_Parse_Value_ReturnsValue()
        {
            SqlMapper.ITypeHandler handler = new SqlMapper.UdtTypeHandler("dbo.Point");
            var result = handler.Parse(typeof(object), "someGeometry");
            Assert.Equal("someGeometry", result);
        }

        // ── ITypeHandler.SetValue — DBNull skips ConfigureUDT ─────────

        [Fact]
        public void UdtTypeHandler_SetValue_DBNull_SetsValueOnly()
        {
            SqlMapper.ITypeHandler handler = new SqlMapper.UdtTypeHandler("dbo.Point");
            var param = new MinimalDbParameter();

            handler.SetValue(param, DBNull.Value);

            Assert.Equal(DBNull.Value, param.Value);
        }

        // ── ITypeHandler.SetValue — non-null calls ConfigureUDT ───────
        // Uses a plain param (no UdtTypeName prop) → StructuredHelper returns no-op

        [Fact]
        public void UdtTypeHandler_SetValue_NonNull_SetsValueAndCallsConfigureUDT()
        {
            SqlMapper.ITypeHandler handler = new SqlMapper.UdtTypeHandler("dbo.Point");
            var param = new MinimalDbParameter();

            handler.SetValue(param, "POINT(1 2)");

            Assert.Equal("POINT(1 2)", param.Value);
        }

        // ── ITypeHandler.SetValue — with UdtTypeName property → IL path ──

        [Fact]
        public void UdtTypeHandler_SetValue_WithUdtTypeName_Property_SetsTypeName()
        {
            SqlMapper.ITypeHandler handler = new SqlMapper.UdtTypeHandler("dbo.Point");
            var param = new FakeParamWithUdt();

            handler.SetValue(param, "POINT(3 4)");

            Assert.Equal("POINT(3 4)", param.Value);
            Assert.Equal("dbo.Point", param.UdtTypeName);
        }

        // ── ITypeHandler.SetValue — cache hit (second call with same type) ──

        [Fact]
        public void UdtTypeHandler_SetValue_SecondCall_UsesCachedDelegate()
        {
            SqlMapper.ITypeHandler handler = new SqlMapper.UdtTypeHandler("dbo.Line");
            var p1 = new FakeParamWithUdt();
            var p2 = new FakeParamWithUdt();

            handler.SetValue(p1, "LINE(0 0,1 1)");
            handler.SetValue(p2, "LINE(2 2,3 3)");

            Assert.Equal("dbo.Line", p1.UdtTypeName);
            Assert.Equal("dbo.Line", p2.UdtTypeName);
        }
    }

    // ── SqlDataRecordHandler ──────────────────────────────────────────

    public class FakeDbSqlDataRecordHandlerTests
    {
        // SqlDataRecordHandler<T> is internal, accessed via InternalsVisibleTo("Dapper.Tests")

        [Fact]
        public void SqlDataRecordHandler_Parse_AlwaysThrows()
        {
            var handler = (SqlMapper.ITypeHandler)new SqlDataRecordHandler<IDataRecord>();
            Assert.Throws<NotSupportedException>(() =>
                handler.Parse(typeof(IEnumerable<IDataRecord>), new object()));
        }

        [Fact]
        public void SqlDataRecordHandler_SetValue_WithNullEnumerable_SetsNull()
        {
            var handler = (SqlMapper.ITypeHandler)new SqlDataRecordHandler<IDataRecord>();
            var param = new MinimalDbParameter();

            // value is not IEnumerable<IDataRecord>, so "value as IEnumerable<T>" == null
            handler.SetValue(param, "not an enumerable");

            Assert.Null(param.Value);
        }

        [Fact]
        public void SqlDataRecordHandler_SetValue_WithEmptyList_SetsNull()
        {
            var handler = (SqlMapper.ITypeHandler)new SqlDataRecordHandler<IDataRecord>();
            var param = new MinimalDbParameter();
            var emptyList = new List<IDataRecord>(); // empty → .Any() is false

            handler.SetValue(param, emptyList);

            Assert.Null(param.Value);
        }

        [Fact]
        public void SqlDataRecordHandler_SetValue_WithRecords_SetsValue()
        {
            var handler = (SqlMapper.ITypeHandler)new SqlDataRecordHandler<IDataRecord>();
            var param = new MinimalDbParameter();
            var records = new List<IDataRecord> { new SimpleDataRecord() };

            handler.SetValue(param, records);

            Assert.NotNull(param.Value);
        }
    }

    // ── Helper types ──────────────────────────────────────────────────

    internal class MinimalDbParameter : IDbDataParameter
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

    internal class FakeParamWithUdt : MinimalDbParameter
    {
        public string? UdtTypeName { get; set; }
        public int SqlDbType { get; set; }
    }

    internal class SimpleDataRecord : IDataRecord
    {
        public int FieldCount => 0;
        public object this[int i] => throw new NotImplementedException();
        public object this[string name] => throw new NotImplementedException();
        public bool GetBoolean(int i) => throw new NotImplementedException();
        public byte GetByte(int i) => throw new NotImplementedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public char GetChar(int i) => throw new NotImplementedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
        public IDataReader GetData(int i) => throw new NotImplementedException();
        public string GetDataTypeName(int i) => throw new NotImplementedException();
        public DateTime GetDateTime(int i) => throw new NotImplementedException();
        public decimal GetDecimal(int i) => throw new NotImplementedException();
        public double GetDouble(int i) => throw new NotImplementedException();
        public Type GetFieldType(int i) => throw new NotImplementedException();
        public float GetFloat(int i) => throw new NotImplementedException();
        public Guid GetGuid(int i) => throw new NotImplementedException();
        public short GetInt16(int i) => throw new NotImplementedException();
        public int GetInt32(int i) => throw new NotImplementedException();
        public long GetInt64(int i) => throw new NotImplementedException();
        public string GetName(int i) => throw new NotImplementedException();
        public int GetOrdinal(string name) => throw new NotImplementedException();
        public string GetString(int i) => throw new NotImplementedException();
        public object GetValue(int i) => throw new NotImplementedException();
        public int GetValues(object[] values) => throw new NotImplementedException();
        public bool IsDBNull(int i) => throw new NotImplementedException();
    }
}
#endif
