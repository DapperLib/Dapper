using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

using Dapper;
using Dapper.Contrib.Extensions;

#pragma warning disable 1573, 1591 // xml comments

namespace Dapper.Contrib.Extensions
{

    public static partial class SqlMapperExtensions
    {

        /// <summary>
        /// Returns a single entity by a single id from table "Ts" asynchronously using .NET 4.5 Task. T must be of interface type. 
        /// Id must be marked with [Key] attribute.
        /// Created entity is tracked/intercepted for changes and used by the Update() extension. 
        /// </summary>
        /// <typeparam name="T">Interface type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="id">Id of the entity to get, must be marked with [Key] attribute</param>
        /// <returns>Entity of T</returns>
        public static async Task<T> GetAsync<T>(this IDbConnection connection, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            string sql;
            if (!GetQueries.TryGetValue(type.TypeHandle, out sql))
            {
                var keys = KeyPropertiesCache(type);
                if (keys.Count() > 1)
                    throw new DataException("Get<T> only supports an entity with a single [Key] property");
                if (!keys.Any())
                    throw new DataException("Get<T> only supports en entity with a [Key] property");

                var onlyKey = keys.First();

                var name = GetTableName(type);

                // TODO: query information schema and only select fields that are both in information schema and underlying class / interface 
                sql = "select * from " + name + " where " + onlyKey.Name + " = @id";
                GetQueries[type.TypeHandle] = sql;
            }

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);


            if (!type.IsInterface)
                return (await connection.QueryAsync<T>(sql, dynParms, transaction, commandTimeout).ConfigureAwait(false)).FirstOrDefault();

            var res = (await connection.QueryAsync<dynamic>(sql, dynParms).ConfigureAwait(false)).FirstOrDefault() as IDictionary<string, object>;

            if (res == null)
                return null;

            var obj = ProxyGenerator.GetInterfaceProxy<T>();

            foreach (var property in TypePropertiesCache(type))
            {
                var val = res[property.Name];
                property.SetValue(obj, Convert.ChangeType(val, property.PropertyType), null);
            }

            ((IProxy)obj).IsDirty = false;   //reset change tracking and return

            return obj;
        }

        /// <summary>
        /// Returns a list of entites from table "Ts".  
        /// Id of T must be marked with [Key] attribute.
        /// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        /// for optimal performance. 
        /// </summary>
        /// <typeparam name="T">Interface or type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <returns>Entity of T</returns>
        public static async Task<IEnumerable<T>> GetAllAsync<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var cacheType = typeof(List<T>);

            string sql;
            if (!GetQueries.TryGetValue(cacheType.TypeHandle, out sql))
            {
                var keys = KeyPropertiesCache(type);
                if (keys.Count() > 1)
                    throw new DataException("Get<T> only supports an entity with a single [Key] property");
                if (!keys.Any())
                    throw new DataException("Get<T> only supports en entity with a [Key] property");

                var name = GetTableName(type);

                // TODO: query information schema and only select fields that are both in information schema and underlying class / interface 
                sql = "select * from " + name;
                GetQueries[cacheType.TypeHandle] = sql;
            }

            if (!type.IsInterface)
            {
                return await connection.QueryAsync<T>(sql, null, transaction, commandTimeout: commandTimeout);
            }

