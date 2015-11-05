﻿using System;
using System.Data;
using System.Reflection;

#if !DNXCORE50
namespace Dapper
{
    /// <summary>
    /// Used to pass a DataTable as a TableValuedParameter
    /// </summary>
    sealed class TableValuedParameter : Dapper.SqlMapper.ICustomQueryParameter
    {
        private readonly DataTable table;
        private readonly string typeName;

        /// <summary>
        /// Create a new instance of TableValuedParameter
        /// </summary>
        public TableValuedParameter(DataTable table) : this(table, null) { }
        /// <summary>
        /// Create a new instance of TableValuedParameter
        /// </summary>
        public TableValuedParameter(DataTable table, string typeName)
        {
            this.table = table;
            this.typeName = typeName;
        }
        static readonly Action<System.Data.SqlClient.SqlParameter, string> setTypeName;
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
            parameter.Value = SqlMapper.SanitizeParameterValue(table);
            if (string.IsNullOrEmpty(typeName) && table != null)
            {
                typeName = SqlMapper.GetTypeName(table);
            }
            if (!string.IsNullOrEmpty(typeName))
            {
                var sqlParam = parameter as System.Data.SqlClient.SqlParameter;
                if (sqlParam != null)
                {
                    if (setTypeName != null) setTypeName(sqlParam, typeName);
                    sqlParam.SqlDbType = SqlDbType.Structured;
                }
            }
        }
    }
}
#endif