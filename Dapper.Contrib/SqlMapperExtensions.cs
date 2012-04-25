using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Threading;
using System.Runtime.CompilerServices;
using Dapper;

namespace Dapper.Contrib.Extensions
{

    public static class SqlMapperExtensions
    {
        public interface IProxy
        {
            bool IsDirty { get; set; }
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> KeyProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> GetQueries = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName = new ConcurrentDictionary<RuntimeTypeHandle, string>();

		private static readonly Dictionary<string, ISqlAdapter> AdapterDictionary = new Dictionary<string, ISqlAdapter>() {
																							{"sqlconnection", new SqlServerAdapter()},
																							{"npgsqlconnection", new PostgresAdapter()}
																						};

        private static IEnumerable<PropertyInfo> KeyPropertiesCache(Type type)
        {

            IEnumerable<PropertyInfo> pi;
            if (KeyProperties.TryGetValue(type.TypeHandle,out pi))
            {
                return pi;
            }

            var allProperties = TypePropertiesCache(type);
            var keyProperties = allProperties.Where(p => p.GetCustomAttributes(true).Any(a => a is KeyAttribute)).ToList();

            if (keyProperties.Count == 0)
            {
                var idProp = allProperties.Where(p => p.Name.ToLower() == "id").FirstOrDefault();
                if (idProp != null)
                {
                    keyProperties.Add(idProp);
                }
            }

            KeyProperties[type.TypeHandle] = keyProperties;
            return keyProperties;
        }
        private static IEnumerable<PropertyInfo> TypePropertiesCache(Type type)
        {
            IEnumerable<PropertyInfo> pis;
            if (TypeProperties.TryGetValue(type.TypeHandle, out pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable);
            TypeProperties[type.TypeHandle] = properties;
            return properties;
        }

		public static bool IsWriteable(PropertyInfo pi)
		{
			object[] attributes = pi.GetCustomAttributes(typeof (WriteAttribute), false);
			if (attributes.Length == 1)
			{
				WriteAttribute write = (WriteAttribute) attributes[0];
				return write.Write;
			}
			return true;
		}

    	/// <summary>
        /// Returns a single entity by a single id from table "Ts". T must be of interface type. 
        /// Id must be marked with [Key] attribute.
        /// Created entity is tracked/intercepted for changes and used by the Update() extension. 
        /// </summary>
        /// <typeparam name="T">Interface type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="id">Id of the entity to get, must be marked with [Key] attribute</param>
        /// <returns>Entity of T</returns>
        public static T Get<T>(this IDbConnection connection, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            string sql;
            if (!GetQueries.TryGetValue(type.TypeHandle, out sql))
            { 
                var keys = KeyPropertiesCache(type);
                if (keys.Count() > 1)
                    throw new DataException("Get<T> only supports an entity with a single [Key] property");
                if (keys.Count() == 0)
                    throw new DataException("Get<T> only supports en entity with a [Key] property");

                var onlyKey = keys.First();

                var name = GetTableName(type);

                // TODO: pluralizer 
                // TODO: query information schema and only select fields that are both in information schema and underlying class / interface 
                sql = "select * from " + name + " where " + onlyKey.Name + " = @id";
                GetQueries[type.TypeHandle] = sql;
            }
           
            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);

            T obj = null;

            if (type.IsInterface)
            {
                var res = connection.Query(sql, dynParms).FirstOrDefault() as IDictionary<string, object>;

                if (res == null)
                    return (T)((object)null);

                obj = ProxyGenerator.GetInterfaceProxy<T>();

                foreach (var property in TypePropertiesCache(type))
                {
                    var val = res[property.Name];
                    property.SetValue(obj, val, null);
                }

                ((IProxy)obj).IsDirty = false;   //reset change tracking and return
            }
            else
            {
                obj = connection.Query<T>(sql, dynParms, transaction: transaction, commandTimeout: commandTimeout).FirstOrDefault();
            }
            return obj;
        }