            var result = await connection.QueryAsync(sql);
            var list = new List<T>();
            foreach (IDictionary<string, object> res in result)
            {
                var obj = ProxyGenerator.GetInterfaceProxy<T>();
                foreach (var property in TypePropertiesCache(type))
                {
                    var val = res[property.Name];
                    property.SetValue(obj, Convert.ChangeType(val, property.PropertyType), null);
                }
                ((IProxy)obj).IsDirty = false;   //reset change tracking and return
                list.Add(obj);
            }
            return list;
        }

        /// <summary>
        /// Inserts an entity into table "Ts" asynchronously using .NET 4.5 Task and returns identity id.
        /// </summary>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToInsert">Entity to insert</param>
        /// <returns>Identity of inserted entity</returns>
        public static async Task<int> InsertAsync<T>(this IDbConnection connection, T entityToInsert, IDbTransaction transaction = null,
            int? commandTimeout = null, ISqlAdapter sqlAdapter = null) where T : class
        {
            //NOTE: sqlAdapter parameter is no longer used, and that parameter is missing from the Insert<T> method in the "non-async" file
            return await connection.InsertAsync<int>(entityToInsert, transaction, commandTimeout);
        }

        /// <summary>
        /// Inserts an entity into table based on the entity name  and returns identity id of type T or number of inserted rows (long) if inserting a list.
        /// </summary>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToInsert">Entity to insert, can be list of entities</param>
        /// <returns>Identity of inserted entity, or number of inserted rows if inserting a list</returns>
        public static async Task<TKey> InsertAsync<TKey>(this IDbConnection connection, object entityToInsert, IDbTransaction transaction = null,
            int? commandTimeout = null)
        {
            var type = entityToInsert.GetType();
            if (type.IsArray || type.IsGenericType)
            {
                type = type.GetGenericArguments()[0];
            }

            var name = GetTableName(type);
            var sbColumnList = new StringBuilder(null);
            var allProperties = TypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
            if (!keyProperties.Any() && !explicitKeyProperties.Any())
                throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

            var computedProperties = ComputedPropertiesCache(type);
            var allPropertiesExceptKeyAndComputed = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

            var adapter = GetFormatter(connection);

            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count(); i++)
            {
                var property = allPropertiesExceptKeyAndComputed.ElementAt(i);
                adapter.AppendColumnName(sbColumnList, property.Name);  //fix for issue #336
                if (i < allPropertiesExceptKeyAndComputed.Count() - 1)
                    sbColumnList.Append(", ");
            }

            var sbParameterList = new StringBuilder(null);
            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count(); i++)
            {
                var property = allPropertiesExceptKeyAndComputed.ElementAt(i);
                sbParameterList.AppendFormat("@{0}", property.Name);
                if (i < allPropertiesExceptKeyAndComputed.Count() - 1)
                    sbParameterList.Append(", ");
            }

            var wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed) connection.Open();

            type = entityToInsert.GetType();
            if (type.IsArray || type.IsGenericType) //a list is inserted, should return nr of affected rows
            {
                var cmd = string.Format("insert into {0} ({1}) values ({2})", name, sbColumnList, sbParameterList);
                var affectedRows = await connection.ExecuteAsync(cmd, entityToInsert, transaction, commandTimeout);
                if (wasClosed) connection.Close();
                return (TKey)Convert.ChangeType(affectedRows, typeof(int));
            }

            var id = await adapter.InsertAsync<TKey>(connection, transaction, commandTimeout, name, sbColumnList.ToString(),
                sbParameterList.ToString(), keyProperties, explicitKeyProperties, entityToInsert);

            if (wasClosed) connection.Close();
            return id;
        }

        /// <summary>
        /// Updates entity in table "Ts" asynchronously using .NET 4.5 Task, checks if the entity is modified if the entity is tracked by the Get() extension.
        /// </summary>
        /// <typeparam name="T">Type to be updated</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToUpdate">Entity to be updated</param>
        /// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
        public static async Task<bool> UpdateAsync<T>(this IDbConnection connection, T entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var proxy = entityToUpdate as IProxy;
            if (proxy != null)
            {
                if (!proxy.IsDirty) return false;
            }

            var type = typeof(T);

            if (type.IsArray || type.IsGenericType)
                type = type.GetGenericArguments()[0];

            var keyProperties = KeyPropertiesCache(type);
            var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
            if (!keyProperties.Any() && !explicitKeyProperties.Any())
                throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

            var name = GetTableName(type);

            var sb = new StringBuilder();
            sb.AppendFormat("update {0} set ", name);

            var allProperties = TypePropertiesCache(type);
            keyProperties.AddRange(explicitKeyProperties);
            var computedProperties = ComputedPropertiesCache(type);
            var nonIdProps = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

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
            var updated = await connection.ExecuteAsync(sb.ToString(), entityToUpdate, commandTimeout: commandTimeout, transaction: transaction).ConfigureAwait(false);
            return updated > 0;
        }

        /// <summary>
        /// Delete entity in table "Ts" asynchronously using .NET 4.5 Task.
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToDelete">Entity to delete</param>
        /// <returns>true if deleted, false if not found</returns>
        public static async Task<bool> DeleteAsync<T>(this IDbConnection connection, T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            if (entityToDelete == null)
                throw new ArgumentException("Cannot Delete null Object", "entityToDelete");

            var type = typeof(T);

            if (type.IsArray || type.IsGenericType)
                type = type.GetGenericArguments()[0];

            var keyProperties = KeyPropertiesCache(type);
            var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
            if (!keyProperties.Any() && !explicitKeyProperties.Any())
                throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

            var name = GetTableName(type);
            keyProperties.AddRange(explicitKeyProperties);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from {0} where ", name);

            for (var i = 0; i < keyProperties.Count(); i++)
            {
                var property = keyProperties.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.Name, property.Name);
                if (i < keyProperties.Count() - 1)
                    sb.AppendFormat(" and ");
            }
            var deleted = await connection.ExecuteAsync(sb.ToString(), entityToDelete, transaction, commandTimeout).ConfigureAwait(false);
            return deleted > 0;
        }

        /// <summary>
        /// Delete all entities in the table related to the type T asynchronously using .NET 4.5 Task.
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <returns>true if deleted, false if none found</returns>
        public static async Task<bool> DeleteAllAsync<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var name = GetTableName(type);
            var statement = String.Format("delete from {0}", name);
            var deleted = await connection.ExecuteAsync(statement, null, transaction, commandTimeout).ConfigureAwait(false);
            return deleted > 0;
        }
    }
}

