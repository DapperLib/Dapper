using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Xunit;

namespace Dapper.Tests;

/// <summary>
/// Regression tests for the 2.1.86 direct-unbox gate changes (#2231, #2234): array columns into
/// interface members, and sql_variant (object-reported) columns into primitive members. These run
/// against a mock reader so every column-type shape can be pinned exactly, in particular the
/// Npgsql 7/8/10 shape where GetFieldType reports typeof(Array) rather than the concrete array type.
/// </summary>
public class TypeMappingRegressionTests
{
    internal sealed class SingleRowDataReader : DbDataReader
    {
        private readonly string[] _names;
        private readonly Type[] _types;
        private readonly object[] _values;
        private bool _read;

        public SingleRowDataReader(string[] names, Type[] types, object[] values)
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
        public override bool IsDBNull(int ordinal) => _values[ordinal] is null or DBNull;
        public override bool Read()
        {
            if (_read) return false;
            _read = true;
            return true;
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
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
        public override float GetFloat(int ordinal) => (float)_values[ordinal];
        public override Guid GetGuid(int ordinal) => (Guid)_values[ordinal];
        public override short GetInt16(int ordinal) => (short)_values[ordinal];
        public override int GetInt32(int ordinal) => (int)_values[ordinal];
        public override long GetInt64(int ordinal) => (long)_values[ordinal];
        public override string GetString(int ordinal) => (string)_values[ordinal];
        public override T GetFieldValue<T>(int ordinal) => (T)_values[ordinal];
        public override int GetValues(object[] values)
        {
            Array.Copy(_values, values, _values.Length);
            return _values.Length;
        }
        public override bool NextResult() => false;
    }

    private static TRow ParseSingleColumn<TRow>(Type columnType, object value)
    {
        using var reader = new SingleRowDataReader(["X"], [columnType], [value]);
        var parser = reader.GetRowParser<TRow>();
        reader.Read();
        return parser(reader);
    }

    private class RoListRow { public IReadOnlyList<short>? X { get; set; } }
    private class EnumerableRow { public IEnumerable<int>? X { get; set; } }
    private class ArrayRow { public short[]? X { get; set; } }
    private class BoolRow { public bool X { get; set; } }
    private class NullableBoolRow { public bool? X { get; set; } }
    private class LongRow { public long X { get; set; } }
    private class StringRow { public string? X { get; set; } }
    private class GuidRow { public Guid X { get; set; } }
    private class NullableGuidRow { public Guid? X { get; set; } }

    [Fact] // #2231: Npgsql 9.0.0-9.0.4 report the concrete array type
    public void ArrayColumn_ConcreteType_ToInterfaceMember()
    {
        var row = ParseSingleColumn<RoListRow>(typeof(short[]), new short[] { 1, 2, 3 });
        Assert.Equal(new short[] { 1, 2, 3 }, row.X);
    }

    [Fact] // #2231: Npgsql 7, 8 and 10 report array columns as typeof(Array), which does not
    // implement IReadOnlyList<T>; only the runtime value proves the match, so this shape
    // requires the runtime-checked castclass, not a static assignability test
    public void ArrayColumn_WeakType_ToInterfaceMember()
    {
        var row = ParseSingleColumn<RoListRow>(typeof(Array), new short[] { 1, 2, 3 });
        Assert.Equal(new short[] { 1, 2, 3 }, row.X);
    }

    [Fact]
    public void ArrayColumn_WeakType_ToEnumerableMember()
    {
        var row = ParseSingleColumn<EnumerableRow>(typeof(Array), new[] { 10, 20, 30 });
        Assert.Equal(new[] { 10, 20, 30 }, row.X);
    }

    [Fact]
    public void ArrayColumn_WeakType_ToArrayMember()
    {
        var row = ParseSingleColumn<ArrayRow>(typeof(Array), new short[] { 1, 2 });
        Assert.Equal(new short[] { 1, 2 }, row.X);
    }

    [Fact] // #2234: sql_variant holding an int must coerce, not blind-unbox
    public void ObjectColumn_Int32_ToBool()
        => Assert.True(ParseSingleColumn<BoolRow>(typeof(object), 1).X);

