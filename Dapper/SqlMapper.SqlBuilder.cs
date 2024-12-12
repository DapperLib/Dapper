#if NET8_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Dapper;

public partial class SqlMapper
{
    /// <inheritdoc cref="SqlMapper.Execute(IDbConnection, string, object?, IDbTransaction?, int?, CommandType?)" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
    public static int Execute(
        this IDbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var cmd = sql.GetCommandAndReset(transaction, commandTimeout);
        return ExecuteImpl(connection, new(cmd), cmd);
    }

#pragma warning disable CS1591, RS0016 // Missing XML comment for publicly visible type or member
    public static int ExecuteNQ(
#pragma warning restore CS1591, RS0016 // Missing XML comment for publicly visible type or member
    this IDbConnection connection,
    [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
    IDbTransaction? transaction = null,
    int? commandTimeout = null) => Execute(connection, ref sql, transaction, commandTimeout);

    /// <inheritdoc cref="SqlMapper.Query{T}(IDbConnection, string, object?, IDbTransaction?, bool, int?, CommandType?)" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
    public static IEnumerable<T> QueryUnbuffered<T>(
        this IDbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var cmd = sql.GetCommandAndReset(transaction, commandTimeout);
        return QueryImpl<T>(connection, new(cmd), typeof(T), cmd);
    }

    /// <inheritdoc cref="SqlMapper.Query{T}(IDbConnection, string, object?, IDbTransaction?, bool, int?, CommandType?)" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
    public static IEnumerable<T> Query<T>(
        this IDbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
        IDbTransaction? transaction = null,
        bool buffered = false,
        int? commandTimeout = null)
    {
        var cmd = sql.GetCommandAndReset(transaction, commandTimeout);
        var result = QueryImpl<T>(connection, new(cmd, buffered ? CommandFlags.Buffered : CommandFlags.None), typeof(T), cmd);
        return buffered ? result.ToList() : result;
    }

    /// <inheritdoc cref="SqlMapper.Query{T}(IDbConnection, string, object?, IDbTransaction?, bool, int?, CommandType?)" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
    public static List<T> QueryBuffered<T>(
        this IDbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var cmd = sql.GetCommandAndReset(transaction, commandTimeout);
        return QueryImpl<T>(connection, new(cmd, CommandFlags.Buffered), typeof(T), cmd).ToList();
    }

    /// <inheritdoc cref="ExecuteScalar(IDbConnection, string, object?, IDbTransaction?, int?, CommandType?)"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
    public static object? ExecuteScalar(
        this IDbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var cmd = sql.GetCommandAndReset(transaction, commandTimeout);
        return ExecuteScalarImpl<object>(connection, new(cmd), cmd);
    }

    /// <inheritdoc cref="ExecuteScalar{T}(IDbConnection, string, object?, IDbTransaction?, int?, CommandType?)"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
    public static T? ExecuteScalar<T>(
        this IDbConnection connection,
        [InterpolatedStringHandlerArgument("connection")] ref SqlBuilder sql,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var cmd = sql.GetCommandAndReset(transaction, commandTimeout);
        return ExecuteScalarImpl<T?>(connection, new(cmd), cmd);
    }

    /// <summary>
    /// Allows efficient construction of SQL, intended for use with interpolated string literals.
    /// </summary>
    [InterpolatedStringHandler]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0064:Make readonly fields writable", Justification = "Only Dispose impacted")]
    public ref struct SqlBuilder
    {
        DefaultInterpolatedStringHandler _handler;

        private readonly IDbCommand _command;
        private int _argIndex;
#if NET9_0_OR_GREATER
        private bool _formatted;
#endif

        /// <summary>
        /// Create a new builder instance.
        /// </summary>
        public SqlBuilder(int literalLength, int formattedCount, IDbConnection connection)
        {
            _handler = new(literalLength, formattedCount, CultureInfo.InvariantCulture);
            _command = connection.CreateCommand();
            _command.CommandType = CommandType.Text;
            if (_command.Connection is null) _command.Connection = connection;
        }

        /// <summary>
        /// Returns the <see cref="DbCommand"/> for this operation with the <see cref="DbCommand.CommandText"/> set to the final SQL.
        /// </summary>
        public IDbCommand GetCommandAndReset(IDbTransaction? transaction, int? commandTimeout)
        {
            _command.Transaction = transaction;
            var sql = GetText(ref _handler);
            if (!sql.SequenceEqual(_command.CommandText))
            {
#if NET9_0_OR_GREATER
                if (_formatted)
                {
                    // unique per-call
                    _command.CommandText = sql.ToString();
                }
                else
                {
                    // repeated
                    var cache = _sqlCache.GetAlternateLookup<ReadOnlySpan<char>>();
                    if (!cache.TryGetValue(sql, out var sqlString))
                    {
                        sqlString = sql.ToString();
                        cache[sqlString] = sqlString;
                    }
                    _command.CommandText = sqlString;
                }
#else
                _command.CommandText = sql.ToString();
#endif
            }
            if (commandTimeout.HasValue)
            {
                _command.CommandTimeout = commandTimeout.GetValueOrDefault();
            }
            else if (Settings.CommandTimeout is int globalTimeout)
            {
                _command.CommandTimeout = globalTimeout;
            }
            Debug.WriteLine($"built command with {_command.Parameters.Count} parameters: {_command.CommandText}");

            var cmd = _command;
            Dispose();
            return cmd;
        }

        /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendLiteral(string)"/>
        public void AppendLiteral(string value) => _handler.AppendLiteral(value);

        // avoid allocating composed names repeatedly
        private static readonly ConcurrentDictionary<(char token, string expression), string> _expressionCache = [];
        private static readonly ConcurrentDictionary<(char token, int index), string> _indexCache = [];

#if NET9_0_OR_GREATER
        private static readonly ConcurrentDictionary<string, string> _sqlCache = [];
#endif

        string ProposeAndAppendName(char token, string expression)
        {
            // for simple names, use the expression to name the parameter, so @{name} becomes @name
            // otherwise, invent, so @{id + 2} becomes @p0
            if (Regex.IsMatch(expression, "^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                _handler.AppendLiteral(expression);
                var key = (token, expression);
                if (!_expressionCache.TryGetValue(key, out var composed))
                {
                    _expressionCache[key] = composed = $"{key.token}{key.expression}";
                }
                return composed;
            }
            else
            {
                _handler.AppendLiteral("p");
                _handler.AppendFormatted(_argIndex);
                var key = (token, index: _argIndex++);
                if (!_indexCache.TryGetValue(key, out var composed))
                {
                    _indexCache[key] = composed = $"{key.token}p{key.index}";
                }
                return composed;
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Text")]
        private static extern ReadOnlySpan<char> GetText(ref DefaultInterpolatedStringHandler handler);

        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        private static extern void Clear(ref DefaultInterpolatedStringHandler handler);

        private bool IsParameter(out char prefix)
        {
            var sql = GetText(ref _handler);
            if (!sql.IsEmpty)
            {
                prefix = sql[sql.Length - 1];
                return prefix is '@' or ':' or '$';
            }
            prefix = default;
            return false;
        }

        /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
        public void AppendFormatted(bool value, string format = "", [CallerArgumentExpression(nameof(value))] string expression = "")
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                if (IsParameter(out var prefix))
                {
                    AppendParameter(value, expression, prefix);
                }
                else
                {
                    // treat as literal 0/1
                    _handler.AppendLiteral(value ? "1" : "0");
                }
            }
            else
            {
                _handler.AppendFormatted(value, format);
#if NET9_0_OR_GREATER
                _formatted = true;
#endif
            }
        }

        /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous")]
        public void AppendFormatted<T>(T value, string format = "", [CallerArgumentExpression(nameof(value))] string expression = "")
        {
            if (string.IsNullOrWhiteSpace(format) && IsParameter(out var prefix))
            {
                AppendParameter(value, expression, prefix);
            }
            else
            {
                _handler.AppendFormatted(value, format);
#if NET9_0_OR_GREATER
                _formatted = true;
#endif
            }
        }
        private void AppendParameter<T>(T value, string expression, char prefix)
        {
            var fullName = ProposeAndAppendName(prefix, expression);
            if (!HasParam(fullName))
            {
                var param = _command.CreateParameter();
                param.ParameterName = fullName;
                param.Value = (object?)value ?? DBNull.Value;
                param.Direction = ParameterDirection.Input;
                _command.Parameters.Add(param);
            }
        }

        private bool HasParam(string name)
        {
            foreach (DbParameter param in _command.Parameters)
            {
                if (string.Equals(param.ParameterName, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            var cmd = _command;
            Clear(ref _handler);
            cmd?.Dispose();
            this = default;
        }
    }
}
#endif
