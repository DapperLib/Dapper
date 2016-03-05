using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
#if !COREFX
namespace Dapper
{
    /// <summary>
    /// Used to pass a IEnumerable&lt;SqlDataRecord&gt; as a SqlDataRecordListTVPParameter
    /// </summary>
    sealed class SqlDataRecordListTVPParameter : SqlMapper.ICustomQueryParameter
    {
        private readonly IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord> data;
        private readonly string typeName;
        /// <summary>
        /// Create a new instance of SqlDataRecordListTVPParameter
        /// </summary>
        public SqlDataRecordListTVPParameter(IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord> data, string typeName)
        {
            this.data = data;
            this.typeName = typeName;
        }
        static readonly Action<System.Data.SqlClient.SqlParameter, string> setTypeName;
        static SqlDataRecordListTVPParameter()
        {
            var prop = typeof(System.Data.SqlClient.SqlParameter).GetProperty(nameof(System.Data.SqlClient.SqlParameter.TypeName), BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
            {
                setTypeName = (Action<System.Data.SqlClient.SqlParameter, string>)
                    Delegate.CreateDelegate(typeof(Action<System.Data.SqlClient.SqlParameter, string>), prop.GetSetMethod());
            }
        }
        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            Set(param, data, typeName);
            command.Parameters.Add(param);
        }
        internal static void Set(IDbDataParameter parameter, IEnumerable<Microsoft.SqlServer.Server.SqlDataRecord> data, string typeName)
        {
            parameter.Value = (object)data ?? DBNull.Value;
            var sqlParam = parameter as System.Data.SqlClient.SqlParameter;
            if (sqlParam != null)
            {
                sqlParam.SqlDbType = SqlDbType.Structured;
                sqlParam.TypeName = typeName;
            }
        }
    }
}
#endif