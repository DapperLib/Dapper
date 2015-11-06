using System.Data;

#if DNXCORE50
using IDbDataParameter = global::System.Data.Common.DbParameter;
using IDataParameter = global::System.Data.Common.DbParameter;
using IDbTransaction = global::System.Data.Common.DbTransaction;
using IDbConnection = global::System.Data.Common.DbConnection;
using IDbCommand = global::System.Data.Common.DbCommand;
using IDataReader = global::System.Data.Common.DbDataReader;
using IDataRecord = global::System.Data.Common.DbDataReader;
using IDataParameterCollection = global::System.Data.Common.DbParameterCollection;
using DataException = global::System.InvalidOperationException;
using ApplicationException = global::System.InvalidOperationException;
#endif

namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Implement this interface to pass an arbitrary db specific parameter to Dapper
        /// </summary>
        public interface ICustomQueryParameter
        {
            /// <summary>
            /// Add the parameter needed to the command before it executes
            /// </summary>
            /// <param name="command">The raw command prior to execution</param>
            /// <param name="name">Parameter name</param>
            void AddParameter(IDbCommand command, string name);
        }
    }
}
