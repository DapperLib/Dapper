using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Reflection;
using Dapper;

namespace System.Data
{

	/// <summary>
	/// Shortcuts I found useful
	/// </summary>
	public static class DapperHelper
	{
        public static int Execute(
            this IDbTransaction transaction,
            string sql,
            object param = null,
            CommandType? commandType = null
        )
        { return transaction.Connection.Execute(sql, param, transaction, 0, commandType); }

        public static IEnumerable<T> Query<T>(
            this IDbTransaction transaction,
            string sql,
            object param = null,
            CommandType? commandType = null
        )
        { return transaction.Connection.Query<T>(sql, param, transaction, true, 0, commandType); }

        public static int Execute(
            this IDbConnection cnn,
            string sql,
            object param = null,
            CommandType? commandType = null,
            IDbTransaction transaction = null
        )
        { return cnn.Execute(sql, param, transaction, 0, commandType); }

#if !CSHARP30
        public static IEnumerable<dynamic> Query(
            this IDbConnection cnn,
            string sql,
            object param = null,
            CommandType? commandType = null,
            IDbTransaction transaction = null
        )
        { return cnn.Query(sql, param, transaction, true, 0, commandType); }
#endif
        public static IEnumerable<T> Query<T>(
            this IDbConnection cnn,
            string sql,
            object param = null,
            CommandType? commandType = null,
            IDbTransaction transaction = null
        )
        { return cnn.Query<T>(sql, param, transaction, true, 0, commandType); }

		public static  SqlMapper.GridReader QueryMultiple(
			this IDbConnection cnn,
			string sql,
			object param = null,
			CommandType? commandType = null,
			IDbTransaction transaction = null)
		{
			return cnn.QueryMultiple(sql, param, transaction, 0, commandType);
		}
	}
}
