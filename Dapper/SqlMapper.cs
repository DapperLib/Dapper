/*
 License: http://www.apache.org/licenses/LICENSE-2.0 
 Home page: http://code.google.com/p/dapper-dot-net/

 Note: to build on C# 3.0 + .NET 3.5, include the CSHARP30 compiler symbol (and yes,
 I know the difference between language and runtime versions; this is a compromise).
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace Dapper
{
    /// <summary>
    /// Dapper, a light weight object mapper for ADO.NET
    /// </summary>
    public static partial class SqlMapper
    {
        /// <summary>
        /// Implement this interface to pass an arbitrary db specific set of parameters to Dapper
        /// </summary>
        public interface IDynamicParameters
        {
            /// <summary>
            /// Add all the parameters needed to the command just before it executes
            /// </summary>
            /// <param name="command">The raw command prior to execution</param>
            /// <param name="identity">Information about the query</param>
            void AddParameters(IDbCommand command, Identity identity);
        }
        static Link<Type, Action<IDbCommand, bool>> bindByNameCache;
        static Action<IDbCommand, bool> GetBindByName(Type commandType)
        {
            if (commandType == null) return null; // GIGO
            Action<IDbCommand, bool> action;
            if (Link<Type, Action<IDbCommand, bool>>.TryGet(bindByNameCache, commandType, out action))
            {
                return action;
            }
            var prop = commandType.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo setter;
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool)
                && ((indexers = prop.GetIndexParameters()) == null || indexers.Length == 0)
                && (setter = prop.GetSetMethod()) != null
                )
            {
                var method = new DynamicMethod(commandType.Name + "_BindByName", null, new Type[] { typeof(IDbCommand), typeof(bool) });
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, commandType);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, setter, null);
                il.Emit(OpCodes.Ret);
                action = (Action<IDbCommand, bool>)method.CreateDelegate(typeof(Action<IDbCommand, bool>));
            }
            // cache it            
            Link<Type, Action<IDbCommand, bool>>.TryAdd(ref bindByNameCache, commandType, ref action);
            return action;
        }
        /// <summary>
        /// This is a micro-cache; suitable when the number of terms is controllable (a few hundred, for example),
        /// and strictly append-only; you cannot change existing values. All key matches are on **REFERENCE**
        /// equality. The type is fully thread-safe.
        /// </summary>
        class Link<TKey, TValue> where TKey : class
        {
            public static bool TryGet(Link<TKey, TValue> link, TKey key, out TValue value)
            {
                while (link != null)
                {
                    if ((object)key == (object)link.Key)
                    {
                        value = link.Value;
                        return true;
                    }
                    link = link.Tail;
                }
                value = default(TValue);
                return false;
            }
            public static bool TryAdd(ref Link<TKey, TValue> head, TKey key, ref TValue value)
            {
                bool tryAgain;
                do
                {
                    var snapshot = Interlocked.CompareExchange(ref head, null, null);
                    TValue found;
                    if (TryGet(snapshot, key, out found))
                    { // existing match; report the existing value instead
                        value = found;
                        return false;
                    }
                    var newNode = new Link<TKey, TValue>(key, value, snapshot);
                    // did somebody move our cheese?
                    tryAgain = Interlocked.CompareExchange(ref head, newNode, snapshot) != snapshot;
                } while (tryAgain);
                return true;
            }
            private Link(TKey key, TValue value, Link<TKey, TValue> tail)
            {
                Key = key;
                Value = value;
                Tail = tail;
            }
            public TKey Key { get; private set; }
            public TValue Value { get; private set; }
            public Link<TKey, TValue> Tail { get; private set; }
        }
        class CacheInfo
        {
            public Func<IDataReader, object> Deserializer { get; set; }
            public Func<IDataReader, object>[] OtherDeserializers { get; set; }
            public Action<IDbCommand, object> ParamReader { get; set; }
            private int hitCount;
            public int GetHitCount() { return Interlocked.CompareExchange(ref hitCount, 0, 0); }
            public void RecordHit() { Interlocked.Increment(ref hitCount); }
        }

        /// <summary>
        /// Called if the query cache is purged via PurgeQueryCache
        /// </summary>
        public static event EventHandler QueryCachePurged;
        private static void OnQueryCachePurged()
        {
            var handler = QueryCachePurged;
            if (handler != null) handler(null, EventArgs.Empty);
        }
#if CSHARP30
        private static readonly Dictionary<Identity, CacheInfo> _queryCache = new Dictionary<Identity, CacheInfo>();
        // note: conflicts between readers and writers are so short-lived that it isn't worth the overhead of
        // ReaderWriterLockSlim etc; a simple lock is faster
        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            lock (_queryCache) { _queryCache[key] = value; }
        }
        private static bool TryGetQueryCache(Identity key, out CacheInfo value)
        {
            lock (_queryCache) { return _queryCache.TryGetValue(key, out value); }
        }
        public static void PurgeQueryCache()
        {
            lock (_queryCache)
            {
                 _queryCache.Clear();
            }
            OnQueryCachePurged();
        }
#else
        static readonly System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo> _queryCache = new System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo>();
        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            if(Interlocked.Increment(ref collect)==COLLECT_PER_ITEMS)
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
            if(_queryCache.TryGetValue(key, out value))
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
            OnQueryCachePurged();
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
        public static IEnumerable<Tuple<int,int>> GetHashCollissions()
        {
            var counts = new Dictionary<int, int>();
            foreach(var key in _queryCache.Keys)
            {
                int count;
                if(!counts.TryGetValue(key.hashCode, out count))
                {
                    counts.Add(key.hashCode, 1);
                } else
                {
                    counts[key.hashCode] = count + 1;
                }
            }
            return from pair in counts
                   where pair.Value > 1
                   select Tuple.Create(pair.Key, pair.Value);

        }
#endif


        static readonly Dictionary<Type, DbType> typeMap;

        static SqlMapper()
        {
            typeMap = new Dictionary<Type, DbType>();
            typeMap[typeof(byte)] = DbType.Byte;
            typeMap[typeof(sbyte)] = DbType.SByte;
            typeMap[typeof(short)] = DbType.Int16;
            typeMap[typeof(ushort)] = DbType.UInt16;
            typeMap[typeof(int)] = DbType.Int32;
            typeMap[typeof(uint)] = DbType.UInt32;
            typeMap[typeof(long)] = DbType.Int64;
            typeMap[typeof(ulong)] = DbType.UInt64;
            typeMap[typeof(float)] = DbType.Single;
            typeMap[typeof(double)] = DbType.Double;
            typeMap[typeof(decimal)] = DbType.Decimal;
            typeMap[typeof(bool)] = DbType.Boolean;
            typeMap[typeof(string)] = DbType.String;
            typeMap[typeof(char)] = DbType.StringFixedLength;
            typeMap[typeof(Guid)] = DbType.Guid;
            typeMap[typeof(DateTime)] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            typeMap[typeof(byte[])] = DbType.Binary;
            typeMap[typeof(byte?)] = DbType.Byte;
            typeMap[typeof(sbyte?)] = DbType.SByte;
            typeMap[typeof(short?)] = DbType.Int16;
            typeMap[typeof(ushort?)] = DbType.UInt16;
            typeMap[typeof(int?)] = DbType.Int32;
            typeMap[typeof(uint?)] = DbType.UInt32;
            typeMap[typeof(long?)] = DbType.Int64;
            typeMap[typeof(ulong?)] = DbType.UInt64;
            typeMap[typeof(float?)] = DbType.Single;
            typeMap[typeof(double?)] = DbType.Double;
            typeMap[typeof(decimal?)] = DbType.Decimal;
            typeMap[typeof(bool?)] = DbType.Boolean;
            typeMap[typeof(char?)] = DbType.StringFixedLength;
            typeMap[typeof(Guid?)] = DbType.Guid;
            typeMap[typeof(DateTime?)] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
        }

        private const string LinqBinary = "System.Data.Linq.Binary";
        private static DbType LookupDbType(Type type, string name)
        {
            DbType dbType;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.IsEnum)
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
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                // use xml to denote its a list, hacky but will work on any DB
                return DbType.Xml;
            }


            throw new NotSupportedException(string.Format("The member {0} of type {1} cannot be used as a parameter value", name, type));
        }

        /// <summary>
        /// Identity of a cached query in Dapper, used for extensability
        /// </summary>
        public class Identity : IEquatable<Identity>
        {
            internal Identity ForGrid(Type primaryType, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, null, gridIndex);
            }

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);
            }
            /// <summary>
            /// Create an identity for use with DynamicParameters, internal use only
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public Identity ForDynamicParameters(Type type)
            {
                return new Identity(sql, commandType, connectionString, this.type ,type, null, -1);
            }

            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType, Type[] otherTypes)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, otherTypes, 0)
            { }
            private Identity(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, Type[] otherTypes, int gridIndex)
            {
                this.sql = sql;
                this.commandType = commandType;
                this.connectionString = connectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.gridIndex = gridIndex;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                    hashCode = hashCode * 23 + commandType.GetHashCode();
                    hashCode = hashCode * 23 + gridIndex.GetHashCode();
                    hashCode = hashCode * 23 + (sql == null ? 0 : sql.GetHashCode());
                    hashCode = hashCode * 23 + (type == null ? 0 : type.GetHashCode());
                    if (otherTypes != null)
                    {
                        foreach (var t in otherTypes)
                        {
                            hashCode = hashCode * 23 + (t == null ? 0 : t.GetHashCode());
                        }
                    }
                    hashCode = hashCode * 23 + (connectionString == null ? 0 : connectionString.GetHashCode());
                    hashCode = hashCode * 23 + (parametersType == null ? 0 : parametersType.GetHashCode());
                }
            }
            
            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return Equals(obj as Identity);
            }
            /// <summary>
            /// The sql
            /// </summary>
            public readonly string sql;
            /// <summary>
            /// The command type 
            /// </summary>
            public readonly CommandType? commandType;
            
            /// <summary>
            /// 
            /// </summary>
            public readonly int hashCode, gridIndex;
            private readonly Type type;
            /// <summary>
            /// 
            /// </summary>
            public readonly string connectionString;
            /// <summary>
            /// 
            /// </summary>
            public readonly Type parametersType;
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return hashCode;
            }
            /// <summary>
            /// Compare 2 Identity objects
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(Identity other)
            {
                return
                    other != null &&
                    gridIndex == other.gridIndex &&
                    type == other.type &&
                    sql == other.sql &&
                    commandType == other.commandType &&
                    connectionString == other.connectionString &&
                    parametersType == other.parametersType;
            }
        }

#if CSHARP30
        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param)
        {
            return Execute(cnn, sql, param, null, null, null);
        }
        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new <space> get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param)
        {
            return Query<T>(cnn, sql, param, null, true, null, null);
        }

#endif
        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(
#if CSHARP30
            this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
#endif
)
        {
            IEnumerable multiExec = (object)param as IEnumerable;
            Identity identity;
            CacheInfo info = null;
            if (multiExec != null && !(multiExec is string))
            { 
                bool isFirst = true;
                int total = 0;
                using (var cmd = SetupCommand(cnn, transaction, sql, null, null, commandTimeout, commandType))
                {
                    
                    string masterSql = null; 
                    foreach (var obj in multiExec)
                    {
                        if (isFirst)
                        {
                            masterSql = cmd.CommandText;
                            isFirst = false;
                            identity = new Identity(sql, cmd.CommandType, cnn, null, obj.GetType(), null);
                            info = GetCacheInfo(identity);
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
                return total;
            }

            // nice and simple
            if ((object)param != null)
            {
                identity = new Identity(sql, commandType, cnn, null, (object) param == null ? null : ((object) param).GetType(), null);
                info = GetCacheInfo(identity);
            }
            return ExecuteCommand(cnn, transaction, sql, (object)param == null ? null : info.ParamReader, (object)param, commandTimeout, commandType);
        }
#if !CSHARP30
        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return Query<FastExpando>(cnn, sql, param as object, transaction, buffered, commandTimeout, commandType);
        }
#endif
        
        /// <summary>
        /// Executes a query, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static IEnumerable<T> Query<T>(
#if CSHARP30
            this IDbConnection cnn, string sql, object param, IDbTransaction transaction, bool buffered, int? commandTimeout, CommandType? commandType
#else
this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null
#endif
)
        {
            var data = QueryInternal<T>(cnn, sql, param as object, transaction, commandTimeout, commandType);
            return buffered ? data.ToList() : data;
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(
#if CSHARP30  
            this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType
#else
this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null
#endif
)
        {
            Identity identity = new Identity(sql, commandType, cnn, typeof(GridReader), (object)param == null ? null : ((object)param).GetType(), null);
            CacheInfo info = GetCacheInfo(identity);

            IDbCommand cmd = null;
            IDataReader reader = null;
            try
            {
                cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, (object)param, commandTimeout, commandType);
                reader = cmd.ExecuteReader();
                return new GridReader(cmd, reader, identity);
            }
            catch
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Return a typed list of objects, reader is closed after the call
        /// </summary>
        private static IEnumerable<T> QueryInternal<T>(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType)
        {
            var identity = new Identity(sql, commandType, cnn, typeof(T), param == null ? null : param.GetType(), null);
            var info = GetCacheInfo(identity);

            using (var cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout, commandType))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    Func<Func<IDataReader, object>> cacheDeserializer =  () =>
                    {
                        info.Deserializer = GetDeserializer(typeof(T), reader, 0, -1, false);
                        SetQueryCache(identity, info);
                        return info.Deserializer;
                    };

                    if (info.Deserializer == null)
                    {
                        cacheDeserializer();
                    }

                    var deserializer = info.Deserializer;

                    while (reader.Read())
                    {
                        object next;
                        try
                        {
                            next = deserializer(reader);
                        }
                        catch (DataException)
                        {
                            // give it another shot, in case the underlying schema changed
                            deserializer = cacheDeserializer();
                            next = deserializer(reader);
                        }
                        yield return (T)next;
                    }
                    
                }
            }
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset</typeparam>
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
#if CSHARP30  
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType
#else
this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
#endif
)
        {
            return MultiMap<TFirst, TSecond, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout, commandType);
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
#if CSHARP30
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType
#else
this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
#endif
)
        {
            return MultiMap<TFirst, TSecond, TThird, DontMap, DontMap, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout, commandType);
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
#if CSHARP30
            this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType
#else
this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
#endif
)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, DontMap, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }
#if !CSHARP30
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
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout, commandType);
        }
#endif
        class DontMap { }
        static IEnumerable<TReturn> MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
            this IDbConnection cnn, string sql, object map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType)
        {
            var results = MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(cnn, sql, map, param, transaction, splitOn, commandTimeout, commandType, null, null);
            return buffered ? results.ToList() : results;
        }

         
        static IEnumerable<TReturn> MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, object map, object param, IDbTransaction transaction, string splitOn, int? commandTimeout, CommandType? commandType, IDataReader reader, Identity identity)
        {
            identity = identity ?? new Identity(sql, commandType, cnn, typeof(TFirst), (object)param == null ? null : ((object)param).GetType(), new[] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth) });
            CacheInfo cinfo = GetCacheInfo(identity);

            IDbCommand ownedCommand = null;
            IDataReader ownedReader = null;

            try
            {
                if (reader == null)
                {
                    ownedCommand = SetupCommand(cnn, transaction, sql, cinfo.ParamReader, (object)param, commandTimeout, commandType);
                    ownedReader = ownedCommand.ExecuteReader();
                    reader = ownedReader;
                }
                Func<IDataReader, object> deserializer = null;
                Func<IDataReader, object>[] otherDeserializers = null;

                Action cacheDeserializers = () =>
                { 
                    var deserializers = GenerateDeserializers(new Type[] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth)}, splitOn, reader);
                    deserializer = cinfo.Deserializer = deserializers[0];
                    otherDeserializers = cinfo.OtherDeserializers = deserializers.Skip(1).ToArray();
                    SetQueryCache(identity, cinfo);
                };

                if ((deserializer = cinfo.Deserializer) == null || (otherDeserializers = cinfo.OtherDeserializers) == null)
                {
                    cacheDeserializers();
                }

                Func<IDataReader, TReturn> mapIt = GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(deserializer, otherDeserializers, map);

                if (mapIt != null)
                {
                    while (reader.Read())
                    {
                        TReturn next;
                        try
                        {
                            next = mapIt(reader);
                        }
                        catch (DataException)
                        {
                            cacheDeserializers();
                            mapIt = GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(deserializer, otherDeserializers, map);
                            next = mapIt(reader);
                        }
                        yield return next;
                    }
                }
            }
            finally
            {
                try
                {
                    if (ownedReader != null)
                    {
                        ownedReader.Dispose();
                    }
                }
                finally
                {
                    if (ownedCommand != null)
                    {
                        ownedCommand.Dispose();
                    }
                }
            }
        }

        private static Func<IDataReader, TReturn> GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, object map)
        {
            switch(otherDeserializers.Length)
            {
                case 1:
                    return r => ((Func<TFirst, TSecond, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r));
                case 2:
                    return r => ((Func<TFirst, TSecond, TThird, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r));
                case 3:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r));
#if !CSHARP30
                case 4:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r));
#endif
                default:
                    throw new NotSupportedException();
            }
        }

        private static Func<IDataReader, object>[] GenerateDeserializers(Type[] types, string splitOn, IDataReader reader)
        {
            int current = 0;
            var splits = splitOn.Split(',').ToArray();
            var splitIndex = 0;

            Func<Type, int> nextSplit = type =>
            {
                var currentSplit = splits[splitIndex];
                if (splits.Length > splitIndex + 1)
                {
                    splitIndex++;
                }

                bool skipFirst = false;
                int startingPos = current + 1;
                // if our current type has the split, skip the first time you see it. 
                if (type != typeof(Object))
                {
                    var props = GetSettableProps(type);
                    var fields = GetSettableFields(type);

                    foreach (var name in props.Select(p => p.Name).Concat(fields.Select(f => f.Name)))
                    {
                        if (string.Equals(name, currentSplit, StringComparison.OrdinalIgnoreCase))
                        {
                            skipFirst = true;
                            startingPos = current;
                            break;
                        }
                    }

                }

                int pos;
                for (pos = startingPos; pos < reader.FieldCount; pos++)
                {
                    // some people like ID some id ... assuming case insensitive splits for now
                    if (splitOn == "*")
                    {
                        break;
                    }
                    if (string.Equals(reader.GetName(pos), currentSplit, StringComparison.OrdinalIgnoreCase))
                    {
                        if (skipFirst)
                        {
                            skipFirst = false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                current = pos;
                return pos;
            };

            var deserializers = new List<Func<IDataReader, object>>();
            int split = 0;
            bool first = true;
            foreach (var type in types)
            {
                if (type != typeof(DontMap))
                {
                    int next = nextSplit(type);
                    deserializers.Add(GetDeserializer(type, reader, split, next - split, /* returnNullIfFirstMissing: */ !first));
                    first = false;
                    split = next;
                }
            }

            return deserializers.ToArray();
        }

        private static CacheInfo GetCacheInfo(Identity identity)
        {
            CacheInfo info;
            if (!TryGetQueryCache(identity, out info))
            {
                info = new CacheInfo();
                if (identity.parametersType != null)
                {
                    if (typeof(IDynamicParameters).IsAssignableFrom(identity.parametersType))
                    {
                        info.ParamReader = (cmd, obj) => { (obj as IDynamicParameters).AddParameters(cmd,identity); };
                    }
                    else
                    {
                        info.ParamReader = CreateParamInfoGenerator(identity);
                    }
                }
                SetQueryCache(identity, info);
            }
            return info;
        }

        private static Func<IDataReader, object> GetDeserializer(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
#if !CSHARP30
            // dynamic is passed in as Object ... by c# design
            if (type == typeof(object)
                || type == typeof(FastExpando))
            {
                return GetDynamicDeserializer(reader, startBound, length, returnNullIfFirstMissing);
            }
#endif

            if (!(typeMap.ContainsKey(type) || type.FullName == LinqBinary))
            {
                return GetTypeDeserializer(type, reader, startBound, length, returnNullIfFirstMissing);
            }
            return GetStructDeserializer(type, startBound);

        }
