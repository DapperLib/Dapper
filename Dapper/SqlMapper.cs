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
    public static partial class SqlMapper
    {
        public interface IDynamicParameters
        {
            void AddParameters(IDbCommand command);
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
            public object Deserializer { get; set; }
            public object[] OtherDeserializers { get; set; }
            public Action<IDbCommand, object> ParamReader { get; set; }
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
        private static void PurgeQueryCache(Identity key)
        {
            lock (_queryCache) { _queryCache.Remove(key); }
        }

#else
        static readonly System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo> _queryCache = new System.Collections.Concurrent.ConcurrentDictionary<Identity, CacheInfo>();
        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            _queryCache[key] = value;
        }
        private static bool TryGetQueryCache(Identity key, out CacheInfo value)
        {
            return _queryCache.TryGetValue(key, out value);
        }
        private static void PurgeQueryCache(Identity key)
        {
            CacheInfo info;
            _queryCache.TryRemove(key, out info);
        }
#endif
        static readonly Dictionary<RuntimeTypeHandle, DbType> typeMap;

        static SqlMapper()
        {
            typeMap = new Dictionary<RuntimeTypeHandle, DbType>();
            typeMap[typeof(byte).TypeHandle] = DbType.Byte;
            typeMap[typeof(sbyte).TypeHandle] = DbType.SByte;
            typeMap[typeof(short).TypeHandle] = DbType.Int16;
            typeMap[typeof(ushort).TypeHandle] = DbType.UInt16;
            typeMap[typeof(int).TypeHandle] = DbType.Int32;
            typeMap[typeof(uint).TypeHandle] = DbType.UInt32;
            typeMap[typeof(long).TypeHandle] = DbType.Int64;
            typeMap[typeof(ulong).TypeHandle] = DbType.UInt64;
            typeMap[typeof(float).TypeHandle] = DbType.Single;
            typeMap[typeof(double).TypeHandle] = DbType.Double;
            typeMap[typeof(decimal).TypeHandle] = DbType.Decimal;
            typeMap[typeof(bool).TypeHandle] = DbType.Boolean;
            typeMap[typeof(string).TypeHandle] = DbType.String;
            typeMap[typeof(char).TypeHandle] = DbType.StringFixedLength;
            typeMap[typeof(Guid).TypeHandle] = DbType.Guid;
            typeMap[typeof(DateTime).TypeHandle] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset).TypeHandle] = DbType.DateTimeOffset;
            typeMap[typeof(byte[]).TypeHandle] = DbType.Binary;
            typeMap[typeof(byte?).TypeHandle] = DbType.Byte;
            typeMap[typeof(sbyte?).TypeHandle] = DbType.SByte;
            typeMap[typeof(short?).TypeHandle] = DbType.Int16;
            typeMap[typeof(ushort?).TypeHandle] = DbType.UInt16;
            typeMap[typeof(int?).TypeHandle] = DbType.Int32;
            typeMap[typeof(uint?).TypeHandle] = DbType.UInt32;
            typeMap[typeof(long?).TypeHandle] = DbType.Int64;
            typeMap[typeof(ulong?).TypeHandle] = DbType.UInt64;
            typeMap[typeof(float?).TypeHandle] = DbType.Single;
            typeMap[typeof(double?).TypeHandle] = DbType.Double;
            typeMap[typeof(decimal?).TypeHandle] = DbType.Decimal;
            typeMap[typeof(bool?).TypeHandle] = DbType.Boolean;
            typeMap[typeof(char?).TypeHandle] = DbType.StringFixedLength;
            typeMap[typeof(Guid?).TypeHandle] = DbType.Guid;
            typeMap[typeof(DateTime?).TypeHandle] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset?).TypeHandle] = DbType.DateTimeOffset;
        }

        private static DbType LookupDbType(Type type, string name)
        {
            DbType dbType;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (typeMap.TryGetValue(type.TypeHandle, out dbType))
            {
                return dbType;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                // use xml to denote its a list, hacky but will work on any DB
                return DbType.Xml;
            }


            throw new NotSupportedException(string.Format("The member {0} of type {1} cannot be used as a parameter value", name, type));
        }

        internal class Identity : IEquatable<Identity>
        {
            internal Identity ForGrid(Type primaryType, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, null, gridIndex);
            }

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);
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
            public override bool Equals(object obj)
            {
                return Equals(obj as Identity);
            }
            internal readonly string sql;
            internal readonly CommandType? commandType;
            private readonly int hashCode, gridIndex;
            private readonly Type type;
            private readonly string connectionString;
            internal readonly Type parametersType;
            public override int GetHashCode()
            {
                return hashCode;
            }
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
            identity = new Identity(sql, commandType, cnn, null, (object)param == null ? null : ((object)param).GetType(), null);
            info = GetCacheInfo(identity);
            return ExecuteCommand(cnn, transaction, sql, info.ParamReader, (object)param, commandTimeout, commandType);
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

        // the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new <space> get new object
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
            bool clean = true;
            try
            {
                using (var cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout, commandType))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (info.Deserializer == null)
                        {
                            info.Deserializer = GetDeserializer<T>(reader, 0, -1, false);
                            SetQueryCache(identity, info);
                        }

                        var deserializer = (Func<IDataReader, T>)info.Deserializer;

                        while (reader.Read())
                        {
                            clean = false;
                            var next = deserializer(reader);
                            clean = true;
                            yield return next;
                        }
                    }
                }
            }
            finally
            {   // throw away query plan on failure - could 
                if (!clean)
                {
                    PurgeQueryCache(identity);
                }
            }
        }

        /// <summary>
        /// Maps a query to objects
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
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
                object deserializer;
                object[] otherDeserializers;
                if ((deserializer = cinfo.Deserializer) == null || (otherDeserializers = cinfo.OtherDeserializers) == null)
                {
                    int current = 0;

                    var splits = splitOn.Split(',').ToArray();
                    var splitIndex = 0;

                    Func<Type,int> nextSplit = type =>
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

                    var otherDeserializer = new List<object>();

                    int split = nextSplit(typeof(TFirst));
                    deserializer = cinfo.Deserializer = GetDeserializer<TFirst>(reader, 0, split, false);

                    if (typeof(TSecond) != typeof(DontMap))
                    {
                        var next = nextSplit(typeof(TSecond));
                        otherDeserializer.Add(GetDeserializer<TSecond>(reader, split, next - split, true));
                        split = next;
                    }
                    if (typeof(TThird) != typeof(DontMap))
                    {
                        var next = nextSplit(typeof(TThird));
                        otherDeserializer.Add(GetDeserializer<TThird>(reader, split, next - split, true));
                        split = next;
                    }
                    if (typeof(TFourth) != typeof(DontMap))
                    {
                        var next = nextSplit(typeof(TFourth));
                        otherDeserializer.Add(GetDeserializer<TFourth>(reader, split, next - split, true));
                        split = next;
                    }
                    if (typeof(TFifth) != typeof(DontMap))
                    {
                        var next = nextSplit(typeof(TFifth));
                        otherDeserializer.Add(GetDeserializer<TFifth>(reader, split, next - split, true));
                    }

                    otherDeserializers = cinfo.OtherDeserializers = otherDeserializer.ToArray();

                    SetQueryCache(identity, cinfo);
                }

                var rootDeserializer = (Func<IDataReader, TFirst>)deserializer;
                var deserializer2 = (Func<IDataReader, TSecond>)otherDeserializers[0];


                Func<IDataReader, TReturn> mapIt = null;

                if (otherDeserializers.Length == 1)
                {
                    mapIt = r => ((Func<TFirst, TSecond, TReturn>)map)(rootDeserializer(r), deserializer2(r));
                }

                if (otherDeserializers.Length > 1)
                {
                    var deserializer3 = (Func<IDataReader, TThird>)otherDeserializers[1];

                    if (otherDeserializers.Length == 2)
                    {
                        mapIt = r => ((Func<TFirst, TSecond, TThird, TReturn>)map)(rootDeserializer(r), deserializer2(r), deserializer3(r));
                    }
                    if (otherDeserializers.Length > 2)
                    {
                        var deserializer4 = (Func<IDataReader, TFourth>)otherDeserializers[2];
                        if (otherDeserializers.Length == 3)
                        {
                            mapIt = r => ((Func<TFirst, TSecond, TThird, TFourth, TReturn>)map)(rootDeserializer(r), deserializer2(r), deserializer3(r), deserializer4(r));
                        }

                        if (otherDeserializers.Length > 3)
                        {
#if CSHARP30
                            throw new NotSupportedException();
#else
                            var deserializer5 = (Func<IDataReader, TFifth>)otherDeserializers[3];
                            mapIt = r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>)map)(rootDeserializer(r), deserializer2(r), deserializer3(r), deserializer4(r), deserializer5(r));
#endif
                        }
                    }
                }

                if (mapIt != null)
                {
                    bool clean = true;
                    try
                    {
                        while (reader.Read())
                        {
                            clean = false;
                            TReturn next = mapIt(reader);
                            clean = true;
                            yield return next;
                        }
                    }
                    finally
                    {
                        if (!clean) PurgeQueryCache(identity);
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
                        info.ParamReader = (cmd, obj) => { (obj as IDynamicParameters).AddParameters(cmd); };
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

        private static Func<IDataReader, T> GetDeserializer<T>(IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
            Type type = typeof(T);
#if !CSHARP30
            // dynamic is passed in as Object ... by c# design
            if (type == typeof(object)
                || type == typeof(FastExpando))
            {
                return GetDynamicDeserializer<T>(reader, startBound, length, returnNullIfFirstMissing);
            }
#endif

            if (type.IsClass && type != typeof(string) && type != typeof(byte[]))
            {
                return GetClassDeserializer<T>(reader, startBound, length, returnNullIfFirstMissing);
            }
            return GetStructDeserializer<T>(startBound);

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
                    throw new NotImplementedException();
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


        private static Func<IDataReader, T> GetDynamicDeserializer<T>(IDataRecord reader, int startBound, int length, bool returnNullIfFirstMissing)
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
                             return default(T);
                         }
                     }
                     //we know this is an object so it will not box
                     return (T)(object)FastExpando.Attach(row);
                 };
        }
