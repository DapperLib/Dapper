using System;
using System.Data;
using Xunit;

namespace Dapper.Tests;

public class NullableFieldTypeTests
{
    private class ConstructorRow<T>
    {
        public T Value { get; }
        public ConstructorRow(T value) => Value = value;
    }
    private class PropertyRow<T> { public T Value { get; set; } = default!; }
    private struct StructRow<T> { public T Value { get; set; } }

    private static T Read<T>(Type columnType, object value, int shape)
    {
        using var reader = new TypeMappingRegressionTests.SingleRowDataReader(["Value"], [columnType], [value]);
        switch (shape)
        {
            case 0:
                var ctorParser = reader.GetRowParser<ConstructorRow<T>>();
                Assert.True(reader.Read());
                return ctorParser(reader).Value;
            case 1:
                var propertyParser = reader.GetRowParser<PropertyRow<T>>();
                Assert.True(reader.Read());
                return propertyParser(reader).Value;
            default:
                var structParser = reader.GetRowParser<StructRow<T>>();
                Assert.True(reader.Read());
                return structParser(reader).Value;
        }
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    public void DateTimeValue_Issue1498(int shape, bool nullableColumn, bool dbNull)
    {
        DateTime? value = dbNull ? null : new DateTime(2026, 1, 2);
        Assert.Equal(value, Read<DateTime?>(nullableColumn ? typeof(DateTime?) : typeof(DateTime), (object?)value ?? DBNull.Value, shape));
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    public void IntegerValue_Issue1498(int shape, bool nullableColumn, bool dbNull)
    {
        int? value = dbNull ? null : 42;
        Assert.Equal(value, Read<int?>(nullableColumn ? typeof(int?) : typeof(int), (object?)value ?? DBNull.Value, shape));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonNullableTarget_Issue1498(bool nullableColumn)
    {
        Assert.Equal(42, Read<int>(nullableColumn ? typeof(int?) : typeof(int), 42, 0));
        var date = new DateTime(2026, 1, 2);
        Assert.Equal(date, Read<DateTime>(nullableColumn ? typeof(DateTime?) : typeof(DateTime), date, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NullableEnumColumn_Issue1498(int shape)
    {
        Assert.Equal(DayOfWeek.Friday, Read<DayOfWeek?>(typeof(int?), 5, shape));
        Assert.Null(Read<DayOfWeek?>(typeof(int?), DBNull.Value, shape));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GuidAndCharValues_Issue1498(int shape)
    {
        var guid = new Guid("071f4b04-fda0-4175-90df-659e7df3d653");
        Assert.Equal(guid, Read<Guid?>(typeof(Guid?), guid, shape));
        Assert.Null(Read<Guid?>(typeof(Guid?), DBNull.Value, shape));
        Assert.Equal('x', Read<char?>(typeof(char?), 'x', shape));
        Assert.Null(Read<char?>(typeof(char?), DBNull.Value, shape));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonNullableGuidAndEnumTargets_Issue1498(bool nullableColumn)
    {
        var guid = new Guid("071f4b04-fda0-4175-90df-659e7df3d653");
        Assert.Equal(guid, Read<Guid>(nullableColumn ? typeof(Guid?) : typeof(Guid), guid, 0));
        Assert.Equal(DayOfWeek.Friday, Read<DayOfWeek>(nullableColumn ? typeof(int?) : typeof(int), 5, 0));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RegisteredHandler_Issue1498(bool nullableColumn, bool dbNull)
    {
        var handler = new NumberHandler();
        SqlMapper.ResetTypeHandlers();
        try
        {
            SqlMapper.AddTypeHandler(handler);
            var value = Read<Number?>(nullableColumn ? typeof(int?) : typeof(int), dbNull ? DBNull.Value : (object)42, 0);
            if (dbNull) Assert.Null(value);
            else Assert.Equal(42, value!.Value.Value);
            Assert.Equal(dbNull ? 0 : 1, handler.Calls);
        }
        finally
        {
            SqlMapper.ResetTypeHandlers();
        }
    }

    [Fact]
    public void IncompatibleConstructorStillRejected_Issue1498()
    {
        var map = new DefaultTypeMap(typeof(ConstructorRow<int?>));
        Assert.Null(map.FindConstructor(["Value"], [typeof(double?)]));
        Assert.Null(map.FindConstructor(["Other"], [typeof(int?)]));
    }

    private readonly struct Number(int value) { public int Value { get; } = value; }
    private class NumberHandler : SqlMapper.TypeHandler<Number>
    {
        public int Calls;
        public override Number Parse(object value) { Calls++; return new Number((int)value); }
        public override void SetValue(IDbDataParameter parameter, Number value) => parameter.Value = value.Value;
    }
}