#if !CSHARP30
        private class FastExpando : System.Dynamic.DynamicObject, IDictionary<string, object>
        {
            IDictionary<string, object> data;

            public static FastExpando Attach(IDictionary<string, object> data)
            {
                return new FastExpando { data = data };
            }

            public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value)
            {
                data[binder.Name] = value;
                return true;
            }

            public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
            {
                return data.TryGetValue(binder.Name, out result);
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
	            return data.Keys;
            }

            #region IDictionary<string,object> Members

            void IDictionary<string, object>.Add(string key, object value)
            {
                throw new NotImplementedException();
            }

            bool IDictionary<string, object>.ContainsKey(string key)
            {
                return data.ContainsKey(key);
            }

            ICollection<string> IDictionary<string, object>.Keys
            {
                get { return data.Keys; }
            }

            bool IDictionary<string, object>.Remove(string key)
            {
                throw new NotImplementedException();
            }

            bool IDictionary<string, object>.TryGetValue(string key, out object value)
            {
                return data.TryGetValue(key, out value);
            }

            ICollection<object> IDictionary<string, object>.Values
            {
                get { return data.Values; }
            }

            object IDictionary<string, object>.this[string key]
            {
                get
                {
                    return data[key];
                }
                set
                {
                    if (!data.ContainsKey(key)) 
                    {
	                    throw new NotImplementedException();
                    }
                    data[key] = value;
                }
            }

            #endregion

            #region ICollection<KeyValuePair<string,object>> Members

            void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            void ICollection<KeyValuePair<string, object>>.Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
            {
                return data.Contains(item);
            }

            void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                data.CopyTo(array, arrayIndex);
            }

            int ICollection<KeyValuePair<string, object>>.Count
            {
                get { return data.Count; }
            }

            bool ICollection<KeyValuePair<string, object>>.IsReadOnly
            {
                get { return true; }
            }

            bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IEnumerable<KeyValuePair<string,object>> Members

            IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
            {
                return data.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return data.GetEnumerator();
            }

            #endregion
        }


        private static Func<IDataReader, object> GetDynamicDeserializer(IDataRecord reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
            var fieldCount = reader.FieldCount;
            if (length == -1)
            {
                length = fieldCount - startBound;
            }

            if (fieldCount <= startBound)
            {
                throw new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
            }

            return
                 r =>
                 {
                     IDictionary<string, object> row = new Dictionary<string, object>(length);
                     for (var i = startBound; i < startBound + length; i++)
                     {
                         var tmp = r.GetValue(i);
                         tmp = tmp == DBNull.Value ? null : tmp;
                         row[r.GetName(i)] = tmp;
                         if (returnNullIfFirstMissing && i == startBound && tmp == null)
                         {
                             return null;
                         }
                     }
                     //we know this is an object so it will not box
                     return FastExpando.Attach(row);
                 };
        }
