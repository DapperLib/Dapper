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

namespace SqlMapper
{
    public static class SqlMapper
    {
        static SqlMapper()
        {
            typeMap = new Dictionary<Type, SqlDbType>();
            typeMap[typeof(int)] = SqlDbType.Int;
            typeMap[typeof(int?)] = SqlDbType.Int;
            typeMap[typeof(string)] = SqlDbType.NVarChar;

            // weird ... I know see: http://msdn.microsoft.com/en-us/library/ms131092.aspx
            typeMap[typeof(double)] = SqlDbType.Float;
            typeMap[typeof(double?)] = SqlDbType.Float;

            typeMap[typeof(bool)] = SqlDbType.Bit;
            typeMap[typeof(bool?)] = SqlDbType.Bit;

            typeMap[typeof(Guid)] = SqlDbType.UniqueIdentifier;
            typeMap[typeof(Guid?)] = SqlDbType.UniqueIdentifier;
            typeMap[typeof(int[])] = SqlDbType.Structured;
            typeMap[typeof(List<int>)] = SqlDbType.Structured;
            typeMap[typeof(string[])] = SqlDbType.Structured;
            typeMap[typeof(List<string>)] = SqlDbType.Structured;
        }


        class ParamInfo
        {
            private ParamInfo()
            {
            }

            public static ParamInfo Create(string name, SqlDbType type, object val)
            {
                return new ParamInfo { Name = name, Type = type, Val = val};
            }

            public SqlDbType Type { get; private set; }
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

        static ConcurrentDictionary<Identity, object> cachedSerializers = new ConcurrentDictionary<Identity, object>();
        static ConcurrentDictionary<Type, Func<object, List<ParamInfo>>> cachedParamReaders = new ConcurrentDictionary<Type, Func<object, List<ParamInfo>>>();

        static Dictionary<Type, SqlDbType> typeMap;

        public static List<dynamic> ExecuteMapperQuery (this SqlConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
        {
            // TODO: get rid of casting hackery
            return ExecuteMapperQuery<ExpandoObject>(cnn, sql, param, transaction).Select(s => s as dynamic).ToList();
        }

        public static List<T> ExecuteMapperQuery<T>(this SqlConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
        {
            var identity = new Identity(sql, typeof(T));
            var rval = new List<T>();


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

            
            using (var reader = GetReader<T>(cnn, transaction, sql, paramInfo))
            {
                object oDeserializer;
                if (!cachedSerializers.TryGetValue(identity, out oDeserializer))
                {
                    if (typeof(T) == typeof(ExpandoObject))
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
                Func<SqlDataReader, T> deserializer = (Func<SqlDataReader, T>)oDeserializer;
                while (reader.Read())
                {
                    rval.Add(deserializer(reader));
                }
                // ignore any other grids; note that this might mean we miss exceptions that happen
                // late in the TDS stream, but that is bad design anyhow
            }

            return rval;
        }

        private static object GetDynamicDeserializer(SqlDataReader reader)
        {
            List<string> colNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                colNames.Add(reader.GetName(i));
            }

            Func<SqlDataReader, ExpandoObject> rval =
                r => 
                {
                    IDictionary<string, object> row = new ExpandoObject();
                    int i = 0;
                    foreach (var colName in colNames)
                    {
                        var tmp = reader.GetValue(i);
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
                // we want to call list.Add(ParamInfo.Create(string name, SqlDbType type, object val))

                il.Emit(OpCodes.Dup); // stack is now [list] [list]

                il.Emit(OpCodes.Ldstr, prop.Name); // stack is  [list] [list] [name]
                il.Emit(OpCodes.Ldc_I4, (int)typeMap[prop.PropertyType]); // stack is [list] [list] [name] [dbtype]
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


        private static SqlDataReader GetReader<T>(SqlConnection cnn, SqlTransaction tranaction, string sql, List<ParamInfo> paramInfo)
        {
            using (var cmd = cnn.CreateCommand())
            {
                cmd.Transaction = tranaction;
                cmd.CommandText = sql;
                if (paramInfo != null)
                {
                    foreach (var info in paramInfo)
                    {
                        var param = cmd.Parameters.Add("@" + info.Name, info.Type);
                        param.Value = info.Val ?? DBNull.Value;
                        param.Direction = ParameterDirection.Input;
                        if (info.Type == SqlDbType.NVarChar)
                        {
                            param.Size = 4000;
                        }

                        if (info.Type == SqlDbType.Structured)
                        {
                            List<SqlDataRecord> items = new List<SqlDataRecord>();
                            SqlMetaData[] metadata;  
                            
                            var intList = info.Val as IEnumerable<int>;
                            if (intList != null)
                            {
                                metadata = new[] { new SqlMetaData("Id", SqlDbType.Int) };
                                foreach (int id in intList)
                                {
                                    SqlDataRecord rec = new SqlDataRecord(metadata);
                                    rec.SetInt32(0, id);
                                    items.Add(rec);
                                }
                                param.TypeName = "int_list";
                            }
                            else
                            {
                                var strList = info.Val as IEnumerable<string>;
                                if (strList != null)
                                {
                                    metadata = new[] { new SqlMetaData("Name", SqlDbType.NVarChar, 4000) };
                                    foreach (var str in strList)
                                    {
                                        SqlDataRecord rec = new SqlDataRecord(metadata);
                                        rec.SetString(0, str);
                                        items.Add(rec);
                                    }
                                    param.TypeName = "string_list";
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }

                            param.Direction = ParameterDirection.Input;
                            param.Value = items;
                        }
                    }
                }
                return cmd.ExecuteReader();
            }
        }


        private static object GetStructDeserializer<T>(SqlDataReader reader)
        {
            Func<SqlDataReader, T> deserializer = null;
            deserializer = r => (T)r.GetValue(0);
            return deserializer;
        }

        public static Func<SqlDataReader, T> GetClassDeserializer<T>(SqlDataReader reader)
        {
            DynamicMethod dm = new DynamicMethod("Deserialize" + Guid.NewGuid().ToString(), typeof(T), new Type[] { typeof(SqlDataReader) }, true);

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


            var getItem = typeof(SqlDataReader).GetProperties(BindingFlags.Instance | BindingFlags.Public)
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

            return (Func<SqlDataReader, T>)dm.CreateDelegate(typeof(Func<SqlDataReader, T>));
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
