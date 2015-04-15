using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Dapper
{
	public static class DapperSqlBuilderMixin
	{
		public static IEnumerable<TReturn> Query<TReturn>(
			this IDbConnection cnn, 
			SqlBuilder.Template query, 
			IDbTransaction transaction = null, 
			bool buffered = true, 
			int? commandTimeout = null, 
			CommandType? commandType = null
		)
		{
			if (query == null) throw new ArgumentNullException("query");

			return cnn.Query<TReturn>(query.RawSql, query.Parameters);
		}
	}
}