        private static string GetTableName(Type type)
        {
            string name;
            if (!TypeTableName.TryGetValue(type.TypeHandle, out name))
            {
                name = type.Name + "s";
                if (type.IsInterface && name.StartsWith("I"))
                    name = name.Substring(1);
              
                //NOTE: This as dynamic trick should be able to handle both our own Table-attribute as well as the one in EntityFramework 
                var tableattr = type.GetCustomAttributes(false).Where(attr => attr.GetType().Name == "TableAttribute").SingleOrDefault() as
                    dynamic;
                if (tableattr != null)
                    name = tableattr.Name;
                TypeTableName[type.TypeHandle] = name;
            }
            return name;
        }
        
        /// <summary>
        /// Inserts an entity into table "Ts" and returns identity id.
        /// </summary>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToInsert">Entity to insert</param>
        /// <returns>Identity of inserted entity</returns>
        public static long Insert<T>(this IDbConnection connection, T entityToInsert, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            
            var type = typeof(T);

            var name = GetTableName(type);

            var sbColumnList = new StringBuilder(null);

			var allProperties = TypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var allPropertiesExceptKey = allProperties.Except(keyProperties);

            for (var i = 0; i < allPropertiesExceptKey.Count(); i++)
            {
                var property = allPropertiesExceptKey.ElementAt(i);
				sbColumnList.Append(property.Name);
                if (i < allPropertiesExceptKey.Count() - 1)
					sbColumnList.Append(", ");
            }

			var sbParameterList = new StringBuilder(null);
			for (var i = 0; i < allPropertiesExceptKey.Count(); i++)
            {
                var property = allPropertiesExceptKey.ElementAt(i);
                sbParameterList.AppendFormat("@{0}", property.Name);
                if (i < allPropertiesExceptKey.Count() - 1)
                    sbParameterList.Append(", ");
            }
			ISqlAdapter adapter = GetFormatter(connection);
			int id = adapter.Insert(connection, transaction, commandTimeout, name, sbColumnList.ToString(), sbParameterList.ToString(),  keyProperties, entityToInsert);
			return id;
        }

        /// <summary>
        /// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
        /// </summary>
        /// <typeparam name="T">Type to be updated</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToUpdate">Entity to be updated</param>
        /// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
        public static bool Update<T>(this IDbConnection connection, T entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var proxy = entityToUpdate as IProxy;
            if (proxy != null)
            {
                if (!proxy.IsDirty) return false;
            }

            var type = typeof(T);

            var keyProperties = KeyPropertiesCache(type);
            if (!keyProperties.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(type);

            var sb = new StringBuilder();
            sb.AppendFormat("update {0} set ", name);

            var allProperties = TypePropertiesCache(type);
            var nonIdProps = allProperties.Where(a => !keyProperties.Contains(a));

            for (var i = 0; i < nonIdProps.Count(); i++)
            {
                var property = nonIdProps.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.Name, property.Name);
                if (i < nonIdProps.Count() - 1)
                    sb.AppendFormat(", ");
            }
            sb.Append(" where ");
            for (var i = 0; i < keyProperties.Count(); i++)
            {
                var property = keyProperties.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.Name, property.Name);
                if (i < keyProperties.Count() - 1)
                    sb.AppendFormat(" and ");
            }
            var updated = connection.Execute(sb.ToString(), entityToUpdate, commandTimeout: commandTimeout, transaction: transaction);
            return updated > 0;
        }

        /// <summary>
        /// Delete entity in table "Ts".
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToDelete">Entity to delete</param>
        /// <returns>true if deleted, false if not found</returns>
        public static bool Delete<T>(this IDbConnection connection, T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
			if (entityToDelete == null)
				throw new ArgumentException("Cannot Delete null Object", "entityToDelete");

            var type = typeof(T);

            var keyProperties = KeyPropertiesCache(type);
            if (keyProperties.Count() == 0)
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(type);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from {0} where ", name);

            for (var i = 0; i < keyProperties.Count(); i++)
            {
                var property = keyProperties.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.Name, property.Name);
                if (i < keyProperties.Count() - 1)
                    sb.AppendFormat(" and ");
            }
            var deleted = connection.Execute(sb.ToString(), entityToDelete, transaction: transaction, commandTimeout: commandTimeout);
            return deleted > 0;
        }

