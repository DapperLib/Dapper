/*
 License: http://www.apache.org/licenses/LICENSE-2.0
 Home page: https://github.com/StackExchange/dapper-dot-net
 */

#if COREFX
using IDbDataParameter = System.Data.Common.DbParameter;
using IDataParameter = System.Data.Common.DbParameter;
using IDbTransaction = System.Data.Common.DbTransaction;
using IDbConnection = System.Data.Common.DbConnection;
using IDbCommand = System.Data.Common.DbCommand;
using IDataReader = System.Data.Common.DbDataReader;
using IDataRecord = System.Data.Common.DbDataReader;
using IDataParameterCollection = System.Data.Common.DbParameterCollection;
using DataException = System.InvalidOperationException;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Dapper
{

    /// <summary>
    /// Dapper, a light weight object mapper for ADO.NET
    /// </summary>
    public static partial class SqlMapper
    {
        static int GetColumnHash(IDataReader reader, int startBound = 0, int length = -1)
        {
            unchecked
            {
                int max = length < 0 ? reader.FieldCount : startBound + length;
                int hash = (-37 * startBound) + max;
                for (int i = startBound; i < max; i++)
                {   
                    object tmp = reader.GetName(i);
                    hash = -79 * ((hash * 31) + (tmp?.GetHashCode() ?? 0)) + (reader.GetFieldType(i)?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }


        /// <summary>
        /// Called if the query cache is purged via PurgeQueryCache
        /// </summary>
        public static event EventHandler QueryCachePurged;
        private static void OnQueryCachePurged()
        {
            var handler = QueryCachePurged;
            handler?.Invoke(null, EventArgs.Empty);
        }

        static readonly System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo> _queryCache = new System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo>();
        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            if (Interlocked.Increment(ref collect) == COLLECT_PER_ITEMS)
            {
                CollectCacheGarbage();
            }
            _queryCache[key] = value;
        }

        private static void CollectCacheGarbage()
        {
            try
            {
                foreach (var pair in _queryCache)
                {
                    if (pair.Value.GetHitCount() <= COLLECT_HIT_COUNT_MIN)
                    {
                        CacheInfo cache;
                        _queryCache.TryRemove(pair.Key, out cache);
                    }
                }
            }

            finally
            {
                Interlocked.Exchange(ref collect, 0);
            }
        }

        private const int COLLECT_PER_ITEMS = 1000, COLLECT_HIT_COUNT_MIN = 0;
        private static int collect;
        private static bool TryGetQueryCache(Identity key, out CacheInfo value)
        {
            if (_queryCache.TryGetValue(key, out value))
            {
                value.RecordHit();
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Purge the query cache
        /// </summary>
        public static void PurgeQueryCache()
        {
            _queryCache.Clear();
            TypeDeserializerCache.Purge();
            OnQueryCachePurged();
        }

        private static void PurgeQueryCacheByType(Type type)
        {
            foreach (var entry in _queryCache)
            {
                CacheInfo cache;
                if (entry.Key.type == type)
                    _queryCache.TryRemove(entry.Key, out cache);
            }
            TypeDeserializerCache.Purge(type);
        }

        /// <summary>
        /// Return a count of all the cached queries by dapper
        /// </summary>
        /// <returns></returns>
        public static int GetCachedSQLCount()
        {
            return _queryCache.Count;
        }

        /// <summary>
        /// Return a list of all the queries cached by dapper
        /// </summary>
        /// <param name="ignoreHitCountAbove"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, string, int>> GetCachedSQL(int ignoreHitCountAbove = int.MaxValue)
        {
            var data = _queryCache.Select(pair => Tuple.Create(pair.Key.connectionString, pair.Key.sql, pair.Value.GetHitCount()));
            if (ignoreHitCountAbove < int.MaxValue) data = data.Where(tuple => tuple.Item3 <= ignoreHitCountAbove);
            return data;
        }

        /// <summary>
        /// Deep diagnostics only: find any hash collisions in the cache
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Tuple<int, int>> GetHashCollissions()
        {
            var counts = new Dictionary<int, int>();
            foreach (var key in _queryCache.Keys)
            {
                int count;
                if (!counts.TryGetValue(key.hashCode, out count))
                {
                    counts.Add(key.hashCode, 1);
                }
                else
                {
                    counts[key.hashCode] = count + 1;
                }
            }
            return from pair in counts
                   where pair.Value > 1
                   select Tuple.Create(pair.Key, pair.Value);

        }


        static Dictionary<Type, DbType> typeMap;

        static SqlMapper()
        {
            typeMap = new Dictionary<Type, DbType>
                      {
                          [typeof(byte)] = DbType.Byte,
                          [typeof(sbyte)] = DbType.SByte,
                          [typeof(short)] = DbType.Int16,
                          [typeof(ushort)] = DbType.UInt16,
                          [typeof(int)] = DbType.Int32,
                          [typeof(uint)] = DbType.UInt32,
                          [typeof(long)] = DbType.Int64,
                          [typeof(ulong)] = DbType.UInt64,
                          [typeof(float)] = DbType.Single,
                          [typeof(double)] = DbType.Double,
                          [typeof(decimal)] = DbType.Decimal,
                          [typeof(bool)] = DbType.Boolean,
                          [typeof(string)] = DbType.String,
                          [typeof(char)] = DbType.StringFixedLength,
                          [typeof(Guid)] = DbType.Guid,
                          [typeof(DateTime)] = DbType.DateTime,
                          [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                          [typeof(TimeSpan)] = DbType.Time,
                          [typeof(byte[])] = DbType.Binary,
                          [typeof(byte?)] = DbType.Byte,
                          [typeof(sbyte?)] = DbType.SByte,
                          [typeof(short?)] = DbType.Int16,
                          [typeof(ushort?)] = DbType.UInt16,
                          [typeof(int?)] = DbType.Int32,
                          [typeof(uint?)] = DbType.UInt32,
                          [typeof(long?)] = DbType.Int64,
                          [typeof(ulong?)] = DbType.UInt64,
                          [typeof(float?)] = DbType.Single,
                          [typeof(double?)] = DbType.Double,
                          [typeof(decimal?)] = DbType.Decimal,
                          [typeof(bool?)] = DbType.Boolean,
                          [typeof(char?)] = DbType.StringFixedLength,
                          [typeof(Guid?)] = DbType.Guid,
                          [typeof(DateTime?)] = DbType.DateTime,
                          [typeof(DateTimeOffset?)] = DbType.DateTimeOffset,
                          [typeof(TimeSpan?)] = DbType.Time,
                          [typeof(object)] = DbType.Object
                      };
            ResetTypeHandlers(false);
        }

        /// <summary>
        /// Clear the registered type handlers
        /// </summary>
        public static void ResetTypeHandlers()
        {
            ResetTypeHandlers(true);
        }
        private static void ResetTypeHandlers(bool clone)
        {
            typeHandlers = new Dictionary<Type, ITypeHandler>();
#if !COREFX
            AddTypeHandlerImpl(typeof(DataTable), new DataTableHandler(), clone);
            try // see https://github.com/StackExchange/dapper-dot-net/issues/424
            {
                AddSqlDataRecordsTypeHandler(clone);
            } catch { }
            allowedCommandBehaviors = DefaultAllowedCommandBehaviors;
#endif
        }
#if !COREFX
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddSqlDataRecordsTypeHandler(bool clone)
        {
            AddTypeHandlerImpl(typeof(IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord>), new SqlDataRecordHandler(), clone);
        }
#endif

        /// <summary>
        /// Configure the specified type to be mapped to a given db-type
        /// </summary>
        public static void AddTypeMap(Type type, DbType dbType)
        {
            // use clone, mutate, replace to avoid threading issues
            var snapshot = typeMap;

            DbType oldValue;
            if (snapshot.TryGetValue(type, out oldValue) && oldValue == dbType) return; // nothing to do

            var newCopy = new Dictionary<Type, DbType>(snapshot) { [type] = dbType };
            typeMap = newCopy;
        }

        /// <summary>
        /// Configure the specified type to be processed by a custom handler
        /// </summary>
        public static void AddTypeHandler(Type type, ITypeHandler handler)
        {
            AddTypeHandlerImpl(type, handler, true);
        }

        internal static bool HasTypeHandler(Type type)
        {
            return typeHandlers.ContainsKey(type);
        }

        /// <summary>
        /// Configure the specified type to be processed by a custom handler
        /// </summary>
        public static void AddTypeHandlerImpl(Type type, ITypeHandler handler, bool clone)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            Type secondary = null;
            if(type.IsValueType())
            {
                var underlying = Nullable.GetUnderlyingType(type);
                if(underlying == null)
                {
                    secondary = typeof(Nullable<>).MakeGenericType(type); // the Nullable<T>
                    // type is already the T
                }
                else
                {
                    secondary = type; // the Nullable<T>
                    type = underlying; // the T
                }
            }

            var snapshot = typeHandlers;
            ITypeHandler oldValue;
            if (snapshot.TryGetValue(type, out oldValue) && handler == oldValue) return; // nothing to do

            var newCopy = clone ? new Dictionary<Type, ITypeHandler>(snapshot) : snapshot;

#pragma warning disable 618
            typeof(TypeHandlerCache<>).MakeGenericType(type).GetMethod(nameof(TypeHandlerCache<int>.SetHandler), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { handler });
            if(secondary != null)
            {
                typeof(TypeHandlerCache<>).MakeGenericType(secondary).GetMethod(nameof(TypeHandlerCache<int>.SetHandler), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { handler });
            }
#pragma warning restore 618
            if (handler == null)
            {
                newCopy.Remove(type);
                if (secondary != null) newCopy.Remove(secondary);
            }
            else
            {
                newCopy[type] = handler;
                if(secondary != null) newCopy[secondary] = handler;
            }
            typeHandlers = newCopy;
        }

        /// <summary>
        /// Configure the specified type to be processed by a custom handler
        /// </summary>
        public static void AddTypeHandler<T>(TypeHandler<T> handler)
        {
            AddTypeHandlerImpl(typeof(T), handler, true);
        }

        private static Dictionary<Type, ITypeHandler> typeHandlers;

        internal const string LinqBinary = "System.Data.Linq.Binary";

        private const string ObsoleteInternalUsageOnly = "This method is for internal use only";

        /// <summary>
        /// Get the DbType that maps to a given value
        /// </summary>
        [Obsolete(ObsoleteInternalUsageOnly, false)]
#if !COREFX
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static DbType GetDbType(object value)
        {
            if (value == null || value is DBNull) return DbType.Object;

            ITypeHandler handler;
            return LookupDbType(value.GetType(), "n/a", false, out handler);

        }
        [Obsolete(ObsoleteInternalUsageOnly, false)]
#if !COREFX
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static DbType LookupDbType(Type type, string name, bool demand, out ITypeHandler handler)
        {
            DbType dbType;
            handler = null;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.IsEnum() && !typeMap.ContainsKey(type))
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (typeMap.TryGetValue(type, out dbType))
            {
                return dbType;
            }
            if (type.FullName == LinqBinary)
            {
                return DbType.Binary;
            }
            if (typeHandlers.TryGetValue(type, out handler))
            {
                return DbType.Object;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return DynamicParameters.EnumerableMultiParameter;
            }

#if !COREFX
            switch (type.FullName)
            {
                case "Microsoft.SqlServer.Types.SqlGeography":
                    AddTypeHandler(type, handler = new UdtTypeHandler("geography"));
                    return DbType.Object;
                case "Microsoft.SqlServer.Types.SqlGeometry":
                    AddTypeHandler(type, handler = new UdtTypeHandler("geometry"));
                    return DbType.Object;
                case "Microsoft.SqlServer.Types.SqlHierarchyId":
                    AddTypeHandler(type, handler = new UdtTypeHandler("hierarchyid"));
                    return DbType.Object;
            }
#endif
            if(demand)
                throw new NotSupportedException($"The member {name} of type {type.FullName} cannot be used as a parameter value");
            return DbType.Object;

        }



        /// <summary>
        /// Obtains the data as a list; if it is *already* a list, the original object is returned without
        /// any duplication; otherwise, ToList() is invoked.
        /// </summary>
        public static List<T> AsList<T>(this IEnumerable<T> source)
        {
            return (source == null || source is List<T>) ? (List<T>)source : source.ToList();
        }

        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
            return ExecuteImpl(cnn, ref command);
        }
        /// <summary>
        /// Execute parameterized SQL
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteImpl(cnn, ref command);
        }


        /// <summary>
        /// Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static object ExecuteScalar(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
            return ExecuteScalarImpl<object>(cnn, ref command);
        }

        /// <summary>
        /// Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static T ExecuteScalar<T>(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
            return ExecuteScalarImpl<T>(cnn, ref command);
        }

        /// <summary>
        /// Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static object ExecuteScalar(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteScalarImpl<object>(cnn, ref command);
        }

        /// <summary>
        /// Execute parameterized SQL that selects a single value
        /// </summary>
        /// <returns>The first cell selected</returns>
        public static T ExecuteScalar<T>(this IDbConnection cnn, CommandDefinition command)
        {
            return ExecuteScalarImpl<T>(cnn, ref command);
        }

        private static IEnumerable GetMultiExec(object param)
        {
            return (param is IEnumerable &&
                    !(param is string ||
                      param is IEnumerable<KeyValuePair<string, object>> ||
                      param is IDynamicParameters)
                ) ? (IEnumerable) param : null;
        }

        private static int ExecuteImpl(this IDbConnection cnn, ref CommandDefinition command)
        {
            object param = command.Parameters;
            IEnumerable multiExec = GetMultiExec(param);
            Identity identity;
            CacheInfo info = null;
            if (multiExec != null)
            {
#if ASYNC
                if((command.Flags & CommandFlags.Pipelined) != 0)
                {
                    // this includes all the code for concurrent/overlapped query
                    return ExecuteMultiImplAsync(cnn, command, multiExec).Result;
                }
#endif
                bool isFirst = true;
                int total = 0;
                bool wasClosed = cnn.State == ConnectionState.Closed;
                try
                {
                    if (wasClosed) cnn.Open();
                    using (var cmd = command.SetupCommand(cnn, null))
                    {
                        string masterSql = null;
                        foreach (var obj in multiExec)
                        {
                            if (isFirst)
                            {
                                masterSql = cmd.CommandText;
                                isFirst = false;
                                identity = new Identity(command.CommandText, cmd.CommandType, cnn, null, obj.GetType(), null);
                                info = GetCacheInfo(identity, obj, command.AddToCache);
                            }
                            else
                            {
                                cmd.CommandText = masterSql; // because we do magic replaces on "in" etc
                                cmd.Parameters.Clear(); // current code is Add-tastic
                            }
                            info.ParamReader(cmd, obj);
                            total += cmd.ExecuteNonQuery();
                        }
                    }
                    command.OnCompleted();
                } finally
                {
                    if (wasClosed) cnn.Close();
                }
                return total;
            }

            // nice and simple
            if (param != null)
            {
                identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType(), null);
                info = GetCacheInfo(identity, param, command.AddToCache);
            }
            return ExecuteCommand(cnn, ref command, param == null ? null : info.ParamReader);
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
        /// or <see cref="DataSet"/>.
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
        public static IDataReader ExecuteReader(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
            IDbCommand dbcmd;
            var reader = ExecuteReaderImpl(cnn, ref command, CommandBehavior.Default, out dbcmd);
            return new WrappedReader(dbcmd, reader);
        }

        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
        /// or <see cref="DataSet"/>.
        /// </remarks>
        public static IDataReader ExecuteReader(this IDbConnection cnn, CommandDefinition command)
        {
            IDbCommand dbcmd;
            var reader = ExecuteReaderImpl(cnn, ref command, CommandBehavior.Default, out dbcmd);
            return new WrappedReader(dbcmd, reader);
        }
        /// <summary>
        /// Execute parameterized SQL and return an <see cref="IDataReader"/>
        /// </summary>
        /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
        /// or <see cref="DataSet"/>.
        /// </remarks>
        public static IDataReader ExecuteReader(this IDbConnection cnn, CommandDefinition command, CommandBehavior commandBehavior)
        {
            IDbCommand dbcmd;
            var reader = ExecuteReaderImpl(cnn, ref command, commandBehavior, out dbcmd);
            return new WrappedReader(dbcmd, reader);
        }

        /// <summary>
        /// Return a sequence of dynamic objects with properties matching the columns
        /// </summary>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return Query<DapperRow>(cnn, sql, param as object, transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        /// Return a dynamic object with properties matching the columns
        /// </summary>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static dynamic QueryFirst(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return QueryFirst<DapperRow>(cnn, sql, param as object, transaction, commandTimeout, commandType);
        }
        /// <summary>
        /// Return a dynamic object with properties matching the columns
        /// </summary>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static dynamic QueryFirstOrDefault(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return QueryFirstOrDefault<DapperRow>(cnn, sql, param as object, transaction, commandTimeout, commandType);
        }
        /// <summary>
        /// Return a dynamic object with properties matching the columns
        /// </summary>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static dynamic QuerySingle(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return QuerySingle<DapperRow>(cnn, sql, param as object, transaction, commandTimeout, commandType);
        }
        /// <summary>
        /// Return a dynamic object with properties matching the columns
        /// </summary>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public static dynamic QuerySingleOrDefault(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return QuerySingleOrDefault<DapperRow>(cnn, sql, param as object, transaction, commandTimeout, commandType);
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
            var data = QueryImpl<T>(cnn, command, typeof(T));
            return command.Buffered ? data.ToList() : data;
        }

        /// <summary>
        /// Executes a single-row query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QueryFirst<T>(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<T>(cnn, Row.First, ref command, typeof(T));
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QueryFirstOrDefault<T>(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<T>(cnn, Row.FirstOrDefault, ref command, typeof(T));
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QuerySingle<T>(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<T>(cnn, Row.Single, ref command, typeof(T));
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QuerySingleOrDefault<T>(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<T>(cnn, Row.SingleOrDefault, ref command, typeof(T));
        }

        /// <summary>
        /// Executes a single-row query, returning the data typed as per the Type suggested
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<object> Query(
            this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
            var data = QueryImpl<object>(cnn, command, type);
            return command.Buffered ? data.ToList() : data;
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per the Type suggested
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static object QueryFirst(
            this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType,  CommandFlags.None);
            return QueryRowImpl<object>(cnn, Row.First, ref command, type);
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per the Type suggested
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static object QueryFirstOrDefault(
            this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<object>(cnn, Row.FirstOrDefault, ref command, type);
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per the Type suggested
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static object QuerySingle(
            this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<object>(cnn, Row.Single, ref command, type);
        }
        /// <summary>
        /// Executes a single-row query, returning the data typed as per the Type suggested
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static object QuerySingleOrDefault(
            this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
            return QueryRowImpl<object>(cnn, Row.SingleOrDefault, ref command, type);
        }
        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, CommandDefinition command)
        {
            var data = QueryImpl<T>(cnn, command, typeof(T));
            return command.Buffered ? data.ToList() : data;
        }

        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QueryFirst<T>(this IDbConnection cnn, CommandDefinition command)
        {
            return QueryRowImpl<T>(cnn, Row.First, ref command, typeof(T));
        }
        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QueryFirstOrDefault<T>(this IDbConnection cnn, CommandDefinition command)
        {
            return QueryRowImpl<T>(cnn, Row.FirstOrDefault, ref command, typeof(T));
        }
        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QuerySingle<T>(this IDbConnection cnn, CommandDefinition command)
        {
            return QueryRowImpl<T>(cnn, Row.Single, ref command, typeof(T));
        }
        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static T QuerySingleOrDefault<T>(this IDbConnection cnn, CommandDefinition command)
        {
            return QueryRowImpl<T>(cnn, Row.SingleOrDefault, ref command, typeof(T));
        }


        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(
            this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
        )
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
            return QueryMultipleImpl(cnn, ref command);
        }
        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(this IDbConnection cnn, CommandDefinition command)
        {
            return QueryMultipleImpl(cnn, ref command);
        }

        private static GridReader QueryMultipleImpl(this IDbConnection cnn, ref CommandDefinition command)
        {
            object param = command.Parameters;
            Identity identity = new Identity(command.CommandText, command.CommandType, cnn, typeof(GridReader), param?.GetType(), null);
            CacheInfo info = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand cmd = null;
            IDataReader reader = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed) cnn.Open();
                cmd = command.SetupCommand(cnn, info.ParamReader);
                reader = ExecuteReaderWithFlagsFallback(cmd, wasClosed, CommandBehavior.SequentialAccess);

                var result = new GridReader(cmd, reader, identity, command.Parameters as DynamicParameters, command.AddToCache);
                cmd = null; // now owned by result
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
                    if (!reader.IsClosed) try { cmd?.Cancel(); }
                        catch { /* don't spoil the existing exception */ }
                    reader.Dispose();
                }
                cmd?.Dispose();
                if (wasClosed) cnn.Close();
                throw;
            }
        }
        private static IDataReader ExecuteReaderWithFlagsFallback(IDbCommand cmd, bool wasClosed, CommandBehavior behavior)
        {
            try
            {
                return cmd.ExecuteReader(GetBehavior(wasClosed, behavior));
            }
            catch (ArgumentException ex)
            { // thanks, Sqlite!
                if (DisableCommandBehaviorOptimizations(behavior, ex))
                {
                    // we can retry; this time it will have different flags
                    return cmd.ExecuteReader(GetBehavior(wasClosed, behavior));
                }
                throw;
            }
        }
        private static IEnumerable<T> QueryImpl<T>(this IDbConnection cnn, CommandDefinition command, Type effectiveType)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, effectiveType, param?.GetType(), null);
            var info = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand cmd = null;
            IDataReader reader = null;

            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = command.SetupCommand(cnn, info.ParamReader);

                if (wasClosed) cnn.Open();
                reader = ExecuteReaderWithFlagsFallback(cmd, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
                wasClosed = false; // *if* the connection was closed and we got this far, then we now have a reader
                // with the CloseConnection flag, so the reader will deal with the connection; we
                // still need something in the "finally" to ensure that broken SQL still results
                // in the connection closing itself
                var tuple = info.Deserializer;
                int hash = GetColumnHash(reader);
                if (tuple.Func == null || tuple.Hash != hash)
                {
                    if (reader.FieldCount == 0) //https://code.google.com/p/dapper-dot-net/issues/detail?id=57
                        yield break;
                    tuple = info.Deserializer = new DeserializerState(hash, GetDeserializer(effectiveType, reader, 0, -1, false));
                    if(command.AddToCache) SetQueryCache(identity, info);
                }

                var func = tuple.Func;
                var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                while (reader.Read())
                {
                    object val = func(reader);
					if (val == null || val is T) {
                        yield return (T)val;
                    } else {
                        yield return (T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
                    }
                }
                while (reader.NextResult()) { }
                // happy path; close the reader cleanly - no
                // need for "Cancel" etc
                reader.Dispose();
                reader = null;

                command.OnCompleted();
            }
            finally
            {
                if (reader != null)
                {
                    if (!reader.IsClosed) try { cmd.Cancel(); }
                        catch { /* don't spoil the existing exception */ }
                    reader.Dispose();
                }
                if (wasClosed) cnn.Close();
                cmd?.Dispose();
            }
        }

        [Flags]
        internal enum Row
        {
            First = 0,
            FirstOrDefault = 1, //  &FirstOrDefault != 0: allow zero rows
            Single = 2, // & Single != 0: demand at least one row
            SingleOrDefault = 3
        }
        static readonly int[] ErrTwoRows = new int[2], ErrZeroRows = new int[0];
        static void ThrowMultipleRows(Row row)
        {
            switch (row)
            {  // get the standard exception from the runtime
                case Row.Single: ErrTwoRows.Single(); break;
                case Row.SingleOrDefault: ErrTwoRows.SingleOrDefault(); break;
                default: throw new InvalidOperationException();
            }
        }
        static void ThrowZeroRows(Row row)
        {
            switch (row)
            { // get the standard exception from the runtime
                case Row.First: ErrZeroRows.First(); break;
                case Row.Single: ErrZeroRows.Single(); break;
                default: throw new InvalidOperationException();
            }
        }
        private static T QueryRowImpl<T>(IDbConnection cnn, Row row, ref CommandDefinition command, Type effectiveType)
        {
            object param = command.Parameters;
            var identity = new Identity(command.CommandText, command.CommandType, cnn, effectiveType, param?.GetType(), null);
            var info = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand cmd = null;
            IDataReader reader = null;

            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = command.SetupCommand(cnn, info.ParamReader);

                if (wasClosed) cnn.Open();
                reader = ExecuteReaderWithFlagsFallback(cmd, wasClosed, (row & Row.Single) != 0
                    ? CommandBehavior.SequentialAccess | CommandBehavior.SingleResult // need to allow multiple rows, to check fail condition
                    : CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
                wasClosed = false; // *if* the connection was closed and we got this far, then we now have a reader

                T result = default(T);
                if (reader.Read() && reader.FieldCount != 0)
                {
                    // with the CloseConnection flag, so the reader will deal with the connection; we
                    // still need something in the "finally" to ensure that broken SQL still results
                    // in the connection closing itself
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
                    if ((row & Row.Single) != 0 && reader.Read()) ThrowMultipleRows(row);
                    while (reader.Read()) { }
                }
                else if ((row & Row.FirstOrDefault) == 0) // demanding a row, and don't have one
                {
                    ThrowZeroRows(row);
                }
                while (reader.NextResult()) { }
                // happy path; close the reader cleanly - no
                // need for "Cancel" etc
                reader.Dispose();
                reader = null;

                command.OnCompleted();
                return result;
            }
            finally
            {
                if (reader != null)
                {
                    if (!reader.IsClosed) try { cmd.Cancel(); }
                        catch { /* don't spoil the existing exception */ }
                    reader.Dispose();
                }
                if (wasClosed) cnn.Close();
                cmd?.Dispose();
            }
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        /// <typeparam name="TFirst">The first type in the record set</typeparam>
        /// <typeparam name="TSecond">The second type in the record set</typeparam>
        /// <typeparam name="TReturn">The return type</typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
        )
        {
            return MultiMap<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
        )
        {
            return MultiMap<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Perform a multi mapping query with 4 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
        )
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Perform a multi mapping query with 5 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TFifth"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Perform a multi mapping query with 6 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TFifth"></typeparam>
        /// <typeparam name="TSixth"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }


        /// <summary>
        /// Perform a multi mapping query with 7 input parameters
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TThird"></typeparam>
        /// <typeparam name="TFourth"></typeparam>
        /// <typeparam name="TFifth"></typeparam>
        /// <typeparam name="TSixth"></typeparam>
        /// <typeparam name="TSeventh"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        /// Perform a multi mapping query with arbitrary input parameters
        /// </summary>
        /// <typeparam name="TReturn">The return type</typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="types">array of types in the record set</param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns></returns>
        public static IEnumerable<TReturn> Query<TReturn>(this IDbConnection cnn, string sql, Type[] types, Func<object[], TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
            var results = MultiMapImpl<TReturn>(cnn, command, types, map, splitOn, null, null, true);
            return buffered ? results.ToList() : results;
        }

        static IEnumerable<TReturn> MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
            this IDbConnection cnn, string sql, Delegate map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
            var results = MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, command, map, splitOn, null, null, true);
            return buffered ? results.ToList() : results;
        }

        static IEnumerable<TReturn> MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, CommandDefinition command, Delegate map, string splitOn, IDataReader reader, Identity identity, bool finalize)
        {
            object param = command.Parameters;
            identity = identity ?? new Identity(command.CommandText, command.CommandType, cnn, typeof(TFirst), param?.GetType(), new[] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh) });
            CacheInfo cinfo = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand ownedCommand = null;
            IDataReader ownedReader = null;

            bool wasClosed = cnn != null && cnn.State == ConnectionState.Closed;
            try
            {
                if (reader == null)
                {
                    ownedCommand = command.SetupCommand(cnn, cinfo.ParamReader);
                    if (wasClosed) cnn.Open();
                    ownedReader = ExecuteReaderWithFlagsFallback(ownedCommand, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
                    reader = ownedReader;
                }
                DeserializerState deserializer = default(DeserializerState);
                Func<IDataReader, object>[] otherDeserializers;

                int hash = GetColumnHash(reader);
                if ((deserializer = cinfo.Deserializer).Func == null || (otherDeserializers = cinfo.OtherDeserializers) == null || hash != deserializer.Hash)
                {
                    var deserializers = GenerateDeserializers(new [] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh) }, splitOn, reader);
                    deserializer = cinfo.Deserializer = new DeserializerState(hash, deserializers[0]);
                    otherDeserializers = cinfo.OtherDeserializers = deserializers.Skip(1).ToArray();
                    if(command.AddToCache) SetQueryCache(identity, cinfo);
                }

                Func<IDataReader, TReturn> mapIt = GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(deserializer.Func, otherDeserializers, map);

                if (mapIt != null)
                {
                    while (reader.Read())
                    {
                        yield return mapIt(reader);
                    }
                    if(finalize)
                    {
                        while (reader.NextResult()) { }
                        command.OnCompleted();
                    }
                }
            }
            finally
            {
                try
                {
                    ownedReader?.Dispose();
                }
                finally
                {
                    ownedCommand?.Dispose();
                    if (wasClosed) cnn.Close();
                }
            }
        }
        const CommandBehavior DefaultAllowedCommandBehaviors = ~((CommandBehavior)0);
        static CommandBehavior allowedCommandBehaviors = DefaultAllowedCommandBehaviors;
        private static bool DisableCommandBehaviorOptimizations(CommandBehavior behavior, Exception ex)
        {
            if(allowedCommandBehaviors == DefaultAllowedCommandBehaviors
                && (behavior & (CommandBehavior.SingleResult | CommandBehavior.SingleRow)) != 0)
            {
                if (ex.Message.Contains(nameof(CommandBehavior.SingleResult))
                    || ex.Message.Contains(nameof(CommandBehavior.SingleRow)))
                { // some providers just just allow these, so: try again without them and stop issuing them
                    allowedCommandBehaviors = ~(CommandBehavior.SingleResult | CommandBehavior.SingleRow);
                    return true;
                }
            }
            return false;
        }
        private static CommandBehavior GetBehavior(bool close, CommandBehavior @default)
        {
            return (close ? (@default | CommandBehavior.CloseConnection) : @default) & allowedCommandBehaviors;
        }
        static IEnumerable<TReturn> MultiMapImpl<TReturn>(this IDbConnection cnn, CommandDefinition command, Type[] types, Func<object[], TReturn> map, string splitOn, IDataReader reader, Identity identity, bool finalize)
        {
            if (types.Length < 1)
            {
                throw new ArgumentException("you must provide at least one type to deserialize");
            }

            object param = command.Parameters;
            identity = identity ?? new Identity(command.CommandText, command.CommandType, cnn, types[0], param?.GetType(), types);
            CacheInfo cinfo = GetCacheInfo(identity, param, command.AddToCache);

            IDbCommand ownedCommand = null;
            IDataReader ownedReader = null;

            bool wasClosed = cnn != null && cnn.State == ConnectionState.Closed;
            try
            {
                if (reader == null)
                {
                    ownedCommand = command.SetupCommand(cnn, cinfo.ParamReader);
                    if (wasClosed) cnn.Open();
                    ownedReader = ExecuteReaderWithFlagsFallback(ownedCommand, wasClosed, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
                    reader = ownedReader;
                }
                DeserializerState deserializer;
                Func<IDataReader, object>[] otherDeserializers;

                int hash = GetColumnHash(reader);
                if ((deserializer = cinfo.Deserializer).Func == null || (otherDeserializers = cinfo.OtherDeserializers) == null || hash != deserializer.Hash)
                {
                    var deserializers = GenerateDeserializers(types, splitOn, reader);
                    deserializer = cinfo.Deserializer = new DeserializerState(hash, deserializers[0]);
                    otherDeserializers = cinfo.OtherDeserializers = deserializers.Skip(1).ToArray();
                    SetQueryCache(identity, cinfo);
                }

                Func<IDataReader, TReturn> mapIt = GenerateMapper(types.Length, deserializer.Func, otherDeserializers, map);

                if (mapIt != null)
                {
                    while (reader.Read())
                    {
                        yield return mapIt(reader);
                    }
                    if (finalize)
                    {
                        while (reader.NextResult()) { }
                        command.OnCompleted();
                    }
                }
            }
            finally
            {
                try
                {
                    ownedReader?.Dispose();
                }
                finally
                {
                    ownedCommand?.Dispose();
                    if (wasClosed) cnn.Close();
                }
            }
        }

        private static Func<IDataReader, TReturn> GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, object map)
        {
            switch (otherDeserializers.Length)
            {
                case 1:
                    return r => ((Func<TFirst, TSecond, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r));
                case 2:
                    return r => ((Func<TFirst, TSecond, TThird, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r));
                case 3:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r));
                case 4:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r));
                case 5:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r), (TSixth)otherDeserializers[4](r));
                case 6:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r), (TSixth)otherDeserializers[4](r), (TSeventh)otherDeserializers[5](r));
                default:
                    throw new NotSupportedException();
            }
        }

        private static Func<IDataReader, TReturn> GenerateMapper<TReturn>(int length, Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, Func<object[], TReturn> map)
        {
            return r =>
            {
                var objects = new object[length];
                objects[0] = deserializer(r);

                for (var i = 1; i < length; ++i)
                {
                    objects[i] = otherDeserializers[i - 1](r);
                }

                return map(objects);
            };
        }

        private static Func<IDataReader, object>[] GenerateDeserializers(Type[] types, string splitOn, IDataReader reader)
        {
            var deserializers = new List<Func<IDataReader, object>>();
            var splits = splitOn.Split(',').Select(s => s.Trim()).ToArray();
                bool isMultiSplit = splits.Length > 1;
            if (types.First() == typeof(object))
            {
                // we go left to right for dynamic multi-mapping so that the madness of TestMultiMappingVariations
                // is supported
                bool first = true;
                int currentPos = 0;
                int splitIdx = 0;
                string currentSplit = splits[splitIdx];
                foreach (var type in types)
                {
                    if (type == typeof(DontMap))
                    {
                        break;
                    }

                    int splitPoint = GetNextSplitDynamic(currentPos, currentSplit, reader);
                    if (isMultiSplit && splitIdx < splits.Length - 1)
                    {
                        currentSplit = splits[++splitIdx];
                    }
                    deserializers.Add((GetDeserializer(type, reader, currentPos, splitPoint - currentPos, !first)));
                    currentPos = splitPoint;
                    first = false;
                }
            }
            else
            {
                // in this we go right to left through the data reader in order to cope with properties that are
                // named the same as a subsequent primary key that we split on
                int currentPos = reader.FieldCount;
                int splitIdx = splits.Length - 1;
                var currentSplit = splits[splitIdx];
                for (var typeIdx = types.Length - 1; typeIdx >= 0; --typeIdx)
                {
                    var type = types[typeIdx];
                    if (type == typeof (DontMap))
                    {
                        continue;
                    }

                    int splitPoint = 0;
                    if (typeIdx > 0)
                    {
                        splitPoint = GetNextSplit(currentPos, currentSplit, reader);
                        if (isMultiSplit && splitIdx > 0)
                        {
                            currentSplit = splits[--splitIdx];
                        }
                    }

                    deserializers.Add((GetDeserializer(type, reader, splitPoint, currentPos - splitPoint, typeIdx > 0)));
                    currentPos = splitPoint;
                }

                deserializers.Reverse();

            }
            return deserializers.ToArray();
        }

        private static int GetNextSplitDynamic(int startIdx, string splitOn, IDataReader reader)
        {
            if (startIdx == reader.FieldCount)
            {
                throw MultiMapException(reader);
            }

            if (splitOn == "*")
            {
                return ++startIdx;
            }

            for (var i = startIdx + 1; i < reader.FieldCount; ++i)
            {
                if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return reader.FieldCount;
        }

        private static int GetNextSplit(int startIdx, string splitOn, IDataReader reader)
        {
            if (splitOn == "*")
            {
                return --startIdx;
            }

            for (var i = startIdx - 1; i > 0; --i)
            {
                if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw MultiMapException(reader);
        }

        private static CacheInfo GetCacheInfo(Identity identity, object exampleParameters, bool addToCache)
        {
            CacheInfo info;
            if (!TryGetQueryCache(identity, out info))
            {
                if(GetMultiExec(exampleParameters) != null)
                {
                    throw new InvalidOperationException("An enumerable sequence of parameters (arrays, lists, etc) is not allowed in this context");
                }
                info = new CacheInfo();
                if (identity.parametersType != null)
                {
                    Action<IDbCommand, object> reader;
                    if (exampleParameters is IDynamicParameters)
                    {
                        reader = (cmd, obj) => { ((IDynamicParameters)obj).AddParameters(cmd, identity); };
                    }
                    else if (exampleParameters is IEnumerable<KeyValuePair<string, object>>)
                    {
                        reader = (cmd, obj) =>
                        {
                            IDynamicParameters mapped = new DynamicParameters(obj);
                            mapped.AddParameters(cmd, identity);
                        };
                    }
                    else
                    {
                        var literals = GetLiteralTokens(identity.sql);
                        reader = CreateParamInfoGenerator(identity, false, true, literals);
                    }
                    if((identity.commandType == null || identity.commandType == CommandType.Text) && ShouldPassByPosition(identity.sql))
                    {
                        var tail = reader;
                        reader = (cmd, obj) =>
                        {
                            tail(cmd, obj);
                            PassByPosition(cmd);
                        };
                    }
                    info.ParamReader = reader;
                }
                if(addToCache) SetQueryCache(identity, info);
            }
            return info;
        }

        private static bool ShouldPassByPosition(string sql)
        {
            return sql != null && sql.IndexOf('?') >= 0 && pseudoPositional.IsMatch(sql);
        }

        private static void PassByPosition(IDbCommand cmd)
        {
            if (cmd.Parameters.Count == 0) return;

            Dictionary<string, IDbDataParameter> parameters = new Dictionary<string, IDbDataParameter>(StringComparer.Ordinal);

            foreach(IDbDataParameter param in cmd.Parameters)
            {
                if (!string.IsNullOrEmpty(param.ParameterName)) parameters[param.ParameterName] = param;
            }
            HashSet<string> consumed = new HashSet<string>(StringComparer.Ordinal);
            bool firstMatch = true;
            cmd.CommandText = pseudoPositional.Replace(cmd.CommandText, match =>
            {
                string key = match.Groups[1].Value;
                IDbDataParameter param;
                if (!consumed.Add(key))
                {
                    throw new InvalidOperationException("When passing parameters by position, each parameter can only be referenced once");
                }
                else if (parameters.TryGetValue(key, out param))
                {
                    if(firstMatch)
                    {
                        firstMatch = false;
                        cmd.Parameters.Clear(); // only clear if we are pretty positive that we've found this pattern successfully
                    }
                    // if found, return the anonymous token "?"
                    cmd.Parameters.Add(param);
                    parameters.Remove(key);
                    consumed.Add(key);
                    return "?";
                }
                else
                {
                    // otherwise, leave alone for simple debugging
                    return match.Value;
                }
            });
        }

        private static Func<IDataReader, object> GetDeserializer(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
        {

            // dynamic is passed in as Object ... by c# design
            if (type == typeof(object)
                || type == typeof(DapperRow))
            {
                return GetDapperRowDeserializer(reader, startBound, length, returnNullIfFirstMissing);
            }
            Type underlyingType = null;
            if (!(typeMap.ContainsKey(type) || type.IsEnum() || type.FullName == LinqBinary ||
                (type.IsValueType()  && (underlyingType = Nullable.GetUnderlyingType(type)) != null && underlyingType.IsEnum())))
            {
                ITypeHandler handler;
                if (typeHandlers.TryGetValue(type, out handler))
                {
                    return GetHandlerDeserializer(handler, type, startBound);
                }
                return GetTypeDeserializer(type, reader, startBound, length, returnNullIfFirstMissing);
            }
            return GetStructDeserializer(type, underlyingType ?? type, startBound);
        }
        private static Func<IDataReader, object> GetHandlerDeserializer(ITypeHandler handler, Type type, int startBound)
        {
            return reader => handler.Parse(type, reader.GetValue(startBound));
        }


        private static Exception MultiMapException(IDataRecord reader)
        {
            bool hasFields = false;
            try {
                hasFields = reader != null && reader.FieldCount != 0;
            } catch { }
            if (hasFields)
                return new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
            else
                return new InvalidOperationException("No columns were selected");
        }

        internal static Func<IDataReader, object> GetDapperRowDeserializer(IDataRecord reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
            var fieldCount = reader.FieldCount;
            if (length == -1)
            {
                length = fieldCount - startBound;
            }

            if (fieldCount <= startBound)
            {
                throw MultiMapException(reader);
            }

            var effectiveFieldCount = Math.Min(fieldCount - startBound, length);

            DapperTable table = null;

            return
                r =>
                {
                    if (table == null)
                    {
                        string[] names = new string[effectiveFieldCount];
                        for (int i = 0; i < effectiveFieldCount; i++)
                        {
                            names[i] = r.GetName(i + startBound);
                        }
                        table = new DapperTable(names);
                    }

                    var values = new object[effectiveFieldCount];

                    if (returnNullIfFirstMissing)
                    {
                        values[0] = r.GetValue(startBound);
                        if (values[0] is DBNull)
                        {
                            return null;
                        }
                    }

                    if (startBound == 0)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            object val = r.GetValue(i);
                            values[i] = val is DBNull ? null : val;
                        }
                    }
                    else
                    {
                        var begin = returnNullIfFirstMissing ? 1 : 0;
                        for (var iter = begin; iter < effectiveFieldCount; ++iter)
                        {
                            object obj = r.GetValue(iter + startBound);
                            values[iter] = obj is DBNull ? null : obj;
                        }
                    }
                    return new DapperRow(table, values);
                };
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
#if !COREFX
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(ObsoleteInternalUsageOnly, false)]
        public static char ReadChar(object value)
        {
            if (value == null || value is DBNull) throw new ArgumentNullException(nameof(value));
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", nameof(value));
            return s[0];
        }

        /// <summary>
        /// Internal use only
        /// </summary>
#if !COREFX
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(ObsoleteInternalUsageOnly, false)]
        public static char? ReadNullableChar(object value)
        {
            if (value == null || value is DBNull) return null;
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", nameof(value));
            return s[0];
        }


        /// <summary>
        /// Internal use only
        /// </summary>
