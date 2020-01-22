using System.Data;

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
            if (!string.IsNullOrEmpty(typeName)) StructuredHelper.ConfigureTVP(parameter, typeName);
        }
    }
}
