using System;
using System.Data;
using System.Reflection;

#if !NETSTANDARD1_3
namespace Dapper
{
    /// <summary>
    /// Used to pass a DataTable as a TableValuedParameter
    /// </summary>
    internal sealed class TableValuedParameter : SqlMapper.ICustomQueryParameter
    {
        private readonly DataTable table;
        private readonly string typeName;

        /// <summary>
        /// Create a new instance of <see cref="TableValuedParameter"/>.
        /// </summary>
        /// <param name="table">The <see cref="DataTable"/> to create this parameter for</param>
        public TableValuedParameter(DataTable table) : this(table, null) { /* run base */ }

        /// <summary>
        /// Create a new instance of <see cref="TableValuedParameter"/>.
        /// </summary>
        /// <param name="table">The <see cref="DataTable"/> to create this parameter for.</param>
        /// <param name="typeName">The name of the type this parameter is for.</param>
        public TableValuedParameter(DataTable table, string typeName)
        {
            this.table = table;
            this.typeName = typeName;
        }

        private static readonly Action<System.Data.SqlClient.SqlParameter, string> setTypeName;
        static TableValuedParameter()
        {
            var prop = typeof(System.Data.SqlClient.SqlParameter).GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public);
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
            Set(param, table, typeName);
            command.Parameters.Add(param);
        }

        internal static void Set(IDbDataParameter parameter, DataTable table, string typeName)
        {
#pragma warning disable 0618
            parameter.Value = SqlMapper.SanitizeParameterValue(table);
#pragma warning restore 0618
            if (string.IsNullOrEmpty(typeName) && table != null)
            {
                typeName = table.GetTypeName();
            }
            if (!string.IsNullOrEmpty(typeName) && (parameter is System.Data.SqlClient.SqlParameter sqlParam))
            {
                setTypeName?.Invoke(sqlParam, typeName);
                sqlParam.SqlDbType = SqlDbType.Structured;
            }
        }
    }
}
#endif