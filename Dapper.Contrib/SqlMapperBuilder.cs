using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using Dapper.Contrib.Extensions;

namespace Dapper.Contrib
{
    public static class TypeMappers
    {
        public static void Register<T>(TypeMapper<T> mapper) where T : class
        {
        }

        public static ITypeMapperBuilder<T> MapType<T>() where T : class
        {
            return new CommonTypeMapper<T>();
        }

        public static void ResetTypeMapping<T>()
        {
            var typeHandle = typeof(T).TypeHandle;
            IEnumerable<PropertyInfo> abc;
            string tableName;
            SqlMapperExtensions.InternalComputedProperties.TryRemove(typeHandle, out abc);
            SqlMapperExtensions.InternalExplicitKeyProperties.TryRemove(typeHandle, out abc);
            SqlMapperExtensions.InternalKeyProperties.TryRemove(typeHandle, out abc);
            SqlMapperExtensions.InternalTypeProperties.TryRemove(typeHandle, out abc);
            SqlMapperExtensions.InternalTypeTableName.TryRemove(typeHandle, out tableName);
        }
    }

    public partial interface ITypeMapperBuilder<T> where T : class
    {
        ITypeMapperBuilder<T> TableName(string name);
        IPropertyMapperBuilder<T> For(Expression<Func<T, object>> expression);
        ITypeMapperBuilder<T> Key(Expression<Func<T, object>> expression);
    }

    public partial interface IPropertyMapperBuilder<T> where T : class
    {
        IPropertyMapperBuilder<T> For(Expression<Func<T, object>> expression);
        IPropertyMapperBuilder<T> ExplicitKey();
        IPropertyMapperBuilder<T> NotWritable();
        IPropertyMapperBuilder<T> Computed();
    }

    internal class CommonTypeMapper<T> : TypeMapper<T> where T : class
    {
    }

    public abstract partial class TypeMapper<T> : ITypeMapperBuilder<T>, IPropertyMapperBuilder<T> where T : class
    {
        private static readonly RuntimeTypeHandle TypeHandle = typeof(T).TypeHandle;
        private PropertyInfo _currentProperty;

        private static void TryAdd(IDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> dic, PropertyInfo property)
        {
            if (!dic.ContainsKey(TypeHandle))
            {
                dic[TypeHandle] = new List<PropertyInfo>
                {
                    property
                };
            }
            else
            {
                var list = new List<PropertyInfo>(dic[TypeHandle]) {property};
                dic[TypeHandle] = list;
            }
        }

        private static PropertyInfo GetProperty(Expression<Func<T, object>>  expression)
        {
            return ReflectionHelper.GetProperty(expression) as PropertyInfo;
        }

        public ITypeMapperBuilder<T> TableName(string name)
        {
            SqlMapperExtensions.InternalTypeTableName[typeof(T).TypeHandle] = name;
            return this;            
        }
        
        public IPropertyMapperBuilder<T> For(Expression<Func<T, object>> expression)
        {
            _currentProperty = GetProperty(expression);
            return this;
        }

        public ITypeMapperBuilder<T> Key(Expression<Func<T, object>> expression)
        {
            TryAdd(SqlMapperExtensions.InternalKeyProperties, GetProperty(expression));
            return this;
        }


        public IPropertyMapperBuilder<T> ExplicitKey()
        {
            TryAdd(SqlMapperExtensions.InternalExplicitKeyProperties, _currentProperty);
            return this;
        }

        public IPropertyMapperBuilder<T> NotWritable()
        {
            var properties = SqlMapperExtensions.InternalTypePropertiesCache(typeof (T));
            var matchProperty = properties.FirstOrDefault(p => p.Equals(_currentProperty));
            if (matchProperty != null && properties.Remove(matchProperty))
            {
                SqlMapperExtensions.InternalTypeProperties[TypeHandle] = properties;
            }
            return this;
        }

        public IPropertyMapperBuilder<T> Computed()
        {
            TryAdd(SqlMapperExtensions.InternalComputedProperties, _currentProperty);
            return this;
        }
    }
}