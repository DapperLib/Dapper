using System;
using System.Data;
using System.Data.SqlTypes;
using Xunit;

namespace Dapper.Tests;

public class ColumnErrorDiagnosticTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReaderFailure_DoesNotReportPreviousColumnsValue(bool constructor)
    {
        using var reader = new FailingReader();
        Action parse;
        if (constructor)
        {
            var parser = reader.GetRowParser<ConstructorRow>();
            parse = () => parser(reader);
        }
        else
        {
            var parser = reader.GetRowParser<Row>();
            parse = () => parser(reader);
        }
        reader.Read();

        var error = Assert.Throws<DataException>(parse);
        Assert.Same(reader.Error, error.InnerException);
        Assert.StartsWith("Error parsing column 1 (Failing=", error.Message);
        Assert.DoesNotContain("previous-column-value", error.Message);
    }

    [Fact]
    public void TypedReaderFailure_DoesNotReportPreviousColumnsValue()
    {
        using var reader = new TypedReader { FailRead = true };
        var parser = reader.GetRowParser<TypedRow>();
        reader.Read();

        var error = Assert.Throws<DataException>(() => parser(reader));
        Assert.Same(reader.Error, error.InnerException);
        Assert.StartsWith("Error parsing column 1 (Failing=n/a - ", error.Message);
        Assert.DoesNotContain("previous-column-value", error.Message);
    }

    [Fact]
    public void TypedSetterFailure_ReportsCurrentColumnsValue()
    {
        using var reader = new TypedReader();
        var parser = reader.GetRowParser<TypedRow>();
        reader.Read();

        var error = Assert.Throws<DataException>(() => parser(reader));
        Assert.IsType<InvalidOperationException>(error.InnerException);
        Assert.Contains("Failing=42 - SqlDecimal", error.Message);
        Assert.DoesNotContain("previous-column-value", error.Message);
    }

    [Fact]
    public void ConversionFailure_ReportsCurrentColumnsValue()
    {
        using var reader = new TypeMappingRegressionTests.SingleRowDataReader(
            ["Previous", "Failing"], [typeof(string), typeof(string)], ["previous-column-value", "invalid-date"]);
        var parser = reader.GetRowParser<Row>();
        reader.Read();

        var error = Assert.Throws<DataException>(() => parser(reader));
        Assert.Contains("Failing=invalid-date - String", error.Message);
        Assert.DoesNotContain("previous-column-value", error.Message);
    }

    private class Row
    {
        public string? Previous { get; set; }
        public DateTime Failing { get; set; }
    }

    private record ConstructorRow(string Previous, DateTime Failing);

    private class TypedRow
    {
        public string? Previous { get; set; }
        public SqlDecimal Failing { get => default; set => throw new InvalidOperationException("setter failed"); }
    }

    private sealed class TypedReader() : TypeMappingRegressionTests.SingleRowDataReader(
        ["Previous", "Failing"], [typeof(string), typeof(SqlDecimal)], ["previous-column-value", new SqlDecimal(42)])
    {
        public bool FailRead { get; set; }
        public InvalidCastException Error { get; } = new("provider could not read the decimal");
        public override T GetFieldValue<T>(int ordinal) => FailRead && ordinal == 1 ? throw Error : base.GetFieldValue<T>(ordinal);
    }

    private sealed class FailingReader() : TypeMappingRegressionTests.SingleRowDataReader(
        ["Previous", "Failing"], [typeof(string), typeof(DateTime)], ["previous-column-value", new DateTime(2026, 1, 1)])
    {
        public InvalidCastException Error { get; } = new("provider could not read the date");
        public override object this[int ordinal] => GetValue(ordinal);
        public override object GetValue(int ordinal) => ordinal == 1 ? throw Error : base.GetValue(ordinal);
    }
}
