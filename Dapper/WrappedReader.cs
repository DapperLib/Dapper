using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    internal sealed class DisposedReader : DbDataReader
    {
        internal static readonly DisposedReader Instance = new DisposedReader();
        private DisposedReader() { }
        public override int Depth => 0;
        public override int FieldCount => 0;
        public override bool IsClosed => true;
        public override bool HasRows => false;
        public override int RecordsAffected => -1;
        public override int VisibleFieldCount => 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowDisposed<T>() => throw new ObjectDisposedException(nameof(DbDataReader));
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async static Task<T> ThrowDisposedAsync<T>()
        {
            var result = ThrowDisposed<T>();
            await Task.Yield(); // will never hit this - already thrown and handled
            return result;
        }
        public override void Close() { }
        public override DataTable GetSchemaTable() => ThrowDisposed<DataTable>();
        public override object InitializeLifetimeService() => ThrowDisposed<object>();
        protected override void Dispose(bool disposing) { }
        public override bool GetBoolean(int ordinal) => ThrowDisposed<bool>();
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => ThrowDisposed<long>();
        public override float GetFloat(int ordinal) => ThrowDisposed<float>();
        public override short GetInt16(int ordinal) => ThrowDisposed<short>();
        public override byte GetByte(int ordinal) => ThrowDisposed<byte>();
        public override char GetChar(int ordinal) => ThrowDisposed<char>();
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => ThrowDisposed<long>();
        public override string GetDataTypeName(int ordinal) => ThrowDisposed<string>();
        public override DateTime GetDateTime(int ordinal) => ThrowDisposed<DateTime>();
        protected override DbDataReader GetDbDataReader(int ordinal) => ThrowDisposed<DbDataReader>();
        public override decimal GetDecimal(int ordinal) => ThrowDisposed<decimal>();
        public override double GetDouble(int ordinal) => ThrowDisposed<double>();
        public override IEnumerator GetEnumerator() => ThrowDisposed<IEnumerator>();
        public override Type GetFieldType(int ordinal) => ThrowDisposed<Type>();
        public override T GetFieldValue<T>(int ordinal) => ThrowDisposed<T>();
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => ThrowDisposedAsync<T>();
        public override Guid GetGuid(int ordinal) => ThrowDisposed<Guid>();
        public override int GetInt32(int ordinal) => ThrowDisposed<int>();
        public override long GetInt64(int ordinal) => ThrowDisposed<long>();
        public override string GetName(int ordinal) => ThrowDisposed<string>();
        public override int GetOrdinal(string name) => ThrowDisposed<int>();
        public override Type GetProviderSpecificFieldType(int ordinal) => ThrowDisposed<Type>();
        public override object GetProviderSpecificValue(int ordinal) => ThrowDisposed<object>();
        public override int GetProviderSpecificValues(object[] values) => ThrowDisposed<int>();
        public override Stream GetStream(int ordinal) => ThrowDisposed<Stream>();
        public override string GetString(int ordinal) => ThrowDisposed<string>();
        public override TextReader GetTextReader(int ordinal) => ThrowDisposed<TextReader>();
        public override object GetValue(int ordinal) => ThrowDisposed<object>();
        public override int GetValues(object[] values) => ThrowDisposed<int>();
        public override bool IsDBNull(int ordinal) => ThrowDisposed<bool>();
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => ThrowDisposedAsync<bool>();
        public override bool NextResult() => ThrowDisposed<bool>();
        public override bool Read() => ThrowDisposed<bool>();
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => ThrowDisposedAsync<bool>();
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => ThrowDisposedAsync<bool>();
        public override object this[int ordinal] => ThrowDisposed<object>();
        public override object this[string name] => ThrowDisposed<object>();
    }

    internal static class WrappedReader
    {
        // the purpose of wrapping here is to allow closing a reader to *also* close
        // the command, without having to explicitly hand the command back to the
        // caller; what that actually looks like depends on what we get: if we are
        // given a DbDataReader, we will surface a DbDataReader; if we are given
        // a raw IDataReader, we will surface that; and if null: null
        public static IDataReader Create(IDbCommand cmd, IDataReader reader)
        {
            if (cmd == null) return reader; // no need to wrap if no command

            if (reader is DbDataReader dbr) return new DbWrappedReader(cmd, dbr);
            if (reader != null) return new BasicWrappedReader(cmd, reader);
            cmd.Dispose();
            return null; // GIGO
        }
        public static DbDataReader Create(IDbCommand cmd, DbDataReader reader)
        {
            if (cmd == null) return reader; // no need to wrap if no command

            if (reader != null) return new DbWrappedReader(cmd, reader);
            cmd.Dispose();
            return null; // GIGO
        }
    }
    internal sealed class DbWrappedReader : DbDataReader, IWrappedDataReader
    {
        private DbDataReader _reader;
        private IDbCommand _cmd;

        IDataReader IWrappedDataReader.Reader => _reader;

        IDbCommand IWrappedDataReader.Command => _cmd;

        public DbWrappedReader(IDbCommand cmd, DbDataReader reader)
        {
            _cmd = cmd;
            _reader = reader;
        }

        public override bool HasRows => _reader.HasRows;

        public override void Close() => _reader.Close();
        public override DataTable GetSchemaTable() => _reader.GetSchemaTable();
        public override object InitializeLifetimeService() => _reader.InitializeLifetimeService();

        public override int Depth => _reader.Depth;

        public override bool IsClosed => _reader.IsClosed;

        public override bool NextResult() => _reader.NextResult();

        public override bool Read() => _reader.Read();

        public override int RecordsAffected => _reader.RecordsAffected;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Close();
                _reader.Dispose();
                _reader = DisposedReader.Instance; // all future ops are no-ops
                _cmd?.Dispose();
                _cmd = null;
            }
        }

        public override int FieldCount => _reader.FieldCount;

        public override bool GetBoolean(int i) => _reader.GetBoolean(i);

        public override byte GetByte(int i) => _reader.GetByte(i);

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        public override char GetChar(int i) => _reader.GetChar(i);

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) =>
            _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        public override string GetDataTypeName(int i) => _reader.GetDataTypeName(i);

        public override DateTime GetDateTime(int i) => _reader.GetDateTime(i);

        public override decimal GetDecimal(int i) => _reader.GetDecimal(i);

        public override double GetDouble(int i) => _reader.GetDouble(i);

        public override Type GetFieldType(int i) => _reader.GetFieldType(i);

        public override float GetFloat(int i) => _reader.GetFloat(i);

        public override Guid GetGuid(int i) => _reader.GetGuid(i);

        public override short GetInt16(int i) => _reader.GetInt16(i);

        public override int GetInt32(int i) => _reader.GetInt32(i);

        public override long GetInt64(int i) => _reader.GetInt64(i);

        public override string GetName(int i) => _reader.GetName(i);

        public override int GetOrdinal(string name) => _reader.GetOrdinal(name);

        public override string GetString(int i) => _reader.GetString(i);

        public override object GetValue(int i) => _reader.GetValue(i);

        public override int GetValues(object[] values) => _reader.GetValues(values);

        public override bool IsDBNull(int i) => _reader.IsDBNull(i);

        public override object this[string name] => _reader[name];

        public override object this[int i] => _reader[i];

        public override T GetFieldValue<T>(int ordinal) => _reader.GetFieldValue<T>(ordinal);
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => _reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        public override IEnumerator GetEnumerator() => _reader.GetEnumerator();
        public override Type GetProviderSpecificFieldType(int ordinal) => _reader.GetProviderSpecificFieldType(ordinal);
        public override object GetProviderSpecificValue(int ordinal) => _reader.GetProviderSpecificValue(ordinal);
        public override int GetProviderSpecificValues(object[] values) => _reader.GetProviderSpecificValues(values);
        public override Stream GetStream(int ordinal) => _reader.GetStream(ordinal);
        public override TextReader GetTextReader(int ordinal) => _reader.GetTextReader(ordinal);
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => _reader.IsDBNullAsync(ordinal, cancellationToken);
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => _reader.NextResultAsync(cancellationToken);
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => _reader.ReadAsync(cancellationToken);
        public override int VisibleFieldCount => _reader.VisibleFieldCount;
        protected override DbDataReader GetDbDataReader(int ordinal) => (((IDataReader)_reader).GetData(ordinal) as DbDataReader) ?? throw new NotSupportedException();
    }

    internal class BasicWrappedReader : IWrappedDataReader
    {
        private IDataReader _reader;
        private IDbCommand _cmd;

        IDataReader IWrappedDataReader.Reader => _reader;

        IDbCommand IWrappedDataReader.Command => _cmd;

        public BasicWrappedReader(IDbCommand cmd, IDataReader reader)
        {
            _cmd = cmd;
            _reader = reader;
        }

        void IDataReader.Close() => _reader.Close();

        int IDataReader.Depth => _reader.Depth;

        DataTable IDataReader.GetSchemaTable() => _reader.GetSchemaTable();

        bool IDataReader.IsClosed => _reader.IsClosed;

        bool IDataReader.NextResult() => _reader.NextResult();

        bool IDataReader.Read() => _reader.Read();

        int IDataReader.RecordsAffected => _reader.RecordsAffected;

        void IDisposable.Dispose()
        {
            _reader.Close();
            _reader.Dispose();
            _reader = DisposedReader.Instance;
            _cmd?.Dispose();
            _cmd = null;
        }

        int IDataRecord.FieldCount => _reader.FieldCount;

        bool IDataRecord.GetBoolean(int i) => _reader.GetBoolean(i);

        byte IDataRecord.GetByte(int i) => _reader.GetByte(i);

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        char IDataRecord.GetChar(int i) => _reader.GetChar(i);

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) =>
            _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        IDataReader IDataRecord.GetData(int i) => _reader.GetData(i);

        string IDataRecord.GetDataTypeName(int i) => _reader.GetDataTypeName(i);

        DateTime IDataRecord.GetDateTime(int i) => _reader.GetDateTime(i);

        decimal IDataRecord.GetDecimal(int i) => _reader.GetDecimal(i);

        double IDataRecord.GetDouble(int i) => _reader.GetDouble(i);

        Type IDataRecord.GetFieldType(int i) => _reader.GetFieldType(i);

        float IDataRecord.GetFloat(int i) => _reader.GetFloat(i);

        Guid IDataRecord.GetGuid(int i) => _reader.GetGuid(i);

        short IDataRecord.GetInt16(int i) => _reader.GetInt16(i);

        int IDataRecord.GetInt32(int i) => _reader.GetInt32(i);

        long IDataRecord.GetInt64(int i) => _reader.GetInt64(i);

        string IDataRecord.GetName(int i) => _reader.GetName(i);

        int IDataRecord.GetOrdinal(string name) => _reader.GetOrdinal(name);

        string IDataRecord.GetString(int i) => _reader.GetString(i);

        object IDataRecord.GetValue(int i) => _reader.GetValue(i);

        int IDataRecord.GetValues(object[] values) => _reader.GetValues(values);

        bool IDataRecord.IsDBNull(int i) => _reader.IsDBNull(i);

        object IDataRecord.this[string name] => _reader[name];

        object IDataRecord.this[int i] => _reader[i];
    }
}
