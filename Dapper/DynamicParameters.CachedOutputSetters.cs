using System;
using System.Collections;
using System.Collections.Generic;

#if DNXCORE50
using IDbDataParameter = System.Data.Common.DbParameter;
using IDataParameter = System.Data.Common.DbParameter;
using IDbTransaction = System.Data.Common.DbTransaction;
using IDbConnection = System.Data.Common.DbConnection;
using IDbCommand = System.Data.Common.DbCommand;
using IDataReader = System.Data.Common.DbDataReader;
using IDataRecord = System.Data.Common.DbDataReader;
using IDataParameterCollection = System.Data.Common.DbParameterCollection;
using DataException = System.InvalidOperationException;
using ApplicationException = System.InvalidOperationException;
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
