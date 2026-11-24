using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Dapper.Tests;

public class GridReaderExceptionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReadUnbufferedAsync_PreservesReadException(bool cancel, bool nextResultFails)
    {
        Exception expected = cancel ? new OperationCanceledException() : new InvalidOperationException("read failed");
        using var reader = new TestReader { ReadError = expected, NextResultError = nextResultFails ? new InvalidOperationException("next result failed") : null };
        using var grid = new TestGridReader(reader);

        var actual = await Record.ExceptionAsync(async () =>
        {
            await foreach (var value in grid.ReadUnbufferedAsync<int>()) { }
        });

        Assert.Same(expected, actual);
        Assert.Equal(1, reader.NextResultCalls);
    }

    [Fact]
    public async Task ReadUnbufferedAsync_PreservesConversionException()
    {
        using var reader = new TestReader { Value = "not an integer", NextResultError = new InvalidOperationException("next result failed") };
        using var grid = new TestGridReader(reader);

        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            await foreach (var value in grid.ReadUnbufferedAsync<int>()) { }
        });
        Assert.Equal(1, reader.NextResultCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadUnbufferedAsync_PropagatesNextResultException(bool stopEarly)
    {
        var expected = new InvalidOperationException("next result failed");
        using var reader = new TestReader { NextResultError = expected };
        using var grid = new TestGridReader(reader);

        var actual = await Record.ExceptionAsync(async () =>
        {
            await foreach (var value in grid.ReadUnbufferedAsync<int>())
            {
                if (stopEarly) break;
            }
        });

        Assert.Same(expected, actual);
        Assert.Equal(1, reader.NextResultCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadUnbufferedAsync_AdvancesToNextGrid(bool stopEarly)
    {
        using var reader = new TestReader();
        using var grid = new TestGridReader(reader);
        for (int i = 0; i < 2; i++)
        {
            int count = 0;
            await foreach (var value in grid.ReadUnbufferedAsync<int>())
            {
                Assert.Equal(42, value);
                count++;
                if (stopEarly) break;
            }
            Assert.Equal(1, count);
        }
        Assert.Equal(2, reader.NextResultCalls);
        Assert.True(reader.IsClosed);
    }

    private sealed class TestGridReader(DbDataReader reader) : SqlMapper.GridReader(
        null!, reader,
        new SqlMapper.Identity("issue2092", CommandType.Text, new SqlConnection(), typeof(int), null));

    private sealed class TestReader : DbDataReader
    {
        public Exception? ReadError { get; set; }
        public Exception? NextResultError { get; set; }
        public object Value { get; set; } = 42;
        public int NextResultCalls { get; private set; }
        private bool read, closed;

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (ReadError is not null) throw ReadError;
            return Read();
        }

        public override async Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            NextResultCalls++;
            if (NextResultError is not null) throw NextResultError;
            read = false;
            return NextResultCalls < 2;
        }

        public override bool Read()
        {
            if (read) return false;
            read = true;
            return true;
        }

        public override void Close() => closed = true;
        protected override void Dispose(bool disposing) { if (disposing) Close(); base.Dispose(disposing); }
        public override bool IsClosed => closed;
        public override int FieldCount => 1;
        public override string GetName(int ordinal) => "Value";
        public override Type GetFieldType(int ordinal) => Value.GetType();
        public override object GetValue(int ordinal) => Value;
        public override int GetValues(object[] values) { values[0] = Value; return 1; }
        public override bool IsDBNull(int ordinal) => Value is DBNull;
        public override object this[int ordinal] => Value;
        public override object this[string name] => Value;
        public override int GetOrdinal(string name) => 0;
        public override bool HasRows => true;
        public override int RecordsAffected => -1;
        public override int Depth => 0;
        public override bool NextResult() => throw new NotSupportedException();
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override bool GetBoolean(int ordinal) => (bool)Value;
        public override byte GetByte(int ordinal) => (byte)Value;
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override char GetChar(int ordinal) => (char)Value;
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override DateTime GetDateTime(int ordinal) => (DateTime)Value;
        public override decimal GetDecimal(int ordinal) => (decimal)Value;
        public override double GetDouble(int ordinal) => (double)Value;
        public override float GetFloat(int ordinal) => (float)Value;
        public override Guid GetGuid(int ordinal) => (Guid)Value;
        public override short GetInt16(int ordinal) => (short)Value;
        public override int GetInt32(int ordinal) => (int)Value;
        public override long GetInt64(int ordinal) => (long)Value;
        public override string GetString(int ordinal) => (string)Value;
    }
}
