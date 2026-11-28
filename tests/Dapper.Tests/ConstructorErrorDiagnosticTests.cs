using System;
using System.Data;
using Xunit;

namespace Dapper.Tests;

public class ConstructorErrorDiagnosticTests
{
    private sealed class IncompatibleColumn
    {
        public IncompatibleColumn(int DataType, int? CharacterMaximumLength, short? NumericPrecisionRadix, byte? NumericPrecision) { }
    }

    private sealed class CompatibleColumn
    {
        public string DataType { get; }
        public int? CharacterMaximumLength { get; }
        public short? NumericPrecisionRadix { get; }
        public byte? NumericPrecision { get; }

        public CompatibleColumn(string DataType, int? CharacterMaximumLength, short? NumericPrecisionRadix, byte? NumericPrecision)
        {
            this.DataType = DataType;
            this.CharacterMaximumLength = CharacterMaximumLength;
            this.NumericPrecisionRadix = NumericPrecisionRadix;
            this.NumericPrecision = NumericPrecision;
        }
    }

    private static DataTable CreateColumns(bool nullValues)
    {
        var table = new DataTable();
        table.Columns.Add("DataType", typeof(string));
        table.Columns.Add("CharacterMaximumLength", typeof(int));
        table.Columns.Add("NumericPrecisionRadix", typeof(short));
        table.Columns.Add("NumericPrecision", typeof(byte));
        table.Rows.Add("varchar", nullValues ? DBNull.Value : (object)42,
            nullValues ? DBNull.Value : (object)(short)10,
            nullValues ? DBNull.Value : (object)(byte)5);
        return table;
    }

    [Fact]
    public void ConstructorErrorDescribesColumnsRatherThanAnExactConstructorSignature()
    {
        using var table = CreateColumns(false);
        using var reader = table.CreateDataReader();
        var error = Assert.Throws<InvalidOperationException>(() => reader.GetRowParser<IncompatibleColumn>());

        Assert.Contains("column signature (System.String DataType, System.Int32 CharacterMaximumLength, System.Int16 NumericPrecisionRadix, System.Byte NumericPrecision)", error.Message);
        Assert.Contains(typeof(IncompatibleColumn).FullName!, error.Message);
        Assert.Contains("Nullable constructor parameters are supported.", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullableConstructorParametersAlreadyMatchNonNullableColumnTypes(bool nullValues)
    {
        using var table = CreateColumns(nullValues);
        using var reader = table.CreateDataReader();
        var parser = reader.GetRowParser<CompatibleColumn>();
        Assert.True(reader.Read());
        var row = parser(reader);

        Assert.Equal("varchar", row.DataType);
        Assert.Equal(nullValues ? (int?)null : 42, row.CharacterMaximumLength);
        Assert.Equal(nullValues ? (short?)null : (short)10, row.NumericPrecisionRadix);
        Assert.Equal(nullValues ? (byte?)null : (byte)5, row.NumericPrecision);
    }
}
