using System;
using System.ComponentModel;
using System.Data;

#if DOTNET5_2
using IDbDataParameter = System.Data.Common.DbParameter;
#endif

namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Not intended for direct usage
        /// </summary>
        [Obsolete("Not intended for direct usage", false)]
#if !DOTNET5_2
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
