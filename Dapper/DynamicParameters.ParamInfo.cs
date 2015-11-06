using System;
using System.Data;

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
    partial class DynamicParameters
    {
        private sealed class ParamInfo
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public ParameterDirection ParameterDirection { get; set; }

            public DbType? DbType { get; set; }

            public int? Size { get; set; }

            public IDbDataParameter AttachedParam { get; set; }

            internal Action<object, DynamicParameters> OutputCallback { get; set; }

            internal object OutputTarget { get; set; }

            internal bool CameFromTemplate { get; set; }

            public byte? Precision { get; set; }

            public byte? Scale { get; set; }
        }
    }
}
