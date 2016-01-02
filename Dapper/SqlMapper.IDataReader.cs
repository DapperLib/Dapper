using System;
using System.Collections.Generic;
using System.Data;

#if COREFX
using IDataReader = System.Data.Common.DbDataReader;
#endif

namespace Dapper
{
    partial class SqlMapper
    {
        public static IEnumerable<T> Parse<T>(this IDataReader reader)
        {
            if(reader.Read())
            {
                var deser = GetDeserializer(typeof(T), reader, 0, -1, false);
                do
                {
                    yield return (T)deser(reader);
                } while (reader.Read());
            }
        }
        public static IEnumerable<object> Parse(this IDataReader reader, Type type)
        {
            if (reader.Read())
            {
                var deser = GetDeserializer(type, reader, 0, -1, false);
                do
                {
                    yield return deser(reader);
                } while (reader.Read());
            }
        }
        public static IEnumerable<dynamic> Parse(this IDataReader reader)
        {
            if (reader.Read())
            {
                var deser = GetDapperRowDeserializer(reader, 0, -1, false);
                do
                {
                    yield return deser(reader);
                } while (reader.Read());
            }
        }

        public static Func<IDataReader, object> GetRowParser(this IDataReader reader, Type type,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            return GetDeserializer(type, reader, startIndex, length, returnNullIfFirstMissing);
        }
    }
}