#if !COREFX
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(ObsoleteInternalUsageOnly, true)]
        public static IDbDataParameter FindOrAddParameter(IDataParameterCollection parameters, IDbCommand command, string name)
        {
            IDbDataParameter result;
            if (parameters.Contains(name))
            {
                result = (IDbDataParameter)parameters[name];
            }
            else
            {
                result = command.CreateParameter();
                result.ParameterName = name;
                parameters.Add(result);
            }
            return result;
        }

        internal static int GetListPaddingExtraCount(int count)
        {
            switch(count)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    return 0; // no padding
            }
            if (count < 0) return 0;

            int padFactor;
            if (count <= 150) padFactor = 10;
            else if (count <= 750) padFactor = 50;
            else if (count <= 2000) padFactor = 100; // note: max param count for SQL Server
            else if (count <= 2070) padFactor = 10; // try not to over-pad as we approach that limit
            else if (count <= 2100) return 0; // just don't pad between 2070 and 2100, to minimize the crazy
            else padFactor = 200; // above that, all bets are off!

            // if we have 17, factor = 10; 17 % 10 = 7, we need 3 more
            int intoBlock = count % padFactor;
            return intoBlock == 0 ? 0 : (padFactor - intoBlock);
        }

        /// <summary>
        /// Internal use only
        /// </summary>
