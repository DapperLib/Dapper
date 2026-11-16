using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Xunit;

namespace Dapper.Tests;

public class TypeMappingRegressionTests
{
    private class MockDataReader : DbDataReader
    {
        private readonly string[] _names;
        private readonly Type[] _types;
        private readonly object[] _values;
        private bool _read = false;

        public MockDataReader(string[] names, Type[] types, object[] values)
        {
            _names = names;
            _types = types;
            _values = values;
        }

        public override object this[int ordinal] => _values[ordinal];
        public override object this[string name] => _values[GetOrdinal(name)];
        public override int FieldCount => _names.Length;
        public override string GetName(int ordinal) => _names[ordinal];
        public override int GetOrdinal(string name) => Array.IndexOf(_names, name);
        public override Type GetFieldType(int ordinal) => _types[ordinal];
        public override object GetValue(int ordinal) => _values[ordinal];
        public override bool IsDBNull(int ordinal) => _values[ordinal] is null || _values[ordinal] is DBNull;
        public override bool Read()
        {
            if (!_read)
            {
                _read = true;
                return true;
            }
            return false;
        }

        public override bool HasRows => true;
        public override bool IsClosed => false;
        public override int RecordsAffected => 1;
        public override int Depth => 0;

        public override bool GetBoolean(int ordinal) => (bool)_values[ordinal];
        public override byte GetByte(int ordinal) => (byte)_values[ordinal];
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => (char)_values[ordinal];
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => _types[ordinal].Name;
        public override DateTime GetDateTime(int ordinal) => (DateTime)_values[ordinal];
        public override decimal GetDecimal(int ordinal) => (decimal)_values[ordinal];
        public override double GetDouble(int ordinal) => (double)_values[ordinal];
        public override IEnumerator GetEnumerator() => throw new NotImplementedException();
        public override float GetFloat(int ordinal) => (float)_values[ordinal];
        public override Guid GetGuid(int ordinal) => (Guid)_values[ordinal];
        public override short GetInt16(int ordinal) => (short)_values[ordinal];
        public override int GetInt32(int ordinal) => (int)_values[ordinal];
        public override long GetInt64(int ordinal) => (long)_values[ordinal];
        public override string GetString(int ordinal) => (string)_values[ordinal];
        public override int GetValues(object[] values)
        {
            Array.Copy(_values, values, _values.Length);
            return _values.Length;
        }
        public override bool NextResult() => false;
    }

    public sealed class TestRowSqlVariant
    {
        public bool IsLocalDB { get; set; }
        public bool? NullableBool { get; set; }
        public long BigIntValue { get; set; }
    }

    public sealed class TestRowArrayMapping
    {
        public IReadOnlyList<short> SeparatorPositions { get; set; } = [];
        public IEnumerable<int> Numbers { get; set; } = [];
    }

    [Fact]
    public void Issue2234_SqlVariant_Int32_To_Bool_Mapping()
    {
        using var reader = new MockDataReader(
            ["IsLocalDB", "NullableBool", "BigIntValue"],
            [typeof(object), typeof(object), typeof(object)],
            [1, 0, 42]
        );

        var parser = reader.GetRowParser<TestRowSqlVariant>();
        reader.Read();
        var row = parser(reader);

        Assert.True(row.IsLocalDB);
        Assert.False(row.NullableBool);
        Assert.Equal(42L, row.BigIntValue);
    }

    [Fact]
    public void Issue2231_Array_To_ReadOnlyList_And_Enumerable_Mapping()
    {
        var shorts = new short[] { 1, 2, 3 };
        var ints = new int[] { 10, 20, 30 };

        using var reader = new MockDataReader(
            ["SeparatorPositions", "Numbers"],
            [typeof(short[]), typeof(int[])],
            [shorts, ints]
        );

        var parser = reader.GetRowParser<TestRowArrayMapping>();
        reader.Read();
        var row = parser(reader);

        Assert.NotNull(row.SeparatorPositions);
        Assert.Equal([1, 2, 3], row.SeparatorPositions);
        Assert.NotNull(row.Numbers);
        Assert.Equal([10, 20, 30], row.Numbers);
    }
}
