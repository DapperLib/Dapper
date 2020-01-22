using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static Task<IEnumerable<dynamic>> QueryAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryAsync<dynamic>(cnn, typeof(DapperRow), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered, default(CancellationToken)));

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static Task<IEnumerable<dynamic>> QueryAsync(this IDbConnection cnn, CommandDefinition command) =>
            QueryAsync<dynamic>(cnn, typeof(DapperRow), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static Task<dynamic> QueryFirstAsync(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<dynamic>(cnn, Row.First, typeof(DapperRow), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static Task<dynamic> QueryFirstOrDefaultAsync(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<dynamic>(cnn, Row.FirstOrDefault, typeof(DapperRow), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static Task<dynamic> QuerySingleAsync(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<dynamic>(cnn, Row.Single, typeof(DapperRow), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static Task<dynamic> QuerySingleOrDefaultAsync(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<dynamic>(cnn, Row.SingleOrDefault, typeof(DapperRow), command);

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        /// A sequence of data of <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryAsync<T>(cnn, typeof(T), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<T> QueryFirstAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<T>(cnn, Row.First, typeof(T), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<T>(cnn, Row.FirstOrDefault, typeof(T), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<T> QuerySingleAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<T>(cnn, Row.Single, typeof(T), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<T> QuerySingleOrDefaultAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<T>(cnn, Row.SingleOrDefault, typeof(T), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<dynamic> QueryFirstAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<dynamic>(cnn, Row.First, typeof(DapperRow), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<dynamic> QueryFirstOrDefaultAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<dynamic>(cnn, Row.FirstOrDefault, typeof(DapperRow), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<dynamic> QuerySingleAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<dynamic>(cnn, Row.Single, typeof(DapperRow), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        public static Task<dynamic> QuerySingleOrDefaultAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryRowAsync<dynamic>(cnn, Row.SingleOrDefault, typeof(DapperRow), new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static Task<IEnumerable<object>> QueryAsync(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return QueryAsync<object>(cnn, type, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered, default(CancellationToken)));
        }

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static Task<object> QueryFirstAsync(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return QueryRowAsync<object>(cnn, Row.First, type, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));
        }
        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static Task<object> QueryFirstOrDefaultAsync(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return QueryRowAsync<object>(cnn, Row.FirstOrDefault, type, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));
        }
        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static Task<object> QuerySingleAsync(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return QueryRowAsync<object>(cnn, Row.Single, type, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));
        }
        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static Task<object> QuerySingleOrDefaultAsync(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return QueryRowAsync<object>(cnn, Row.SingleOrDefault, type, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None, default(CancellationToken)));
        }

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        /// <returns>
        /// A sequence of data of <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection cnn, CommandDefinition command) =>
            QueryAsync<T>(cnn, typeof(T), command);

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<IEnumerable<object>> QueryAsync(this IDbConnection cnn, Type type, CommandDefinition command) =>
            QueryAsync<object>(cnn, type, command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<object> QueryFirstAsync(this IDbConnection cnn, Type type, CommandDefinition command) =>
            QueryRowAsync<object>(cnn, Row.First, type, command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<T> QueryFirstAsync<T>(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<T>(cnn, Row.First, typeof(T), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<object> QueryFirstOrDefaultAsync(this IDbConnection cnn, Type type, CommandDefinition command) =>
            QueryRowAsync<object>(cnn, Row.FirstOrDefault, type, command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<T>(cnn, Row.FirstOrDefault, typeof(T), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<object> QuerySingleAsync(this IDbConnection cnn, Type type, CommandDefinition command) =>
            QueryRowAsync<object>(cnn, Row.Single, type, command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<T> QuerySingleAsync<T>(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<T>(cnn, Row.Single, typeof(T), command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="type">The type to return.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<object> QuerySingleOrDefaultAsync(this IDbConnection cnn, Type type, CommandDefinition command) =>
            QueryRowAsync<object>(cnn, Row.SingleOrDefault, type, command);

        /// <summary>
        /// Execute a single-row query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command used to query on this connection.</param>
        public static Task<T> QuerySingleOrDefaultAsync<T>(this IDbConnection cnn, CommandDefinition command) =>
            QueryRowAsync<T>(cnn, Row.SingleOrDefault, typeof(T), command);

        private static Task<DbDataReader> ExecuteReaderWithFlagsFallbackAsync(DbCommand cmd, bool wasClosed, CommandBehavior behavior, CancellationToken cancellationToken)
        {
            var task = cmd.ExecuteReaderAsync(GetBehavior(wasClosed, behavior), cancellationToken);
            if (task.Status == TaskStatus.Faulted && Settings.DisableCommandBehaviorOptimizations(behavior, task.Exception.InnerException))
            { // we can retry; this time it will have different flags
                return cmd.ExecuteReaderAsync(GetBehavior(wasClosed, behavior), cancellationToken);
            }
            return task;
        }

        /// <summary>
        /// Attempts to open a connection asynchronously, with a better error message for unsupported usages.
        /// </summary>
        private static Task TryOpenAsync(this IDbConnection cnn, CancellationToken cancel)
        {
            if (cnn is DbConnection dbConn)
            {
                return dbConn.OpenAsync(cancel);
            }
            else
            {
                throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection");
            }
        }

        /// <summary>
        /// Attempts setup a <see cref="DbCommand"/> on a <see cref="DbConnection"/>, with a better error message for unsupported usages.
        /// </summary>
        private static DbCommand TrySetupAsyncCommand(this CommandDefinition command, IDbConnection cnn, Action<IDbCommand, object> paramReader)
        {
            if (command.SetupCommand(cnn, paramReader) is DbCommand dbCommand)
            {
                return dbCommand;
            }
            else
            {
                throw new InvalidOperationException("Async operations require use of a DbConnection or an IDbConnection where .CreateCommand() returns a DbCommand");
            }
        }

        private static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection cnn, Type effectiveType, CommandDefinition command)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, effectiveType, param?.GetType());
            var info = GetCacheInfo(identity, param, command.AddToCache);
            bool wasClosed = cnn.State == ConnectionState.Closed;
            var cancel = command.CancellationToken;
            using (var cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader))
            {
                DbDataReader reader = null;
                try
                {
                    if (wasClosed) await cnn.TryOpenAsync(cancel).ConfigureAwait(false);
                    reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, cancel).ConfigureAwait(false);

                    var tuple = info.Deserializer;
                    int hash = GetColumnHash(reader);
                    if (tuple.Func == null || tuple.Hash != hash)
                    {
                        if (reader.FieldCount == 0)
                            return Enumerable.Empty<T>();
                        tuple = info.Deserializer = new DeserializerState(hash, GetDeserializer(effectiveType, reader, 0, -1, false));
                        if (command.AddToCache) SetQueryCache(identity, info);
                    }

                    var func = tuple.Func;

                    if (command.Buffered)
                    {
                        var buffer = new List<T>();
                        var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            object val = func(reader);
                            if (val == null || val is T)
                            {
                                buffer.Add((T)val);
                            }
                            else
                            {
                                buffer.Add((T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture));
                            }
                        }
                        while (await reader.NextResultAsync(cancel).ConfigureAwait(false)) { /* ignore subsequent result sets */ }
                        command.OnCompleted();
                        return buffer;
                    }
                    else
                    {
                        // can't use ReadAsync / cancellation; but this will have to do
                        wasClosed = false; // don't close if handing back an open reader; rely on the command-behavior
                        var deferred = ExecuteReaderSync<T>(reader, func, command.Parameters);
                        reader = null; // to prevent it being disposed before the caller gets to see it
                        return deferred;
                    }
                }
                finally
                {
                    using (reader) { /* dispose if non-null */ }
                    if (wasClosed) cnn.Close();
                }
            }
        }

        private static async Task<T> QueryRowAsync<T>(this IDbConnection cnn, Row row, Type effectiveType, CommandDefinition command)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, effectiveType, param?.GetType());
            var info = GetCacheInfo(identity, param, command.AddToCache);
            bool wasClosed = cnn.State == ConnectionState.Closed;
            var cancel = command.CancellationToken;
            using (var cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader))
            {
                DbDataReader reader = null;
                try
                {
                    if (wasClosed) await cnn.TryOpenAsync(cancel).ConfigureAwait(false);
                    reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, (row & Row.Single) != 0
                    ? CommandBehavior.SequentialAccess | CommandBehavior.SingleResult // need to allow multiple rows, to check fail condition
                    : CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancel).ConfigureAwait(false);

                    T result = default(T);
                    if (await reader.ReadAsync(cancel).ConfigureAwait(false) && reader.FieldCount != 0)
                    {
                        var tuple = info.Deserializer;
                        int hash = GetColumnHash(reader);
                        if (tuple.Func == null || tuple.Hash != hash)
                        {
                            tuple = info.Deserializer = new DeserializerState(hash, GetDeserializer(effectiveType, reader, 0, -1, false));
                            if (command.AddToCache) SetQueryCache(identity, info);
                        }

                        var func = tuple.Func;

                        object val = func(reader);
                        if (val == null || val is T)
                        {
                            result = (T)val;
                        }
                        else
                        {
                            var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                            result = (T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
                        }
                        if ((row & Row.Single) != 0 && await reader.ReadAsync(cancel).ConfigureAwait(false)) ThrowMultipleRows(row);
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false)) { /* ignore rows after the first */ }
                    }
                    else if ((row & Row.FirstOrDefault) == 0) // demanding a row, and don't have one
                    {
                        ThrowZeroRows(row);
                    }
                    while (await reader.NextResultAsync(cancel).ConfigureAwait(false)) { /* ignore result sets after the first */ }
                    return result;
                }
                finally
                {
                    using (reader) { /* dispose if non-null */ }
                    if (wasClosed) cnn.Close();
                }
            }
        }

        /// <summary>
        /// Execute a command asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The number of rows affected.</returns>
        public static Task<int> ExecuteAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            ExecuteAsync(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered, default(CancellationToken)));

        /// <summary>
        /// Execute a command asynchronously using Task.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute on this connection.</param>
        /// <returns>The number of rows affected.</returns>
        public static Task<int> ExecuteAsync(this IDbConnection cnn, CommandDefinition command)
        {
            object param = command.Parameters;
            IEnumerable multiExec = GetMultiExec(param);
            if (multiExec != null)
            {
                return ExecuteMultiImplAsync(cnn, command, multiExec);
            }
            else
            {
                return ExecuteImplAsync(cnn, command, param);
            }
        }

        private struct AsyncExecState
        {
            public readonly DbCommand Command;
            public readonly Task<int> Task;
            public AsyncExecState(DbCommand command, Task<int> task)
            {
                Command = command;
                Task = task;
            }
        }

        private static async Task<int> ExecuteMultiImplAsync(IDbConnection cnn, CommandDefinition command, IEnumerable multiExec)
        {
            bool isFirst = true;
            int total = 0;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);

                CacheInfo info = null;
                string masterSql = null;
                if ((command.Flags & CommandFlags.Pipelined) != 0)
                {
                    const int MAX_PENDING = 100;
                    var pending = new Queue<AsyncExecState>(MAX_PENDING);
                    DbCommand cmd = null;
                    try
                    {
                        foreach (var obj in multiExec)
                        {
                            if (isFirst)
                            {
                                isFirst = false;
                                cmd = command.TrySetupAsyncCommand(cnn, null);
                                masterSql = cmd.CommandText;
                                var identity = new Identity(command.CommandText, cmd.CommandType, cnn, null, obj.GetType());
                                info = GetCacheInfo(identity, obj, command.AddToCache);
                            }
                            else if (pending.Count >= MAX_PENDING)
                            {
                                var recycled = pending.Dequeue();
                                total += await recycled.Task.ConfigureAwait(false);
                                cmd = recycled.Command;
                                cmd.CommandText = masterSql; // because we do magic replaces on "in" etc
                                cmd.Parameters.Clear(); // current code is Add-tastic
                            }
                            else
                            {
                                cmd = command.TrySetupAsyncCommand(cnn, null);
                            }
                            info.ParamReader(cmd, obj);

                            var task = cmd.ExecuteNonQueryAsync(command.CancellationToken);
                            pending.Enqueue(new AsyncExecState(cmd, task));
                            cmd = null; // note the using in the finally: this avoids a double-dispose
                        }
                        while (pending.Count != 0)
                        {
                            var pair = pending.Dequeue();
                            using (pair.Command) { /* dispose commands */ }
                            total += await pair.Task.ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        // this only has interesting work to do if there are failures
                        using (cmd) { /* dispose commands */ }
                        while (pending.Count != 0)
                        { // dispose tasks even in failure
                            using (pending.Dequeue().Command) { /* dispose commands */ }
                        }
                    }
                }
                else
                {
                    using (var cmd = command.TrySetupAsyncCommand(cnn, null))
                    {
                        foreach (var obj in multiExec)
                        {
                            if (isFirst)
                            {
                                masterSql = cmd.CommandText;
                                isFirst = false;
                                var identity = new Identity(command.CommandText, cmd.CommandType, cnn, null, obj.GetType());
                                info = GetCacheInfo(identity, obj, command.AddToCache);
                            }
                            else
                            {
                                cmd.CommandText = masterSql; // because we do magic replaces on "in" etc
                                cmd.Parameters.Clear(); // current code is Add-tastic
                            }
                            info.ParamReader(cmd, obj);
                            total += await cmd.ExecuteNonQueryAsync(command.CancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                command.OnCompleted();
            }
            finally
            {
                if (wasClosed) cnn.Close();
            }
            return total;
        }

        private static async Task<int> ExecuteImplAsync(IDbConnection cnn, CommandDefinition command, object param)
        {
            var identity = new Identity(command.CommandText, command.CommandType, cnn, null, param?.GetType());
            var info = GetCacheInfo(identity, param, command.AddToCache);
            bool wasClosed = cnn.State == ConnectionState.Closed;
            using (var cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader))
            {
                try
                {
                    if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                    var result = await cmd.ExecuteNonQueryAsync(command.CancellationToken).ConfigureAwait(false);
                    command.OnCompleted();
                    return result;
                }
                finally
                {
                    if (wasClosed) cnn.Close();
                }
            }
        }

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, command, map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 3 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 3 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, command, map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 4 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 4 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(cnn, command, map, splitOn);

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
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

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
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(cnn, command, map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 6 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 6 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn = "Id") =>
             MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(cnn, command, map, splitOn);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 7 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TSeventh">The seventh type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null) =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken)), map, splitOn);

        /// <summary>
        /// Perform an asynchronous multi-mapping query with 7 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TSeventh">The seventh type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, string splitOn = "Id") =>
            MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, command, map, splitOn);

        private static async Task<IEnumerable<TReturn>> MultiMapAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, CommandDefinition command, Delegate map, string splitOn)
        {
            object param = command.Parameters;
            var identity = new Identity<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(command.CommandText, command.CommandType, cnn, typeof(TFirst), param?.GetType());
            var info = GetCacheInfo(identity, param, command.AddToCache);
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                using (var cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader))
                using (var reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, command.CancellationToken).ConfigureAwait(false))
                {
                    if (!command.Buffered) wasClosed = false; // handing back open reader; rely on command-behavior
                    var results = MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(null, CommandDefinition.ForCallback(command.Parameters), map, splitOn, reader, identity, true);
                    return command.Buffered ? results.ToList() : results;
                }
            }
            finally
            {
                if (wasClosed) cnn.Close();
            }
        }

        /// <summary>
        /// Perform a asynchronous multi-mapping query with an arbitrary number of input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public static Task<IEnumerable<TReturn>> QueryAsync<TReturn>(this IDbConnection cnn, string sql, Type[] types, Func<object[], TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None, default(CancellationToken));
            return MultiMapAsync(cnn, command, types, map, splitOn);
        }

        private static async Task<IEnumerable<TReturn>> MultiMapAsync<TReturn>(this IDbConnection cnn, CommandDefinition command, Type[] types, Func<object[], TReturn> map, string splitOn)
        {
            if (types.Length < 1)
            {
                throw new ArgumentException("you must provide at least one type to deserialize");
            }

            object param = command.Parameters;
            var identity = new IdentityWithTypes(command.CommandText, command.CommandType, cnn, types[0], param?.GetType(), types);
            var info = GetCacheInfo(identity, param, command.AddToCache);
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                using (var cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader))
                using (var reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, command.CancellationToken).ConfigureAwait(false))
                {
                    var results = MultiMapImpl(null, default(CommandDefinition), types, map, splitOn, reader, identity, true);
                    return command.Buffered ? results.ToList() : results;
                }
            }
            finally
            {
                if (wasClosed) cnn.Close();
            }
        }

        private static IEnumerable<T> ExecuteReaderSync<T>(IDataReader reader, Func<IDataReader, object> func, object parameters)
        {
            using (reader)
            {
                while (reader.Read())
                {
                    yield return (T)func(reader);
                }
                while (reader.NextResult()) { /* ignore subsequent result sets */ }
                (parameters as IParameterCallbacks)?.OnCompleted();
            }
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public static Task<GridReader> QueryMultipleAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            QueryMultipleAsync(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered));

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="cnn">The connection to query on.</param>
        /// <param name="command">The command to execute for this query.</param>
        public static async Task<GridReader> QueryMultipleAsync(this IDbConnection cnn, CommandDefinition command)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, typeof(GridReader), param?.GetType());
            CacheInfo info = GetCacheInfo(identity, param, command.AddToCache);

            DbCommand cmd = null;
            IDataReader reader = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                cmd = command.TrySetupAsyncCommand(cnn, info.ParamReader);
                reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, CommandBehavior.SequentialAccess, command.CancellationToken).ConfigureAwait(false);

                var result = new GridReader(cmd, reader, identity, command.Parameters as DynamicParameters, command.AddToCache, command.CancellationToken);
                wasClosed = false; // *if* the connection was closed and we got this far, then we now have a reader
                // with the CloseConnection flag, so the reader will deal with the connection; we
                // still need something in the "finally" to ensure that broken SQL still results
                // in the connection closing itself
                return result;
            }
            catch
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                    {
                        try { cmd.Cancel(); }
                        catch
                        { /* don't spoil the existing exception */
                        }
                    }
                    reader.Dispose();
                }
                cmd?.Dispose();
                if (wasClosed) cnn.Close();
                throw;
            }
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
        /// or <see cref="T:DataSet"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// DataTable table = new DataTable("MyTable");
        /// using (var reader = ExecuteReader(cnn, sql, param))
        /// {
        ///     table.Load(reader);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public static Task<IDataReader> ExecuteReaderAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            ExecuteWrappedReaderImplAsync(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered), CommandBehavior.Default).CastResult<DbDataReader, IDataReader>();

        /// <summary>
        /// Execute parameterized SQL and return a <see cref="DbDataReader"/>.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            ExecuteWrappedReaderImplAsync(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered), CommandBehavior.Default);

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
        /// or <see cref="T:DataSet"/>.
        /// </remarks>
        public static Task<IDataReader> ExecuteReaderAsync(this IDbConnection cnn, CommandDefinition command) =>
            ExecuteWrappedReaderImplAsync(cnn, command, CommandBehavior.Default).CastResult<DbDataReader, IDataReader>();

        /// <summary>
        /// Execute parameterized SQL and return a <see cref="DbDataReader"/>.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute.</param>
        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection cnn, CommandDefinition command) =>
            ExecuteWrappedReaderImplAsync(cnn, command, CommandBehavior.Default);

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="commandBehavior">The <see cref="CommandBehavior"/> flags for this reader.</param>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
        /// or <see cref="T:DataSet"/>.
        /// </remarks>
        public static Task<IDataReader> ExecuteReaderAsync(this IDbConnection cnn, CommandDefinition command, CommandBehavior commandBehavior) =>
            ExecuteWrappedReaderImplAsync(cnn, command, commandBehavior).CastResult<DbDataReader, IDataReader>();

        /// <summary>
        /// Execute parameterized SQL and return a <see cref="DbDataReader"/>.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="commandBehavior">The <see cref="CommandBehavior"/> flags for this reader.</param>
        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection cnn, CommandDefinition command, CommandBehavior commandBehavior) =>
            ExecuteWrappedReaderImplAsync(cnn, command, commandBehavior);

        private static async Task<DbDataReader> ExecuteWrappedReaderImplAsync(IDbConnection cnn, CommandDefinition command, CommandBehavior commandBehavior)
        {
            Action<IDbCommand, object> paramReader = GetParameterReader(cnn, ref command);

            DbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed, disposeCommand = true;
            try
            {
                cmd = command.TrySetupAsyncCommand(cnn, paramReader);
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                var reader = await ExecuteReaderWithFlagsFallbackAsync(cmd, wasClosed, commandBehavior, command.CancellationToken).ConfigureAwait(false);
                wasClosed = false;
                disposeCommand = false;
                return WrappedReader.Create(cmd, reader);
            }
            finally
            {
                if (wasClosed) cnn.Close();
                if (cmd != null && disposeCommand) cmd.Dispose();
            }
        }

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The first cell returned, as <see cref="object"/>.</returns>
        public static Task<object> ExecuteScalarAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            ExecuteScalarImplAsync<object>(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered));

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="transaction">The transaction to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
        public static Task<T> ExecuteScalarAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            ExecuteScalarImplAsync<T>(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered));

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The first cell selected as <see cref="object"/>.</returns>
        public static Task<object> ExecuteScalarAsync(this IDbConnection cnn, CommandDefinition command) =>
            ExecuteScalarImplAsync<object>(cnn, command);

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="cnn">The connection to execute on.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The first cell selected as <typeparamref name="T"/>.</returns>
        public static Task<T> ExecuteScalarAsync<T>(this IDbConnection cnn, CommandDefinition command) =>
            ExecuteScalarImplAsync<T>(cnn, command);

        private static async Task<T> ExecuteScalarImplAsync<T>(IDbConnection cnn, CommandDefinition command)
        {
            Action<IDbCommand, object> paramReader = null;
            object param = command.Parameters;
            if (param != null)
            {
                var identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType());
                paramReader = GetCacheInfo(identity, command.Parameters, command.AddToCache).ParamReader;
            }

            DbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            object result;
            try
            {
                cmd = command.TrySetupAsyncCommand(cnn, paramReader);
                if (wasClosed) await cnn.TryOpenAsync(command.CancellationToken).ConfigureAwait(false);
                result = await cmd.ExecuteScalarAsync(command.CancellationToken).ConfigureAwait(false);
                command.OnCompleted();
            }
            finally
            {
                if (wasClosed) cnn.Close();
                cmd?.Dispose();
            }
            return Parse<T>(result);
        }
    }
}