#if !COREFX
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(ObsoleteInternalUsageOnly, false)]
        public static void PackListParameters(IDbCommand command, string namePrefix, object value)
        {
            // initially we tried TVP, however it performs quite poorly.
            // keep in mind SQL support up to 2000 params easily in sp_executesql, needing more is rare

            if (FeatureSupport.Get(command.Connection).Arrays)
            {
                var arrayParm = command.CreateParameter();
                arrayParm.Value = SanitizeParameterValue(value);
                arrayParm.ParameterName = namePrefix;
                command.Parameters.Add(arrayParm);
            }
            else
            {
                var list = value as IEnumerable;
                var count = 0;
                bool isString = value is IEnumerable<string>;
                bool isDbString = value is IEnumerable<DbString>;
                DbType dbType = 0;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (++count == 1) // first item: fetch some type info
                        {
                            if(item == null)
                            {
                                throw new NotSupportedException("The first item in a list-expansion cannot be null");
                            }
                            if (!isDbString)
                            {
                                ITypeHandler handler;
                                dbType = LookupDbType(item.GetType(), "", true, out handler);
                            }
                        }
                        var listParam = command.CreateParameter();
                        listParam.ParameterName = namePrefix + count.ToString();
                        if (isString)
                        {
                            listParam.Size = DbString.DefaultLength;
                            if (item != null && ((string) item).Length > DbString.DefaultLength)
                            {
                                listParam.Size = -1;
                            }
                        }
                        if (isDbString && item as DbString != null)
                        {
                            var str = item as DbString;
                            str.AddParameter(command, listParam.ParameterName);
                        }
                        else
                        {
                            listParam.Value = SanitizeParameterValue(item);
                            if (listParam.DbType != dbType)
                            {
                                listParam.DbType = dbType;
                            }
                            command.Parameters.Add(listParam);
                        }
                    }
                    if (Settings.PadListExpansions && !isDbString)
                    {
                        int padCount = GetListPaddingExtraCount(count);
                        for(int i = 0; i < padCount; i++)
                        {
                            count++;
                            var padParam = command.CreateParameter();
                            padParam.ParameterName = namePrefix + count.ToString();
                            if(isString) padParam.Size = DbString.DefaultLength;
                            padParam.DbType = dbType;
                            padParam.Value = DBNull.Value;
                            command.Parameters.Add(padParam);
                        }
                    }
                }

                var regexIncludingUnknown = @"([?@:]" + Regex.Escape(namePrefix) + @")(?!\w)(\s+(?i)unknown(?-i))?";
                if (count == 0)
                {
                    command.CommandText = Regex.Replace(command.CommandText, regexIncludingUnknown, match =>
                    {
                        var variableName = match.Groups[1].Value;
                        if (match.Groups[2].Success)
                        {
                            // looks like an optimize hint; leave it alone!
                            return match.Value;
                        }
                        else
                        {
                            return "(SELECT " + variableName + " WHERE 1 = 0)";
                        }
                    }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                    var dummyParam = command.CreateParameter();
                    dummyParam.ParameterName = namePrefix;
                    dummyParam.Value = DBNull.Value;
                    command.Parameters.Add(dummyParam);
                }
                else
                {
                    command.CommandText = Regex.Replace(command.CommandText, regexIncludingUnknown, match =>
                    {
                        var variableName = match.Groups[1].Value;
                        if (match.Groups[2].Success)
                        {
                            // looks like an optimize hint; expand it
                            var suffix = match.Groups[2].Value;

                            var sb = GetStringBuilder().Append(variableName).Append(1).Append(suffix);
                            for (int i = 2; i <= count; i++)
                            {
                                sb.Append(',').Append(variableName).Append(i).Append(suffix);
                            }
                            return sb.__ToStringRecycle();
                        }
                        else
                        {
                            var sb = GetStringBuilder().Append('(').Append(variableName).Append(1);
                            for (int i = 2; i <= count; i++)
                            {
                                sb.Append(',').Append(variableName).Append(i);
                            }
                            return sb.Append(')').__ToStringRecycle();
                        }
                    }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                }
            }

        }

        [Obsolete(ObsoleteInternalUsageOnly, false)]
        public static object SanitizeParameterValue(object value)
        {
            if (value == null) return DBNull.Value;
            if (value is Enum)
            {
                TypeCode typeCode;
                if (value is IConvertible)
                {
                    typeCode = ((IConvertible)value).GetTypeCode();
                }
                else
                {
                    typeCode = TypeExtensions.GetTypeCode(Enum.GetUnderlyingType(value.GetType()));
                }
                switch (typeCode)
                {
                    case TypeCode.Byte: return (byte)value;
                    case TypeCode.SByte: return (sbyte)value;
                    case TypeCode.Int16: return (short)value;
                    case TypeCode.Int32: return (int)value;
                    case TypeCode.Int64: return (long)value;
                    case TypeCode.UInt16: return (ushort)value;
                    case TypeCode.UInt32: return (uint)value;
                    case TypeCode.UInt64: return (ulong)value;
                }
            }
            return value;
        }
        private static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
        {
            return parameters.Where(p => Regex.IsMatch(sql, @"[?@:]" + p.Name + "([^a-z0-9_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant));
        }

        // look for ? / @ / : *by itself*
        static readonly Regex smellsLikeOleDb = new Regex(@"(?<![a-z0-9@_])[?@:](?![a-z0-9@_])", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled),
            literalTokens = new Regex(@"(?<![a-z0-9_])\{=([a-z0-9_]+)\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled),
            pseudoPositional = new Regex(@"\?([a-z_][a-z0-9_]*)\?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);



        /// <summary>
        /// Replace all literal tokens with their text form
        /// </summary>
        public static void ReplaceLiterals(this IParameterLookup parameters, IDbCommand command)
        {
            var tokens = GetLiteralTokens(command.CommandText);
            if (tokens.Count != 0) ReplaceLiterals(parameters, command, tokens);
        }

        internal static readonly MethodInfo format = typeof(SqlMapper).GetMethod("Format", BindingFlags.Public | BindingFlags.Static);
        /// <summary>
        /// Convert numeric values to their string form for SQL literal purposes
        /// </summary>
        [Obsolete(ObsoleteInternalUsageOnly)]
        public static string Format(object value)
        {
            if (value == null)
            {
                return "null";
            }
            else
            {
                switch (TypeExtensions.GetTypeCode(value.GetType()))
                {
#if !COREFX
                    case TypeCode.DBNull:
                        return "null";
#endif
                    case TypeCode.Boolean:
                        return ((bool)value) ? "1" : "0";
                    case TypeCode.Byte:
                        return ((byte)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.SByte:
                        return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.UInt16:
                        return ((ushort)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.Int16:
                        return ((short)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.UInt32:
                        return ((uint)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.Int32:
                        return ((int)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.UInt64:
                        return ((ulong)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.Int64:
                        return ((long)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.Single:
                        return ((float)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.Double:
                        return ((double)value).ToString(CultureInfo.InvariantCulture);
                    case TypeCode.Decimal:
                        return ((decimal)value).ToString(CultureInfo.InvariantCulture);
                    default:
                        var multiExec = GetMultiExec(value);
                        if(multiExec != null)
                        {
                            StringBuilder sb = null;
                            bool first = true;
                            foreach (object subval in multiExec)
                            {
                                if(first)
                                {
                                    sb = GetStringBuilder().Append('(');
                                    first = false;
                                }
                                else
                                {
                                    sb.Append(',');
                                }
                                sb.Append(Format(subval));
                            }
                            if(first)
                            {
                                return "(select null where 1=0)";
                            }
                            else
                            {
                                return sb.Append(')').__ToStringRecycle();
                            }
                        }
                        throw new NotSupportedException(value.GetType().Name);
                }
            }
        }


        internal static void ReplaceLiterals(IParameterLookup parameters, IDbCommand command, IList<LiteralToken> tokens)
        {
            var sql = command.CommandText;
            foreach (var token in tokens)
            {
                object value = parameters[token.Member];
#pragma warning disable 0618
                string text = Format(value);
#pragma warning restore 0618
                sql = sql.Replace(token.Token, text);
            }
            command.CommandText = sql;
        }

        internal static IList<LiteralToken> GetLiteralTokens(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return LiteralToken.None;
            if (!literalTokens.IsMatch(sql)) return LiteralToken.None;

            var matches = literalTokens.Matches(sql);
            var found = new HashSet<string>(StringComparer.Ordinal);
            List<LiteralToken> list = new List<LiteralToken>(matches.Count);
            foreach(Match match in matches)
            {
                string token = match.Value;
                if(found.Add(match.Value))
                {
                    list.Add(new LiteralToken(token, match.Groups[1].Value));
                }
            }
            return list.Count == 0 ? LiteralToken.None : list;
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        public static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity, bool checkForDuplicates, bool removeUnused)
        {
            return CreateParamInfoGenerator(identity, checkForDuplicates, removeUnused, GetLiteralTokens(identity.sql));
        }

        internal static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity, bool checkForDuplicates, bool removeUnused, IList<LiteralToken> literals)
        {
            Type type = identity.parametersType;

            bool filterParams = false;
            if (removeUnused && identity.commandType.GetValueOrDefault(CommandType.Text) == CommandType.Text)
            {
                filterParams = !smellsLikeOleDb.IsMatch(identity.sql);
            }
            var dm = new DynamicMethod($"ParamInfo{Guid.NewGuid()}", null, new[] { typeof(IDbCommand), typeof(object) }, type, true);

            var il = dm.GetILGenerator();

            bool isStruct = type.IsValueType();
            bool haveInt32Arg1 = false;
            il.Emit(OpCodes.Ldarg_1); // stack is now [untyped-param]
            if (isStruct)
            {
                il.DeclareLocal(type.MakePointerType());
                il.Emit(OpCodes.Unbox, type); // stack is now [typed-param]
            }
            else
            {
                il.DeclareLocal(type); // 0
                il.Emit(OpCodes.Castclass, type); // stack is now [typed-param]
            }
            il.Emit(OpCodes.Stloc_0);// stack is now empty

            il.Emit(OpCodes.Ldarg_0); // stack is now [command]
            il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetProperty(nameof(IDbCommand.Parameters)).GetGetMethod(), null); // stack is now [parameters]

            var propsArr = type.GetProperties().Where(p => p.GetIndexParameters().Length == 0).ToArray();
            var ctors = type.GetConstructors();
            ParameterInfo[] ctorParams;
            IEnumerable<PropertyInfo> props = null;
            // try to detect tuple patterns, e.g. anon-types, and use that to choose the order
            // otherwise: alphabetical
            if (ctors.Length == 1 && propsArr.Length == (ctorParams = ctors[0].GetParameters()).Length)
            {
                // check if reflection was kind enough to put everything in the right order for us
                bool ok = true;
                for (int i = 0; i < propsArr.Length; i++)
                {
                    if (!string.Equals(propsArr[i].Name, ctorParams[i].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ok = false;
                        break;
                    }
                }
                if(ok)
                {
                    // pre-sorted; the reflection gods have smiled upon us
                    props = propsArr;
                }
                else { // might still all be accounted for; check the hard way
                    var positionByName = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
                    foreach(var param in ctorParams)
                    {
                        positionByName[param.Name] = param.Position;
                    }
                    if (positionByName.Count == propsArr.Length)
                    {
                        int[] positions = new int[propsArr.Length];
                        ok = true;
                        for (int i = 0; i < propsArr.Length; i++)
                        {
                            int pos;
                            if (!positionByName.TryGetValue(propsArr[i].Name, out pos))
                            {
                                ok = false;
                                break;
                            }
                            positions[i] = pos;
                        }
                        if (ok)
                        {
                            Array.Sort(positions, propsArr);
                            props = propsArr;
                        }
                    }
                }
            }
            if(props == null) props = propsArr.OrderBy(x => x.Name);
            if (filterParams)
            {
                props = FilterParameters(props, identity.sql);
            }

            var callOpCode = isStruct ? OpCodes.Call : OpCodes.Callvirt;
            foreach (var prop in props)
            {
                if (typeof(ICustomQueryParameter).IsAssignableFrom(prop.PropertyType))
                {
                    il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [typed-param]
                    il.Emit(callOpCode, prop.GetGetMethod()); // stack is [parameters] [custom]
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [custom] [command]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [custom] [command] [name]
                    il.EmitCall(OpCodes.Callvirt, prop.PropertyType.GetMethod(nameof(ICustomQueryParameter.AddParameter)), null); // stack is now [parameters]
                    continue;
                }
                ITypeHandler handler;
#pragma warning disable 618
                DbType dbType = LookupDbType(prop.PropertyType, prop.Name, true, out handler);
#pragma warning restore 618
                if (dbType == DynamicParameters.EnumerableMultiParameter)
                {
                    // this actually represents special handling for list types;
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [command]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [command] [name]
                    il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [command] [name] [typed-param]
                    il.Emit(callOpCode, prop.GetGetMethod()); // stack is [parameters] [command] [name] [typed-value]
                    if (prop.PropertyType.IsValueType())
                    {
                        il.Emit(OpCodes.Box, prop.PropertyType); // stack is [parameters] [command] [name] [boxed-value]
                    }
                    il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(nameof(SqlMapper.PackListParameters)), null); // stack is [parameters]
                    continue;
                }
                il.Emit(OpCodes.Dup); // stack is now [parameters] [parameters]

                il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [parameters] [command]

                if (checkForDuplicates)
                {
                    // need to be a little careful about adding; use a utility method
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [parameters] [command] [name]
                    il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(nameof(SqlMapper.FindOrAddParameter)), null); // stack is [parameters] [parameter]
                }
                else
                {
                    // no risk of duplicates; just blindly add
                    il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetMethod(nameof(IDbCommand.CreateParameter)), null);// stack is now [parameters] [parameters] [parameter]

                    il.Emit(OpCodes.Dup);// stack is now [parameters] [parameters] [parameter] [parameter]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [parameters] [parameter] [parameter] [name]
                    il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty(nameof(IDataParameter.ParameterName)).GetSetMethod(), null);// stack is now [parameters] [parameters] [parameter]
                }
                if (dbType != DbType.Time && handler == null) // https://connect.microsoft.com/VisualStudio/feedback/details/381934/sqlparameter-dbtype-dbtype-time-sets-the-parameter-to-sqldbtype-datetime-instead-of-sqldbtype-time
                {
                    il.Emit(OpCodes.Dup);// stack is now [parameters] [[parameters]] [parameter] [parameter]
                    if (dbType == DbType.Object && prop.PropertyType == typeof(object)) // includes dynamic
                    {
                        // look it up from the param value
                        il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [[parameters]] [parameter] [parameter] [typed-param]
                        il.Emit(callOpCode, prop.GetGetMethod()); // stack is [parameters] [[parameters]] [parameter] [parameter] [object-value]
                        il.Emit(OpCodes.Call, typeof(SqlMapper).GetMethod(nameof(SqlMapper.GetDbType), BindingFlags.Static | BindingFlags.Public)); // stack is now [parameters] [[parameters]] [parameter] [parameter] [db-type]
                    }
                    else
                    {
                        // constant value; nice and simple
                        EmitInt32(il, (int)dbType);// stack is now [parameters] [[parameters]] [parameter] [parameter] [db-type]
                    }
                    il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty(nameof(IDataParameter.DbType)).GetSetMethod(), null);// stack is now [parameters] [[parameters]] [parameter]
                }

                il.Emit(OpCodes.Dup);// stack is now [parameters] [[parameters]] [parameter] [parameter]
                EmitInt32(il, (int)ParameterDirection.Input);// stack is now [parameters] [[parameters]] [parameter] [parameter] [dir]
                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty(nameof(IDataParameter.Direction)).GetSetMethod(), null);// stack is now [parameters] [[parameters]] [parameter]

                il.Emit(OpCodes.Dup);// stack is now [parameters] [[parameters]] [parameter] [parameter]
                il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [[parameters]] [parameter] [parameter] [typed-param]
                il.Emit(callOpCode, prop.GetGetMethod()); // stack is [parameters] [[parameters]] [parameter] [parameter] [typed-value]
                bool checkForNull = true;
                if (prop.PropertyType.IsValueType())
                {
                    var propType = prop.PropertyType;
                    var nullType = Nullable.GetUnderlyingType(propType);
                    bool callSanitize = false;
                    
                    if((nullType ?? propType).IsEnum())
                    {
                        if(nullType != null)
                        {
                            // Nullable<SomeEnum>; we want to box as the underlying type; that's just *hard*; for
                            // simplicity, box as Nullable<SomeEnum> and call SanitizeParameterValue
                            callSanitize = true;
                        }
                        else
                        {
                            // non-nullable enum; we can do that! just box to the wrong type! (no, really)
                            switch (TypeExtensions.GetTypeCode(Enum.GetUnderlyingType(propType)))
                            {
                                case TypeCode.Byte: propType = typeof(byte); break;
                                case TypeCode.SByte: propType = typeof(sbyte); break;
                                case TypeCode.Int16: propType = typeof(short); break;
                                case TypeCode.Int32: propType = typeof(int); break;
                                case TypeCode.Int64: propType = typeof(long); break;
                                case TypeCode.UInt16: propType = typeof(ushort); break;
                                case TypeCode.UInt32: propType = typeof(uint); break;
                                case TypeCode.UInt64: propType = typeof(ulong); break;
                            }
                        }                        
                    }
                    else if (nullType == null)
                    {   // struct but not Nullable<T>; boxed value cannot be null
                        checkForNull = false;
                    }
                    il.Emit(OpCodes.Box, propType); // stack is [parameters] [[parameters]] [parameter] [parameter] [boxed-value]
                    if (callSanitize)
                    {
                        checkForNull = false; // handled by sanitize
                        il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(nameof(SanitizeParameterValue)), null);
                        // stack is [parameters] [[parameters]] [parameter] [parameter] [boxed-value]
                    }
                }
                if (checkForNull)
                {
                    if ((dbType == DbType.String || dbType == DbType.AnsiString) && !haveInt32Arg1)
                    {
                        il.DeclareLocal(typeof(int));
                        haveInt32Arg1 = true;
                    }
                    // relative stack: [boxed value]
                    il.Emit(OpCodes.Dup);// relative stack: [boxed value] [boxed value]
                    Label notNull = il.DefineLabel();
                    Label? allDone = (dbType == DbType.String || dbType == DbType.AnsiString) ? il.DefineLabel() : (Label?)null;
                    il.Emit(OpCodes.Brtrue_S, notNull);
                    // relative stack [boxed value = null]
                    il.Emit(OpCodes.Pop); // relative stack empty
                    il.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField(nameof(DBNull.Value))); // relative stack [DBNull]
                    if (dbType == DbType.String || dbType == DbType.AnsiString)
                    {
                        EmitInt32(il, 0);
                        il.Emit(OpCodes.Stloc_1);
                    }
                    if (allDone != null) il.Emit(OpCodes.Br_S, allDone.Value);
                    il.MarkLabel(notNull);
                    if (prop.PropertyType == typeof(string))
                    {
                        il.Emit(OpCodes.Dup); // [string] [string]
                        il.EmitCall(OpCodes.Callvirt, typeof(string).GetProperty(nameof(string.Length)).GetGetMethod(), null); // [string] [length]
                        EmitInt32(il, DbString.DefaultLength); // [string] [length] [4000]
                        il.Emit(OpCodes.Cgt); // [string] [0 or 1]
                        Label isLong = il.DefineLabel(), lenDone = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, isLong);
                        EmitInt32(il, DbString.DefaultLength); // [string] [4000]
                        il.Emit(OpCodes.Br_S, lenDone);
                        il.MarkLabel(isLong);
                        EmitInt32(il, -1); // [string] [-1]
                        il.MarkLabel(lenDone);
                        il.Emit(OpCodes.Stloc_1); // [string]
                    }
                    if (prop.PropertyType.FullName == LinqBinary)
                    {
                        il.EmitCall(OpCodes.Callvirt, prop.PropertyType.GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance), null);
                    }
                    if (allDone != null) il.MarkLabel(allDone.Value);
                    // relative stack [boxed value or DBNull]
                }

                if (handler != null)
                {
#pragma warning disable 618
                    il.Emit(OpCodes.Call, typeof(TypeHandlerCache<>).MakeGenericType(prop.PropertyType).GetMethod(nameof(TypeHandlerCache<int>.SetValue))); // stack is now [parameters] [[parameters]] [parameter]
#pragma warning restore 618
                }
                else
                {
                    il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty(nameof(IDataParameter.Value)).GetSetMethod(), null);// stack is now [parameters] [[parameters]] [parameter]
                }

                if (prop.PropertyType == typeof(string))
                {
                    var endOfSize = il.DefineLabel();
                    // don't set if 0
                    il.Emit(OpCodes.Ldloc_1); // [parameters] [[parameters]] [parameter] [size]
                    il.Emit(OpCodes.Brfalse_S, endOfSize); // [parameters] [[parameters]] [parameter]

                    il.Emit(OpCodes.Dup);// stack is now [parameters] [[parameters]] [parameter] [parameter]
                    il.Emit(OpCodes.Ldloc_1); // stack is now [parameters] [[parameters]] [parameter] [parameter] [size]
                    il.EmitCall(OpCodes.Callvirt, typeof(IDbDataParameter).GetProperty(nameof(IDbDataParameter.Size)).GetSetMethod(), null); // stack is now [parameters] [[parameters]] [parameter]

                    il.MarkLabel(endOfSize);
                }
                if (checkForDuplicates)
                {
                    // stack is now [parameters] [parameter]
                    il.Emit(OpCodes.Pop); // don't need parameter any more
                }
                else
                {
                    // stack is now [parameters] [parameters] [parameter]
                    // blindly add
                    il.EmitCall(OpCodes.Callvirt, typeof(IList).GetMethod(nameof(IList.Add)), null); // stack is now [parameters]
                    il.Emit(OpCodes.Pop); // IList.Add returns the new index (int); we don't care
                }
            }

            // stack is currently [parameters]
            il.Emit(OpCodes.Pop); // stack is now empty

            if(literals.Count != 0 && propsArr != null)
            {
                il.Emit(OpCodes.Ldarg_0); // command
                il.Emit(OpCodes.Ldarg_0); // command, command
                var cmdText = typeof(IDbCommand).GetProperty(nameof(IDbCommand.CommandText));
                il.EmitCall(OpCodes.Callvirt, cmdText.GetGetMethod(), null); // command, sql
                Dictionary<Type, LocalBuilder> locals = null;
                LocalBuilder local = null;
                foreach (var literal in literals)
                {
                    // find the best member, preferring case-sensitive
                    PropertyInfo exact = null, fallback = null;
                    string huntName = literal.Member;
                    for(int i = 0; i < propsArr.Length;i++)
                    {
                        string thisName = propsArr[i].Name;
                        if(string.Equals(thisName, huntName, StringComparison.OrdinalIgnoreCase))
                        {
                            fallback = propsArr[i];
                            if(string.Equals(thisName, huntName, StringComparison.Ordinal))
                            {
                                exact = fallback;
                                break;
                            }
                        }
                    }
                    var prop = exact ?? fallback;

                    if(prop != null)
                    {
                        il.Emit(OpCodes.Ldstr, literal.Token);
                        il.Emit(OpCodes.Ldloc_0); // command, sql, typed parameter
                        il.EmitCall(callOpCode, prop.GetGetMethod(), null); // command, sql, typed value
                        Type propType = prop.PropertyType;
                        var typeCode = TypeExtensions.GetTypeCode(propType);
                        switch (typeCode)
                        {
                            case TypeCode.Boolean:
                                Label ifTrue = il.DefineLabel(), allDone = il.DefineLabel();
                                il.Emit(OpCodes.Brtrue_S, ifTrue);
                                il.Emit(OpCodes.Ldstr, "0");
                                il.Emit(OpCodes.Br_S, allDone);
                                il.MarkLabel(ifTrue);
                                il.Emit(OpCodes.Ldstr, "1");
                                il.MarkLabel(allDone);
                                break;
                            case TypeCode.Byte:
                            case TypeCode.SByte:
                            case TypeCode.UInt16:
                            case TypeCode.Int16:
                            case TypeCode.UInt32:
                            case TypeCode.Int32:
                            case TypeCode.UInt64:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                // need to stloc, ldloca, call
                                // re-use existing locals (both the last known, and via a dictionary)
                                var convert = GetToString(typeCode);
                                if (local == null || local.LocalType != propType)
                                {
                                    if (locals == null)
                                    {
                                        locals = new Dictionary<Type, LocalBuilder>();
                                        local = null;
                                    }
                                    else
                                    {
                                        if (!locals.TryGetValue(propType, out local)) local = null;
                                    }
                                    if (local == null)
                                    {
                                        local = il.DeclareLocal(propType);
                                        locals.Add(propType, local);
                                    }
                                }
                                il.Emit(OpCodes.Stloc, local); // command, sql
                                il.Emit(OpCodes.Ldloca, local); // command, sql, ref-to-value
                                il.EmitCall(OpCodes.Call, InvariantCulture, null); // command, sql, ref-to-value, culture
                                il.EmitCall(OpCodes.Call, convert, null); // command, sql, string value
                                break;
                            default:
                                if (propType.IsValueType()) il.Emit(OpCodes.Box, propType); // command, sql, object value
                                il.EmitCall(OpCodes.Call, format, null); // command, sql, string value
                                break;

                        }
                        il.EmitCall(OpCodes.Callvirt, StringReplace, null);
                    }
                }
                il.EmitCall(OpCodes.Callvirt, cmdText.GetSetMethod(), null); // empty
            }

            il.Emit(OpCodes.Ret);
            return (Action<IDbCommand, object>)dm.CreateDelegate(typeof(Action<IDbCommand, object>));
        }
        static readonly Dictionary<TypeCode, MethodInfo> toStrings = new[]
        {
            typeof(bool), typeof(sbyte), typeof(byte), typeof(ushort), typeof(short),
            typeof(uint), typeof(int), typeof(ulong), typeof(long), typeof(float), typeof(double), typeof(decimal)
        }.ToDictionary(x => TypeExtensions.GetTypeCode(x), x => x.GetPublicInstanceMethod(nameof(object.ToString), new[] { typeof(IFormatProvider) }));
        static MethodInfo GetToString(TypeCode typeCode)
        {
            MethodInfo method;
            return toStrings.TryGetValue(typeCode, out method) ? method : null;
        }
        static readonly MethodInfo StringReplace = typeof(string).GetPublicInstanceMethod(nameof(string.Replace), new Type[] { typeof(string), typeof(string) }),
            InvariantCulture = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture), BindingFlags.Public | BindingFlags.Static).GetGetMethod();

        private static int ExecuteCommand(IDbConnection cnn, ref CommandDefinition command, Action<IDbCommand, object> paramReader)
        {
            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = command.SetupCommand(cnn, paramReader);
                if (wasClosed) cnn.Open();
                int result = cmd.ExecuteNonQuery();
                command.OnCompleted();
                return result;
            }
            finally
            {
                if (wasClosed) cnn.Close();
                cmd?.Dispose();
            }
        }

        private static T ExecuteScalarImpl<T>(IDbConnection cnn, ref CommandDefinition command)
        {
            Action<IDbCommand, object> paramReader = null;
            object param = command.Parameters;
            if (param != null)
            {
                var identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType(), null);
                paramReader = GetCacheInfo(identity, command.Parameters, command.AddToCache).ParamReader;
            }

            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            object result;
            try
            {
                cmd = command.SetupCommand(cnn, paramReader);
                if (wasClosed) cnn.Open();
                result =cmd.ExecuteScalar();
                command.OnCompleted();
            }
            finally
            {
                if (wasClosed) cnn.Close();
                cmd?.Dispose();
            }
            return Parse<T>(result);
        }

        private static IDataReader ExecuteReaderImpl(IDbConnection cnn, ref CommandDefinition command, CommandBehavior commandBehavior, out IDbCommand cmd)
        {
            Action<IDbCommand, object> paramReader = GetParameterReader(cnn, ref command);
            cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed, disposeCommand = true;
            try
            {
                cmd = command.SetupCommand(cnn, paramReader);
                if (wasClosed) cnn.Open();
                var reader = ExecuteReaderWithFlagsFallback(cmd, wasClosed, commandBehavior);
                wasClosed = false; // don't dispose before giving it to them!
                disposeCommand = false;
                // note: command.FireOutputCallbacks(); would be useless here; parameters come at the **end** of the TDS stream
                return reader;
            }
            finally
            {
                if (wasClosed) cnn.Close();
                if (cmd != null && disposeCommand) cmd.Dispose();
            }
        }

        private static Action<IDbCommand, object> GetParameterReader(IDbConnection cnn, ref CommandDefinition command)
        {
            object param = command.Parameters;
            IEnumerable multiExec = GetMultiExec(param);
            CacheInfo info = null;
            if (multiExec != null)
            {
                throw new NotSupportedException("MultiExec is not supported by ExecuteReader");
            }

            // nice and simple
            if (param != null)
            {
                var identity = new Identity(command.CommandText, command.CommandType, cnn, null, param.GetType(), null);
                info = GetCacheInfo(identity, param, command.AddToCache);
            }
            var paramReader = info?.ParamReader;
            return paramReader;
        }

        private static Func<IDataReader, object> GetStructDeserializer(Type type, Type effectiveType, int index)
        {
            // no point using special per-type handling here; it boils down to the same, plus not all are supported anyway (see: SqlDataReader.GetChar - not supported!)
#pragma warning disable 618
            if (type == typeof(char))
            { // this *does* need special handling, though
                return r => ReadChar(r.GetValue(index));
            }
            if (type == typeof(char?))
            {
                return r => ReadNullableChar(r.GetValue(index));
            }
            if (type.FullName == LinqBinary)
            {
                return r => Activator.CreateInstance(type, r.GetValue(index));
            }
#pragma warning restore 618

            if (effectiveType.IsEnum())
            {   // assume the value is returned as the correct type (int/byte/etc), but box back to the typed enum
                return r =>
                {
                    var val = r.GetValue(index);
                    if(val is float || val is double || val is decimal)
                    {
                        val = Convert.ChangeType(val, Enum.GetUnderlyingType(effectiveType), CultureInfo.InvariantCulture);
                    }
                    return val is DBNull ? null : Enum.ToObject(effectiveType, val);
                };
            }
            ITypeHandler handler;
            if(typeHandlers.TryGetValue(type, out handler))
            {
                return r =>
                {
                    var val = r.GetValue(index);
                    return val is DBNull ? null : handler.Parse(type, val);
                };
            }
            return r =>
            {
                var val = r.GetValue(index);
                return val is DBNull ? null : val;
            };
        }

        private static T Parse<T>(object value)
        {
            if (value == null || value is DBNull) return default(T);
            if (value is T) return (T)value;
            var type = typeof(T);
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum())
            {
                if (value is float || value is double || value is decimal)
                {
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                }
                return (T)Enum.ToObject(type, value);
            }
            ITypeHandler handler;
            if (typeHandlers.TryGetValue(type, out handler))
            {
                return (T)handler.Parse(type, value);
            }
            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        static readonly MethodInfo
                    enumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new Type[] { typeof(Type), typeof(string), typeof(bool) }),
                    getItem = typeof(IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.GetIndexParameters().Any() && p.GetIndexParameters()[0].ParameterType == typeof(int))
                        .Select(p => p.GetGetMethod()).First();

        /// <summary>
        /// Gets type-map for the given type
        /// </summary>
        /// <returns>Type map instance, default is to create new instance of DefaultTypeMap</returns>
        public static Func<Type, ITypeMap> TypeMapProvider = ( Type type ) => new DefaultTypeMap( type );

        /// <summary>
        /// Gets type-map for the given type
        /// </summary>
        /// <returns>Type map implementation, DefaultTypeMap instance if no override present</returns>
        public static ITypeMap GetTypeMap(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var map = (ITypeMap)_typeMaps[type];
            if (map == null)
            {
                lock (_typeMaps)
                {   // double-checked; store this to avoid reflection next time we see this type
                    // since multiple queries commonly use the same domain-entity/DTO/view-model type
                    map = (ITypeMap)_typeMaps[type];

                    if (map == null)
                    {
                        map = TypeMapProvider( type );
                        _typeMaps[type] = map;
                    }
                }
            }
            return map;
        }

        // use Hashtable to get free lockless reading
        private static readonly Hashtable _typeMaps = new Hashtable();

        /// <summary>
        /// Set custom mapping for type deserializers
        /// </summary>
        /// <param name="type">Entity type to override</param>
        /// <param name="map">Mapping rules impementation, null to remove custom map</param>
        public static void SetTypeMap(Type type, ITypeMap map)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (map == null || map is DefaultTypeMap)
            {
                lock (_typeMaps)
                {
                    _typeMaps.Remove(type);
                }
            }
            else
            {
                lock (_typeMaps)
                {
                    _typeMaps[type] = map;
                }
            }

            PurgeQueryCacheByType(type);
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="startBound"></param>
        /// <param name="length"></param>
        /// <param name="returnNullIfFirstMissing"></param>
        /// <returns></returns>
        public static Func<IDataReader, object> GetTypeDeserializer(
            Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false
        )
        {
            return TypeDeserializerCache.GetReader(type, reader, startBound, length, returnNullIfFirstMissing);
        }
        static LocalBuilder GetTempLocal(ILGenerator il, ref Dictionary<Type, LocalBuilder> locals, Type type, bool initAndLoad)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (locals == null) locals = new Dictionary<Type, LocalBuilder>();
            LocalBuilder found;
            if (!locals.TryGetValue(type, out found))
            {
                found = il.DeclareLocal(type);
                locals.Add(type, found);
            }
            if (initAndLoad)
            {
                il.Emit(OpCodes.Ldloca, (short)found.LocalIndex);
                il.Emit(OpCodes.Initobj, type);
                il.Emit(OpCodes.Ldloca, (short)found.LocalIndex);
                il.Emit(OpCodes.Ldobj, type);
            }
            return found;
        }
        private static Func<IDataReader, object> GetTypeDeserializerImpl(
            Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false
        )
        {
            var returnType = type.IsValueType() ? typeof(object) : type;
            var dm = new DynamicMethod($"Deserialize{Guid.NewGuid()}", returnType, new[] { typeof(IDataReader) }, type, true);
            var il = dm.GetILGenerator();
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(type);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);

            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            if (reader.FieldCount <= startBound)
            {
                throw MultiMapException(reader);
            }

            var names = Enumerable.Range(startBound, length).Select(i => reader.GetName(i)).ToArray();

            ITypeMap typeMap = GetTypeMap(type);

            int index = startBound;

            ConstructorInfo specializedConstructor = null;

