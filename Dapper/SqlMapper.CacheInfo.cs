using System;
using System.Data;
using System.Threading;

#if DNXCORE50
using IDbDataParameter = global::System.Data.Common.DbParameter;
using IDataParameter = global::System.Data.Common.DbParameter;
using IDbTransaction = global::System.Data.Common.DbTransaction;
using IDbConnection = global::System.Data.Common.DbConnection;
using IDbCommand = global::System.Data.Common.DbCommand;
using IDataReader = global::System.Data.Common.DbDataReader;
using IDataRecord = global::System.Data.Common.DbDataReader;
using IDataParameterCollection = global::System.Data.Common.DbParameterCollection;
using DataException = global::System.InvalidOperationException;
using ApplicationException = global::System.InvalidOperationException;
#endif

namespace Dapper
{
    partial class SqlMapper
    {
        private class CacheInfo
        {
            public DeserializerState Deserializer { get; set; }

            public Func<IDataReader, object>[] OtherDeserializers { get; set; }

            public Action<IDbCommand, object> ParamReader { get; set; }

            private int hitCount;

            public int GetHitCount()
            {
                return Interlocked.CompareExchange(ref hitCount, 0, 0);
            }

            public void RecordHit()
            {
                Interlocked.Increment(ref hitCount);
            }
        }
    }
}
