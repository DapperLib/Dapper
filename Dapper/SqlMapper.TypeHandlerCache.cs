using System;
using System.ComponentModel;
using System.Data;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Not intended for direct usage
        /// </summary>
        /// <typeparam name="T">The type to have a cache for.</typeparam>
        [Obsolete(ObsoleteInternalUsageOnly, false)]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static class TypeHandlerCache<T>
        {
            /// <summary>
            /// Not intended for direct usage.
            /// </summary>
            /// <param name="value">The object to parse.</param>
            [Obsolete(ObsoleteInternalUsageOnly, true)]
            public static T? Parse(object value) => (T?)handler.Parse(typeof(T), value);

            /// <summary>
            /// Not intended for direct usage.
            /// </summary>
            /// <param name="parameter">The parameter to set a value for.</param>
            /// <param name="value">The value to set.</param>
            [Obsolete(ObsoleteInternalUsageOnly, true)]
            public static void SetValue(IDbDataParameter parameter, object value) => handler.SetValue(parameter, value);

            internal static void SetHandler(ITypeHandler handler)
            {
                TypeHandlerCache<T>.handler = handler;
            }

            private static ITypeHandler handler = null!;
        }
    }
}