#if !COREFX
            bool supportInitialize = false;
#endif
            Dictionary<Type, LocalBuilder> structLocals = null;
            if (type.IsValueType())
            {
                il.Emit(OpCodes.Ldloca_S, (byte)1);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                var types = new Type[length];
                for (int i = startBound; i < startBound + length; i++)
                {
                    types[i - startBound] = reader.GetFieldType(i);
                }

                var explicitConstr = typeMap.FindExplicitConstructor();
                if (explicitConstr != null)
                {
                    var consPs = explicitConstr.GetParameters();
                    foreach(var p in consPs)
                    {
                        if(!p.ParameterType.IsValueType())
                        {
                            il.Emit(OpCodes.Ldnull);
                        }
                        else
                        {
                            GetTempLocal(il, ref structLocals, p.ParameterType, true);
                        }
                    }

                    il.Emit(OpCodes.Newobj, explicitConstr);
                    il.Emit(OpCodes.Stloc_1);
#if !COREFX
                    supportInitialize = typeof(ISupportInitialize).IsAssignableFrom(type);
                    if (supportInitialize)
                    {
                        il.Emit(OpCodes.Ldloc_1);
                        il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.BeginInit)), null);
                    }
#endif
                }
                else
                {
                    var ctor = typeMap.FindConstructor(names, types);
                    if (ctor == null)
                    {
                        string proposedTypes = $"({string.Join(", ", types.Select((t, i) => t.FullName + " " + names[i]).ToArray())})";
                        throw new InvalidOperationException($"A parameterless default constructor or one matching signature {proposedTypes} is required for {type.FullName} materialization");
                    }

                    if (ctor.GetParameters().Length == 0)
                    {
                        il.Emit(OpCodes.Newobj, ctor);
                        il.Emit(OpCodes.Stloc_1);
#if !COREFX
                        supportInitialize = typeof(ISupportInitialize).IsAssignableFrom(type);
                        if (supportInitialize)
                        {
                            il.Emit(OpCodes.Ldloc_1);
                            il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.BeginInit)), null);
                        }
