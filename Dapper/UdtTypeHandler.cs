using System;
using System.Data;

namespace Dapper
{
    partial class SqlMapper
    {
#if !COREFX
        /// <summary>
        /// A type handler for data-types that are supported by the underlying provider, but which need
        /// a well-known UdtTypeName to be specified
        /// </summary>
        public class UdtTypeHandler : ITypeHandler
        {
            private readonly string udtTypeName;
            /// <summary>
            /// Creates a new instance of UdtTypeHandler with the specified UdtTypeName
            /// </summary>
            public UdtTypeHandler(string udtTypeName)
            {
                if (string.IsNullOrEmpty(udtTypeName)) throw new ArgumentException("Cannot be null or empty", udtTypeName);
                this.udtTypeName = udtTypeName;
            }
            object ITypeHandler.Parse(Type destinationType, object value)
            {
                return value is DBNull ? null : value;
            }

            void ITypeHandler.SetValue(IDbDataParameter parameter, object value)
            {
#pragma warning disable 0618
                parameter.Value = SanitizeParameterValue(value);
#pragma warning restore 0618
                if (parameter is System.Data.SqlClient.SqlParameter && !(value is DBNull))
                {
                    ((System.Data.SqlClient.SqlParameter)parameter).SqlDbType = SqlDbType.Udt;
                    ((System.Data.SqlClient.SqlParameter)parameter).UdtTypeName = udtTypeName;
                }
            }
        }
#endif
    }
}
