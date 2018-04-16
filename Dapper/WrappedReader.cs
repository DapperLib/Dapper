using System;
using System.Data;

namespace Dapper
{
    internal class WrappedReader : IWrappedDataReader
    {
        private IDataReader reader;
        private IDbCommand cmd;

        public IDataReader Reader
        {
            get
            {
                var tmp = reader;
                if (tmp == null) throw new ObjectDisposedException(GetType().Name);
                return tmp;
            }
        }

        IDbCommand IWrappedDataReader.Command
        {
            get
            {
                var tmp = cmd;
                if (tmp == null) throw new ObjectDisposedException(GetType().Name);
                return tmp;
            }
        }

        public WrappedReader(IDbCommand cmd, IDataReader reader)
        {
            this.cmd = cmd;
            this.reader = reader;
        }

        void IDataReader.Close() => reader?.Close();

        int IDataReader.Depth => Reader.Depth;

        DataTable IDataReader.GetSchemaTable() => Reader.GetSchemaTable();

        bool IDataReader.IsClosed => reader?.IsClosed ?? true;

        bool IDataReader.NextResult() => Reader.NextResult();

        bool IDataReader.Read() => Reader.Read();

        int IDataReader.RecordsAffected => Reader.RecordsAffected;

        void IDisposable.Dispose()
        {
            reader?.Close();
            reader?.Dispose();
            reader = null;
            cmd?.Dispose();
            cmd = null;
        }

        int IDataRecord.FieldCount => Reader.FieldCount;

        bool IDataRecord.GetBoolean(int i) => Reader.GetBoolean(i);

        byte IDataRecord.GetByte(int i) => Reader.GetByte(i);

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            Reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        char IDataRecord.GetChar(int i) => Reader.GetChar(i);

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) =>
            Reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        IDataReader IDataRecord.GetData(int i) => Reader.GetData(i);

        string IDataRecord.GetDataTypeName(int i) => Reader.GetDataTypeName(i);

        DateTime IDataRecord.GetDateTime(int i) => Reader.GetDateTime(i);

        decimal IDataRecord.GetDecimal(int i) => Reader.GetDecimal(i);

        double IDataRecord.GetDouble(int i) => Reader.GetDouble(i);

        Type IDataRecord.GetFieldType(int i) => Reader.GetFieldType(i);

        float IDataRecord.GetFloat(int i) => Reader.GetFloat(i);

        Guid IDataRecord.GetGuid(int i) => Reader.GetGuid(i);

        short IDataRecord.GetInt16(int i) => Reader.GetInt16(i);

        int IDataRecord.GetInt32(int i) => Reader.GetInt32(i);

        long IDataRecord.GetInt64(int i) => Reader.GetInt64(i);

        string IDataRecord.GetName(int i) => Reader.GetName(i);

        int IDataRecord.GetOrdinal(string name) => Reader.GetOrdinal(name);

        string IDataRecord.GetString(int i) => Reader.GetString(i);

        object IDataRecord.GetValue(int i) => Reader.GetValue(i);

        int IDataRecord.GetValues(object[] values) => Reader.GetValues(values);

        bool IDataRecord.IsDBNull(int i) => Reader.IsDBNull(i);

        object IDataRecord.this[string name] => Reader[name];

        object IDataRecord.this[int i] => Reader[i];
    }
}
