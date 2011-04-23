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

namespace Dapper
{
    public static class SqlMapper
    {
        class CacheInfo
        {
            public object Deserializer { get; set; }
            public Func<object, List<ParamInfo>> ParamReader { get; set; }
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

        class ParamInfo
        {
            private ParamInfo()
            {
            }

            public static ParamInfo Create(string name, DbType type, object val)
            {
                return new ParamInfo { Name = name, Type = type, Val = val };
            }

            public DbType Type { get; private set; }
            public string Name { get; private set; }
            public object Val { get; private set; }
        }

        private class Identity : IEquatable<Identity>
        {

            public String ConnectionString { get { return connectionString; } }
            public Type Type { get { return type; } }
            public string Sql { get { return sql; } }
            public Type ParametersType { get { return ParametersType; } }
            internal Identity(string sql, IDbConnection cnn, Type type, Type parametersType)
            {
                this.sql = sql;
                this.connectionString = cnn.ConnectionString;
                this.type = type;
                this.parametersType = parametersType;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                    hashCode = hashCode * 23 + (sql == null ? 0 : sql.GetHashCode());
                    hashCode = hashCode * 23 + (type == null ? 0 : type.GetHashCode());
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
            List<ParamInfo> paramInfos = null;
            if (info.ParamReader != null)
            {
                paramInfos = info.ParamReader(param);
            }

            return ExecuteCommand(cnn, transaction, sql, paramInfos);
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

            List<ParamInfo> paramInfos = null;
            if (info.ParamReader != null)
            {
                paramInfos = info.ParamReader(param);
            }

            using (var reader = GetReader(cnn, transaction, sql, paramInfos))
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

        public static List<T> Query<T,U>(this IDbConnection cnn, string sql, Action<T,U> map, object param = null, IDbTransaction transaction = null)
        {
            return null;
        }


        static class DynamicStub
        {
            public static Type Type = typeof(DynamicStub);
        }

        static Func<IDataReader, T> GetDeserializer<T>(Identity identity, IDataReader reader)
        {
            object oDeserializer;

            if (typeof(T) == DynamicStub.Type || typeof(T) == typeof(ExpandoObject))
            {
                oDeserializer = GetDynamicDeserializer(reader);
            }
            else if (typeof(T).IsClass && typeof(T) != typeof(string))
            {
                oDeserializer = GetClassDeserializer<T>(reader);
            }
            else
            {
                oDeserializer = GetStructDeserializer<T>(reader);
            }

            Func<IDataReader, T> deserializer = (Func<IDataReader, T>)oDeserializer;
            return deserializer;
        }

        private static object GetDynamicDeserializer(IDataReader reader)
        {
            List<string> colNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                colNames.Add(reader.GetName(i));
            }

            Func<IDataReader, ExpandoObject> rval =
                r =>
                {
                    IDictionary<string, object> row = new ExpandoObject();
                    int i = 0;
                    foreach (var colName in colNames)
                    {
                        var tmp = r.GetValue(i);
                        row[colName] = tmp == DBNull.Value ? null : tmp;
                        i++;
                    }
                    return (ExpandoObject)row;
                };

            return rval;
        }

