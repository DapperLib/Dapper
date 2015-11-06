using System;
using System.ComponentModel;
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
        /// Not intended for direct usage
        /// </summary>
        [Obsolete("Not intended for direct usage", false)]
#if !DNXCORE50
        [Browsable(false)]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static class TypeHandlerCache<T>
        {
            /// <summary>
            /// Not intended for direct usage
            /// </summary>
            [Obsolete("Not intended for direct usage", true)]
            public static T Parse(object value)
            {
                return (T)handler.Parse(typeof(T), value);
            }

            /// <summary>
            /// Not intended for direct usage
            /// </summary>
            [Obsolete("Not intended for direct usage", true)]
            public static void SetValue(IDbDataParameter parameter, object value)
            {
                handler.SetValue(parameter, value);
            }

            internal static void SetHandler(ITypeHandler handler)
            {
#pragma warning disable 618
                TypeHandlerCache<T>.handler = handler;
#pragma warning restore 618
            }

            private static ITypeHandler handler;
        }
    }
}
