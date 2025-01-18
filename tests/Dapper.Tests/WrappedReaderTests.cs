using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using Xunit.Abstractions;

namespace Dapper.Tests;

public class WrappedReaderTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void DbWrappedReader_Dispose_DoesNotThrow()
    {
        var reader = new DbWrappedReader(new DummyDbCommand(), new ThrowOnCloseDbDataReader(testOutputHelper));
        reader.Dispose();
    }

#if !NETFRAMEWORK
    [Fact]
    public async System.Threading.Tasks.Task DbWrappedReader_DisposeAsync_DoesNotThrow()
    {
        var reader = new DbWrappedReader(new DummyDbCommand(), new ThrowOnCloseDbDataReader(testOutputHelper));
        await reader.DisposeAsync();
    }
#endif

    [Fact]
    public void WrappedBasicReader_Dispose_DoesNotThrow()
    {
        var reader = new WrappedBasicReader(new ThrowOnCloseIDataReader());
        reader.Dispose();
    }

#if !NETFRAMEWORK
    [Fact]
    public async System.Threading.Tasks.Task WrappedBasicReader_DisposeAsync_DoesNotThrow()
    {
        var reader = new WrappedBasicReader(new ThrowOnCloseIDataReader());
        await reader.DisposeAsync();
    }
#endif

    private class DummyDbCommand : DbCommand
    {
        public override void Cancel() => throw new NotSupportedException();
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() => throw new NotSupportedException();

#pragma warning disable CS8765 // nullability of value
        public override string CommandText { get; set; } = "";
#pragma warning restore CS8765 // nullability of value
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
        protected override DbTransaction? DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }
        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private class DummyDbException(string message) : DbException(message);

    private class ThrowOnCloseDbDataReader(ITestOutputHelper testOutputHelper) : DbDataReader
    {
        // This is basically what SqlClient does, see https://github.com/dotnet/SqlClient/blob/v5.2.1/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/SqlClient/SqlDataReader.cs#L835-L849
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    Close();
                }
                base.Dispose(disposing);
            }
            catch (DbException e)
            {
                testOutputHelper.WriteLine($"Ignored exception when disposing {e}");
            }
        }

        public override void Close() => throw new DummyDbException("Exception during Close()");

        public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
        public override byte GetByte(int ordinal) => throw new NotSupportedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override char GetChar(int ordinal) => throw new NotSupportedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override string GetDataTypeName(int ordinal) => throw new NotSupportedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
        public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
        public override double GetDouble(int ordinal) => throw new NotSupportedException();
        public override Type GetFieldType(int ordinal) => throw new NotSupportedException();
        public override float GetFloat(int ordinal) => throw new NotSupportedException();
        public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
        public override short GetInt16(int ordinal) => throw new NotSupportedException();
        public override int GetInt32(int ordinal) => throw new NotSupportedException();
        public override long GetInt64(int ordinal) => throw new NotSupportedException();
        public override string GetName(int ordinal) => throw new NotSupportedException();
        public override int GetOrdinal(string name) => throw new NotSupportedException();
        public override string GetString(int ordinal) => throw new NotSupportedException();
        public override object GetValue(int ordinal) => throw new NotSupportedException();
        public override int GetValues(object[] values) => throw new NotSupportedException();
        public override bool IsDBNull(int ordinal) => throw new NotSupportedException();
        public override int FieldCount => throw new NotSupportedException();
        public override object this[int ordinal] => throw new NotSupportedException();
        public override object this[string name] => throw new NotSupportedException();
        public override int RecordsAffected => throw new NotSupportedException();
        public override bool HasRows => throw new NotSupportedException();
        public override bool IsClosed => throw new NotSupportedException();
        public override bool NextResult() => throw new NotSupportedException();
        public override bool Read() => throw new NotSupportedException();
        public override int Depth => throw new NotSupportedException();
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
    }

    private class ThrowOnCloseIDataReader : IDataReader
    {
        public void Dispose()
        {
            // Assume that IDataReader Dispose implementation does not throw
        }

        public void Close() => throw new DummyDbException("Exception during Close()");

        public bool GetBoolean(int i) => throw new NotSupportedException();
        public byte GetByte(int i) => throw new NotSupportedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public char GetChar(int i) => throw new NotSupportedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public string GetDataTypeName(int i) => throw new NotSupportedException();
        public DateTime GetDateTime(int i) => throw new NotSupportedException();
        public decimal GetDecimal(int i) => throw new NotSupportedException();
        public double GetDouble(int i) => throw new NotSupportedException();
        public Type GetFieldType(int i) => throw new NotSupportedException();
        public float GetFloat(int i) => throw new NotSupportedException();
        public Guid GetGuid(int i) => throw new NotSupportedException();
        public short GetInt16(int i) => throw new NotSupportedException();
        public int GetInt32(int i) => throw new NotSupportedException();
        public long GetInt64(int i) => throw new NotSupportedException();
        public string GetName(int i) => throw new NotSupportedException();
        public int GetOrdinal(string name) => throw new NotSupportedException();
        public string GetString(int i) => throw new NotSupportedException();
        public object GetValue(int i) => throw new NotSupportedException();
        public int GetValues(object[] values) => throw new NotSupportedException();
        public bool IsDBNull(int i) => throw new NotSupportedException();
        public int FieldCount => throw new NotSupportedException();
        public object this[int i] => throw new NotSupportedException();
        public object this[string name] => throw new NotSupportedException();
        public DataTable? GetSchemaTable() => throw new NotSupportedException();
        public bool NextResult() => throw new NotSupportedException();
        public bool Read() => throw new NotSupportedException();
        public int Depth => throw new NotSupportedException();
        public bool IsClosed => throw new NotSupportedException();
        public int RecordsAffected => throw new NotSupportedException();
    }
}