#endif
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static char ReadChar(object value)
        {
            if (value == null || value is DBNull) throw new ArgumentNullException("value");
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];         
        }
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", false)]
        public static char? ReadNullableChar(object value)
        {
            if (value == null || value is DBNull) return null;
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];            
        }
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
                bool isString = value is IEnumerable<string>;
                foreach (var item in list)
                {
                    count++;
                    var listParam = command.CreateParameter();
                    listParam.ParameterName = namePrefix + count;
                    listParam.Value = item ?? DBNull.Value;
                    if (isString)
                    {
                        listParam.Size = 4000;
                        if (item != null && ((string)item).Length > 4000)
                        {
                            listParam.Size = -1;
                        }
                    }
                    command.Parameters.Add(listParam);
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

        private static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
        {
            return parameters.Where(p => Regex.IsMatch(sql, "[@:]" + p.Name + "([^a-zA-Z0-9_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline));
        }

        private static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity)
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


        private static int ExecuteCommand(IDbConnection cnn, IDbTransaction tranaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            using (var cmd = SetupCommand(cnn, tranaction, sql, paramReader, obj, commandTimeout, commandType))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static Func<IDataReader, T> GetStructDeserializer<T>(int index)
        {
            // no point using special per-type handling here; it boils down to the same, plus not all are supported anyway (see: SqlDataReader.GetChar - not supported!)
#pragma warning disable 618
            if (typeof(T) == typeof(char))
            { // this *does* need special handling, though
                return (Func<IDataReader, T>)(object)new Func<IDataReader, char>(r => SqlMapper.ReadChar(r.GetValue(index)));
            }
            if (typeof(T) == typeof(char?))
            {
                return (Func<IDataReader, T>)(object)new Func<IDataReader, char?>(r => SqlMapper.ReadNullableChar(r.GetValue(index)));
            }
#pragma warning restore 618
            return r =>
            {
                var val = r.GetValue(index);
                if (val == DBNull.Value)
                {
                    val = null;
                }
                return (T)val;
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
                      Setter = p.DeclaringType == t ? p.GetSetMethod(true) : p.DeclaringType.GetProperty(p.Name).GetSetMethod(true),
                      Type = p.PropertyType
                  })
                  .Where(info => info.Setter != null)
                  .ToList();  
        }

        static List<FieldInfo> GetSettableFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
        }

        public static Func<IDataReader, T> GetClassDeserializer<T>(
#if CSHARP30
            IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing
#else
IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false
#endif
)
        {
            var dm = new DynamicMethod(string.Format("Deserialize{0}", Guid.NewGuid()), typeof(T), new[] { typeof(IDataReader) }, true);

            var il = dm.GetILGenerator();
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(typeof(T));
            bool haveEnumLocal = false;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);
            var properties = GetSettableProps(typeof(T));
            var fields = GetSettableFields(typeof(T));
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
                            let field = prop != null ? null : (fields.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)) // field case sensitive third
                                ?? fields.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))) // field case insensitive fourth
                            select new { Name = n, Property = prop, Field = field }
                          ).ToList();

            int index = startBound;

            il.BeginExceptionBlock();
            // stack is empty
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes)); // stack is now [target]
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
                        il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                        if (nullUnderlyingType != null && nullUnderlyingType.IsEnum)
                        {
                            il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { nullUnderlyingType }));
                        }
                    }
                    if (item.Property != null)
                    {
                        il.Emit(OpCodes.Callvirt, item.Property.Setter); // stack is now [target]
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
            il.Emit(OpCodes.Stloc_1); // stack is empty
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof(Exception)); // stack is Exception
            il.Emit(OpCodes.Ldloc_0); // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0); // stack is Exception, index, reader
            il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("ThrowDataException"), null);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc_1); // to make it verifiable
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc_1); // stack is empty
            il.Emit(OpCodes.Ret);

            return (Func<IDataReader, T>)dm.CreateDelegate(typeof(Func<IDataReader, T>));
        }
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
            /// <summary>
            /// Read the next grid of results
            /// </summary>
            public IEnumerable<T> Read<T>()
            {
                if (reader == null) throw new ObjectDisposedException(GetType().Name);
                if (consumed) throw new InvalidOperationException("Each grid can only be iterated once");
                var typedIdentity = identity.ForGrid(typeof(T), gridIndex);
                CacheInfo cache = GetCacheInfo(typedIdentity);
                var deserializer = (Func<IDataReader, T>)cache.Deserializer;
                if (deserializer == null)
                {
                    deserializer = GetDeserializer<T>(reader, 0, -1, false);
                    cache.Deserializer = deserializer;
                }
                consumed = true;
                return ReadDeferred(gridIndex, deserializer, typedIdentity);
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

#if CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn = "id")
#endif
            {
                return MultiReadInternal<TFirst, TSecond, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
            }

#if CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id")
#endif
            {
                return MultiReadInternal<TFirst, TSecond, TThird, DontMap, DontMap, TReturn>(func, splitOn);
            }

#if CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn)
#else
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id")
#endif
            {
                return MultiReadInternal<TFirst, TSecond, TThird, TFourth, DontMap, TReturn>(func, splitOn);
            }

#if !CSHARP30  
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id")

            {
                return MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(func, splitOn);
            }
#endif

            private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, T> deserializer, Identity typedIdentity)
            {

                bool clean = true;
                try
                {
                    while (index == gridIndex && reader.Read())
                    {
                        clean = false;
                        T next = deserializer(reader);
                        clean = true;
                        yield return next;
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (!clean)
                    {
                        PurgeQueryCache(typedIdentity);
                    }
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
    public class DynamicParameters : SqlMapper.IDynamicParameters
    {
        Dictionary<string, ParamInfo> parameters = new Dictionary<string, ParamInfo>();

        class ParamInfo
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public ParameterDirection ParameterDirection { get; set; }
            public DbType? DbType { get; set; }
            public int? Size { get; set; }
            public IDbDataParameter AttachedParam { get; set; }
        }

        public DynamicParameters() { }
        public DynamicParameters(object template)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (template != null)
            {
                foreach (PropertyInfo prop in template.GetType().GetProperties(bindingFlags))
                {
                    if (!prop.CanRead) continue;
                    var idx = prop.GetIndexParameters();
                    if (idx != null && idx.Length != 0) continue;
                    Add(prop.Name, prop.GetValue(template, null), null, ParameterDirection.Input, null);
                }
                foreach (FieldInfo field in template.GetType().GetFields(bindingFlags))
                {
                    Add(field.Name, field.GetValue(template), null, ParameterDirection.Input, null);
                }

            }
        }


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

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command)
        {
            foreach (var param in parameters.Values)
            {
                var p = command.CreateParameter();
                var val = param.Value;
                p.ParameterName = param.Name;
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
                command.Parameters.Add(p);
                param.AttachedParam = p;
            }
        }

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
    public sealed class DbString
    {
        public DbString() { Length = -1; }
        public bool IsAnsi { get; set; }
        public bool IsFixedLength { get; set; }
        public int Length { get; set; }
        public string Value { get; set; }
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
}
