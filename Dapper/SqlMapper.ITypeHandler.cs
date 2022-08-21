using System;
using System.Data;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Implement this interface to perform custom type-based parameter handling and value parsing
        /// </summary>
        public interface ITypeHandler
        {
            /// <summary>
            /// Assign the value of a parameter before a command executes
            /// </summary>
            /// <param name="parameter">The parameter to configure</param>
            /// <param name="value">Parameter value</param>
            void SetValue(IDbDataParameter parameter, object value);

            /// <summary>
            /// Parse a database value back to a typed value
            /// </summary>
            /// <param name="value">The value from the database</param>
            /// <param name="destinationType">The type to parse to</param>
            /// <returns>The typed value</returns>
            object Parse(Type destinationType, object value);
        }

        /// <summary>
        /// Implement this interface along side ITypeHandler if you want nulls to be passed to your custom value parsing
        /// </summary>
        public interface INullTypeHandler
        {
            /// <summary>
            /// Parse a database back to a typed value (or null if not required)
            /// </summary>
            /// <param name="destinationType">The type to parse to</param>
            /// <returns>The typed value</returns>
            object ParseNull(Type destinationType);
        }
    }
}
