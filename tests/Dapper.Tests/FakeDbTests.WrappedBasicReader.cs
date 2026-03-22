#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Tests that trigger WrappedBasicReader by passing a non-DbDataReader IDataReader
    /// to Dapper's Parse/GetRowParser methods.
    /// WrappedBasicReader wraps IDataReader → DbDataReader for internal Dapper use.
    /// </summary>
    public class FakeDbWrappedBasicReaderTests
    {
        // A minimal IDataReader that is NOT a DbDataReader
        private sealed class MinimalDataReader : IDataReader
        {
            private readonly IReadOnlyList<Dictionary<string, object?>> _rows;
            private int _pos = -1;

            public MinimalDataReader(IReadOnlyList<Dictionary<string, object?>> rows)
            {
                _rows = rows;
            }

            private Dictionary<string, object?> Current => _rows[_pos];
            private List<string> Columns => _rows.Count == 0
                ? new List<string>()
                : _rows[0].Keys.ToList();

            public int FieldCount => _rows.Count == 0 ? 0 : _rows[0].Count;
            public bool Read() => ++_pos < _rows.Count;
            public bool NextResult() => false;
            public bool IsClosed => _pos >= _rows.Count;
            public void Close() { _pos = _rows.Count; }
            public int Depth => 0;
            public int RecordsAffected => -1;

            public string GetName(int i) => Columns[i];
            public int GetOrdinal(string name) => ((List<string>)Columns).IndexOf(name);
            public object GetValue(int i) => Current[Columns[i]] ?? DBNull.Value;
            public bool IsDBNull(int i) => Current[Columns[i]] is null || Current[Columns[i]] is DBNull;
            public object this[int i] => GetValue(i);
            public object this[string name] => GetValue(GetOrdinal(name));

            public int GetValues(object[] values)
            {
                for (int i = 0; i < FieldCount; i++)
                    values[i] = GetValue(i);
                return FieldCount;
            }

            public bool GetBoolean(int i) => (bool)GetValue(i);
            public byte GetByte(int i) => (byte)GetValue(i);
            public char GetChar(int i) => (char)GetValue(i);
            public short GetInt16(int i) => Convert.ToInt16(GetValue(i));
            public int GetInt32(int i) => Convert.ToInt32(GetValue(i));
            public long GetInt64(int i) => Convert.ToInt64(GetValue(i));
            public float GetFloat(int i) => (float)GetValue(i);
            public double GetDouble(int i) => Convert.ToDouble(GetValue(i));
            public decimal GetDecimal(int i) => (decimal)GetValue(i);
            public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
            public Guid GetGuid(int i) => (Guid)GetValue(i);
            public string GetString(int i) => (string)GetValue(i);

            public string GetDataTypeName(int i) => GetFieldType(i).Name;
            // GetFieldType must work before Read() — use first row metadata
            public Type GetFieldType(int i)
            {
                if (_rows.Count == 0) return typeof(object);
                var colName = Columns[i];
                var val = _rows[0][colName];
                return val?.GetType() ?? typeof(object);
            }
            public DataTable? GetSchemaTable() => null;

            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
            public IDataReader GetData(int i) => throw new NotSupportedException();

            public void Dispose() => Close();
        }

        private class User { public int Id { get; set; } public string? Name { get; set; } }

        // ── Parse<T>(IDataReader) with non-DbDataReader ───────────────

        [Fact]
        public void Parse_Generic_NonDbDataReader_TriggersWrappedBasicReader()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } },
                new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } },
            });

            var results = ((IDataReader)reader).Parse<User>().ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal("Alice", results[0].Name);
            Assert.Equal("Bob", results[1].Name);
        }

        [Fact]
        public void Parse_Dynamic_NonDbDataReader_Works()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Id", 5 }, { "Name", "Charlie" } }
            });

            var results = ((IDataReader)reader).Parse().ToList();

            Assert.Single(results);
            Assert.Equal(5, (int)results[0].Id);
        }

        [Fact]
        public void Parse_ByType_NonDbDataReader_Works()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Id", 3 }, { "Name", "Dave" } }
            });

            var results = ((IDataReader)reader).Parse(typeof(User)).ToList();

            Assert.Single(results);
            Assert.Equal("Dave", ((User)results[0]).Name);
        }

        [Fact]
        public void GetRowParser_IDataReader_NonDbDataReader_Works()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Id", 7 }, { "Name", "Eve" } }
            });

            IDataReader idr = reader;
            var parser = idr.GetRowParser<User>();

            Assert.True(idr.Read());
            var user = parser(idr);
            Assert.Equal(7, user.Id);
            Assert.Equal("Eve", user.Name);
        }

        [Fact]
        public void GetRowParser_IDataReader_ByType_NonDbDataReader_Works()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Id", 9 }, { "Name", "Frank" } }
            });

            IDataReader idr = reader;
            var parser = idr.GetRowParser(typeof(User));

            Assert.True(idr.Read());
            var user = (User)parser(idr);
            Assert.Equal(9, user.Id);
        }

        // ── Parse<int> with value type (covers IsValueType branch) ────

        [Fact]
        public void Parse_ValueType_NonDbDataReader_Works()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Val", 42 } },
                new Dictionary<string, object?> { { "Val", 43 } },
            });

            var results = ((IDataReader)reader).Parse<int>().ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal(42, results[0]);
            Assert.Equal(43, results[1]);
        }

        // ── Exercise WrappedBasicReader delegate methods ───────────────

        [Fact]
        public void WrappedBasicReader_DelegatesRead_Works()
        {
            var reader = new MinimalDataReader(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 } }
            });

            // GetRowParser wraps in WrappedBasicReader
            IDataReader idr = reader;
            var parser = idr.GetRowParser<User>(typeof(User),
                startIndex: 0, length: -1, returnNullIfFirstMissing: false);

            Assert.True(idr.Read());
            var user = parser(idr);
            Assert.Equal(1, user.Id);
        }
    }
}
#endif
