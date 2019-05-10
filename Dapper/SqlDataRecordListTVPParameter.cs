using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
    /// <summary>
    /// Used to pass a IEnumerable&lt;SqlDataRecord&gt; as a SqlDataRecordListTVPParameter
    /// </summary>
    internal sealed class SqlDataRecordListTVPParameter<T> : SqlMapper.ICustomQueryParameter
#if !NETSTANDARD1_3
        where T : IDataRecord
#endif
    {
        private readonly IEnumerable<T> data;
        private readonly string typeName;
        /// <summary>
        /// Create a new instance of <see cref="SqlDataRecordListTVPParameter&lt;T&gt;"/>.
        /// </summary>
        /// <param name="data">The data records to convert into TVPs.</param>
        /// <param name="typeName">The parameter type name.</param>
        public SqlDataRecordListTVPParameter(IEnumerable<T> data, string typeName)
        {
            this.data = data;
            this.typeName = typeName;
        }

        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            Set(param, data, typeName);
            command.Parameters.Add(param);
        }

        internal static void Set(IDbDataParameter parameter, IEnumerable<T> data, string typeName)
        {
            parameter.Value = data != null && data.Any() ? data : null;
            var type = parameter.GetType();
            if (type.Name == "SqlParameter")
            {
                var prop = type.GetProperty("SqlDbType");
                prop?.SetValue(parameter, SqlDbType.Structured);

                prop = type.GetProperty("TypeName");
                prop?.SetValue(parameter, typeName);
            }
        }
    }
}