        private static Func<object, List<ParamInfo>> CreateParamInfoGenerator(Type type)
        {
            DynamicMethod dm = new DynamicMethod("ParamInfo" + Guid.NewGuid().ToString(), typeof(List<ParamInfo>), new Type[] { typeof(object) }, true);

            var il = dm.GetILGenerator();

            il.DeclareLocal(type); // 0
            il.Emit(OpCodes.Ldarg_0); // stack is now [untyped-param]
            il.Emit(OpCodes.Unbox_Any, type); // stack is now [typed-param]
            il.Emit(OpCodes.Stloc_0);// stack is now empty

            il.Emit(OpCodes.Newobj, typeof(List<ParamInfo>).GetConstructor(Type.EmptyTypes)); // stack is now [list]

            foreach (var prop in type.GetProperties().OrderBy(p => p.Name))
            {
                // we want to call list.Add(ParamInfo.Create(string name, DbType type, object val))

                il.Emit(OpCodes.Dup); // stack is now [list] [list]

                il.Emit(OpCodes.Ldstr, prop.Name); // stack is  [list] [list] [name]
                il.Emit(OpCodes.Ldc_I4, (int)LookupDbType(prop.PropertyType)); // stack is [list] [list] [name] [dbtype]
                il.Emit(OpCodes.Ldloc_0); // stack is [list] [list] [name] [dbtype] [typed-param]
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod()); // stack is [list] [list] [name] [dbtype] [typed-value]
                if (prop.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, prop.PropertyType); // stack is [list] [list] [name] [dbtype] [untyped-value]
                }
                il.Emit(OpCodes.Call, typeof(ParamInfo).GetMethod("Create", BindingFlags.Static | BindingFlags.Public)); // stack is [list] [list] [param-info]
                il.Emit(OpCodes.Callvirt, typeof(List<ParamInfo>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance)); // stack is [list]
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, List<ParamInfo>>)dm.CreateDelegate(typeof(Func<object, List<ParamInfo>>));
        }

        private static IDbCommand SetupCommand(IDbConnection cnn, IDbTransaction tranaction, string sql, List<ParamInfo> paramInfo)
        {
            var cmd = cnn.CreateCommand();

            cmd.Transaction = tranaction;
            cmd.CommandText = sql;
            if (paramInfo != null)
            {
                foreach (var info in paramInfo)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = "@" + info.Name;
                    param.DbType = info.Type;
                    param.Value = info.Val ?? DBNull.Value;
                    param.Direction = ParameterDirection.Input;
                    if (info.Type == DbType.String)
                    {
                        param.Size = 4000;
                        if (info.Val != null && ((string)info.Val).Length > 4000)
                        {
                            param.Size = -1;
                        }
                    }

                    if (info.Type == DbType.Xml)
                    {

                        // initially we tried TVP, however it performs quite poorly.
                        // keep in mind SQL support up to 2000 params easily in sp_executesql, needing more is rare

                        var list = info.Val as IEnumerable;
                        var count = 0;
                        bool isString = info.Val is IEnumerable<string>;

                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                count++;
                                var listParam = cmd.CreateParameter();
                                listParam.ParameterName = "@" + info.Name + count;
                                listParam.Value = item ?? DBNull.Value;
                                if (isString)
                                {
                                    listParam.Size = 4000;
                                    if (item != null && ((string)item).Length > 4000)
                                    {
                                        listParam.Size = -1;
                                    }
                                }
                                cmd.Parameters.Add(listParam);
                            }

                            cmd.CommandText = cmd.CommandText.Replace("@" + info.Name,
                                "(" + string.Join(
                                    ",", Enumerable.Range(1, count).Select(i => "@" + info.Name + i)
                                ) + ")");
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        cmd.Parameters.Add(param);
                    }
                }
            }
            return cmd;
        }


        private static int ExecuteCommand(IDbConnection cnn, IDbTransaction tranaction, string sql, List<ParamInfo> paramInfo)
        {
            using (var cmd = SetupCommand(cnn, tranaction, sql, paramInfo))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static IDataReader GetReader(IDbConnection cnn, IDbTransaction tranaction, string sql, List<ParamInfo> paramInfo)
        {
            using (var cmd = SetupCommand(cnn, tranaction, sql, paramInfo))
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

        public static Func<IDataReader, T> GetClassDeserializer<T>(IDataReader reader)
        {
            DynamicMethod dm = new DynamicMethod("Deserialize" + Guid.NewGuid().ToString(), typeof(T), new Type[] { typeof(IDataReader) }, true);

            var il = dm.GetILGenerator();

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(p => new { Name = p.Name, Setter = p.GetSetMethod(true), Type = p.PropertyType })
                .Where(info => info.Setter != null)
                .ToList();

            var names = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
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

            int index = 0;

            // stack is empty
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes)); // stack is now [target]
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

                    il.MarkLabel(finishLabel);
                }
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
