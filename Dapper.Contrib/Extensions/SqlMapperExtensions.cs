using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dapper.Contrib.Extensions
{

    public static class SqlMapperExtensions
    {

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> KeyProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> TypeProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();

        private static IEnumerable<PropertyInfo> KeyPropertiesCache(Type type)
        {
            if (KeyProperties.ContainsKey(type))
            {
                return KeyProperties[type];
            }

            var allProperties = TypePropertiesCache(type);
            var keyProperties = allProperties.Where(p => p.GetCustomAttributes(true).Any(a => a is KeyAttribute));
            KeyProperties.Add(type, keyProperties);
            return keyProperties;
        }

        private static IEnumerable<PropertyInfo> TypePropertiesCache(Type type)
        {
            if (TypeProperties.ContainsKey(type))
            {
                return TypeProperties[type];
            }

            var properties = type.GetProperties();
            TypeProperties.Add(type, properties);
            return properties;
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
        public static T Get<T>(this IDbConnection connection, object id)
        {
            var type = typeof(T);
            if (!type.IsInterface)
                throw new DataException("This version of Get<T>() only supports interfaces.");

            var keys = KeyPropertiesCache(type);
            if (keys.Count() > 1)
                throw new DataException("Get<T> only supports an entity with a single [Key] property");
            if (keys.Count() == 0)
                throw new DataException("Get<T> only supports en entity with a [Key] property");

            var onlyKey = keys.First();
            var name = type.Name;
            if (type.IsInterface && name.StartsWith("I"))
                name = name.Substring(1);
            var sql = "select * from " + name + "s where " + onlyKey.Name + " = @" + onlyKey.Name;
            var dynParms = new DynamicParameters();
            dynParms.Add("@" + onlyKey.Name, id);
            var res = connection.Query(sql, dynParms).FirstOrDefault() as SqlMapper.FastExpando;
            
            if (res == null)
                return (T) ((object) null);
            
            var proxy = ProxyGenerator.GetInterfaceProxy<T>();
            foreach (var property in TypePropertiesCache(type))
            {
                var val = res.GetProperty(property.Name);
                property.SetValue(proxy, val, null);
            }
            ((IProxy)proxy).IsDirty = false;   //reset change tracking and return
            return proxy;

        }

        /// <summary>
        /// Inserts an entity into table "Ts" and returns identity id.
        /// </summary>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToInsert">Entity to insert</param>
        /// <returns>Identity of inserted entity</returns>
        public static long Insert<T>(this IDbConnection connection, T entityToInsert)
        {
            var tx = connection.BeginTransaction();
            var name = entityToInsert.GetType().Name;
            var sb = new StringBuilder(null);
            sb.AppendFormat("insert into {0}s (", name);

            var allProperties = TypePropertiesCache(typeof(T));
            var keyProperties = KeyPropertiesCache(typeof(T));

            for (var i = 0; i < allProperties.Count(); i++)
            {
                var property = allProperties.ElementAt(i);
                if (keyProperties.Contains(property)) continue;

                sb.Append(property.Name);
                if (i < allProperties.Count() - 1)
                    sb.Append(", ");
            }
            sb.Append(") values (");
            for (var i = 0; i < allProperties.Count(); i++)
            {
                var property = allProperties.ElementAt(i);
                if (keyProperties.Contains(property)) continue;

                sb.AppendFormat("@{0}", property.Name);
                if (i < allProperties.Count() - 1)
                    sb.Append(", ");
            }
            sb.Append(") ");
            connection.Execute(sb.ToString(), entityToInsert);
            //NOTE: would prefer to use IDENT_CURRENT('tablename') or IDENT_SCOPE but these are not available on SQLCE
            var r = connection.Query("select @@IDENTITY id");
            tx.Commit();
            return (int)r.First().id;
        }

        /// <summary>
        /// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
        /// </summary>
        /// <typeparam name="T">Type to be updated</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToUpdate">Entity to be updated</param>
        /// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
        public static bool Update<T>(this IDbConnection connection, T entityToUpdate)
        {
            var proxy = ((IProxy)entityToUpdate);
            if (proxy != null)
            {
                if (!proxy.IsDirty) return false;
            }

            var type = typeof(T);

            var keyProperties = KeyPropertiesCache(type);
            if (keyProperties.Count() == 0)
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = type.Name;
            if (type.IsInterface && name.StartsWith("I"))
                name = name.Substring(1);

            var sb = new StringBuilder();
            sb.AppendFormat("update {0}s set ", name);

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
            var updated = connection.Execute(sb.ToString(), entityToUpdate);
            return updated > 0;
        }

        /// <summary>
        /// Delete entity in table "Ts".
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToDelete">Entity to delete</param>
        /// <returns>true if deleted, false if not found</returns>
        public static bool Delete<T>(this IDbConnection connection, T entityToDelete)
        {
            var type = typeof(T);

            var keyProperties = KeyPropertiesCache(type);
            if (keyProperties.Count() == 0)
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = type.Name;
            if (type.IsInterface && name.StartsWith("I"))
                name = name.Substring(1);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from {0}s where ", name);

            for (var i = 0; i < keyProperties.Count(); i++)
            {
                var property = keyProperties.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.Name, property.Name);
                if (i < keyProperties.Count() - 1)
                    sb.AppendFormat(" and ");
            }
            var deleted = connection.Execute(sb.ToString(), entityToDelete);
            return deleted > 0;
        }
    }
}