		public static ISqlAdapter GetFormatter(IDbConnection connection)
		{
			string name = connection.GetType().Name.ToLower();
			if (!AdapterDictionary.ContainsKey(name))
				return new SqlServerAdapter();
			return AdapterDictionary[name];
		}

    	class ProxyGenerator
        {
            private static readonly Dictionary<Type, object> TypeCache = new Dictionary<Type, object>();

            private static AssemblyBuilder GetAsmBuilder(string name)
            {
                var assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(new AssemblyName { Name = name },
                    AssemblyBuilderAccess.Run);       //NOTE: to save, use RunAndSave

                return assemblyBuilder;
            }

            public static T GetClassProxy<T>()
            {
                // A class proxy could be implemented if all properties are virtual
                //  otherwise there is a pretty dangerous case where internal actions will not update dirty tracking
                throw new NotImplementedException();
            }


            public static T GetInterfaceProxy<T>()
            {
                Type typeOfT = typeof(T);

                object k;
                if (TypeCache.TryGetValue(typeOfT, out k))
                {
                    return (T)k;
                }
                var assemblyBuilder = GetAsmBuilder(typeOfT.Name);

                var moduleBuilder = assemblyBuilder.DefineDynamicModule("SqlMapperExtensions." + typeOfT.Name); //NOTE: to save, add "asdasd.dll" parameter

                var interfaceType = typeof(Dapper.Contrib.Extensions.SqlMapperExtensions.IProxy);
                var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "_" + Guid.NewGuid(),
                    TypeAttributes.Public | TypeAttributes.Class);
                typeBuilder.AddInterfaceImplementation(typeOfT);
                typeBuilder.AddInterfaceImplementation(interfaceType);

                //create our _isDirty field, which implements IProxy
                var setIsDirtyMethod = CreateIsDirtyProperty(typeBuilder);

                // Generate a field for each property, which implements the T
                foreach (var property in typeof(T).GetProperties())
                {
                    var isId = property.GetCustomAttributes(true).Any(a => a is KeyAttribute);
                    CreateProperty<T>(typeBuilder, property.Name, property.PropertyType, setIsDirtyMethod, isId);
                }

                var generatedType = typeBuilder.CreateType();

                //assemblyBuilder.Save(name + ".dll");  //NOTE: to save, uncomment

                var generatedObject = Activator.CreateInstance(generatedType);

                TypeCache.Add(typeOfT, generatedObject);
                return (T)generatedObject;
            }


            private static MethodInfo CreateIsDirtyProperty(TypeBuilder typeBuilder)
            {
                var propType = typeof(bool);
                var field = typeBuilder.DefineField("_" + "IsDirty", propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty("IsDirty",
                                               System.Reflection.PropertyAttributes.None,
                                               propType,
                                               new Type[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.SpecialName |
                                                    MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig;

                // Define the "get" and "set" accessor methods
                var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + "IsDirty",
                                             getSetAttr,
                                             propType,
                                             Type.EmptyTypes);
                var currGetIL = currGetPropMthdBldr.GetILGenerator();
                currGetIL.Emit(OpCodes.Ldarg_0);
                currGetIL.Emit(OpCodes.Ldfld, field);
                currGetIL.Emit(OpCodes.Ret);
                var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + "IsDirty",
                                             getSetAttr,
                                             null,
                                             new Type[] { propType });
                var currSetIL = currSetPropMthdBldr.GetILGenerator();
                currSetIL.Emit(OpCodes.Ldarg_0);
                currSetIL.Emit(OpCodes.Ldarg_1);
                currSetIL.Emit(OpCodes.Stfld, field);
                currSetIL.Emit(OpCodes.Ret);

                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
                var getMethod = typeof(Dapper.Contrib.Extensions.SqlMapperExtensions.IProxy).GetMethod("get_" + "IsDirty");
                var setMethod = typeof(Dapper.Contrib.Extensions.SqlMapperExtensions.IProxy).GetMethod("set_" + "IsDirty");
                typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
                typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);

                return currSetPropMthdBldr;
            }

