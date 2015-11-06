using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Microsoft.SqlServer.Server;

#if !DNXCORE50
namespace Dapper
{
    /// <summary>
    /// Used to pass a IEnumerable&lt;SqlDataRecord&gt; as a SqlDataRecordListTVPParameter
    /// </summary>
    internal sealed class SqlDataRecordListTVPParameter : SqlMapper.ICustomQueryParameter
    {
        private readonly IEnumerable<SqlDataRecord> data;
        private readonly string typeName;

        /// <summary>
        /// Create a new instance of SqlDataRecordListTVPParameter
        /// </summary>
        public SqlDataRecordListTVPParameter(IEnumerable<SqlDataRecord> data, string typeName)
        {
            this.data = data;
            this.typeName = typeName;
        }

        private static readonly Action<SqlParameter, string> setTypeName;

        static SqlDataRecordListTVPParameter()
        {
            var prop = typeof(SqlParameter).GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
            {
                setTypeName = (Action<SqlParameter, string>)
                              Delegate.CreateDelegate(typeof(Action<SqlParameter, string>), prop.GetSetMethod());
            }
        }

        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            Set(param, data, typeName);
            command.Parameters.Add(param);
        }

        internal static void Set(IDbDataParameter parameter, IEnumerable<SqlDataRecord> data, string typeName)
        {
            parameter.Value = (object)data ?? DBNull.Value;
            var sqlParam = parameter as SqlParameter;
            if (sqlParam != null)
            {
                sqlParam.SqlDbType = SqlDbType.Structured;
                sqlParam.TypeName = typeName;
            }
        }
    }
}
#endif
