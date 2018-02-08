using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.Contrib.Extensions
{
    enum DbActionType
    {
        Insert,
        Update,
        Delete,
        DeleteAll,
        //Get,
        //GetAll
    }

    class DbAction<T> where T : class
    {
        public DbActionType ActionType { get; }

        public T Entity { get; }

        public DbAction(DbActionType actionType, T entity)
        {
            ActionType = actionType;
            Entity = entity;
        }
    }

    /// <summary>
    /// The manager wrapped IDbConnection
    /// </summary>
    public class DbManager
    {
        private Lazy<IDbConnection> internalConnection;
        private IDbTransaction internalDbTransaction = null;
        private bool isOpened = false;
        private Queue DbActions { get; } = new Queue(8);
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, Delegate> insertDelegates = new ConcurrentDictionary<RuntimeTypeHandle, Delegate>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, Delegate> updateDelegates = new ConcurrentDictionary<RuntimeTypeHandle, Delegate>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, Delegate> deleteDelegates = new ConcurrentDictionary<RuntimeTypeHandle, Delegate>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, Delegate> getDelegates = new ConcurrentDictionary<RuntimeTypeHandle, Delegate>();

        /// <summary>
        /// construct the DbManager 
        /// </summary>
        /// <param name="factory">a delegate to build the IDbConnection derived class instance</param>
        public DbManager(Func<IDbConnection> factory) => internalConnection = new Lazy<IDbConnection>(factory, true);

        /// <summary>
        /// Prepared to insert an entity into table "Ts" and returns identity id or number of inserted rows if inserting a list.
        /// </summary>
        /// <typeparam name="T">The type to insert.</typeparam>        
        /// <param name="entity">Entity to insert, can be list of entities</param>
        /// <returns>Identity of inserted entity, or number of inserted rows if inserting a list</returns>
        public void Insert<T>(T entity) where T : class
        {
            DbActions.Enqueue(new DbAction<T>(DbActionType.Insert, entity));
        }

        /// <summary>
        /// Prepared to update entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
        /// </summary>
        /// <typeparam name="T">Type to be updated</typeparam>        
        /// <param name="entity">Entity to be updated</param>
        public void Update<T>(T entity) where T : class
        {
            DbActions.Enqueue(new DbAction<T>(DbActionType.Update, entity));
        }

        /// <summary>
        /// Prepared to delete entity in table "Ts".
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>        
        /// <param name="entity">Entity to delete</param>
        public void Delete<T>(T entity) where T : class
        {
            DbActions.Enqueue(new DbAction<T>(DbActionType.Delete, entity));
        }

        /// <summary>
        /// Returns a single entity by a single id from table "Ts".  
        /// Id must be marked with [Key] attribute.
        /// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        /// for optimal performance. 
        /// </summary>
        /// <typeparam name="T">Interface or type to create and populate</typeparam>
        /// <param name="id">Id of the entity to get, must be marked with [Key] attribute</param>       
        /// <returns>Entity of T</returns>
        public T Get<T>(dynamic id)
        {
            var entityType = typeof(T);
            var expr = GetGetDelegate(entityType);
            return (T)expr.DynamicInvoke(GetConnection(), id, null, null);
        }

        private void OpenConnection()
        {
            var con = GetConnection();
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
        }

        private void CloseConnection()
        {
            var con = GetConnection();
            if (con.State != ConnectionState.Closed)
            {
                con.Close();
            }
        }

        /// <summary>
        /// Fetch a trasaction from current connection
        /// </summary>
        public void BeginTrasaction()
        {
            if (internalDbTransaction == null)
            {
                OpenConnection();
                var con = GetConnection();
                internalDbTransaction = con.BeginTransaction();
            }
        }

        /// <summary>
        /// Commit a trasaction in current connection
        /// </summary>
        public void Commit()
        {
            if (internalDbTransaction != null)
            {
                internalDbTransaction.Commit();
            }

            if (!isOpened)
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Rollback a trasaction in current connection
        /// </summary>
        public void Rollback()
        {
            if (internalDbTransaction != null)
            {
                internalDbTransaction.Rollback();
            }

            if (!isOpened)
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Invoke all prepared actions before
        /// </summary>
        public void SaveChanges()
        {
            while (DbActions.Count > 0)
            {
                var action = DbActions.Dequeue();
                InvokeAction(action);
            }
        }

        /// <summary>
        /// Generate a sql-operation mapper that inherited ISqlOperationMapper
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetMapper<T>() where T : ISqlOperationMapper
        {
            return SqlOperationMapperBuilder.GetMapperInstance<T>(GetConnection());
        }

        private dynamic InvokeAction(object action)
        {
            Type dbActionType = action.GetType();

            var entityType = dbActionType.GenericTypeArguments[0];
            var entity = GetEntityOfAction(action);
            if (entityType != entity.GetType())
            {
                throw new InvalidOperationException("");

            }

            var actionType = GetActionTypeOfAction(action);
            Delegate expr;
            switch (actionType)
            {
                case DbActionType.Insert:
                    expr = GetInsertDelegate(dbActionType, entityType);
                    expr.DynamicInvoke(GetConnection(), entity, null, null);
                    break;
                case DbActionType.Update:
                    expr = GetUpdateDelegate(dbActionType, entityType);
                    expr.DynamicInvoke(GetConnection(), entity, null, null);
                    break;
                case DbActionType.Delete:
                    expr = GetDeleteDelegate(dbActionType, entityType);
                    expr.DynamicInvoke(GetConnection(), entity, null, null);
                    break;
                case DbActionType.DeleteAll:
                    break;
                default:
                    break;
            }

            return null;
        }

        private Delegate GetInsertDelegate(Type dbActionType, Type entityType)
        {
            if (insertDelegates.TryGetValue(dbActionType.TypeHandle, out Delegate insertDelegate))
            {
                return insertDelegate;
            }
            var method = typeof(SqlMapperExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "Insert" && x.IsGenericMethodDefinition).MakeGenericMethod(dbActionType.GetGenericArguments());
            var param0 = Expression.Parameter(typeof(IDbConnection));
            var param1 = Expression.Parameter(entityType);
            var param2 = Expression.Parameter(typeof(IDbTransaction));
            var param3 = Expression.Parameter(typeof(int?));
            var source = Expression.Call(method, param0, param1, param2, param3);

            var expr = Expression.Lambda(source, param0, param1, param2, param3).Compile();
            insertDelegates[dbActionType.TypeHandle] = expr;
            return expr;
        }

        private Delegate GetUpdateDelegate(Type dbActionType, Type entityType)
        {
            if (updateDelegates.TryGetValue(dbActionType.TypeHandle, out Delegate updateDelegate))
            {
                return updateDelegate;
            }
            var method = typeof(SqlMapperExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "Update" && x.IsGenericMethodDefinition).MakeGenericMethod(dbActionType.GetGenericArguments());
            var param0 = Expression.Parameter(typeof(IDbConnection));
            var param1 = Expression.Parameter(entityType);
            var param2 = Expression.Parameter(typeof(IDbTransaction));
            var param3 = Expression.Parameter(typeof(int?));
            var source = Expression.Call(method, param0, param1, param2, param3);

            var expr = Expression.Lambda(source, param0, param1, param2, param3).Compile();
            updateDelegates[dbActionType.TypeHandle] = expr;
            return expr;
        }

        private Delegate GetDeleteDelegate(Type dbActionType, Type entityType)
        {
            if (deleteDelegates.TryGetValue(dbActionType.TypeHandle, out Delegate deleteDelegate))
            {
                return deleteDelegate;
            }
            var method = typeof(SqlMapperExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "Delete" && x.IsGenericMethodDefinition).MakeGenericMethod(dbActionType.GetGenericArguments());
            var param0 = Expression.Parameter(typeof(IDbConnection));
            var param1 = Expression.Parameter(entityType);
            var param2 = Expression.Parameter(typeof(IDbTransaction));
            var param3 = Expression.Parameter(typeof(int?));
            var source = Expression.Call(method, param0, param1, param2, param3);

            var expr = Expression.Lambda(source, param0, param1, param2, param3).Compile();
            deleteDelegates[dbActionType.TypeHandle] = expr;
            return expr;
        }

        private Delegate GetGetDelegate(Type entityType)
        {
            if (getDelegates.TryGetValue(entityType.TypeHandle, out Delegate getDelegate))
            {
                return getDelegate;
            }
            var methodInfo = typeof(SqlMapperExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "Get" && x.IsGenericMethodDefinition);
            var method = methodInfo.MakeGenericMethod(new Type[] { entityType });
            var parameterInfo = methodInfo.GetParameters().FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

            var param0 = Expression.Parameter(typeof(IDbConnection));
            var PropertyInfoOfEntityDelegate = GetKeyPropertyInfoOfEntityDelegate(entityType);
            var keyPropertyInfo = PropertyInfoOfEntityDelegate.Invoke(nameof(DbManager), nameof(GetGetDelegate));
            var param1 = Expression.Parameter(parameterInfo.ParameterType);
            var param2 = Expression.Parameter(typeof(IDbTransaction));
            var param3 = Expression.Parameter(typeof(int?));
            var source = Expression.Call(method, param0, param1, param2, param3);

            var expr = Expression.Lambda(source, param0, param1, param2, param3).Compile();
            getDelegates[entityType.TypeHandle] = expr;
            return expr;
        }

        private Func<string, string, PropertyInfo> GetKeyPropertyInfoOfEntityDelegate(Type entityType)
        {
            var method = typeof(SqlMapperExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "GetSingleKeyOfEntity" && x.IsGenericMethodDefinition).MakeGenericMethod(new Type[] { entityType });
            var param0 = Expression.Parameter(typeof(string));
            var param1 = Expression.Parameter(typeof(string));
            var source = Expression.Call(method, param0, param1);

            var expr = Expression.Lambda<Func<string, string, PropertyInfo>>(source, param0, param1).Compile();
            return expr;
        }

        private object GetEntityOfAction(object obj)
        {
            var memberExpr = Expression.PropertyOrField(Expression.Constant(obj), "Entity");
            return Expression.Lambda(memberExpr).Compile().DynamicInvoke();
        }

        private DbActionType GetActionTypeOfAction(object obj)
        {
            var memberExpr = Expression.PropertyOrField(Expression.Constant(obj), "ActionType");
            return (DbActionType)Expression.Lambda(memberExpr).Compile().DynamicInvoke();
        }

        private IDbConnection GetConnection()
        {
            var isFirstCreated = !internalConnection.IsValueCreated;
            var con = internalConnection.Value;
            if (isFirstCreated)
            {
                isOpened = con.State == ConnectionState.Open;
                //CloseConnection();
            }
            return con;
        }
    }

    /// <summary>
    /// The interface used to define sql operation mapper
    /// </summary>
    public interface ISqlOperationMapper
    {

    }

    /// <summary>
    /// Define a querying sql on the method in any mapper interface inherited ISqlOperationMapper
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class QuerySqlAttribute : Attribute
    {

        /// <summary>
        /// Sql statement
        /// </summary>
        public string SqlFormat { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        public QuerySqlAttribute(string sql) => SqlFormat = sql;
    }

    /// <summary>
    /// Define an excuting sql on the method in any mapper interface inherited ISqlOperationMapper
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ExcuteSqlAttribute : Attribute
    {
        /// <summary>
        /// Sql statement
        /// </summary>
        public string SqlFormat { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        public ExcuteSqlAttribute(string sql) => SqlFormat = sql;
    }


}
