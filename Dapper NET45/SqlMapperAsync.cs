using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Execute a query asynchronously using .NET 4.5 Task.  Currently only SqlCommand supports ExecuteReaderAsync so SQL Server is required.
        /// </summary>
        public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var identity = new Identity(sql, commandType, cnn, typeof(T), param == null ? null : param.GetType(), null);
            var info = GetCacheInfo(identity);

            var cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout, commandType);
            if (!(cmd is SqlCommand))
                throw new NotSupportedException("Async support currently only available for SqlCommand.");
            
            var sqlCmd = (SqlCommand)cmd;

            using (var reader = await sqlCmd.ExecuteReaderAsync())
            {
                return ExecuteReader<T>(reader, identity, info).ToList();
            }
        }

        private static IEnumerable<T> ExecuteReader<T>(IDataReader reader, Identity identity, CacheInfo info)
        {
            var tuple = info.Deserializer;
            int hash = GetColumnHash(reader);
            if (tuple.Func == null || tuple.Hash != hash)
            {
                tuple = info.Deserializer = new DeserializerState(hash, GetDeserializer(typeof(T), reader, 0, -1, false));
                SetQueryCache(identity, info);
            }

            var func = tuple.Func;

            while (reader.Read())
            {
                yield return (T)func(reader);
            }
        }
    }
}