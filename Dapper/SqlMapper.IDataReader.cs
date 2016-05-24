using System;
using System.Collections.Generic;
using System.Data;

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
        public static Func<IDataReader, T> GetRowParser<T>(this IDataReader reader, Type concreteType = null,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            if (concreteType == null) concreteType = typeof(T);
            var func = GetDeserializer(concreteType, reader, startIndex, length, returnNullIfFirstMissing);
            if (concreteType.IsValueType())
            {
                return _ => (T)func(_);
            }
            else
            {
                return (Func<IDataReader, T>)(Delegate)func;
            }
        }
    }
}
