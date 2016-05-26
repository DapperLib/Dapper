#if ASYNC
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
            /// Insert a row into the db asynchronously
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
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
            /// Update a record in the DB asynchronously
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public Task<int> UpdateAsync(TId id, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);

                var builder = new StringBuilder();
                builder.Append("update ").Append(TableName).Append(" set ");
                builder.AppendLine(string.Join(",", paramNames.Where(n => n != "Id").Select(p => p + "= @" + p)));
                builder.Append("where Id = @Id");

                DynamicParameters parameters = new DynamicParameters(data);
                parameters.Add("Id", id);

                return database.ExecuteAsync(builder.ToString(), parameters);
            }

            /// <summary>
            /// Delete a record for the DB asynchronously
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public async Task<bool> DeleteAsync(TId id)
            {
                return (await database.ExecuteAsync("delete from " + TableName + " where Id = @id", new { id }).ConfigureAwait(false)) > 0;
            }

            /// <summary>
            /// Grab a record with a particular Id from the DB asynchronously
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public async Task<T> GetAsync(TId id)
            {
                return (await database.QueryAsync<T>("select * from " + TableName + " where Id = @id", new { id }).ConfigureAwait(false)).FirstOrDefault();
            }

            public virtual async Task<T> FirstAsync()
            {
                return (await database.QueryAsync<T>("select top 1 * from " + TableName).ConfigureAwait(false)).FirstOrDefault();
            }

            public Task<IEnumerable<T>> AllAsync()
            {
                return database.QueryAsync<T>("select * from " + TableName);
            }
        }

        public Task<int> ExecuteAsync(string sql, dynamic param = null)
        {
            return _connection.ExecuteAsync(sql, param as object, _transaction, this._commandTimeout);
        }

        public Task<IEnumerable<T>> QueryAsync<T>(string sql, dynamic param = null)
        {
            return _connection.QueryAsync<T>(sql, param as object, _transaction, _commandTimeout);
        }

        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn);
        }

        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn);
        }

        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn);
        }

        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn);
        }

        public Task<IEnumerable<dynamic>> QueryAsync(string sql, dynamic param = null)
        {
            return _connection.QueryAsync(sql, param as object, _transaction);
        }

        public Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return SqlMapper.QueryMultipleAsync(_connection, sql, param, transaction, commandTimeout, commandType);
        }
    }
}
#endif