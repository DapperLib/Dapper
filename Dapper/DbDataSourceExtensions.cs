using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

#if NET7_0_OR_GREATER

namespace Dapper;

/// <summary>
/// Provides extension methods for performing database operations from <see cref="DbConnection"/> instances.
/// </summary>
public static class DbDataSourceExtensions
{
    private const int DefaultTimeout = DbConnectionExtensions.DefaultTimeout;
    internal const CommandFlags DisposeConnection = (CommandFlags)(1 << 30);

    /// <summary>
    /// Execute a non-query command.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <returns>The number of rows affected.</returns>
    public static int Execute(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default)
    {
        return DbConnectionExtensions.Execute(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection);
    }

    /// <summary>
    /// Execute a non-query command.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <returns>The number of rows affected.</returns>
    public static Task<int> ExecuteAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default)
    {
        return DbConnectionExtensions.ExecuteAsync(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection, cancellationToken);
    }

    /// <summary>
    /// Executes a non-buffered query, returning the data typed as <c>dynamic</c> objects with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static IEnumerable<dynamic> QueryUnbuffered(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default)
    {
        return DbConnectionExtensions.QueryUnbuffered(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection);
    }

    /// <summary>
    /// Executes a non-buffered query, returning the data typed as <c>dynamic</c> objects with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    public static IAsyncEnumerable<dynamic> QueryUnbufferedAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default)
    {
        return DbConnectionExtensions.QueryUnbufferedAsync(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection, cancellationToken);
    }

    /// <summary>
    /// Executes a buffered query, returning the data as a list of <c>dynamic</c> objects with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static List<dynamic> Query(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default)
    {
        return DbConnectionExtensions.Query(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection);
    }

    /// <summary>
    /// Executes a buffered query, returning the data as a list of <c>dynamic</c> objects with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    public static Task<List<dynamic>> QueryAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default)
    {
        return DbConnectionExtensions.QueryAsync(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection, cancellationToken);
    }

    /// <summary>
    /// Executes a non-buffered query, returning the data typed as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <returns>
    /// A sequence of data of the supplied type <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column is assumed, otherwise an instance is
    /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
    /// </returns>
    public static IEnumerable<T> QueryUnbuffered<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default)
    {
        return DbConnectionExtensions.QueryUnbuffered<T>(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection);
    }

    /// <summary>
    /// Executes a non-buffered query, returning the data typed as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <returns>
    /// A sequence of data of the supplied type <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column is assumed, otherwise an instance is
    /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
    /// </returns>
    public static IAsyncEnumerable<T> QueryUnbufferedAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default)
    {
        return DbConnectionExtensions.QueryUnbufferedAsync<T>(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection, cancellationToken);
    }

    /// <summary>
    /// Executes a buffered query, returning the data typed as a list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <returns>
    /// A list of data of the supplied type <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column is assumed, otherwise an instance is
    /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
    /// </returns>
    public static List<T> Query<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default)
    {
        return DbConnectionExtensions.Query<T>(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection);
    }

    /// <summary>
    /// Executes a buffered query, returning the data typed as a list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <returns>
    /// A list of data of the supplied type <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column is assumed, otherwise an instance is
    /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
    /// </returns>
    public static async Task<List<T>> QueryAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default)
    {
        return await DbConnectionExtensions.QueryAsync<T>(source.CreateConnection(), sql, param, null, commandTimeout, commandType, flags | DisposeConnection, cancellationToken);
    }

    /// <summary>
    /// Executes a buffered query that demands at least one row, returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static dynamic QueryFirst(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands exactly one row, returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static dynamic QuerySingle(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that returns the first row or <c>null</c> if no rows at returned, returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static dynamic? QueryFirstOrDefault(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands at most one row (returning <c>null</c> if no rows at returned), returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static dynamic? QuerySingleOrDefault(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands at least one row, interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static T QueryFirst<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands exactly one row, interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static T QuerySingle<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that returns the first row or <c>null</c> if no rows at returned, interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static T? QueryFirstOrDefault<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands at most one row (returning <c>null</c> if no rows at returned), interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static T? QuerySingleOrDefault<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();


    /// <summary>
    /// Executes a buffered query that demands at least one row, returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<dynamic> QueryFirstAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands exactly one row, returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<dynamic> QuerySingleAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that returns the first row or <c>null</c> if no rows at returned, returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<dynamic?> QueryFirstOrDefaultAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands at most one row (returning <c>null</c> if no rows at returned), returning the data as a <c>dynamic</c> object with properties matching the columns.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<dynamic?> QuerySingleOrDefaultAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands at least one row, interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<T> QueryFirstAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands exactly one row, interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<T> QuerySingleAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that returns the first row or <c>null</c> if no rows at returned, interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<T?> QueryFirstOrDefaultAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Executes a buffered query that demands at most one row (returning <c>null</c> if no rows at returned), interpreting the data as a <typeparamref name="T"/>
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
    public static Task<T?> QuerySingleOrDefaultAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();


    /// <summary>
    /// Execute parameterized SQL and return an <see cref="DbDataReader"/>.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="param">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="behavior">Optional behaviour for this reader</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <returns>An <see cref="DbDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
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
    public static DbDataReader ExecuteReader(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandBehavior behavior = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Execute parameterized SQL and return an <see cref="DbDataReader"/>.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="param">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="behavior">Optional behaviour for this reader</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <returns>An <see cref="DbDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
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
    public static Task<DbDataReader> ExecuteReaderAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandBehavior behavior = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();


    /// <summary>
    /// Execute a command that returns multiple result sets, and access each in turn.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    public static SqlMapper.GridReader QueryMultiple(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default)
         => throw new NotImplementedException();

    /// <summary>
    /// Execute a command that returns multiple result sets, and access each in turn.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute for this query.</param>
    /// <param name="param">The parameters to use for this query.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    public static Task<SqlMapper.GridReader> QueryMultipleAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout, CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default)
         => throw new NotImplementedException();

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="param">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <returns>The first cell selected as <see cref="object"/>.</returns>
    public static object? ExecuteScalar(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout,
        CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="param">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
    public static T? ExecuteScalar<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout,
        CommandType commandType = default, CommandFlags flags = default) => throw new NotImplementedException();

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="param">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <returns>The first cell selected as <see cref="object"/>.</returns>
    public static Task<object?> ExecuteScalarAsync(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout,
        CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="source">The connection source.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="param">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="commandType">Is it a stored proc or a batch?</param>
    /// <param name="flags">Optional flags for this command</param>
    /// <param name="cancellationToken">Asynchronous cancellation for this command</param>
    /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
    public static Task<T?> ExecuteScalarAsync<T>(this DbDataSource source, string sql, object? param = null, int commandTimeout = DefaultTimeout,
        CommandType commandType = default, CommandFlags flags = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}


#endif