            private static void CreateProperty<T>(TypeBuilder typeBuilder, string propertyName, Type propType, MethodInfo setIsDirtyMethod, bool isIdentity)
            {
                //Define the field and the property 
                var field = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(propertyName,
                                               System.Reflection.PropertyAttributes.None,
                                               propType,
                                               new Type[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.Virtual |
                                                    MethodAttributes.HideBySig;

                // Define the "get" and "set" accessor methods
                var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName,
                                             getSetAttr,
                                             propType,
                                             Type.EmptyTypes);

                var currGetIL = currGetPropMthdBldr.GetILGenerator();
                currGetIL.Emit(OpCodes.Ldarg_0);
                currGetIL.Emit(OpCodes.Ldfld, field);
                currGetIL.Emit(OpCodes.Ret);

                var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                                             getSetAttr,
                                             null,
                                             new Type[] { propType });

                //store value in private field and set the isdirty flag
                var currSetIL = currSetPropMthdBldr.GetILGenerator();
                currSetIL.Emit(OpCodes.Ldarg_0);
                currSetIL.Emit(OpCodes.Ldarg_1);
                currSetIL.Emit(OpCodes.Stfld, field);
                currSetIL.Emit(OpCodes.Ldarg_0);
                currSetIL.Emit(OpCodes.Ldc_I4_1);
                currSetIL.Emit(OpCodes.Call, setIsDirtyMethod);
                currSetIL.Emit(OpCodes.Ret);

                //TODO: Should copy all attributes defined by the interface?
                if (isIdentity)
                {
                    var keyAttribute = typeof(KeyAttribute);
                    var myConstructorInfo = keyAttribute.GetConstructor(new Type[] { });
                    var attributeBuilder = new CustomAttributeBuilder(myConstructorInfo, new object[] { });
                    property.SetCustomAttribute(attributeBuilder);
                }

                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
                var getMethod = typeof(T).GetMethod("get_" + propertyName);
                var setMethod = typeof(T).GetMethod("set_" + propertyName);
                typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
                typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);
            }

        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string tableName)
        {
            Name = tableName;
        }
        public string Name { get; private set; }
    }

    // do not want to depend on data annotations that is not in client profile
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
    }

	[AttributeUsage(AttributeTargets.Property)]
	public class WriteAttribute : Attribute
	{
		public WriteAttribute(bool write)
        {
			Write = write;
        }
        public bool Write { get; private set; }
	}
}

public interface ISqlAdapter
{
	int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, String tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert);
}

public class SqlServerAdapter : ISqlAdapter
{
	public int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, String tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
	{
		string cmd = String.Format("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);

		connection.Execute(cmd, entityToInsert, transaction: transaction, commandTimeout: commandTimeout); 

		//NOTE: would prefer to use IDENT_CURRENT('tablename') or IDENT_SCOPE but these are not available on SQLCE
		var r = connection.Query("select @@IDENTITY id", transaction: transaction, commandTimeout: commandTimeout);
		int id = (int)r.First().id;
		if (keyProperties.Any())
			keyProperties.First().SetValue(entityToInsert, id, null);
		return id;
	}
}

public class PostgresAdapter : ISqlAdapter
{
	public int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, String tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendFormat("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);

		// If no primary key then safe to assume a join table with not too much data to return
		if (!keyProperties.Any())
			sb.Append(" RETURNING *");
		else
		{
			sb.Append(" RETURNING ");
			bool first = true;
			foreach (var property in keyProperties)
			{
				if (!first)
					sb.Append(", ");
				first = false;
				sb.Append(property.Name);
			}
		}

		var results = connection.Query(sb.ToString(), entityToInsert, transaction: transaction, commandTimeout: commandTimeout);

		// Return the key by assinging the corresponding property in the object - by product is that it supports compound primary keys
		int id = 0;
		foreach (var p in keyProperties)
		{
			var value = ((IDictionary<string, object>)results.First())[p.Name.ToLower()];
			p.SetValue(entityToInsert, value, null);
			if (id == 0)
				id = Convert.ToInt32(value);
		}
		return id;
	}
}