    [Fact]
    public void ObjectColumn_Int32_ToNullableBool()
        => Assert.False(ParseSingleColumn<NullableBoolRow>(typeof(object), 0).X);

    [Fact]
    public void ObjectColumn_Int32_ToLong()
        => Assert.Equal(42L, ParseSingleColumn<LongRow>(typeof(object), 42).X);

    [Fact]
    public void ObjectColumn_Int32_ToString()
        => Assert.Equal("42", ParseSingleColumn<StringRow>(typeof(object), 42).X);

    [Fact] // TypeCode.Object targets keep the direct unbox of the runtime box
    public void ObjectColumn_Guid_ToGuid()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, ParseSingleColumn<GuidRow>(typeof(object), guid).X);
    }

    [Fact]
    public void ObjectColumn_Guid_ToNullableGuid()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, ParseSingleColumn<NullableGuidRow>(typeof(object), guid).X);
    }

#if NET6_0_OR_GREATER
    private class DateOnlyRow { public DateOnly X { get; set; } }
    private class NullableTimeOnlyRow { public TimeOnly? X { get; set; } }
    private class DateTimeRow { public DateTime X { get; set; } }

    [Fact] // the #2228 conversions must survive any change to the unbox gate
    public void DateTimeColumn_ToDateOnly()
        => Assert.Equal(new DateOnly(2021, 1, 1), ParseSingleColumn<DateOnlyRow>(typeof(DateTime), new DateTime(2021, 1, 1)).X);

    [Fact] // TimeSpan and TimeOnly? are both TypeCode.Object, but a value-type target
    // under that pairing must take the flexible-convert path, never a direct unbox
    public void TimeSpanColumn_ToNullableTimeOnly()
        => Assert.Equal(new TimeOnly(3, 3, 3), ParseSingleColumn<NullableTimeOnlyRow>(typeof(TimeSpan), new TimeSpan(3, 3, 3)).X);

    [Fact]
    public void DateOnlyColumn_ToDateTime()
        => Assert.Equal(new DateTime(2021, 1, 1), ParseSingleColumn<DateTimeRow>(typeof(DateOnly), new DateOnly(2021, 1, 1)).X);
#endif
}

[Collection("SqlVariantTests")]
public sealed class SystemSqlClientSqlVariantTests : SqlVariantTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
[Collection("SqlVariantTests")]
public sealed class MicrosoftSqlClientSqlVariantTests : SqlVariantTests<MicrosoftSqlClientProvider> { }
#endif

public abstract class SqlVariantTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
{
    private class VariantRow
    {
        public bool AsBool { get; set; }
        public bool? AsNullableBool { get; set; }
        public long AsLong { get; set; }
        public string? AsString { get; set; }
        public Guid AsGuid { get; set; }
    }

    [Fact] // #2234
    public void SqlVariantColumns_CoerceIntoTypedMembers()
    {
        var guid = Guid.NewGuid();
        var row = connection.QuerySingle<VariantRow>("""
            select cast(1 as sql_variant) as [AsBool],
                   cast(0 as sql_variant) as [AsNullableBool],
                   cast(42 as sql_variant) as [AsLong],
                   cast(42 as sql_variant) as [AsString],
                   cast(@guid as sql_variant) as [AsGuid]
            """, new { guid });
        Assert.True(row.AsBool);
        Assert.False(row.AsNullableBool);
        Assert.Equal(42L, row.AsLong);
        Assert.Equal("42", row.AsString);
        Assert.Equal(guid, row.AsGuid);
    }

    private class ServerPropertyRow
    {
        public bool IsIntegratedSecurityOnly { get; set; }
    }

    [Fact] // #2234: the original report - SERVERPROPERTY returns sql_variant
    public void ServerProperty_IntoBoolMember()
    {
        var row = connection.QuerySingle<ServerPropertyRow>(
            "select SERVERPROPERTY('IsIntegratedSecurityOnly') as [IsIntegratedSecurityOnly]");
        // any value is fine; pre-fix this threw InvalidCastException
        _ = row.IsIntegratedSecurityOnly;
    }
}
