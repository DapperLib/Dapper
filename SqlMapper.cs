using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection.Emit;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Microsoft.SqlServer.Server;
using System.Dynamic;
using System.Collections;

namespace SqlMapper
{
    public static class SqlMapper
    {
        static ConcurrentDictionary<Identity, object> cachedSerializers = new ConcurrentDictionary<Identity, object>();
        static ConcurrentDictionary<Type, Func<object, List<ParamInfo>>> cachedParamReaders = new ConcurrentDictionary<Type, Func<object, List<ParamInfo>>>();
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

            throw new NotSupportedException("The type : " + type.ToString() + " is not supported by the mapper");
        }


        class ParamInfo
        {
            private ParamInfo()
            {
            }

            public static ParamInfo Create(string name, DbType type, object val)
            {
                return new ParamInfo { Name = name, Type = type, Val = val};
            }

            public DbType Type { get; private set; }
            public string Name { get; private set; }
            public object Val { get; private set; }
        }

        private class Identity : IEquatable<Identity>
        {

            public Type Type { get { return type; } }
            public string Sql { get { return sql; } }
            internal Identity(string sql, Type type)
            {
                this.sql = sql;

                this.type = type;
                hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                hashCode = hashCode * 23 + (sql == null ? 0 : sql.GetHashCode());
                hashCode = hashCode * 23 + (type == null ? 0 : type.GetHashCode());
            }
            public override bool Equals(object obj)
            {
                return Equals(obj as Identity);
            }
            private readonly string sql;
            private readonly int hashCode;
            private readonly Type type;
            public override int GetHashCode()
            {
                return hashCode;
            }
            public bool Equals(Identity other)
            {
                return other != null && this.type == other.type
                    && sql == other.sql;
            }
        }

      

        /// <summary>
        /// Execute parameterized SQL  
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public static int ExecuteMapperCommand(this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
        {
            return ExecuteCommand(cnn, transaction, sql, GetParamInfo(param));
        }

        static class DynamicStub
        {
            public static Type Type = typeof(DynamicStub);
        }

        public static List<dynamic> ExecuteMapperQuery (this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
        {
            var identity = new Identity(sql, DynamicStub.Type);
            var list = new List<dynamic>();

            using (var reader = GetReader(cnn, transaction, sql, GetParamInfo(param)))
            {
                Func<IDataReader, ExpandoObject> deserializer = GetDeserializer<ExpandoObject>(identity, reader);
                while (reader.Read())
                {
                    list.Add(deserializer(reader));
                }
            }
            return list;
        }

       
        public static List<T> ExecuteMapperQuery<T>(this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
        {
            var identity = new Identity(sql, typeof(T));
            var rval = new List<T>();

            using (var reader = GetReader(cnn, transaction, sql, GetParamInfo(param)))
            {
                Func<IDataReader, T> deserializer = GetDeserializer<T>(identity, reader);
                while (reader.Read())
                {
                    rval.Add(deserializer(reader));
                }
                // ignore any other grids; note that this might mean we miss exceptions that happen
                // late in the TDS stream, but that is bad design anyhow
            }

            return rval;
        }

        private static Func<IDataReader, T> GetDeserializer<T>(Identity identity, IDataReader reader)
        {
            object oDeserializer;
            if (!cachedSerializers.TryGetValue(identity, out oDeserializer))
            {
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

                cachedSerializers[identity] = oDeserializer;
            }
            Func<IDataReader, T> deserializer = (Func<IDataReader, T>)oDeserializer;
            return deserializer;
        }

        private static List<ParamInfo> GetParamInfo(object param)
        {
            Func<object, List<ParamInfo>> paramInfoGenerator;
            List<ParamInfo> paramInfo = null;

            if (param != null)
            {
                if (!cachedParamReaders.TryGetValue(param.GetType(), out paramInfoGenerator))
                {
                    paramInfoGenerator = CreateParamInfoGenerator(param.GetType());
                    cachedParamReaders[param.GetType()] = paramInfoGenerator;
                }
                paramInfo = paramInfoGenerator(param);
            }

            return paramInfo;
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
                                listParam.ParameterName =  "@" + info.Name + count; 
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
            deserializer = r => (T)r.GetValue(0);
            return deserializer;
        }

        public static Func<IDataReader, T> GetClassDeserializer<T>(IDataReader reader)
        {
            DynamicMethod dm = new DynamicMethod("Deserialize" + Guid.NewGuid().ToString(), typeof(T), new Type[] { typeof(IDataReader) }, true);

            var il = dm.GetILGenerator();

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Name = p.Name, Setter = p.GetSetMethod(), Type = p.PropertyType })
                .Where(info => info.Setter != null)
                .ToList();

            var names = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                names.Add(reader.GetName(i));
            }

            var setters = (
                            from n in names
                            select new { Name = n, Info = properties.FirstOrDefault(p => p.Name == n) }
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
                    EmitInt32(il, index++); // stack is now [target][target][reader][index]

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