#endif
                    }
                    else
                    {
                        specializedConstructor = ctor;
                    }
                }
            }

            il.BeginExceptionBlock();
            if (type.IsValueType())
            {
                il.Emit(OpCodes.Ldloca_S, (byte)1);// [target]
            }
            else if (specializedConstructor == null)
            {
                il.Emit(OpCodes.Ldloc_1);// [target]
            }

            var members = (specializedConstructor != null
                ? names.Select(n => typeMap.GetConstructorParameter(specializedConstructor, n))
                : names.Select(n => typeMap.GetMember(n))).ToList();

            // stack is now [target]

            bool first = true;
            var allDone = il.DefineLabel();
            int enumDeclareLocal = -1, valueCopyLocal = il.DeclareLocal(typeof(object)).LocalIndex;
            bool applyNullSetting = Settings.ApplyNullValues;
            foreach (var item in members)
            {
                if (item != null)
                {
                    if (specializedConstructor == null)
                        il.Emit(OpCodes.Dup); // stack is now [target][target]
                    Label isDbNullLabel = il.DefineLabel();
                    Label finishLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0); // stack is now [target][target][reader]
                    EmitInt32(il, index); // stack is now [target][target][reader][index]
                    il.Emit(OpCodes.Dup);// stack is now [target][target][reader][index][index]
                    il.Emit(OpCodes.Stloc_0);// stack is now [target][target][reader][index]
                    il.Emit(OpCodes.Callvirt, getItem); // stack is now [target][target][value-as-object]
                    il.Emit(OpCodes.Dup); // stack is now [target][target][value-as-object][value-as-object]
                    StoreLocal(il, valueCopyLocal);
                    Type colType = reader.GetFieldType(index);
                    Type memberType = item.MemberType;

                    if (memberType == typeof(char) || memberType == typeof(char?))
                    {
                        il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(
                            memberType == typeof(char) ? nameof(SqlMapper.ReadChar) : nameof(SqlMapper.ReadNullableChar), BindingFlags.Static | BindingFlags.Public), null); // stack is now [target][target][typed-value]
                    }
                    else
                    {
                        il.Emit(OpCodes.Dup); // stack is now [target][target][value][value]
                        il.Emit(OpCodes.Isinst, typeof(DBNull)); // stack is now [target][target][value-as-object][DBNull or null]
                        il.Emit(OpCodes.Brtrue_S, isDbNullLabel); // stack is now [target][target][value-as-object]

                        // unbox nullable enums as the primitive, i.e. byte etc

                        var nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                        var unboxType = nullUnderlyingType != null && nullUnderlyingType.IsEnum() ? nullUnderlyingType : memberType;

                        if (unboxType.IsEnum())
                        {
                            Type numericType = Enum.GetUnderlyingType(unboxType);
                            if(colType == typeof(string))
                            {
                                if (enumDeclareLocal == -1)
                                {
                                    enumDeclareLocal = il.DeclareLocal(typeof(string)).LocalIndex;
                                }
                                il.Emit(OpCodes.Castclass, typeof(string)); // stack is now [target][target][string]
                                StoreLocal(il, enumDeclareLocal); // stack is now [target][target]
                                il.Emit(OpCodes.Ldtoken, unboxType); // stack is now [target][target][enum-type-token]
                                il.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)), null);// stack is now [target][target][enum-type]
                                LoadLocal(il, enumDeclareLocal); // stack is now [target][target][enum-type][string]
                                il.Emit(OpCodes.Ldc_I4_1); // stack is now [target][target][enum-type][string][true]
                                il.EmitCall(OpCodes.Call, enumParse, null); // stack is now [target][target][enum-as-object]
                                il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                            }
                            else
                            {
                                FlexibleConvertBoxedFromHeadOfStack(il, colType, unboxType, numericType);
                            }

                            if (nullUnderlyingType != null)
                            {
                                il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { nullUnderlyingType })); // stack is now [target][target][typed-value]
                            }
                        }
                        else if (memberType.FullName == LinqBinary)
                        {
                            il.Emit(OpCodes.Unbox_Any, typeof(byte[])); // stack is now [target][target][byte-array]
                            il.Emit(OpCodes.Newobj, memberType.GetConstructor(new Type[] { typeof(byte[]) }));// stack is now [target][target][binary]
                        }
                        else
                        {
                            TypeCode dataTypeCode = TypeExtensions.GetTypeCode(colType), unboxTypeCode = TypeExtensions.GetTypeCode(unboxType);
                            bool hasTypeHandler;
                            if ((hasTypeHandler = typeHandlers.ContainsKey(unboxType)) || colType == unboxType || dataTypeCode == unboxTypeCode || dataTypeCode == TypeExtensions.GetTypeCode(nullUnderlyingType))
                            {
                                if (hasTypeHandler)
                                {
#pragma warning disable 618
                                    il.EmitCall(OpCodes.Call, typeof(TypeHandlerCache<>).MakeGenericType(unboxType).GetMethod(nameof(TypeHandlerCache<int>.Parse)), null); // stack is now [target][target][typed-value]
#pragma warning restore 618
                                }
                                else
                                {
                                    il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                                }
                            }
                            else
                            {
                                // not a direct match; need to tweak the unbox
                                FlexibleConvertBoxedFromHeadOfStack(il, colType, nullUnderlyingType ?? unboxType, null);
                                if (nullUnderlyingType != null)
                                {
                                    il.Emit(OpCodes.Newobj, unboxType.GetConstructor(new[] { nullUnderlyingType })); // stack is now [target][target][typed-value]
                                }
                            }
                        }
                    }
                    if (specializedConstructor == null)
                    {
                        // Store the value in the property/field
                        if (item.Property != null)
                        {
                            il.Emit(type.IsValueType() ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetter(item.Property, type));
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }

                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][value]
                    if (specializedConstructor != null)
                    {
                        il.Emit(OpCodes.Pop);
                        if (item.MemberType.IsValueType())
                        {
                            int localIndex = il.DeclareLocal(item.MemberType).LocalIndex;
                            LoadLocalAddress(il, localIndex);
                            il.Emit(OpCodes.Initobj, item.MemberType);
                            LoadLocal(il, localIndex);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldnull);
                        }
                    }
                    else if(applyNullSetting && (!memberType.IsValueType() || Nullable.GetUnderlyingType(memberType) != null))
                    {
                        il.Emit(OpCodes.Pop); // stack is now [target][target]
                        // can load a null with this value
                        if (memberType.IsValueType())
                        { // must be Nullable<T> for some T
                            GetTempLocal(il, ref structLocals, memberType, true); // stack is now [target][target][null]
                        }
                        else
                        { // regular reference-type
                            il.Emit(OpCodes.Ldnull); // stack is now [target][target][null]
                        }

                        // Store the value in the property/field
                        if (item.Property != null)
                        {
                            il.Emit(type.IsValueType() ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetter(item.Property, type));
                            // stack is now [target]
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Pop); // stack is now [target][target]
                        il.Emit(OpCodes.Pop); // stack is now [target]
                    }

                    if (first && returnNullIfFirstMissing)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull); // stack is now [null]
                        il.Emit(OpCodes.Stloc_1);
                        il.Emit(OpCodes.Br, allDone);
                    }

                    il.MarkLabel(finishLabel);
                }
                first = false;
                index += 1;
            }
            if (type.IsValueType())
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                if (specializedConstructor != null)
                {
                    il.Emit(OpCodes.Newobj, specializedConstructor);
                }
                il.Emit(OpCodes.Stloc_1); // stack is empty
