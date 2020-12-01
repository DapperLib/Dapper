using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Contrib.Extensions
{
    public static partial class SqlMapperExtensions
    {
        /// <summary>
        /// Returns a list of entities from table "Ts" based on predicate expression.  
        /// Id of T must be marked with [Key] attribute.
        /// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        /// for optimal performance.
        /// Entities can be filtered using predicate expression
        /// </summary>
        /// <typeparam name="T">Interface or type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="predicate">Search terms</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Entity of T</returns>
        public static Task<IEnumerable<T>> GetAllAsync<T>(this IDbConnection connection, Expression<Func<T, bool>> predicate,
            IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var cacheType = typeof(List<T>);

            if (!GetQueries.TryGetValue(cacheType.TypeHandle, out string sql))
            {
                GetSingleKey<T>(nameof(GetAll));
                var name = GetTableName(type);

                var where = CreateWhereClause(predicate);

                sql = $"SELECT * FROM {name} {where}";
                GetQueries[cacheType.TypeHandle] = sql;
            }

            return !type.IsInterface
                ? connection.QueryAsync<T>(sql, null, transaction, commandTimeout)
                : GetAllAsyncImpl<T>(connection, transaction, commandTimeout, sql, type);
        }

        /// <summary>
        /// Delete n matching entities in the table related to the type T asynchronously using Task based.
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="predicate">Filter to apply for deletion</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>true if deleted, false if none found</returns>
        public static async Task<bool> DeleteAllAsync<T>(this IDbConnection connection, Expression<Func<T, bool>> predicate,
            IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var where = CreateWhereClause(predicate);

            var statement = $"DELETE FROM {GetTableName(type)} {where}";
            var deleted = await connection.ExecuteAsync(statement, null, transaction, commandTimeout).ConfigureAwait(false);
            return deleted > 0;
        }

        private static string CreateWhereClause<T>(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return "";

            var p = new StringBuilder(predicate.Body.ToString());
            var pName = predicate.Parameters.First();
            p.Replace(pName.Name + ".", "");
            p.Replace("==", "=");
            p.Replace("AndAlso", "and");
            p.Replace("OrElse", "or");
            p.Replace("\"", "\'");
            return $"WHERE {p}";
        }
    }
}
