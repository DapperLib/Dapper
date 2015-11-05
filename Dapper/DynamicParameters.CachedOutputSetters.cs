using System;
using System.Collections;
using System.Collections.Generic;
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
        internal static class CachedOutputSetters<T>
        {
#if DNXCORE50
            public static readonly Dictionary<string, Action<object, DynamicParameters>> Cache = new Dictionary<string, Action<object, DynamicParameters>>();
#else
            public static readonly Hashtable Cache = new Hashtable();
#endif
        }
    }
}