#if !COREFX
                if (supportInitialize)
                {
                    il.Emit(OpCodes.Ldloc_1);
                    il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.EndInit)), null);
                }
#endif
            }
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof(Exception)); // stack is Exception
            il.Emit(OpCodes.Ldloc_0); // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0); // stack is Exception, index, reader
            LoadLocal(il, valueCopyLocal); // stack is Exception, index, reader, value
            il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(nameof(SqlMapper.ThrowDataException)), null);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc_1); // stack is [rval]
            if (type.IsValueType())
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Ret);

            var funcType = System.Linq.Expressions.Expression.GetFuncType(typeof(IDataReader), returnType);
            return (Func<IDataReader, object>)dm.CreateDelegate(funcType);
        }

        private static void FlexibleConvertBoxedFromHeadOfStack(ILGenerator il, Type from, Type to, Type via)
        {
            MethodInfo op;
            if(from == (via ?? to))
            {
                il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
            }
            else if ((op = GetOperator(from,to)) != null)
            {
                // this is handy for things like decimal <===> double
                il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][data-typed-value]
                il.Emit(OpCodes.Call, op); // stack is now [target][target][typed-value]
            }
            else
            {
                bool handled = false;
                OpCode opCode = default(OpCode);
                switch (TypeExtensions.GetTypeCode(from))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        handled = true;
                        switch (TypeExtensions.GetTypeCode(via ?? to))
                        {
                            case TypeCode.Byte:
                                opCode = OpCodes.Conv_Ovf_I1_Un; break;
                            case TypeCode.SByte:
                                opCode = OpCodes.Conv_Ovf_I1; break;
                            case TypeCode.UInt16:
                                opCode = OpCodes.Conv_Ovf_I2_Un; break;
                            case TypeCode.Int16:
                                opCode = OpCodes.Conv_Ovf_I2; break;
                            case TypeCode.UInt32:
                                opCode = OpCodes.Conv_Ovf_I4_Un; break;
                            case TypeCode.Boolean: // boolean is basically an int, at least at this level
                            case TypeCode.Int32:
                                opCode = OpCodes.Conv_Ovf_I4; break;
                            case TypeCode.UInt64:
                                opCode = OpCodes.Conv_Ovf_I8_Un; break;
                            case TypeCode.Int64:
                                opCode = OpCodes.Conv_Ovf_I8; break;
                            case TypeCode.Single:
                                opCode = OpCodes.Conv_R4; break;
                            case TypeCode.Double:
                                opCode = OpCodes.Conv_R8; break;
                            default:
                                handled = false;
                                break;
                        }
                        break;
                }
                if (handled)
                {
                    il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][col-typed-value]
                    il.Emit(opCode); // stack is now [target][target][typed-value]
                    if (to == typeof(bool))
                    { // compare to zero; I checked "csc" - this is the trick it uses; nice
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Ldtoken, via ?? to); // stack is now [target][target][value][member-type-token]
                    il.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)), null); // stack is now [target][target][value][member-type]
                    il.EmitCall(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ChangeType), new Type[] { typeof(object), typeof(Type) }), null); // stack is now [target][target][boxed-member-type-value]
                    il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
                }
            }
        }

        static MethodInfo GetOperator(Type from, Type to)
        {
            if (to == null) return null;
            MethodInfo[] fromMethods, toMethods;
            return ResolveOperator(fromMethods = from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(toMethods = to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(fromMethods, from, to, "op_Explicit")
                ?? ResolveOperator(toMethods, from, to, "op_Explicit");
        }

        static MethodInfo ResolveOperator(MethodInfo[] methods, Type from, Type to, string name)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != name || methods[i].ReturnType != to) continue;
                var args = methods[i].GetParameters();
                if (args.Length != 1 || args[0].ParameterType != from) continue;
                return methods[i];
            }
            return null;
        }

        private static void LoadLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException(nameof(index));
            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldloc_0); break;
                case 1: il.Emit(OpCodes.Ldloc_1); break;
                case 2: il.Emit(OpCodes.Ldloc_2); break;
                case 3: il.Emit(OpCodes.Ldloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, (short)index);
                    }
                    break;
            }
        }
        private static void StoreLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException(nameof(index));
            switch (index)
            {
                case 0: il.Emit(OpCodes.Stloc_0); break;
                case 1: il.Emit(OpCodes.Stloc_1); break;
                case 2: il.Emit(OpCodes.Stloc_2); break;
                case 3: il.Emit(OpCodes.Stloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, (short)index);
                    }
                    break;
            }
        }

        private static void LoadLocalAddress(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException(nameof(index));

            if (index <= 255)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, (short)index);
            }
        }

        /// <summary>
        /// Throws a data exception, only used internally
        /// </summary>
        [Obsolete(ObsoleteInternalUsageOnly, false)]
        public static void ThrowDataException(Exception ex, int index, IDataReader reader, object value)
        {
            Exception toThrow;
            try
            {
                string name = "(n/a)", formattedValue = "(n/a)";
                if (reader != null && index >= 0 && index < reader.FieldCount)
                {
                    name = reader.GetName(index);
                    try
                    {
                        if (value == null || value is DBNull)
                        {
                            formattedValue = "<null>";
                        }
                        else
                        {
                            formattedValue = Convert.ToString(value) + " - " + TypeExtensions.GetTypeCode(value.GetType());
                        }
                    }
                    catch (Exception valEx)
                    {
                        formattedValue = valEx.Message;
                    }
                }
                toThrow = new DataException($"Error parsing column {index} ({name}={formattedValue})", ex);
            }
            catch
            { // throw the **original** exception, wrapped as DataException
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }

        private static void EmitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        /// <summary>
        /// How should connection strings be compared for equivalence? Defaults to StringComparer.Ordinal.
        /// Providing a custom implementation can be useful for allowing multi-tenancy databases with identical
        /// schema to share strategies. Note that usual equivalence rules apply: any equivalent connection strings
        /// <b>MUST</b> yield the same hash-code.
        /// </summary>
        public static IEqualityComparer<string> ConnectionStringComparer
        {
            get { return connectionStringComparer; }
            set { connectionStringComparer = value ?? StringComparer.Ordinal; }
        }
        private static IEqualityComparer<string> connectionStringComparer = StringComparer.Ordinal;

#if !COREFX
        /// <summary>
        /// Key used to indicate the type name associated with a DataTable
        /// </summary>
        private const string DataTableTypeNameKey = "dapper:TypeName";

        /// <summary>
        /// Used to pass a DataTable as a TableValuedParameter
        /// </summary>
        public static ICustomQueryParameter AsTableValuedParameter(this DataTable table, string typeName = null)
        {
            return new TableValuedParameter(table, typeName);
        }

        /// <summary>
        /// Associate a DataTable with a type name
        /// </summary>
        public static void SetTypeName(this DataTable table, string typeName)
        {
            if (table != null)
            {
                if (string.IsNullOrEmpty(typeName))
                    table.ExtendedProperties.Remove(DataTableTypeNameKey);
                else
                    table.ExtendedProperties[DataTableTypeNameKey] = typeName;
            }
        }

        /// <summary>
        /// Fetch the type name associated with a DataTable
        /// </summary>
        public static string GetTypeName(this DataTable table)
        {
            return table?.ExtendedProperties[DataTableTypeNameKey] as string;
        }

        /// <summary>
        /// Used to pass a IEnumerable&lt;SqlDataRecord&gt; as a TableValuedParameter
        /// </summary>
        public static ICustomQueryParameter AsTableValuedParameter(this IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord> list, string typeName = null)
        {
            return new SqlDataRecordListTVPParameter(list, typeName);
        }

#endif

        // one per thread
        [ThreadStatic]
        private static StringBuilder perThreadStringBuilderCache;
        private static StringBuilder GetStringBuilder()
        {
            var tmp = perThreadStringBuilderCache;
            if (tmp != null)
            {
                perThreadStringBuilderCache = null;
                tmp.Length = 0;
                return tmp;
            }
            return new StringBuilder();
        }

        private static string __ToStringRecycle(this StringBuilder obj)
        {
            if (obj == null) return "";
            var s = obj.ToString();
            if(perThreadStringBuilderCache == null)
            {
                perThreadStringBuilderCache = obj;
            }
            return s;
        }
    }
}