public partial interface ISqlAdapter
{
    Task<TKey> InsertAsync<TKey>(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, String tableName,
        string columnList, string parameterList, List<PropertyInfo> keyProperties, List<PropertyInfo> explicitKeyProperties, object entityToInsert);
}

public partial class SqlServerAdapter
{
    public async Task<TKey> InsertAsync<TKey>(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
        string tableName, string columnList, string parameterList, List<PropertyInfo> keyProperties, List<PropertyInfo> explicitKeyProperties, object entityToInsert)
    {
        var keyProperty = keyProperties.Any() ? keyProperties.First() : explicitKeyProperties.First();
        var cmd = string.Format("insert into {0} ({1}) OUTPUT INSERTED.{3} values ({2})", tableName, columnList, parameterList, keyProperty.Name);

        //if <T> matches type of given key
        if (keyProperty.PropertyType == typeof(TKey))
            return (await connection.QueryAsync<TKey>(cmd, entityToInsert, transaction, commandTimeout: commandTimeout)).FirstOrDefault();

        //TODO: merge with query above as idict then check
        var ret = (await connection.QueryAsync(cmd, entityToInsert, transaction, commandTimeout: commandTimeout)).FirstOrDefault() as
            IDictionary<string, object>;

        //we end up here in calls to non-generic Insert() where the return object is non-numeric-integral
        if (ret == null)
            return default(TKey);
        if (!(ret[keyProperty.Name].GetType()).IsNumericType())
            return default(TKey);

        //cast to int for backwards compat...
        return (TKey)Convert.ChangeType(ret[keyProperty.Name], typeof(int));

    }
}

