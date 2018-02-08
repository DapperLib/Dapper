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

        public DbManager(Func<IDbConnection> factory) => internalConnection = new Lazy<IDbConnection>(factory, true);

        public void Insert<T>(T entity) where T : class
        {
            DbActions.Enqueue(new DbAction<T>(DbActionType.Insert, entity));
        }

        public void Update<T>(T entity) where T : class
        {
            DbActions.Enqueue(new DbAction<T>(DbActionType.Update, entity));
        }

        public void Delete<T>(T entity) where T : class
        {
            DbActions.Enqueue(new DbAction<T>(DbActionType.Delete, entity));
        }

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

        public void BeginTrasaction()
        {
            if (internalDbTransaction == null)
            {
                OpenConnection();
                var con = GetConnection();
                internalDbTransaction = con.BeginTransaction();
            }
        }

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

        public void SaveChanges()
        {
            while (DbActions.Count > 0)
            {
                var action = DbActions.Dequeue();
                InvokeAction(action);
            }
        }

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

    public interface ISqlOperationMapper
    {

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class QuerySqlAttribute : Attribute
    {

        public string SqlFormat { get; }

        public QuerySqlAttribute(string sql) => SqlFormat = sql;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ExcuteSqlAttribute : Attribute
    {

        public string SqlFormat { get; }

        public ExcuteSqlAttribute(string sql) => SqlFormat = sql;
    }


}
