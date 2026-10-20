#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests for WrappedBasicReader internal methods using reflection.
    /// WrappedBasicReader is internal sealed, so we must access it via reflection.
    /// It wraps a plain IDataReader as a DbDataReader.
    /// </summary>
    public class FakeDbWrappedBasicReaderReflectionTests
    {
        // MinimalDataReader: a non-DbDataReader IDataReader with various types
        private sealed class MultiTypeDataReader : IDataReader
        {
            private readonly IReadOnlyList<Dictionary<string, object?>> _rows;
            private int _pos = -1;

            public MultiTypeDataReader(IReadOnlyList<Dictionary<string, object?>> rows)
            {
                _rows = rows;
            }

            private Dictionary<string, object?> Current => _rows[_pos];
            private List<string> Columns => _rows.Count == 0
                ? new List<string>()
                : new List<string>(_rows[0].Keys);

            public int FieldCount => _rows.Count == 0 ? 0 : _rows[0].Count;
            public bool Read() => ++_pos < _rows.Count;
            public bool NextResult() => false;
            public bool IsClosed => _pos >= _rows.Count;
            public void Close() { _pos = _rows.Count; }
            public int Depth => 0;
            public int RecordsAffected => -1;

            public string GetName(int i) => Columns[i];
            public int GetOrdinal(string name) => Columns.IndexOf(name);
            public object GetValue(int i)
            {
                var val = Current[Columns[i]];
                return val ?? DBNull.Value;
            }
            public bool IsDBNull(int i) => Current[Columns[i]] is null || Current[Columns[i]] is DBNull;
            public object this[int i] => GetValue(i);
            public object this[string name] => GetValue(GetOrdinal(name));

            public int GetValues(object[] values)
            {
                for (int i = 0; i < FieldCount; i++) values[i] = GetValue(i);
                return FieldCount;
            }

            public bool GetBoolean(int i) => Convert.ToBoolean(Current[Columns[i]]);
            public byte GetByte(int i) => Convert.ToByte(Current[Columns[i]]);
            public char GetChar(int i) => Convert.ToChar(Current[Columns[i]]);
            public short GetInt16(int i) => Convert.ToInt16(Current[Columns[i]]);
            public int GetInt32(int i) => Convert.ToInt32(Current[Columns[i]]);
            public long GetInt64(int i) => Convert.ToInt64(Current[Columns[i]]);
            public float GetFloat(int i) => Convert.ToSingle(Current[Columns[i]]);
            public double GetDouble(int i) => Convert.ToDouble(Current[Columns[i]]);
            public decimal GetDecimal(int i) => Convert.ToDecimal(Current[Columns[i]]);
            public DateTime GetDateTime(int i) => (DateTime)Current[Columns[i]]!;
            public Guid GetGuid(int i) => (Guid)Current[Columns[i]]!;
            public string GetString(int i) => (string)Current[Columns[i]]!;

            public string GetDataTypeName(int i) => GetFieldType(i).Name;
            public Type GetFieldType(int i)
            {
                if (_rows.Count == 0) return typeof(object);
                var val = _rows[0][Columns[i]];
                return val?.GetType() ?? typeof(object);
            }
            public DataTable? GetSchemaTable() => null;
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
            public IDataReader GetData(int i) => throw new NotSupportedException();
            public void Dispose() => Close();
        }

        private static DbDataReader CreateWrappedBasicReader(IDataReader reader)
        {
            var assembly = typeof(SqlMapper).Assembly;
            var type = assembly.GetType("Dapper.WrappedBasicReader", throwOnError: true)!;
            return (DbDataReader)Activator.CreateInstance(type,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { reader }, null)!;
        }

        private static MultiTypeDataReader MakeReader() => new MultiTypeDataReader(new[]
        {
            new Dictionary<string, object?>
            {
                { "BoolVal", true },
                { "ByteVal", (byte)42 },
                { "CharVal", 'Z' },
                { "Int16Val", (short)100 },
                { "Int32Val", 999 },
                { "Int64Val", 1234567890L },
                { "FloatVal", 3.14f },
                { "DoubleVal", 2.718d },
                { "DecimalVal", 1.23m },
                { "DateVal", new DateTime(2024, 1, 15) },
                { "GuidVal", new Guid("12345678-1234-1234-1234-123456789012") },
                { "StringVal", "hello" },
                { "NullVal", null },
            }
        });

        [Fact]
        public void WrappedBasicReader_HasRows_ReturnsTrue()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.HasRows); // always true by contract
        }

        [Fact]
        public void WrappedBasicReader_IsClosed_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.False(wrapped.IsClosed);
        }

        [Fact]
        public void WrappedBasicReader_Depth_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.Equal(0, wrapped.Depth);
        }

        [Fact]
        public void WrappedBasicReader_RecordsAffected_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.Equal(-1, wrapped.RecordsAffected);
        }

        [Fact]
        public void WrappedBasicReader_NextResult_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.False(wrapped.NextResult());
        }

        [Fact]
        public void WrappedBasicReader_FieldCount_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.Equal(13, wrapped.FieldCount);
        }

        [Fact]
        public void WrappedBasicReader_VisibleFieldCount_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.Equal(13, wrapped.VisibleFieldCount);
        }

        [Fact]
        public void WrappedBasicReader_GetSchemaTable_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            // returns null from our implementation
            var table = wrapped.GetSchemaTable();
            // just verify it doesn't throw
        }

        [Fact]
        public void WrappedBasicReader_Close_Works()
        {
            var inner = MakeReader();
            var wrapped = CreateWrappedBasicReader(inner);
            wrapped.Close();
            // after close the inner should be closed
        }

        [Fact]
        public void WrappedBasicReader_GetDataTypeName_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var name = wrapped.GetDataTypeName(0); // BoolVal -> "Boolean"
            Assert.Equal("Boolean", name);
        }

        [Fact]
        public void WrappedBasicReader_GetFieldType_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(typeof(bool), wrapped.GetFieldType(0));
        }

        [Fact]
        public void WrappedBasicReader_GetName_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal("BoolVal", wrapped.GetName(0));
        }

        [Fact]
        public void WrappedBasicReader_GetOrdinal_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(4, wrapped.GetOrdinal("Int32Val"));
        }

        [Fact]
        public void WrappedBasicReader_GetValue_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(true, wrapped.GetValue(0));
        }

        [Fact]
        public void WrappedBasicReader_GetValues_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var values = new object[13];
            var count = wrapped.GetValues(values);
            Assert.Equal(13, count);
        }

        [Fact]
        public void WrappedBasicReader_IsDBNull_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.False(wrapped.IsDBNull(0));
            Assert.True(wrapped.IsDBNull(12)); // NullVal
        }

        [Fact]
        public void WrappedBasicReader_GetBoolean_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.True(wrapped.GetBoolean(0));
        }

        [Fact]
        public void WrappedBasicReader_GetByte_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal((byte)42, wrapped.GetByte(1));
        }

        [Fact]
        public void WrappedBasicReader_GetChar_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal('Z', wrapped.GetChar(2));
        }

        [Fact]
        public void WrappedBasicReader_GetInt16_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal((short)100, wrapped.GetInt16(3));
        }

        [Fact]
        public void WrappedBasicReader_GetInt32_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(999, wrapped.GetInt32(4));
        }

        [Fact]
        public void WrappedBasicReader_GetInt64_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(1234567890L, wrapped.GetInt64(5));
        }

        [Fact]
        public void WrappedBasicReader_GetFloat_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(3.14f, wrapped.GetFloat(6));
        }

        [Fact]
        public void WrappedBasicReader_GetDouble_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(2.718d, wrapped.GetDouble(7));
        }

        [Fact]
        public void WrappedBasicReader_GetDecimal_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(1.23m, wrapped.GetDecimal(8));
        }

        [Fact]
        public void WrappedBasicReader_GetDateTime_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(new DateTime(2024, 1, 15), wrapped.GetDateTime(9));
        }

        [Fact]
        public void WrappedBasicReader_GetGuid_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(new Guid("12345678-1234-1234-1234-123456789012"), wrapped.GetGuid(10));
        }

        [Fact]
        public void WrappedBasicReader_GetString_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal("hello", wrapped.GetString(11));
        }

        [Fact]
        public void WrappedBasicReader_GetFieldValue_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(999, wrapped.GetFieldValue<int>(4));
        }

        [Fact]
        public void WrappedBasicReader_GetFieldValue_Null_ReturnsDefault()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var val = wrapped.GetFieldValue<string?>(12); // NullVal
            Assert.Null(val);
        }

        [Fact]
        public async Task WrappedBasicReader_GetFieldValueAsync_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var val = await wrapped.GetFieldValueAsync<int>(4);
            Assert.Equal(999, val);
        }

        [Fact]
        public async Task WrappedBasicReader_IsDBNullAsync_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.False(await wrapped.IsDBNullAsync(0));
            Assert.True(await wrapped.IsDBNullAsync(12));
        }

        [Fact]
        public async Task WrappedBasicReader_NextResultAsync_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.False(await wrapped.NextResultAsync(CancellationToken.None));
        }

        [Fact]
        public async Task WrappedBasicReader_ReadAsync_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(await wrapped.ReadAsync(CancellationToken.None));
            Assert.False(await wrapped.ReadAsync(CancellationToken.None));
        }

        [Fact]
        public void WrappedBasicReader_Indexer_ByInt_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(true, wrapped[0]);
        }

        [Fact]
        public void WrappedBasicReader_Indexer_ByName_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(999, wrapped["Int32Val"]);
        }

        [Fact]
        public void WrappedBasicReader_GetProviderSpecificFieldType_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(typeof(bool), wrapped.GetProviderSpecificFieldType(0));
        }

        [Fact]
        public void WrappedBasicReader_GetProviderSpecificValue_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Equal(true, wrapped.GetProviderSpecificValue(0));
        }

        [Fact]
        public void WrappedBasicReader_GetProviderSpecificValues_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var values = new object[13];
            wrapped.GetProviderSpecificValues(values);
            Assert.Equal(true, values[0]);
        }

        [Fact]
        public void WrappedBasicReader_GetBytes_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var result = wrapped.GetBytes(0, 0, null, 0, 0);
            Assert.Equal(0, result);
        }

        [Fact]
        public void WrappedBasicReader_GetChars_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            var result = wrapped.GetChars(0, 0, null, 0, 0);
            Assert.Equal(0, result);
        }

        [Fact]
        public void WrappedBasicReader_GetStream_Throws()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Throws<NotSupportedException>(() => wrapped.GetStream(0));
        }

        [Fact]
        public void WrappedBasicReader_GetTextReader_Throws()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            Assert.True(wrapped.Read());
            Assert.Throws<NotSupportedException>(() => wrapped.GetTextReader(0));
        }

#if NET5_0_OR_GREATER
        [Fact]
        public async Task WrappedBasicReader_CloseAsync_Works()
        {
            var inner = MakeReader();
            var wrapped = CreateWrappedBasicReader(inner);
            await wrapped.CloseAsync();
        }

        [Fact]
        public async Task WrappedBasicReader_DisposeAsync_Works()
        {
            var inner = MakeReader();
            var wrapped = CreateWrappedBasicReader(inner);
            await wrapped.DisposeAsync();
        }

        [Fact]
        public async Task WrappedBasicReader_GetSchemaTableAsync_Works()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            var table = await wrapped.GetSchemaTableAsync();
            // returns null, no throw
        }

        [Fact]
        public async Task WrappedBasicReader_GetColumnSchemaAsync_Throws()
        {
            using var inner = MakeReader();
            using var wrapped = CreateWrappedBasicReader(inner);
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                wrapped.GetColumnSchemaAsync());
        }
#endif
    }
}
#endif
