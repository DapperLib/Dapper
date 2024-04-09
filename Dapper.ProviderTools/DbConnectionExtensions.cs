using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.ProviderTools
{
    /// <summary>
    /// Helper utilities for working with database connections
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// Attempt to get the client connection id for a given connection
        /// </summary>
        public static bool TryGetClientConnectionId(this DbConnection connection, out Guid clientConnectionId)
        {
            clientConnectionId = default;
            return connection is not null && ByTypeHelpers.Get(connection.GetType()).TryGetClientConnectionId(
                connection, out clientConnectionId);
        }

        /// <summary>
        /// Clear all pools associated with the provided connection type
        /// </summary>
        public static bool TryClearAllPools(this DbConnection connection)
            => connection is not null && ByTypeHelpers.Get(connection.GetType()).TryClearAllPools();

        /// <summary>
        /// Clear the pools associated with the provided connection
        /// </summary>
        public static bool TryClearPool(this DbConnection connection)
            => connection is not null && ByTypeHelpers.Get(connection.GetType()).TryClearPool(connection);

        private sealed class ByTypeHelpers
        {
            private static readonly ConcurrentDictionary<Type, ByTypeHelpers> s_byType
                = new ConcurrentDictionary<Type, ByTypeHelpers>();
            private readonly Func<DbConnection, Guid>? _getClientConnectionId;

            private readonly Action<DbConnection>? _clearPool;
            private readonly Action? _clearAllPools;

            public bool TryGetClientConnectionId(DbConnection connection, out Guid clientConnectionId)
            {
                if (_getClientConnectionId is null)
                {
                    clientConnectionId = default;
                    return false;
                }
                clientConnectionId = _getClientConnectionId(connection);
                return true;
            }

            public bool TryClearPool(DbConnection connection)
            {
                if (_clearPool is null) return false;
                _clearPool(connection);
                return true;
            }

            public bool TryClearAllPools()
            {
                if (_clearAllPools is null) return false;
                _clearAllPools();
                return true;
            }

            public static ByTypeHelpers Get(Type type)
            {
                if (!s_byType.TryGetValue(type, out var value))
                {
                    s_byType[type] = value = new ByTypeHelpers(type);
                }
                return value;
            }

            private ByTypeHelpers(Type type)
            {
                _getClientConnectionId = TryGetInstanceProperty<Guid>("ClientConnectionId", type);

                try
                {
                    var clearAllPools = type.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static,
                        null, Type.EmptyTypes, null);
                    if (clearAllPools is not null)
                    {
                        _clearAllPools = (Action)Delegate.CreateDelegate(typeof(Action), clearAllPools);
                    }
                }
                catch { }

                try
                {
                    var clearPool = type.GetMethod("ClearPool", BindingFlags.Public | BindingFlags.Static,
                        null, new[] { type }, null);
                    if (clearPool is not null)
                    {
                        var p = Expression.Parameter(typeof(DbConnection), "connection");
                        var body = Expression.Call(clearPool, Expression.Convert(p, type));
                        var lambda = Expression.Lambda<Action<DbConnection>>(body, p);
                        _clearPool = lambda.Compile();
                    }
                }
                catch { }
            }

            private static Func<DbConnection, T>? TryGetInstanceProperty<T>(string name, Type type)
            {
                try
                {
                    var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (prop is null || !prop.CanRead) return null;
                    if (prop.PropertyType != typeof(T)) return null;

                    var p = Expression.Parameter(typeof(DbConnection), "connection");
                    var body = Expression.Property(Expression.Convert(p, type), prop);
                    var lambda = Expression.Lambda<Func<DbConnection, T>>(body, p);
                    return lambda.Compile();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
