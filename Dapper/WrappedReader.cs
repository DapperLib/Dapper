using System;
using System.Data;
using System.Collections;

#if COREFX
using IDbCommand = System.Data.Common.DbCommand;
using IDataReader = System.Data.Common.DbDataReader;
#endif

namespace Dapper
{
#if COREFX
    internal class WrappedReader : WrappedDataReader
    {
        private IDbCommand cmd;
        private IDataReader reader;

        public override IEnumerator GetEnumerator()
        {
            return Reader.GetEnumerator();
        }
        public WrappedReader(IDbCommand cmd, IDataReader reader)
        {
            this.cmd = cmd;
            this.reader = reader;
        }
        public override IDataReader Reader
        {
            get
            {
                var tmp = reader;
                if (tmp == null) throw new ObjectDisposedException(this.GetType().Name);
                return tmp;
            }
        }
        public override IDbCommand Command
        {
            get
            {
                var tmp = cmd;
                if (tmp == null) throw new ObjectDisposedException(this.GetType().Name);
                return tmp;
            }
        }

        public override int Depth => Reader.Depth;

        public override bool IsClosed => reader?.IsClosed ?? true;

        public override bool HasRows => Reader.HasRows;

        public override bool NextResult() => Reader.NextResult();

        public override bool Read() => Reader.Read();

        public override int RecordsAffected => Reader.RecordsAffected;


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                reader?.Dispose();
                reader = null;
                cmd?.Dispose();
                cmd = null;
            }
            base.Dispose(disposing);
        }

        public override int FieldCount => Reader.FieldCount;

        public override bool GetBoolean(int i) => Reader.GetBoolean(i);

        public override byte GetByte(int i) => Reader.GetByte(i);

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return Reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public override char GetChar(int i) => Reader.GetChar(i);

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return Reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        protected override IDataReader GetDbDataReader(int ordinal) => Reader.GetData(ordinal);

        public override string GetDataTypeName(int i) => Reader.GetDataTypeName(i);

        public override DateTime GetDateTime(int i) => Reader.GetDateTime(i);

        public override decimal GetDecimal(int i) => Reader.GetDecimal(i);

        public override double GetDouble(int i) => Reader.GetDouble(i);

        public override Type GetFieldType(int i) => Reader.GetFieldType(i);

        public override float GetFloat(int i) => Reader.GetFloat(i);

        public override Guid GetGuid(int i) => Reader.GetGuid(i);

        public override short GetInt16(int i) => Reader.GetInt16(i);

        public override int GetInt32(int i) => Reader.GetInt32(i);

        public override long GetInt64(int i) => Reader.GetInt64(i);

        public override string GetName(int i) => Reader.GetName(i);

        public override int GetOrdinal(string name) => Reader.GetOrdinal(name);

        public override string GetString(int i) => Reader.GetString(i);

        public override object GetValue(int i) => Reader.GetValue(i);

        public override int GetValues(object[] values) => Reader.GetValues(values);

        public override bool IsDBNull(int i) => Reader.IsDBNull(i);

        public override object this[string name] => Reader[name];

        public override object this[int i] => Reader[i];
    }
#else

    internal class WrappedReader : IDataReader, IWrappedDataReader
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

        void IDataReader.Close()
        {
            reader?.Close();
        }

        int IDataReader.Depth => Reader.Depth;

        DataTable IDataReader.GetSchemaTable()
        {
            return Reader.GetSchemaTable();
        }

        bool IDataReader.IsClosed => reader?.IsClosed ?? true;

        bool IDataReader.NextResult()
        {
            return Reader.NextResult();
        }

        bool IDataReader.Read()
        {
            return Reader.Read();
        }

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

        bool IDataRecord.GetBoolean(int i)
        {
            return Reader.GetBoolean(i);
        }

        byte IDataRecord.GetByte(int i)
        {
            return Reader.GetByte(i);
        }

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return Reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        char IDataRecord.GetChar(int i)
        {
            return Reader.GetChar(i);
        }

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return Reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        IDataReader IDataRecord.GetData(int i)
        {
            return Reader.GetData(i);
        }

        string IDataRecord.GetDataTypeName(int i)
        {
            return Reader.GetDataTypeName(i);
        }

        DateTime IDataRecord.GetDateTime(int i)
        {
            return Reader.GetDateTime(i);
        }

        decimal IDataRecord.GetDecimal(int i)
        {
            return Reader.GetDecimal(i);
        }

        double IDataRecord.GetDouble(int i)
        {
            return Reader.GetDouble(i);
        }

        Type IDataRecord.GetFieldType(int i)
        {
            return Reader.GetFieldType(i);
        }

        float IDataRecord.GetFloat(int i)
        {
            return Reader.GetFloat(i);
        }

        Guid IDataRecord.GetGuid(int i)
        {
            return Reader.GetGuid(i);
        }

        short IDataRecord.GetInt16(int i)
        {
            return Reader.GetInt16(i);
        }

        int IDataRecord.GetInt32(int i)
        {
            return Reader.GetInt32(i);
        }

        long IDataRecord.GetInt64(int i)
        {
            return Reader.GetInt64(i);
        }

        string IDataRecord.GetName(int i)
        {
            return Reader.GetName(i);
        }

        int IDataRecord.GetOrdinal(string name)
        {
            return Reader.GetOrdinal(name);
        }

        string IDataRecord.GetString(int i)
        {
            return Reader.GetString(i);
        }

        object IDataRecord.GetValue(int i)
        {
            return Reader.GetValue(i);
        }

        int IDataRecord.GetValues(object[] values)
        {
            return Reader.GetValues(values);
        }

        bool IDataRecord.IsDBNull(int i)
        {
            return Reader.IsDBNull(i);
        }

        object IDataRecord.this[string name] => Reader[name];

        object IDataRecord.this[int i] => Reader[i];
    }
#endif
}
