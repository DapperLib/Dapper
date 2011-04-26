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
            public object Deserializer2 { get; set; }
            public Action<IDbCommand, object> ParamReader { get; set; }
        }

        static ConcurrentDictionary<Identity, CacheInfo> queryCache = new ConcurrentDictionary<Identity, CacheInfo>();
        static Dictionary<Type, DbType> typeMap;

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
            else
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    // use xml to denote its a list, hacky but will work on any DB
                    return DbType.Xml;
                }
            }

            throw new NotSupportedException("The type : " + type.ToString() + " is not supported by dapper");
        }

        private class Identity : IEquatable<Identity>
        {

            public String ConnectionString { get { return connectionString; } }
            public Type Type { get { return type; } }
            public Type Type2 { get { return Type2; } }
            public string Sql { get { return sql; } }
            public Type ParametersType { get { return ParametersType; } }
            internal Identity(string sql, IDbConnection cnn, Type type, Type parametersType, Type type2 = null)
            {
                this.sql = sql;
                this.connectionString = cnn.ConnectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.type2 = type2;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                    hashCode = hashCode * 23 + (sql == null ? 0 : sql.GetHashCode());
                    hashCode = hashCode * 23 + (type == null ? 0 : type.GetHashCode());
                    hashCode = hashCode * 23 + (type2 == null ? 0 : type2.GetHashCode());
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
            private readonly Type type2;
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
                    this.type == other.type && 
                    sql == other.sql && 
                    connectionString == other.connectionString &&
                    parametersType == other.parametersType;
            }
        }

        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null)
        {
            var identity = new Identity(sql, cnn, null, param == null ? null : param.GetType());
            var info = GetCacheInfo(param, identity);
            return ExecuteCommand(cnn, transaction, sql, info.ParamReader, param);
        }

        /// <summary>
        /// Return a list of dynamic objects, reader is closed after the call
        /// </summary>
        public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true)
        {
            return Query<ExpandoObject>(cnn, sql, param, transaction, buffered);
        }


        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true)
        {
            var data = QueryInternal<T>(cnn, sql, param, transaction);
            if (buffered)
            {
                return data.ToList();
            }
            else
            {
                return data;
            }
        }


        /// <summary>
        /// Return a typed list of objects, reader is closed after the call
        /// </summary>
        private static IEnumerable<T> QueryInternal<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null)
        {
            var identity = new Identity(sql, cnn, typeof(T), param == null ? null : param.GetType());
            var info = GetCacheInfo(param, identity);

            using (var reader = GetReader(cnn, transaction, sql, info.ParamReader, param))
            {
                if (info.Deserializer == null)
                { 
                    info.Deserializer = GetDeserializer<T>(identity, reader);
                    queryCache[identity] = info;
                }

                var deserializer = (Func<IDataReader,T>)info.Deserializer;

                while (reader.Read())
                {
                    yield return deserializer(reader);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="map"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="splitOn">The Field we should split and read the second object from (default: id)</param>
        /// <returns></returns>
        public static IEnumerable<T> Query<T, U>(this IDbConnection cnn, string sql, Action<T, U> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id")
        {
            var identity = new Identity(sql, cnn, typeof(T), param == null ? null : param.GetType());
            var info = GetCacheInfo(param, identity);

            using (var reader = GetReader(cnn, transaction, sql, info.ParamReader, param))
            {
                if (info.Deserializer == null)
                {
                    int start = 0;
                    int length = -1;

                    for (length = 1; length < reader.FieldCount; length++)
                    {
                        if (reader.GetName(length) == splitOn)
                        {
                            break;
                        }
                    }

                    // dynamic comes back as object ... 
                    if (typeof(T) == typeof(object))
                    {
                        info.Deserializer = GetDeserializer<ExpandoObject>(identity, reader, start, length);
                    }
                    else
                    {
                        info.Deserializer = GetDeserializer<T>(identity, reader, start, length);
                    }

                    if (typeof(U) == typeof(object))
                    {
                        info.Deserializer2 = GetDeserializer<ExpandoObject>(identity, reader, start + length,returnNullIfFirstMissing:true);
                    }
                    else
                    {
                        info.Deserializer2 = GetDeserializer<U>(identity, reader, start + length, returnNullIfFirstMissing: true);
                    }

                    queryCache[identity] = info;
                }

                var deserializer = (Func<IDataReader, T>)info.Deserializer;
                var deserializer2 = (Func<IDataReader, U>)info.Deserializer2;

                while (reader.Read())
                {
                    var tmp = deserializer(reader);
                    map(tmp, deserializer2(reader));
                    yield return tmp;
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


        class DynamicStub
        {
            public static Type Type = typeof(DynamicStub);
        }

        static Func<IDataReader, T> GetDeserializer<T>(Identity identity, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            object oDeserializer;

            if (typeof(T) == DynamicStub.Type || typeof(T) == typeof(ExpandoObject))
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

            Func<IDataReader, T> deserializer = (Func<IDataReader, T>)oDeserializer;
            return deserializer;
        }

        private static object GetDynamicDeserializer(IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            List<string> colNames = new List<string>();

            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            for (int i = startBound; i < startBound + length; i++)
            {
                colNames.Add(reader.GetName(i));
            }

            Func<IDataReader, ExpandoObject> rval =
                r =>
                {
                    IDictionary<string, object> row = new ExpandoObject();
                    int i = startBound;
                    bool first = true;
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
                    listParam.ParameterName = namePrefix + count.ToString();
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

                command.CommandText = command.CommandText.Replace(namePrefix,
                    "(" + string.Join(
                        ",", Enumerable.Range(1, count).Select(i => namePrefix + i.ToString())
                    ) + ")");
            }

        }
        private static Action<IDbCommand, object> CreateParamInfoGenerator(Type type)
        {
            DynamicMethod dm = new DynamicMethod("ParamInfo" + Guid.NewGuid().ToString(), null, new Type[] { typeof(IDbCommand), typeof(object) }, type, true);

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

        private static IDbCommand SetupCommand(IDbConnection cnn, IDbTransaction tranaction, string sql, Action<IDbCommand, object> paramReader, object obj)
        {
            var cmd = cnn.CreateCommand();

            cmd.Transaction = tranaction;
            cmd.CommandText = sql;
            if (paramReader != null)
            {
                paramReader(cmd, obj);
            }
            return cmd;
        }


        private static int ExecuteCommand(IDbConnection cnn, IDbTransaction tranaction, string sql, Action<IDbCommand, object> paramReader, object obj)
        {
            using (var cmd = SetupCommand(cnn, tranaction, sql, paramReader, obj))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static IDataReader GetReader(IDbConnection cnn, IDbTransaction tranaction, string sql, Action<IDbCommand, object> paramReader, object obj)
        {
            using (var cmd = SetupCommand(cnn, tranaction, sql, paramReader, obj))
            {
                return cmd.ExecuteReader();
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
            DynamicMethod dm = new DynamicMethod("Deserialize" + Guid.NewGuid().ToString(), typeof(T), new Type[] { typeof(IDataReader) }, true);

            var il = dm.GetILGenerator();

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(p => new { Name = p.Name, Setter = p.GetSetMethod(true), Type = p.PropertyType })
                .Where(info => info.Setter != null)
                .ToList();

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
                            let prop = properties.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.InvariantCulture)) // case sensitive first
                                  ?? properties.FirstOrDefault(p => string.Equals(p.Name, n, StringComparison.InvariantCultureIgnoreCase)) // case insensitive second
                            select new { Name = n, Info = prop }
                          ).ToList();


            var getItem = typeof(IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.GetIndexParameters().Any() && p.GetIndexParameters()[0].ParameterType == typeof(int))
                         .Select(p => p.GetGetMethod()).First();

            int index = startBound;

            // stack is empty
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes)); // stack is now [target]
            bool first = true;
            foreach (var item in setters)
            {
                if (item.Info != null)
                {
                    il.Emit(OpCodes.Dup); // stack is now [target][target]
                    Label isDbNullLabel = il.DefineLabel();
                    Label finishLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0); // stack is now [target][target][reader]
                    EmitInt32(il, index); // stack is now [target][target][reader][index]

                    il.Emit(OpCodes.Callvirt, getItem); // stack is now [target][target][value-as-object]

                    il.Emit(OpCodes.Dup); // stack is now [target][target][value][value]
                    il.Emit(OpCodes.Isinst, typeof(DBNull)); // stack is now [target][target][value-as-object][DBNull or null]
                    il.Emit(OpCodes.Brtrue_S, isDbNullLabel); // stack is now [target][target][value-as-object]

                    il.Emit(OpCodes.Unbox_Any, item.Info.Type); // stack is now [target][target][typed-value]
                    il.Emit(OpCodes.Callvirt, item.Info.Setter); // stack is now [target]
                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][value]

                    il.Emit(OpCodes.Pop); // stack is now [target][target]
                    il.Emit(OpCodes.Pop); // stack is now [target]

                    if (first && returnNullIfFirstMissing)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull); // stack is now [null]
                        il.Emit(OpCodes.Ret);
                    }

                    il.MarkLabel(finishLabel);
                }
                first = false;
                index += 1;
            }
            il.Emit(OpCodes.Ret); // stack is empty

            return (Func<IDataReader, T>)dm.CreateDelegate(typeof(Func<IDataReader, T>));
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
    }
}
