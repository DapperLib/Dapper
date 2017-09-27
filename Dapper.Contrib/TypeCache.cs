using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Extensions;

namespace Dapper.Contrib
{
    internal sealed class TypePropertyCache
    {
        public PropertyInfo[] KeyProperties { get; }
        public PropertyInfo[] ExplicitKeyProperties { get; }
        public PropertyInfo[] TypeProperties { get; }
        public PropertyInfo[] ComputedProperties { get; }
        public PropertyInfo[] RowVersionProperties { get; }

        public TypePropertyCache(
            PropertyInfo[] keyProperties,
            PropertyInfo[] explicitKeyProperties,
            PropertyInfo[] typeProperties,
            PropertyInfo[] computedProperties,
            PropertyInfo[] rowVersionProperties)
        {
            KeyProperties = keyProperties;
            ExplicitKeyProperties = explicitKeyProperties;
            TypeProperties = typeProperties;
            ComputedProperties = computedProperties;
            RowVersionProperties = rowVersionProperties;
        }
    }



    internal static class TypeCache
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, TypePropertyCache> CachedTypes =
            new ConcurrentDictionary<RuntimeTypeHandle, TypePropertyCache>();

        internal static readonly ConcurrentDictionary<RuntimeTypeHandle, string> GetQueries =
            new ConcurrentDictionary<RuntimeTypeHandle, string>();

        internal static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName =
            new ConcurrentDictionary<RuntimeTypeHandle, string>();

        private static TypePropertyCache WalkTypeProperties(Type type)
        {
            var allProperties = type.GetProperties();

            var properties = new List<PropertyInfo>(allProperties.Length);
            var keys = new List<PropertyInfo>();
            var explicitKeys = new List<PropertyInfo>();
            var computed = new List<PropertyInfo>();
            var rowVersions = new List<PropertyInfo>();

            PropertyInfo idPropertyByConvention = null;

            foreach (var property in allProperties.Where(IsWriteable))
            {
                properties.Add(property);

                var attributes = property.GetCustomAttributes(true);

                bool propertyHasExplicitKey = false;

                foreach (var attribute in attributes)
                {
                    if (attribute is KeyAttribute)
                    {
                        keys.Add(property);
                    }
                    else if (attribute is ExplicitKeyAttribute)
                    {
                        explicitKeys.Add(property);
                        propertyHasExplicitKey = true;
                    }
                    else if (attribute is ComputedAttribute)
                    {
                        computed.Add(property);
                    }
                    else if (attribute is RowVersionAttribute)
                    {
                        rowVersions.Add(property);
                    }
                }

                // If we have not yet found the convention-based Id and we have no regular keys and this property isn't an explicit key, keep searching.
                if (idPropertyByConvention == null && keys.Count == 0 && !propertyHasExplicitKey)
                {
                    if (string.Equals(property.Name, "id", StringComparison.CurrentCultureIgnoreCase))
                    {
                        idPropertyByConvention = property;
                    }
                }
            }

            if (keys.Count == 0 && idPropertyByConvention != null)
            {
                keys.Add(idPropertyByConvention);
            }

            // Capture arrays so we don't waste unallocated space in the lists, as we are caching for the duration of the process.
            // TODO When Spans come out, we can allocate one large array of PropertyInfo[] and span across for specific properties.
            var cache = new TypePropertyCache(
                keys.ToArray(),
                explicitKeys.ToArray(),
                properties.ToArray(),
                computed.ToArray(),
                rowVersions.ToArray());

            CachedTypes[type.TypeHandle] = cache;
            return cache;
        }

        internal static PropertyInfo[] RowVersionPropertiesCache(Type type)
        {
            if (CachedTypes.TryGetValue(type.TypeHandle, out TypePropertyCache properties))
            {
                return properties.RowVersionProperties;
            }

            var cache = WalkTypeProperties(type);
            return cache.RowVersionProperties;
        }

        internal static PropertyInfo[] ComputedPropertiesCache(Type type)
        {
            if (CachedTypes.TryGetValue(type.TypeHandle, out TypePropertyCache properties))
            {
                return properties.ComputedProperties;
            }

            var cache = WalkTypeProperties(type);
            return cache.ComputedProperties;
        }

        internal static PropertyInfo[] ExplicitKeyPropertiesCache(Type type)
        {
            if (CachedTypes.TryGetValue(type.TypeHandle, out TypePropertyCache properties))
            {
                return properties.ExplicitKeyProperties;
            }

            var cache = WalkTypeProperties(type);
            return cache.ExplicitKeyProperties;
        }

        internal static PropertyInfo[] KeyPropertiesCache(Type type)
        {
            if (CachedTypes.TryGetValue(type.TypeHandle, out TypePropertyCache properties))
            {
                return properties.KeyProperties;
            }

            var cache = WalkTypeProperties(type);
            return cache.KeyProperties;
        }

        internal static PropertyInfo[] TypePropertiesCache(Type type)
        {
            if (CachedTypes.TryGetValue(type.TypeHandle, out TypePropertyCache properties))
            {
                return properties.TypeProperties;
            }

            var cache = WalkTypeProperties(type);
            return cache.TypeProperties;
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            var attributes = pi.GetCustomAttributes(typeof(WriteAttribute), false).AsList();

            if (attributes.Count != 1)
            {
                return true;
            }

            var writeAttribute = (WriteAttribute)attributes[0];
            return writeAttribute.Write;
        }
    }
}
