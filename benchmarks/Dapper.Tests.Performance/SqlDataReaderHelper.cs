using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace Dapper.Tests.Performance
{
    public static class SqlDataReaderHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetNullableString(this SqlDataReader reader, int index)
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (string)tmp;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? GetNullableValue<T>(this SqlDataReader reader, int index) where T : struct
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (T)tmp;
            }
            return null;
        }
    }
}
