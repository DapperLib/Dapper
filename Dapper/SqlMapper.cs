/*
 License: http://www.apache.org/licenses/LICENSE-2.0 
 Home page: http://code.google.com/p/dapper-dot-net/
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;

namespace Dapper
{
    public static class SqlMapper
    {
        class CacheInfo
        {
            public object Deserializer { get; set; }
            public object[] OtherDeserializers { get; set; }
            public Action<IDbCommand, object> ParamReader { get; set; }
        }

        static readonly ConcurrentDictionary<Identity, CacheInfo> queryCache = new ConcurrentDictionary<Identity, CacheInfo>();
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

        private static DbType LookupDbType(Type type)
        {
            DbType dbType;
            if (typeMap.TryGetValue(type, out dbType))
            {
                return dbType;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                // use xml to denote its a list, hacky but will work on any DB
                return DbType.Xml;
            }

            throw new NotSupportedException(string.Format("The type : {0} is not supported by dapper", type));
        }

        private class Identity : IEquatable<Identity>
        {

            public String ConnectionString { get { return connectionString; } }
            public Type Type { get { return type; } }
            public Type Type2 { get { return Type2; } }
            public string Sql { get { return sql; } }
            public Type ParametersType { get { return ParametersType; } }
            internal Identity(string sql, IDbConnection cnn, Type type, Type parametersType, Type[] otherTypes = null)
            {
                this.sql = sql;
                this.connectionString = cnn.ConnectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.otherTypes = otherTypes;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
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
            private readonly string sql;
            private readonly int hashCode;
            private readonly Type type;
            private readonly Type[] otherTypes;
            private readonly string connectionString;
            private readonly Type parametersType; 
            public override int GetHashCode()
            {
                return hashCode;
            }
            public bool Equals(Identity other)
            {
                return 
                    other != null && 
                    type == other.type && 
                    sql == other.sql && 
                    connectionString == other.connectionString &&
                    parametersType == other.parametersType;
            }
        }

        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var identity = new Identity(sql, cnn, null, param == null ? null : param.GetType());
            var info = GetCacheInfo(param, identity);
            return ExecuteCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null)
        {
            return Query<ExpandoObject>(cnn, sql, param as object, transaction, buffered, commandTimeout);
        }


        // the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new <space> get new object
        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null)
        {
            var data = QueryInternal<T>(cnn, sql, param as object, transaction, commandTimeout);
            return buffered ? data.ToList() : data;
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        public static GridReader QueryMultiple(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var identity = new Identity(sql, cnn, typeof(GridReader), param == null ? null : param.GetType());
            var info = GetCacheInfo(param, identity);

            IDbCommand cmd = null;
            IDataReader reader = null;
            try
            {
                cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout);
                reader = cmd.ExecuteReader();
                return new GridReader(cmd, reader, cnn, sql);
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
        private static IEnumerable<T> QueryInternal<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var identity = new Identity(sql, cnn, typeof(T), param == null ? null : param.GetType());
            var info = GetCacheInfo(param, identity);

            using (var cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (info.Deserializer == null)
                    {
                        info.Deserializer = GetDeserializer<T>(identity, reader);
                        queryCache[identity] = info;
                    }

                    var deserializer = (Func<IDataReader, T>)info.Deserializer;

                    while (reader.Read())
                    {
                        yield return deserializer(reader);
                    }
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
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return MultiMap<TFirst, TSecond, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return MultiMap<TFirst, TSecond, TThird, DontMap, DontMap, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, DontMap, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(cnn, sql, map, param as object, transaction, buffered, splitOn, commandTimeout);
        }

        class DontMap {}
        static IEnumerable<TReturn> MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, object map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            var results = MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(cnn, sql, map, param, transaction, splitOn, commandTimeout);
            return buffered ? results.ToList() : results;
        }

        static IEnumerable<TReturn> MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, object map, object param = null, IDbTransaction transaction = null, string splitOn = "Id", int? commandTimeout = null)
        {
            var identity = new Identity(sql, cnn, typeof(TFirst), param == null ? null : param.GetType(), otherTypes: new[] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth) });
            var info = GetCacheInfo(param, identity);

            using (var cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (info.Deserializer == null)
                    {
                        var split = 0;
                        int current = 0;

                        Func<int> nextSplit = () =>
                        {
                            int pos;
                            for (pos = current + 1; pos < reader.FieldCount; pos++)
                            {
                                // some people like ID some id ... assuming case insensitive splits for now
                                if (string.Equals(reader.GetName(pos), splitOn, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    break;
                                }
                            }
                            current = pos;
                            return pos;
                        };

                        var otherDeserializer = new List<object>();

                        split = nextSplit();
                        info.Deserializer = GetDeserializer<TFirst>(identity, reader, 0, split);

                        if (typeof(TSecond) != typeof(DontMap))
                        {
                            var next = nextSplit();
                            otherDeserializer.Add(GetDeserializer<TSecond>(identity, reader, split, next - split, returnNullIfFirstMissing: true));
                            split = next;
                        }
                        if (typeof(TThird) != typeof(DontMap))
                        {
                            var next = nextSplit();
                            otherDeserializer.Add(GetDeserializer<TThird>(identity, reader, split, next - split, returnNullIfFirstMissing: true));
                            split = next;
                        }
                        if (typeof(TFourth) != typeof(DontMap))
                        {
                            var next = nextSplit();
                            otherDeserializer.Add(GetDeserializer<TFourth>(identity, reader, split, next - split, returnNullIfFirstMissing: true));
                            split = next;
                        }
                        if (typeof(TFifth) != typeof(DontMap))
                        {
                            var next = nextSplit();
                            otherDeserializer.Add(GetDeserializer<TFifth>(identity, reader, split, next - split, returnNullIfFirstMissing: true));
                        }

                        info.OtherDeserializers = otherDeserializer.ToArray();

                        queryCache[identity] = info;
                    }

                    var deserializer = (Func<IDataReader, TFirst>)info.Deserializer;
                    var deserializer2 = (Func<IDataReader, TSecond>)info.OtherDeserializers[0];


                    Func<IDataReader, TReturn> mapIt = null;

                    if (info.OtherDeserializers.Length == 1)
                    {
                        mapIt = r => ((Func<TFirst, TSecond,TReturn>)map)(deserializer(r), deserializer2(r));
                    }

                    if (info.OtherDeserializers.Length > 1)
                    {
                        var deserializer3 = (Func<IDataReader, TThird>)info.OtherDeserializers[1];

                        if (info.OtherDeserializers.Length == 2)
                        {
                            mapIt = r => ((Func<TFirst, TSecond, TThird, TReturn>)map)(deserializer(r), deserializer2(r), deserializer3(r));
                        }
                        if (info.OtherDeserializers.Length > 2)
                        {
                            var deserializer4 = (Func<IDataReader, TFourth>)info.OtherDeserializers[2];
                            if (info.OtherDeserializers.Length == 3)
                            {
                                mapIt = r => ((Func<TFirst, TSecond, TThird, TFourth, TReturn>)map)(deserializer(r), deserializer2(r), deserializer3(r),deserializer4(r));
                            }

                            if (info.OtherDeserializers.Length > 3)
                            {
                                var deserializer5 = (Func<IDataReader, TFifth>)info.OtherDeserializers[3];
                                mapIt = r => ((Func<TFirst, TSecond, TThird, TFourth,TFifth,TReturn>)map)(deserializer(r), deserializer2(r), deserializer3(r), deserializer4(r),deserializer5(r));
                            }
                        }
                    }

                    if (mapIt != null)
                        while (reader.Read())
                        {
                            yield return mapIt(reader);
                        }
                }
            }
        }  
        
        private static CacheInfo GetCacheInfo(object param, Identity identity)
        {
            CacheInfo info;
            if (!queryCache.TryGetValue(identity, out info))
            {
                info = new CacheInfo();
                if (param != null)
                {
                    info.ParamReader = CreateParamInfoGenerator(param.GetType());
                }
            }
            return info;
        }

        private static Func<IDataReader, T> GetDeserializer<T>(Identity identity, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            object oDeserializer;

            // dynamic is passed in as Object ... by c# design
            if (typeof(T) == typeof(object) || typeof(T) == typeof(ExpandoObject))
            {
                oDeserializer = GetDynamicDeserializer(reader,startBound, length, returnNullIfFirstMissing);
            }
            else if (typeof(T).IsClass && typeof(T) != typeof(string))
            {
                oDeserializer = GetClassDeserializer<T>(reader, startBound, length, returnNullIfFirstMissing: returnNullIfFirstMissing);
            }
            else
            {
                oDeserializer = GetStructDeserializer<T>(reader);
            }

            var deserializer = (Func<IDataReader, T>)oDeserializer;
            return deserializer;
        }

        private static object GetDynamicDeserializer(IDataRecord reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            var colNames = new List<string>();

            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            for (var i = startBound; i < startBound + length; i++)
            {
                colNames.Add(reader.GetName(i));
            }

            Func<IDataReader, ExpandoObject> rval =
                r =>
                {
                    IDictionary<string, object> row = new ExpandoObject();
                    var i = startBound;
                    var first = true;
                    foreach (var colName in colNames)
                    {
                        var tmp = r.GetValue(i);
                        tmp = tmp == DBNull.Value ? null : tmp;
                        row[colName] = tmp;
                        if (returnNullIfFirstMissing && first && tmp == null)
                        {
                            return null;
                        }
                        i++;
                        first = false;
                    }
                    return (ExpandoObject)row;
                };

            return rval;
        }
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is for internal usage only", true)]
        public static void PackListParameters(IDbCommand command, string namePrefix, object value)
        {
            // initially we tried TVP, however it performs quite poorly.
            // keep in mind SQL support up to 2000 params easily in sp_executesql, needing more is rare

            var list = value as IEnumerable;
            var count = 0;
            bool isString = value is IEnumerable<string>;

            if (list != null)
            {
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
                    command.CommandText = command.CommandText.Replace(namePrefix, "(SELECT NULL WHERE 1 = 0)");
                }
                else
                {
                    command.CommandText = command.CommandText.Replace(namePrefix,
                        "(" + string.Join(
                            ",", Enumerable.Range(1, count).Select(i => namePrefix + i.ToString())
                        ) + ")");
                }
            }

        }
        private static Action<IDbCommand, object> CreateParamInfoGenerator(Type type)
        {
            var dm = new DynamicMethod(string.Format("ParamInfo{0}", Guid.NewGuid()), null, new[] { typeof(IDbCommand), typeof(object) }, type, true);

            var il = dm.GetILGenerator();

            il.DeclareLocal(type); // 0
            bool haveInt32Arg1 = false;
            il.Emit(OpCodes.Ldarg_1); // stack is now [untyped-param]
            il.Emit(OpCodes.Unbox_Any, type); // stack is now [typed-param]
            il.Emit(OpCodes.Stloc_0);// stack is now empty

            il.Emit(OpCodes.Ldarg_0); // stack is now [command]
            il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetProperty("Parameters").GetGetMethod(), null); // stack is now [parameters]
            
            
            foreach (var prop in type.GetProperties().OrderBy(p => p.Name))
            {
                DbType dbType = LookupDbType(prop.PropertyType);
                if (dbType == DbType.Xml)
                {
                    // this actually represents special handling for list types;
                    il.Emit(OpCodes.Ldarg_0); // stack is now [parameters] [command]
                    il.Emit(OpCodes.Ldstr, "@" + prop.Name); // stack is now [parameters] [command] [name]
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
                il.Emit(OpCodes.Ldstr, "@" + prop.Name); // stack is now [parameters] [parameters] [parameter] [parameter] [name]
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

        private static IDbCommand SetupCommand(IDbConnection cnn, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout)
        {
            var cmd = cnn.CreateCommand();

            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (commandTimeout.HasValue)
                cmd.CommandTimeout = commandTimeout.Value;
            if (paramReader != null)
            {
                paramReader(cmd, obj);
            }
            return cmd;
        }


        private static int ExecuteCommand(IDbConnection cnn, IDbTransaction tranaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout)
        {
            using (var cmd = SetupCommand(cnn, tranaction, sql, paramReader, obj, commandTimeout))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static object GetStructDeserializer<T>(IDataReader reader)
        {
            Func<IDataReader, T> deserializer = null;

            deserializer = r =>
            {
                var val = r.GetValue(0);
                if (val == DBNull.Value)
                {
                    val = null;
                }
                return (T)val;
            };
            return deserializer;
        }

        public static Func<IDataReader, T> GetClassDeserializer<T>(IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            var dm = new DynamicMethod(string.Format("Deserialize{0}", Guid.NewGuid()), typeof(T), new[] { typeof(IDataReader) }, true);

            var il = dm.GetILGenerator();
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(typeof(T));
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);
            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(p => new
                {
                    p.Name,
                    Setter = p.DeclaringType == typeof(T) ? p.GetSetMethod(true) : p.DeclaringType.GetProperty(p.Name).GetSetMethod(true),
                    Type = p.PropertyType
                })
                .Where(info => info.Setter != null)
                .ToList();
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            var names = new List<string>();
            for (int i = startBound; i < startBound + length; i++)
            {
                names.Add(reader.GetName(i));
            }

            var setters = (
                            from n in names
                            let prop = properties.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.InvariantCulture)) // property case sensitive first
                                  ?? properties.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.InvariantCultureIgnoreCase)) // property case insensitive second
                            let field = prop != null ? null : (fields.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.InvariantCulture)) // field case sensitive third
                                ?? fields.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.InvariantCultureIgnoreCase))) // field case insensitive fourth
                            select new { Name = n, Property = prop, Field = field }
                          ).ToList();


            var getItem = typeof(IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.GetIndexParameters().Any() && p.GetIndexParameters()[0].ParameterType == typeof(int))
                         .Select(p => p.GetGetMethod()).First();

            int index = startBound;

            var @try = il.BeginExceptionBlock();
            // stack is empty
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes)); // stack is now [target]
            bool first = true;
            var @allDone = il.DefineLabel();
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

                    il.Emit(OpCodes.Dup); // stack is now [target][target][value][value]
                    il.Emit(OpCodes.Isinst, typeof(DBNull)); // stack is now [target][target][value-as-object][DBNull or null]
                    il.Emit(OpCodes.Brtrue_S, isDbNullLabel); // stack is now [target][target][value-as-object]

                    // unbox nullable enums as the primitive, i.e. byte etc
                    Type memberType = item.Property != null ? item.Property.Type : item.Field.FieldType;
                    var nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                    var unboxType = nullUnderlyingType != null && nullUnderlyingType.IsEnum ? nullUnderlyingType : memberType;
                    il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [target][target][typed-value]
                    if (nullUnderlyingType != null && nullUnderlyingType.IsEnum)
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
                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][value]

                    il.Emit(OpCodes.Pop); // stack is now [target][target]
                    il.Emit(OpCodes.Pop); // stack is now [target]

                    if (first && returnNullIfFirstMissing)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull); // stack is now [null]
                        il.Emit(OpCodes.Stloc_1);
                        il.Emit(OpCodes.Br, @allDone);
                    }

                    il.MarkLabel(finishLabel);
                }
                first = false;
                index += 1;
            }
            il.Emit(OpCodes.Stloc_1); // stack is empty
            il.MarkLabel(@allDone);
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
            throw new DataException(string.Format("Error parsing column {0} ({1}={2})", index, name,value), ex);
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
                default: il.Emit(OpCodes.Ldc_I4, value); break;
            }
        }

        public class GridReader : IDisposable
        {
            private IDataReader reader;
            private IDbConnection connection;
            private IDbCommand command;
            private readonly string sql;
            internal GridReader(IDbCommand command, IDataReader reader, IDbConnection connection, string sql)
            {
                if (reader == null) throw new ArgumentNullException("reader");
                if (connection == null) throw new ArgumentNullException("connection");
                if (sql == null) throw new ArgumentNullException("sql");
                if (command == null) throw new ArgumentNullException("command");

                this.sql = sql;
                this.command = command;
                this.connection = connection;
                this.reader = reader;
            }
            /// <summary>
            /// Read the next grid of results
            /// </summary>
            public IEnumerable<T> Read<T>()
            {
                if (reader == null) throw new ObjectDisposedException(GetType().Name);
                if (consumed) throw new InvalidOperationException("Each grid can only be iterated once");
                var identity = new Identity(sql, connection, typeof(T), null);
                var deserializer = SqlMapper.GetDeserializer<T>(identity, reader);
                consumed = true;
                return ReadDeferred(gridIndex, deserializer);
            }

            // todo multimapping. 

            private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, T> deserializer)
            {
                try
                {
                    while (index == gridIndex && reader.Read())
                    {
                        yield return deserializer(reader);
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
            public void Dispose()
            {
                connection = null;
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
}
