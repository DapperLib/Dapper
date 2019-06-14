using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper
{
    public abstract partial class Database<TDatabase> where TDatabase : Database<TDatabase>, new()
    {
        public partial class Table<T, TId>
        {
            /// <summary>
            /// Insert a row into the db asynchronously.
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type.</param>
            /// <returns>The Id of the inserted row.</returns>
            public virtual async Task<int?> InsertAsync(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                paramNames.Remove("Id");

                string cols = string.Join(",", paramNames);
                string colsParams = string.Join(",", paramNames.Select(p => "@" + p));
                var sql = "set nocount on insert " + TableName + " (" + cols + ") values (" + colsParams + ") select cast(scope_identity() as int)";

                return (await database.QueryAsync<int?>(sql, o).ConfigureAwait(false)).Single();
            }

            /// <summary>
            /// Update a record in the DB asynchronously.
            /// </summary>
            /// <param name="id">The Id of the record to update.</param>
            /// <param name="data">The new record.</param>
            /// <returns>The number of affeced rows.</returns>
            public Task<int> UpdateAsync(TId id, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);

                var builder = new StringBuilder();
                builder.Append("update ").Append(TableName).Append(" set ");
                builder.AppendLine(string.Join(",", paramNames.Where(n => n != "Id").Select(p => p + "= @" + p)));
                builder.Append("where Id = @Id");

                var parameters = new DynamicParameters(data);
                parameters.Add("Id", id);

                return database.ExecuteAsync(builder.ToString(), parameters);
            }

            /// <summary>
            /// Asynchronously deletes a record for the DB.
            /// </summary>
            /// <param name="id">The Id of the record to delete.</param>
            /// <returns>The number of rows affected.</returns>
            public async Task<bool> DeleteAsync(TId id) =>
                (await database.ExecuteAsync("delete from " + TableName + " where Id = @id", new { id }).ConfigureAwait(false)) > 0;

            /// <summary>
            /// Asynchronously gets a record with a particular Id from the DB.
            /// </summary>
            /// <param name="id">The primary key of the table to fetch.</param>
            /// <returns>The record with the specified Id.</returns>
            public Task<T> GetAsync(TId id) =>
                database.QueryFirstOrDefaultAsync<T>("select * from " + TableName + " where Id = @id", new { id });

            /// <summary>
            /// Asynchronously gets the first row from this table (order determined by the database provider).
            /// </summary>
            /// <returns>Data from the first table row.</returns>
            public virtual Task<T> FirstAsync() =>
                database.QueryFirstOrDefaultAsync<T>("select top 1 * from " + TableName);

            /// <summary>
            /// Asynchronously gets the all rows from this table.
            /// </summary>
            /// <returns>Data from all table rows.</returns>
            public Task<IEnumerable<T>> AllAsync() =>
                database.QueryAsync<T>("select * from " + TableName);
        }

        /// <summary>
        /// Asynchronously executes SQL against the current database.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteAsync(string sql, dynamic param = null) =>
            _connection.ExecuteAsync(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Asynchronously queries the current database.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>An enumerable of <typeparamref name="T"/> for the rows fetched.</returns>
        public Task<IEnumerable<T>> QueryAsync<T>(string sql, dynamic param = null) =>
            _connection.QueryAsync<T>(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Asynchronously queries the current database for a single record.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>An enumerable of <typeparamref name="T"/> for the rows fetched.</returns>
        public Task<T> QueryFirstOrDefaultAsync<T>(string sql, dynamic param = null) =>
            _connection.QueryFirstOrDefaultAsync<T>(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 3 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 4 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 5 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public Task<IEnumerable<dynamic>> QueryAsync(string sql, dynamic param = null) =>
            _connection.QueryAsync(sql, param as object, _transaction);

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            SqlMapper.QueryMultipleAsync(_connection, sql, param, transaction, commandTimeout, commandType);
    }
}
