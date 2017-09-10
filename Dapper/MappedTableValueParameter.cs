using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.SqlServer.Server;

namespace Dapper
{
    /// <summary>
    /// Used to map <see cref="IEnumerable{T}"/> of <typeparamref name="T"/> and pass it
    /// as a table-value parameter.
    /// </summary>
    /// <example>
    /// <code>
    /// var parameters = new DynamicParameters();
    /// parameters.Add("Items", SqlMapper.MapTableValuedParameter(items));
    /// 
    /// using (connection)
    /// {
    ///     connection.Execute("sp_InsertTable", parameters, commandType: CommandType.StoredProcedure);
    /// }
    /// </code>
    /// </example>
    internal sealed class MappedTableValueParameter<T> : SqlMapper.ICustomQueryParameter
    {
        private class CacheInfo
        {
            public SqlMetaData[] Metadata;
            public SqlMapper.ITypeHandler[] Handlers;
            public Func<T, object[]> GetValues;
        }

        private readonly IEnumerable<T> items;
        private readonly PropertyInfo[] properties;

        /// <summary>
        /// Construct istance using list of <paramref name="properties"/> to be mapped and
        /// <paramref name="items"/> passed to the table-value parameter.
        /// </summary>
        /// <param name="properties">Properties to be mapped.</param>
        /// <param name="items">Items passed in the table-value parameter.</param>
        public MappedTableValueParameter(IEnumerable<PropertyInfo> properties, IEnumerable<T> items)
        {
            this.properties = properties?.ToArray() ?? throw new ArgumentNullException(nameof(properties));
            this.items = items;
        }

        /// <summary>
        /// Construct instance using <paramref name="items"/> for the table-value parameter.
        /// </summary>
        public MappedTableValueParameter(IEnumerable<T> items)
            : this(typeof(T).GetProperties(), items)
        {

        }

        private static SqlDbType[] _sizedTypes;
        private static readonly ConcurrentDictionary<int, CacheInfo> _cache;

        static MappedTableValueParameter()
        {
            _sizedTypes = new[]
            {
                SqlDbType.Binary,
                SqlDbType.Char,
                SqlDbType.NChar,
                SqlDbType.NVarChar,
                SqlDbType.VarBinary,
                SqlDbType.VarChar
            };

            _cache = new ConcurrentDictionary<int, CacheInfo>();
        }

        private int GetPropsHash()
        {
            unchecked
            {
                var hash = properties.Length;

                for (var index = 0; index < properties.Length; index++)
                {
                    var property = properties[index];
                    hash -= 79 * (hash * 31 + property.Name.GetHashCode());
                    hash += property.PropertyType.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Build internal mapper and wrap it into cache record.
        /// </summary>
        /// <param name="hash">
        /// Unique cash record hash.
        /// </param>
        /// <returns>
        /// Returns <see cref="CacheInfo"/> item with mapper stored in it.
        /// </returns>
        private CacheInfo CreateCacheInfo(int hash)
        {
            var length = properties.Length;
            var result = new CacheInfo()
            {
                Handlers = new SqlMapper.ITypeHandler[length],
                Metadata = new SqlMetaData[length]
            };

            var converter = new SqlParameter();
            var sample = items.FirstOrDefault();
            var typeProperties = typeof(T).GetProperties().ToArray();
            var param = Expression.Parameter(typeof(T));
            var getters = new Expression[length];
            for (var index = 0; index < properties.Length; index++)
            {
                var property = properties[index];

#if !NETSTANDARD1_3
                var exists = typeProperties.Any(item => item.MetadataToken == property.MetadataToken);
                exists = typeProperties.Any(item => item.MetadataToken == property.MetadataToken);
                if (!exists)
                {
                    throw new InvalidOperationException($"Property [{property.Name}] does not belongs to type [{typeof(T).FullName}]");
                }
#endif

#pragma warning disable CS0618
                // TODO: For some strange reason LookupDbType method is "for internal use only".
                // As well as the whole set of table-value implementations like SqlDataRecordListTVPParameter
                // and TableValuedParameter. If this is a kind of black magic that can't be reused,
                // this class can be also made "internal sealed". But probably type mapping for
                // "Dapper - a simple object mapper" should not be hidden and TVP-related stuff
                // can be also a point of extension and reusability.
                converter.DbType = SqlMapper.LookupDbType(property.PropertyType, "", true, out result.Handlers[index]);
#pragma warning restore CS0618

                if (result.Handlers[index] != null)
                {
                    result.Handlers[index].SetValue(converter, property.GetValue(sample));
                }

                result.Metadata[index] = _sizedTypes.Contains(converter.SqlDbType)
                    ? new SqlMetaData(property.Name, converter.SqlDbType, -1)
                    : new SqlMetaData(property.Name, converter.SqlDbType)
                ;

                getters[index] = Expression.Convert(
                    Expression.Property(param, property),
                    typeof(object)
                );
            }

            // Create and compile expression that convert T instance into object[] that later
            // should be passed to SqlDataRecord for table-value parameter. It looks like
            // instance => new object[] { (object)instance.PropOne, (object)instance.PropTwo, ... }
            result.GetValues = Expression.Lambda<Func<T, object[]>>(
                Expression.NewArrayInit(typeof(object), getters),
                param
            ).Compile();

            return result;
        }

        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            var param = command.CreateParameter() as SqlParameter;
            if (param == null)
            {
                throw new NotSupportedException($"Table-Valued Parameters are supported by {nameof(SqlConnection)} only");
            }

            param.ParameterName = name;
            param.SqlDbType = SqlDbType.Structured;
            command.Parameters.Add(param);

            if (items == null) return;

            var hash = GetPropsHash();
            var cache = _cache.GetOrAdd(hash, CreateCacheInfo);

            var result = new List<SqlDataRecord>();
            var handler = new SqlParameter();
            foreach (var item in items)
            {
                var record = new SqlDataRecord(cache.Metadata);

                var values = cache.GetValues(item);
                for (var index = 0; index < values.Length; index++)
                {
                    if (cache.Handlers[index] == null) continue;

                    handler.Value = DBNull.Value;

                    cache.Handlers[index].SetValue(handler, values[index]);
                    values[index] = handler.Value;
                }

                record.SetValues(values);
                result.Add(record);
            }

            param.Value = result.ToArray();
        }
    }
}

