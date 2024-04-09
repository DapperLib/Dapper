using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Parses a data reader to a sequence of data of the supplied type. Used for deserializing a reader without a connection, etc.
        /// </summary>
        /// <typeparam name="T">The type to parse from the <paramref name="reader"/>.</typeparam>
        /// <param name="reader">The data reader to parse results from.</param>
        public static IEnumerable<T> Parse<T>(this IDataReader reader)
        {
            var dbReader = GetDbDataReader(reader);
            if (dbReader.Read())
            {
                var effectiveType = typeof(T);
                var deser = GetDeserializer(effectiveType, dbReader, 0, -1, false);
                var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                do
                {
                    object val = deser(dbReader);
                    if (val is null || val is T)
                    {
                        yield return (T)val!;
                    }
                    else
                    {
                        yield return (T)Convert.ChangeType(val, convertToType, System.Globalization.CultureInfo.InvariantCulture);
                    }
                } while (dbReader.Read());
            }
        }

        /// <summary>
        /// Parses a data reader to a sequence of data of the supplied type (as object). Used for deserializing a reader without a connection, etc.
        /// </summary>
        /// <param name="reader">The data reader to parse results from.</param>
        /// <param name="type">The type to parse from the <paramref name="reader"/>.</param>
        public static IEnumerable<object> Parse(this IDataReader reader, Type type)
        {
            var dbReader = GetDbDataReader(reader);
            if (dbReader.Read())
            {
                var deser = GetDeserializer(type, dbReader, 0, -1, false);
                do
                {
                    yield return deser(dbReader);
                } while (dbReader.Read());
            }
        }

        /// <summary>
        /// Parses a data reader to a sequence of dynamic. Used for deserializing a reader without a connection, etc.
        /// </summary>
        /// <param name="reader">The data reader to parse results from.</param>
        public static IEnumerable<dynamic> Parse(this IDataReader reader)
        {
            var dbReader = GetDbDataReader(reader);
            if (dbReader.Read())
            {
                var deser = GetDapperRowDeserializer(dbReader, 0, -1, false);
                do
                {
                    yield return deser(dbReader);
                } while (dbReader.Read());
            }
        }

        /// <summary>
        /// Gets the row parser for a specific row on a data reader. This allows for type switching every row based on, for example, a TypeId column.
        /// You could return a collection of the base type but have each more specific.
        /// </summary>
        /// <param name="reader">The data reader to get the parser for the current row from</param>
        /// <param name="type">The type to get the parser for</param>
        /// <param name="startIndex">The start column index of the object (default 0)</param>
        /// <param name="length">The length of columns to read (default -1 = all fields following startIndex)</param>
        /// <param name="returnNullIfFirstMissing">Return null if we can't find the first column? (default false)</param>
        /// <returns>A parser for this specific object from this row.</returns>
#if DEBUG // make sure we're not using this internally
        [Obsolete(nameof(DbDataReader) + " API should be preferred")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Grandfathered")]
        public static Func<IDataReader, object> GetRowParser(this IDataReader reader, Type type,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            return WrapObjectReader(GetDeserializer(type, GetDbDataReader(reader), startIndex, length, returnNullIfFirstMissing));
        }

        /// <summary>
        /// Gets the row parser for a specific row on a data reader. This allows for type switching every row based on, for example, a TypeId column.
        /// You could return a collection of the base type but have each more specific.
        /// </summary>
        /// <param name="reader">The data reader to get the parser for the current row from</param>
        /// <param name="type">The type to get the parser for</param>
        /// <param name="startIndex">The start column index of the object (default 0)</param>
        /// <param name="length">The length of columns to read (default -1 = all fields following startIndex)</param>
        /// <param name="returnNullIfFirstMissing">Return null if we can't find the first column? (default false)</param>
        /// <returns>A parser for this specific object from this row.</returns>
        public static Func<DbDataReader, object> GetRowParser(this DbDataReader reader, Type type,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            return GetDeserializer(type, reader, startIndex, length, returnNullIfFirstMissing);
        }

        /// <inheritdoc cref="GetRowParser{T}(DbDataReader, Type, int, int, bool)"/>
#if DEBUG // make sure we're not using this internally
        [Obsolete(nameof(DbDataReader) + " API should be preferred")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Grandfathered")]
        public static Func<IDataReader, T> GetRowParser<T>(this IDataReader reader, Type? concreteType = null,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            concreteType ??= typeof(T);
            var func = GetDeserializer(concreteType, GetDbDataReader(reader), startIndex, length, returnNullIfFirstMissing);
            return Wrap(func);

            // this is just to be very clear about what we're capturing
            static Func<IDataReader, T> Wrap(Func<DbDataReader, object> func)
                => reader => (T)func(GetDbDataReader(reader));
        }

        /// <summary>
        /// Gets the row parser for a specific row on a data reader. This allows for type switching every row based on, for example, a TypeId column.
        /// You could return a collection of the base type but have each more specific.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="reader">The data reader to get the parser for the current row from.</param>
        /// <param name="concreteType">The type to get the parser for.</param>
        /// <param name="startIndex">The start column index of the object (default: 0).</param>
        /// <param name="length">The length of columns to read (default: -1 = all fields following startIndex).</param>
        /// <param name="returnNullIfFirstMissing">Return null if we can't find the first column? (default: false).</param>
        /// <returns>A parser for this specific object from this row.</returns>
        /// <example>
        /// var result = new List&lt;BaseType&gt;();
        /// using (var reader = connection.ExecuteReader(@"
        ///   select 'abc' as Name, 1 as Type, 3.0 as Value
        ///   union all
        ///   select 'def' as Name, 2 as Type, 4.0 as Value"))
        /// {
        ///     if (reader.Read())
        ///     {
        ///         var toFoo = reader.GetRowParser&lt;BaseType&gt;(typeof(Foo));
        ///         var toBar = reader.GetRowParser&lt;BaseType&gt;(typeof(Bar));
        ///         var col = reader.GetOrdinal("Type");
        ///         do
        ///         {
        ///             switch (reader.GetInt32(col))
        ///             {
        ///                 case 1:
        ///                     result.Add(toFoo(reader));
        ///                     break;
        ///                 case 2:
        ///                     result.Add(toBar(reader));
        ///                     break;
        ///             }
        ///         } while (reader.Read());
        ///     }
        /// }
        ///  
        /// abstract class BaseType
        /// {
        ///     public abstract int Type { get; }
        /// }
        /// class Foo : BaseType
        /// {
        ///     public string Name { get; set; }
        ///     public override int Type =&gt; 1;
        /// }
        /// class Bar : BaseType
        /// {
        ///     public float Value { get; set; }
        ///     public override int Type =&gt; 2;
        /// }
        /// </example>
        public static Func<DbDataReader, T> GetRowParser<T>(this DbDataReader reader, Type? concreteType = null,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {
            concreteType ??= typeof(T);
            var func = GetDeserializer(concreteType, reader, startIndex, length, returnNullIfFirstMissing);
            if (concreteType.IsValueType)
            {
                return _ => (T)func(_);
            }
            else
            {
                return (Func<DbDataReader, T>)(Delegate)func;
            }
        }
    }
}