public partial class SqlCeServerAdapter
{
    public async Task<TKey> InsertAsync<TKey>(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
        string tableName, string columnList, string parameterList, List<PropertyInfo> keyProperties, List<PropertyInfo> explicitKeyProperties, object entityToInsert)
    {
        var cmd = string.Format("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);
        await connection.ExecuteAsync(cmd, entityToInsert, transaction, commandTimeout);
        var ret = (await connection.QueryAsync("select @@IDENTITY id", transaction: transaction, commandTimeout: commandTimeout))
            .FirstOrDefault() as IDictionary<string, object>;

        object returnVal;

        if (keyProperties.Any())
        {
            if (ret == null)
                return default(TKey);
            returnVal = ret["id"];
        }
        else //explicit key given, just copy the value back as sqlce cannot do OUTPUT INSERTED...
        {
            returnVal = entityToInsert.GetType().GetProperty(explicitKeyProperties.First().Name).GetValue(entityToInsert, null);
        }

        if (returnVal.GetType() == typeof(TKey))
        {
            return (TKey)returnVal;
        }
        if ((returnVal.GetType()).IsNumericType())
        {
            //cast to int for backwards compat...
            var numVal = Convert.ChangeType(returnVal, typeof(int));
            return (TKey)numVal;
        }

        return default(TKey);
    }
}

public partial class MySqlAdapter : ISqlAdapter
{
    public async Task<TKey> InsertAsync<TKey>(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
        string tableName, string columnList, string parameterList, List<PropertyInfo> keyProperties, List<PropertyInfo> explicitKeyProperties, object entityToInsert)
    {
        var cmd = string.Format("insert into {0} ({1}) values ({2}); select last_insert_id() id", tableName, columnList, parameterList);
        var r = await connection.QueryMultipleAsync(cmd, entityToInsert, transaction, commandTimeout);
        var id = (TKey)r.Read().First().id; //NOTE: MySQL returns last_insert_id() as ulong.
        return id;
    }
}

public partial class PostgresAdapter
{

    public async Task<TKey> InsertAsync<TKey>(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
        string tableName, string columnList, string parameterList, List<PropertyInfo> keyProperties, List<PropertyInfo> explicitKeyProperties, object entityToInsert)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);

        // If no primary key then safe to assume a join table with not too much data to return
        keyProperties.AddRange(explicitKeyProperties);
        if (!keyProperties.Any())
            sb.Append(" RETURNING *");
        else
        {
            sb.Append(" RETURNING ");
            var first = true;
            foreach (var property in keyProperties)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(property.Name);
            }
        }

        var id = (await connection.QueryAsync<TKey>(sb.ToString(), entityToInsert, transaction, commandTimeout)).FirstOrDefault();
        return id;
    }
}

public partial class SQLiteAdapter
{

    public async Task<TKey> InsertAsync<TKey>(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
        string tableName, string columnList, string parameterList, List<PropertyInfo> keyProperties, List<PropertyInfo> explicitKeyProperties, object entityToInsert)
    {
        if (keyProperties.Any())
        {
            var cmd = string.Format("insert into {0} ({1}) values ({2}); select last_insert_rowid() id", tableName, columnList, parameterList);
            var multi = await connection.QueryMultipleAsync(cmd, entityToInsert, transaction, commandTimeout);
            return (TKey)multi.Read().First().id;
        }
        else //insert of object with explicit key
        {
            var cmd = string.Format("insert into {0} ({1}) values ({2});", tableName, columnList, parameterList);
            //insert object, don't care about the inserted id
            await connection.ExecuteAsync(cmd, entityToInsert, transaction, commandTimeout);
            var returnVal = entityToInsert.GetType().GetProperty(explicitKeyProperties.First().Name).GetValue(entityToInsert, null);

            if (returnVal.GetType() == typeof(TKey))
            {
                return (TKey)returnVal;
            }
            if ((returnVal.GetType()).IsNumericType())
            {
                //cast to int for backwards compat...
                var numVal = Convert.ChangeType(returnVal, typeof(int));
                return (TKey)numVal;
            }

            return default(TKey);
        }
    }
}