#endif
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static char ReadChar(object value)
        {
            if (value == null || value is DBNull) throw new ArgumentNullException("value");
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];         
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static char? ReadNullableChar(object value)
        {
            if (value == null || value is DBNull) return null;
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];            
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", true)]
        public static void PackListParameters(IDbCommand command, string namePrefix, object value)
        {
            // initially we tried TVP, however it performs quite poorly.
            // keep in mind SQL support up to 2000 params easily in sp_executesql, needing more is rare

            var list = value as IEnumerable;
            var count = 0;

            if (list != null)
            {
				if (FeatureSupport.Get(command.Connection).Arrays)
				{
					var arrayParm = command.CreateParameter();
					arrayParm.Value = list;
					arrayParm.ParameterName = namePrefix;
					command.Parameters.Add(arrayParm);
				}
				else
				{
					bool isString = value is IEnumerable<string>;
					bool isDbString = value is IEnumerable<DbString>;
					foreach (var item in list)
					{
						count++;
						var listParam = command.CreateParameter();
						listParam.ParameterName = namePrefix + count;
						listParam.Value = item ?? DBNull.Value;
						if (isString)
						{
							listParam.Size = 4000;
							if (item != null && ((string) item).Length > 4000)
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
							command.Parameters.Add(listParam);
						}
					}

					if (count == 0)
					{
						command.CommandText = Regex.Replace(command.CommandText, @"[?@:]" + Regex.Escape(namePrefix), "(SELECT NULL WHERE 1 = 0)");
					}
					else
					{
						command.CommandText = Regex.Replace(command.CommandText, @"[?@:]" + Regex.Escape(namePrefix), match =>
							{
								var grp = match.Value;
								var sb = new StringBuilder("(").Append(grp).Append(1);
								for (int i = 2; i <= count; i++)
								{
									sb.Append(',').Append(grp).Append(i);
								}
								return sb.Append(')').ToString();
							});
					}
				}
            }

        }

        private static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
        {
            return parameters.Where(p => Regex.IsMatch(sql, "[@:]" + p.Name + "([^a-zA-Z0-9_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline));
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        public static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity)
        {
            Type type = identity.parametersType;
            bool filterParams = identity.commandType.GetValueOrDefault(CommandType.Text) == CommandType.Text;
            var dm = new DynamicMethod(string.Format("ParamInfo{0}", Guid.NewGuid()), null, new[] { typeof(IDbCommand), typeof(object) }, type, true);

            var il = dm.GetILGenerator();

            il.DeclareLocal(type); // 0
            bool haveInt32Arg1 = false;
            il.Emit(OpCodes.Ldarg_1); // stack is now [untyped-param]
            il.Emit(OpCodes.Unbox_Any, type); // stack is now [typed-param]
            il.Emit(OpCodes.Stloc_0);// stack is now empty

            il.Emit(OpCodes.Ldarg_0); // stack is now [command]
            il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetProperty("Parameters").GetGetMethod(), null); // stack is now [parameters]

            IEnumerable<PropertyInfo> props = type.GetProperties().OrderBy(p => p.Name);
            if (filterParams)
            {
                props = FilterParameters(props, identity.sql);
            }
            foreach (var prop in props)
            {
                if (filterParams)
                {
                    if (identity.sql.IndexOf("@" + prop.Name, StringComparison.InvariantCultureIgnoreCase) < 0
                        && identity.sql.IndexOf(":" + prop.Name, StringComparison.InvariantCultureIgnoreCase) < 0)
                    { // can't see the parameter in the text (even in a comment, etc) - burn it with fire
                        continue;
                    }                    
                }
                if (prop.PropertyType == typeof(DbString))
                {
                    il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [typed-param]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod()); // stack is [parameters] [dbstring]
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [dbstring] [command]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [dbstring] [command] [name]
                    il.EmitCall(OpCodes.Callvirt, typeof(DbString).GetMethod("AddParameter"), null); // stack is now [parameters]
                    continue;
                }
                DbType dbType = LookupDbType(prop.PropertyType, prop.Name);
                if (dbType == DbType.Xml)
                {
                    // this actually represents special handling for list types;
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [command]
                    il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [command] [name]
                    il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [command] [name] [typed-param]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod()); // stack is [parameters] [command] [name] [typed-value]
                    if (prop.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, prop.PropertyType); // stack is [parameters] [command] [name] [boxed-value]
                    }
                    il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("PackListParameters"), null); // stack is [parameters]
                    continue;
                }
                il.Emit(OpCodes.Dup); // stack is now [parameters] [parameters]

                il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [parameters] [command]
                il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetMethod("CreateParameter"), null);// stack is now [parameters] [parameters] [parameter]

                il.Emit(OpCodes.Dup);// stack is now [parameters] [parameters] [parameter] [parameter]
                il.Emit(OpCodes.Ldstr, prop.Name); // stack is now [parameters] [parameters] [parameter] [parameter] [name]
                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("ParameterName").GetSetMethod(), null);// stack is now [parameters] [parameters] [parameter]

                il.Emit(OpCodes.Dup);// stack is now [parameters] [parameters] [parameter] [parameter]
                EmitInt32(il, (int)dbType);// stack is now [parameters] [parameters] [parameter] [parameter] [db-type]

                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("DbType").GetSetMethod(), null);// stack is now [parameters] [parameters] [parameter]

                il.Emit(OpCodes.Dup);// stack is now [parameters] [parameters] [parameter] [parameter]
                EmitInt32(il, (int)ParameterDirection.Input);// stack is now [parameters] [parameters] [parameter] [parameter] [dir]
                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("Direction").GetSetMethod(), null);// stack is now [parameters] [parameters] [parameter]

                il.Emit(OpCodes.Dup);// stack is now [parameters] [parameters] [parameter] [parameter]
                il.Emit(OpCodes.Ldloc_0); // stack is now [parameters] [parameters] [parameter] [parameter] [typed-param]
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod()); // stack is [parameters] [parameters] [parameter] [parameter] [typed-value]
                bool checkForNull = true;
                if (prop.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, prop.PropertyType); // stack is [parameters] [parameters] [parameter] [parameter] [boxed-value]
                    if (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                    {   // struct but not Nullable<T>; boxed value cannot be null
                        checkForNull = false;
                    }
                }
                if (checkForNull)
                {
                    if (dbType == DbType.String && !haveInt32Arg1)
                    {
                        il.DeclareLocal(typeof(int));
                        haveInt32Arg1 = true;
                    }
                    // relative stack: [boxed value]
                    il.Emit(OpCodes.Dup);// relative stack: [boxed value] [boxed value]
                    Label notNull = il.DefineLabel();
                    Label? allDone = dbType == DbType.String ? il.DefineLabel() : (Label?)null;
                    il.Emit(OpCodes.Brtrue_S, notNull);
                    // relative stack [boxed value = null]
                    il.Emit(OpCodes.Pop); // relative stack empty
                    il.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField("Value")); // relative stack [DBNull]
                    if (dbType == DbType.String)
                    {
                        EmitInt32(il, 0);
                        il.Emit(OpCodes.Stloc_1);
                    }
                    if (allDone != null) il.Emit(OpCodes.Br_S, allDone.Value);
                    il.MarkLabel(notNull);
                    if (prop.PropertyType == typeof(string))
                    {
                        il.Emit(OpCodes.Dup); // [string] [string]
                        il.EmitCall(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod(), null); // [string] [length]
                        EmitInt32(il, 4000); // [string] [length] [4000]
                        il.Emit(OpCodes.Cgt); // [string] [0 or 1]
                        Label isLong = il.DefineLabel(), lenDone = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, isLong);
                        EmitInt32(il, 4000); // [string] [4000]
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
                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("Value").GetSetMethod(), null);// stack is now [parameters] [parameters] [parameter]

                if (prop.PropertyType == typeof(string))
                {
                    var endOfSize = il.DefineLabel();
                    // don't set if 0
                    il.Emit(OpCodes.Ldloc_1); // [parameters] [parameters] [parameter] [size]
                    il.Emit(OpCodes.Brfalse_S, endOfSize); // [parameters] [parameters] [parameter]

                    il.Emit(OpCodes.Dup);// stack is now [parameters] [parameters] [parameter] [parameter]
                    il.Emit(OpCodes.Ldloc_1); // stack is now [parameters] [parameters] [parameter] [parameter] [size]
                    il.EmitCall(OpCodes.Callvirt, typeof(IDbDataParameter).GetProperty("Size").GetSetMethod(), null);// stack is now [parameters] [parameters] [parameter]

                    il.MarkLabel(endOfSize);
                }

                il.EmitCall(OpCodes.Callvirt, typeof(IList).GetMethod("Add"), null); // stack is now [parameters]
                il.Emit(OpCodes.Pop); // IList.Add returns the new index (int); we don't care
            }
            // stack is currently [command]
            il.Emit(OpCodes.Pop); // stack is now empty
            il.Emit(OpCodes.Ret);
            return (Action<IDbCommand, object>)dm.CreateDelegate(typeof(Action<IDbCommand, object>));
        }

        private static IDbCommand SetupCommand(IDbConnection cnn, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            var cmd = cnn.CreateCommand();
            var bindByName = GetBindByName(cmd.GetType());
            if (bindByName != null) bindByName(cmd, true);
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (commandTimeout.HasValue)
                cmd.CommandTimeout = commandTimeout.Value;
            if (commandType.HasValue)
                cmd.CommandType = commandType.Value;
            if (paramReader != null)
            {
                paramReader(cmd, obj);
            }
            return cmd;
        }


        private static int ExecuteCommand(IDbConnection cnn, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            using (var cmd = SetupCommand(cnn, transaction, sql, paramReader, obj, commandTimeout, commandType))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static Func<IDataReader, object> GetStructDeserializer(Type type, int index)
        {
            // no point using special per-type handling here; it boils down to the same, plus not all are supported anyway (see: SqlDataReader.GetChar - not supported!)
#pragma warning disable 618
            if (type == typeof(char))
            { // this *does* need special handling, though
                return r => SqlMapper.ReadChar(r.GetValue(index));
            }
            if (type == typeof(char?))
            {
                return r => SqlMapper.ReadNullableChar(r.GetValue(index));
            }
            if (type.FullName == LinqBinary)
            {
                return r => Activator.CreateInstance(type, r.GetValue(index));
            }
#pragma warning restore 618
            return r =>
            {
                var val = r.GetValue(index);
                return val is DBNull ? null : val;
            };
        }

        static readonly MethodInfo
                    enumParse = typeof(Enum).GetMethod("Parse", new Type[] { typeof(Type), typeof(string), typeof(bool) }),
                    getItem = typeof(IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.GetIndexParameters().Any() && p.GetIndexParameters()[0].ParameterType == typeof(int))
                        .Select(p => p.GetGetMethod()).First();

        class PropInfo
        {
            public string Name { get; set; }
            public MethodInfo Setter { get; set; }
            public Type Type { get; set; }
        }

        static List<PropInfo> GetSettableProps(Type t)
        {
            return t
                  .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                  .Select(p => new PropInfo
                  {
                      Name = p.Name,
                      Setter = p.DeclaringType == t ? 
                        p.GetSetMethod(true) : 
                        p.DeclaringType.GetProperty(p.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(true),
                      Type = p.PropertyType
                  })
                  .Where(info => info.Setter != null)
                  .ToList();  
        }

        static List<FieldInfo> GetSettableFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
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
#if CSHARP30
            Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing
#else
            Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false
#endif
)
        {
            var dm = new DynamicMethod(string.Format("Deserialize{0}", Guid.NewGuid()), typeof(object), new[] { typeof(IDataReader) }, true);

            var il = dm.GetILGenerator();
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(type);
            bool haveEnumLocal = false;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);
            var properties = GetSettableProps(type);
            var fields = GetSettableFields(type);
            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            if (reader.FieldCount <= startBound)
            {
                throw new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
            }

            var names = new List<string>();

            for (int i = startBound; i < startBound + length; i++)
            {
                names.Add(reader.GetName(i));
            }

            var setters = (
                            from n in names
                            let prop = properties.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.Ordinal)) // property case sensitive first
                                  ?? properties.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)) // property case insensitive second
                            let field = prop != null ? null : (fields.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.Ordinal)) // field case sensitive third
                                ?? fields.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))) // field case insensitive fourth
                            select new { Name = n, Property = prop, Field = field }
                          ).ToList();

            int index = startBound;

            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)1);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null); 
                if (ctor == null) 
                {
                    throw new InvalidOperationException("A parameterless default constructor is required to allow for dapper materialization");
                }
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Stloc_1);
            }
            il.BeginExceptionBlock();
            if(type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)1);// [target]
            } else
            {
                il.Emit(OpCodes.Ldloc_1);// [target]
            }

            // stack is now [target]

            bool first = true;
            var allDone = il.DefineLabel();
            foreach (var item in setters)
            {
                if (item.Property != null || item.Field != null)
                {
                    il.Emit(OpCodes.Dup); // stack is now [target][target]
                    Label isDbNullLabel = il.DefineLabel();
                    Label finishLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0); // stack is now [target][target][reader]
                    EmitInt32(il, index); // stack is now [target][target][reader][index]
                    il.Emit(OpCodes.Dup);// stack is now [target][target][reader][index][index]
                    il.Emit(OpCodes.Stloc_0);// stack is now [target][target][reader][index]
                    il.Emit(OpCodes.Callvirt, getItem); // stack is now [target][target][value-as-object]

                    Type memberType = item.Property != null ? item.Property.Type : item.Field.FieldType;

                    if (memberType == typeof(char) || memberType == typeof(char?))
                    {
                        il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(
                            memberType == typeof(char) ? "ReadChar" : "ReadNullableChar", BindingFlags.Static | BindingFlags.Public), null); // stack is now [target][target][typed-value]
                    }
                    else
                    {
                        il.Emit(OpCodes.Dup); // stack is now [target][target][value][value]
                        il.Emit(OpCodes.Isinst, typeof(DBNull)); // stack is now [target][target][value-as-object][DBNull or null]
                        il.Emit(OpCodes.Brtrue_S, isDbNullLabel); // stack is now [target][target][value-as-object]

                        // unbox nullable enums as the primitive, i.e. byte etc
                        
                        var nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                        var unboxType = nullUnderlyingType != null && nullUnderlyingType.IsEnum ? nullUnderlyingType : memberType;

                        if (unboxType.IsEnum)
                        {
                            if (!haveEnumLocal)
                            {
                                il.DeclareLocal(typeof(string));
                                haveEnumLocal = true;
                            }

                            Label isNotString = il.DefineLabel();
                            il.Emit(OpCodes.Dup); // stack is now [target][target][value][value]
                            il.Emit(OpCodes.Isinst, typeof(string)); // stack is now [target][target][value-as-object][string or null]
                            il.Emit(OpCodes.Dup);// stack is now [target][target][value-as-object][string or null][string or null]
                            il.Emit(OpCodes.Stloc_2); // stack is now [target][target][value-as-object][string or null]
                            il.Emit(OpCodes.Brfalse_S, isNotString); // stack is now [target][target][value-as-object]

                            il.Emit(OpCodes.Pop); // stack is now [target][target]


                            il.Emit(OpCodes.Ldtoken, unboxType); // stack is now [target][target][enum-type-token]
                            il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);// stack is now [target][target][enum-type]
                            il.Emit(OpCodes.Ldloc_2); // stack is now [target][target][enum-type][string]
                            il.Emit(OpCodes.Ldc_I4_1); // stack is now [target][target][enum-type][string][true]
                            il.EmitCall(OpCodes.Call, enumParse, null); // stack is now [target][target][enum-as-object]

                            il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]

                            if (nullUnderlyingType != null)
                            {
                                il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { nullUnderlyingType }));
                            }
                            if (item.Property != null)
                            {
                                il.Emit(OpCodes.Callvirt, item.Property.Setter); // stack is now [target]
                            }
                            else
                            {
                                il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                            }
                            il.Emit(OpCodes.Br_S, finishLabel);


                            il.MarkLabel(isNotString);
                        }
                        if (memberType.FullName == LinqBinary)
                        {
                            il.Emit(OpCodes.Unbox_Any, typeof(byte[])); // stack is now [target][target][byte-array]
                            il.Emit(OpCodes.Newobj, memberType.GetConstructor(new Type[] { typeof(byte[]) }));// stack is now [target][target][binary]
                        }
                        else
                        {
                            il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                        }
                        if (nullUnderlyingType != null && nullUnderlyingType.IsEnum)
                        {
                            il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { nullUnderlyingType }));
                        }
                    }
                    if (item.Property != null)
                    {
                        if (type.IsValueType)
                        {
                            il.Emit(OpCodes.Call, item.Property.Setter); // stack is now [target]
                        }
                        else
                        {
                            il.Emit(OpCodes.Callvirt, item.Property.Setter); // stack is now [target]
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                    }
                    
                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][value]

                    il.Emit(OpCodes.Pop); // stack is now [target][target]
                    il.Emit(OpCodes.Pop); // stack is now [target]

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
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.Emit(OpCodes.Stloc_1); // stack is empty
            }
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof(Exception)); // stack is Exception
            il.Emit(OpCodes.Ldloc_0); // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0); // stack is Exception, index, reader
            il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("ThrowDataException"), null);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc_1); // stack is [rval]
            if(type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Ret);

            return (Func<IDataReader, object>)dm.CreateDelegate(typeof(Func<IDataReader,object>));
        }

        /// <summary>
        /// Throws a data exception, only used internally
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="index"></param>
        /// <param name="reader"></param>
        public static void ThrowDataException(Exception ex, int index, IDataReader reader)
        {
            string name = "(n/a)", value = "(n/a)";
            if (reader != null && index >= 0 && index < reader.FieldCount)
            {
                name = reader.GetName(index);
                object val = reader.GetValue(index);
                if (val == null || val is DBNull)
                {
                    value = "<null>";
                }
                else
                {
                    value = Convert.ToString(val) + " - " + Type.GetTypeCode(val.GetType());
                }
            }
            throw new DataException(string.Format("Error parsing column {0} ({1}={2})", index, name, value), ex);
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
        /// The grid reader provides interfaces for reading multiple result sets from a Dapper query 
        /// </summary>
        public class GridReader : IDisposable
        {
            private IDataReader reader;
            private IDbCommand command;
            private Identity identity;
            
            internal GridReader(IDbCommand command, IDataReader reader, Identity identity)
            {
                this.command = command;
                this.reader = reader;
                this.identity = identity;
            }

#if !CSHARP30

            /// <summary>
            /// Read the next grid of results, returned as a dynamic object
            /// </summary>
            public IEnumerable<dynamic> Read()
            {
                return Read<FastExpando>();
            }

#endif

            /// <summary>
            /// Read the next grid of results
            /// </summary>
            public IEnumerable<T> Read<T>()
            {
                if (reader == null) throw new ObjectDisposedException(GetType().Name);
                if (consumed) throw new InvalidOperationException("Each grid can only be iterated once");
                var typedIdentity = identity.ForGrid(typeof(T), gridIndex);
                CacheInfo cache = GetCacheInfo(typedIdentity);
                var deserializer = cache.Deserializer;

                Func<Func<IDataReader, object>> deserializerGenerator = () => 
                {
                    deserializer = GetDeserializer(typeof(T), reader, 0, -1, false);
                    cache.Deserializer = deserializer;
                    return deserializer;
                };

                if (deserializer == null)
                {
                    deserializer = deserializerGenerator();
                }
                consumed = true;
                return ReadDeferred<T>(gridIndex, deserializer, typedIdentity, deserializerGenerator);
            }

            private IEnumerable<TReturn> MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(object func, string splitOn)
            {

                var identity = this.identity.ForGrid(typeof(TReturn), new Type[] { 
                    typeof(TFirst), 
                    typeof(TSecond),
                    typeof(TThird),
                    typeof(TFourth),
                    typeof(TFifth)
                }, gridIndex);
                try
                {
                    foreach (var r in SqlMapper.MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(null, null, func, null, null, splitOn, null, null, reader, identity))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    NextResult();
                }
            }

            /// <summary>
            /// Read multiple objects from a single recordset on the grid
            /// </summary>
            /// <typeparam name="TFirst"></typeparam>
            /// <typeparam name="TSecond"></typeparam>
            /// <typeparam name="TReturn"></typeparam>
            /// <param name="func"></param>
            /// <param name="splitOn"></param>
            /// <returns></returns>
#if CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn = "id")
#endif
            {
                return MultiReadInternal<TFirst, TSecond, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
            }

            /// <summary>
            /// Read multiple objects from a single recordset on the grid
            /// </summary>
            /// <typeparam name="TFirst"></typeparam>
            /// <typeparam name="TSecond"></typeparam>
            /// <typeparam name="TThird"></typeparam>
            /// <typeparam name="TReturn"></typeparam>
            /// <param name="func"></param>
            /// <param name="splitOn"></param>
            /// <returns></returns>
#if CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id")
#endif
            {
                return MultiReadInternal<TFirst, TSecond, TThird, DontMap, DontMap, TReturn>(func, splitOn);
            }

            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TFirst"></typeparam>
            /// <typeparam name="TSecond"></typeparam>
            /// <typeparam name="TThird"></typeparam>
            /// <typeparam name="TFourth"></typeparam>
            /// <typeparam name="TReturn"></typeparam>
            /// <param name="func"></param>
            /// <param name="splitOn"></param>
            /// <returns></returns>
#if CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id")
#endif
            {
                return MultiReadInternal<TFirst, TSecond, TThird, TFourth, DontMap, TReturn>(func, splitOn);
            }

#if !CSHARP30  
            /// <summary>
            /// Read multiple objects from a single record set on the grid
            /// </summary>
            /// <typeparam name="TFirst"></typeparam>
            /// <typeparam name="TSecond"></typeparam>
            /// <typeparam name="TThird"></typeparam>
            /// <typeparam name="TFourth"></typeparam>
            /// <typeparam name="TFifth"></typeparam>
            /// <typeparam name="TReturn"></typeparam>
            /// <param name="func"></param>
            /// <param name="splitOn"></param>
            /// <returns></returns>
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id")

            {
                return MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(func, splitOn);
            }
#endif

            private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, object> deserializer, Identity typedIdentity, Func<Func<IDataReader, object>> deserializerGenerator)
            {
                try
                {
                    while (index == gridIndex && reader.Read())
                    {
                        object next;
                        try
                        {
                            next = deserializer(reader);
                        }
                        catch (DataException)
                        {
                            deserializer = deserializerGenerator();
                            next = deserializer(reader);
                        }
                        yield return (T)next;
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == gridIndex)
                    {
                        NextResult();
                    }
                }
            }
            private int gridIndex;
            private bool consumed;
            private void NextResult()
            {
                if (reader.NextResult())
                {
                    gridIndex++;
                    consumed = false;
                }
                else
                {
                    Dispose();
                }

            }
            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public void Dispose()
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
            }
        }
    }

    /// <summary>
    /// A bag of parameters that can be passed to the Dapper Query and Execute methods
    /// </summary>
    public class DynamicParameters : SqlMapper.IDynamicParameters
    {
        static Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> paramReaderCache = new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();

        Dictionary<string, ParamInfo> parameters = new Dictionary<string, ParamInfo>();
        List<object> templates;

        class ParamInfo
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public ParameterDirection ParameterDirection { get; set; }
            public DbType? DbType { get; set; }
            public int? Size { get; set; }
            public IDbDataParameter AttachedParam { get; set; }
        }

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        public DynamicParameters() { }
        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        /// <param name="template">can be an anonymous type or a DynamicParameters bag</param>
        public DynamicParameters(object template)
        {
            if (template != null)
            {
                AddDynamicParams(template);
            }
        }

        /// <summary>
        /// Append a whole object full of params to the dynamic
        /// EG: AddDynamicParams(new {A = 1, B = 2}) // will add property A and B to the dynamic
        /// </summary>
        /// <param name="param"></param>
        public void AddDynamicParams(
#if CSHARP30
            object param
#else
            dynamic param
#endif
)
        {
            object obj = param as object;

            if (obj != null)
            {
                var subDynamic = obj as DynamicParameters;

                if (subDynamic == null)
                {
                    templates = templates ?? new List<object>();
                    templates.Add(obj);
                }
                else
                {
                    if (subDynamic.parameters != null)
                    {
                        foreach (var kvp in subDynamic.parameters)
                        {
                            parameters.Add(kvp.Key, kvp.Value);
                        }
                    }

                    if (subDynamic.templates != null)
                    {
                        templates = templates ?? new List<object>();
                        foreach (var t in subDynamic.templates)
                        {
                            templates.Add(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a parameter to this dynamic parameter list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <param name="direction"></param>
        /// <param name="size"></param>
        public void Add(
#if CSHARP30
            string name, object value, DbType? dbType, ParameterDirection? direction, int? size
#else
string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null
#endif
)
        {
            parameters[Clean(name)] = new ParamInfo() { Name = name, Value = value, ParameterDirection = direction ?? ParameterDirection.Input, DbType = dbType, Size = size };
        }

        static string Clean(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            if (templates != null)
            {
                foreach (var template in templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());
                    Action<IDbCommand, object> appender;

                    lock (paramReaderCache)
                    {
                        if (!paramReaderCache.TryGetValue(newIdent, out appender))
                        {
                            appender = SqlMapper.CreateParamInfoGenerator(newIdent);
                            paramReaderCache[newIdent] = appender;
                        }
                    }

                    appender(command, template);
                }
            }

            foreach (var param in parameters.Values)
            {
                string name = Clean(param.Name);
                bool add = !command.Parameters.Contains(name);
                IDbDataParameter p;
                if(add)
                {
                    p = command.CreateParameter();
                    p.ParameterName = name;
                } else
                {
                    p = (IDbDataParameter)command.Parameters[name];
                }
                var val = param.Value;
                p.Value = val ?? DBNull.Value;
                p.Direction = param.ParameterDirection;
                var s = val as string;
                if (s != null)
                {
                    if (s.Length <= 4000)
                    {
                        p.Size = 4000;
                    }
                }
                if (param.Size != null)
                {
                    p.Size = param.Size.Value;
                }
                if (param.DbType != null)
                {
                    p.DbType = param.DbType.Value;
                }
                if (add)
                {
                    command.Parameters.Add(p);
                }
                param.AttachedParam = p;
            }
        }

        /// <summary>
        /// All the names of the param in the bag, use Get to yank them out
        /// </summary>
        public IEnumerable<string> ParameterNames
        {
            get
            {
                return parameters.Select(p => p.Key);
            }
        }


        /// <summary>
        /// Get the value of a parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns>The value, note DBNull.Value is not returned, instead the value is returned as null</returns>
        public T Get<T>(string name)
        {
            var val = parameters[Clean(name)].AttachedParam.Value;
            if (val == DBNull.Value)
            {
                if (default(T) != null)
                {
                    throw new ApplicationException("Attempting to cast a DBNull to a non nullable type!");
                }
                return default(T);
            }
            return (T)val;
        }
    }

    /// <summary>
    /// This class represents a SQL string, it can be used if you need to denote your parameter is a Char vs VarChar vs nVarChar vs nChar
    /// </summary>
    public sealed class DbString
    {
        /// <summary>
        /// Create a new DbString
        /// </summary>
        public DbString() { Length = -1; }
        /// <summary>
        /// Ansi vs Unicode 
        /// </summary>
        public bool IsAnsi { get; set; }
        /// <summary>
        /// Fixed length 
        /// </summary>
        public bool IsFixedLength { get; set; }
        /// <summary>
        /// Length of the string -1 for max
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// The value of the string
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Add the parameter to the command... internal use only
        /// </summary>
        /// <param name="command"></param>
        /// <param name="name"></param>
        public void AddParameter(IDbCommand command, string name)
        {
            if (IsFixedLength && Length == -1)
            {
                throw new InvalidOperationException("If specifying IsFixedLength,  a Length must also be specified");
            }
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = (object)Value ?? DBNull.Value;
            if (Length == -1 && Value != null && Value.Length <= 4000)
            {
                param.Size = 4000;
            }
            else
            {
                param.Size = Length;
            }
            param.DbType = IsAnsi ? (IsFixedLength ? DbType.AnsiStringFixedLength : DbType.AnsiString) : (IsFixedLength ? DbType.StringFixedLength : DbType.String);
            command.Parameters.Add(param);
        }
    }

	/// <summary>
	/// Handles variances in features per DBMS
	/// </summary>
	public class FeatureSupport
	{
		/// <summary>
		/// Dictionary of supported features index by connection type name
		/// </summary>
		private static readonly Dictionary<string, FeatureSupport> FeatureList = new Dictionary<string, FeatureSupport>() {
				{"sqlserverconnection", new FeatureSupport { Arrays = false}},
				{"npgsqlconnection", new FeatureSupport {Arrays = true}}
		};

		/// <summary>
		/// Gets the featureset based on the passed connection
		/// </summary>
		public static FeatureSupport Get(IDbConnection connection)
		{
			string name = connection.GetType().Name.ToLower();
			FeatureSupport features;
			return FeatureList.TryGetValue(name, out features) ? features : FeatureList.Values.First();
		}

		/// <summary>
		/// True if the db supports array columns e.g. Postgresql
		/// </summary>
		public bool Arrays { get; set; }
	}

}
