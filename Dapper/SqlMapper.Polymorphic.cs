using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// function which can produce deserializer for a given type
        /// </summary>
        public delegate Func<IDataReader, object> DeserializerProvider(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing);

        /// <summary>
        /// Registers a deserializer provider for Dapper
        /// </summary>
        /// <param name="type">The type to deserialize</param>
        /// <param name="provider">A delegate which will produce the deserializer</param>
        public static void RegisterDeserializer(Type type, DeserializerProvider provider)
        {
            customDeserializers.Add(type, provider);
        }

        /// <summary>
        /// Adds support for loading the base type and its subclasses. 
        /// </summary>
        /// <typeparam name="TBaseType">The common parent type</typeparam>
        /// <typeparam name="TDiscriminator">The type of the discriminator column</typeparam>
        /// <param name="discriminatorColumnName">The name of the discriminator column</param>
        /// <param name="typeFunc">Returns the subclass type based on the value of the discriminant</param>
        public static void RegisterPolymorphicLoader<TBaseType, TDiscriminator>(string discriminatorColumnName, Func<TDiscriminator, Type> typeFunc)
        {
            var loader = new PolymorphicLoader<TBaseType, TDiscriminator>(discriminatorColumnName, typeFunc);
            RegisterDeserializer(typeof(TBaseType), loader.GetDeserializer);
        }

        private class PolymorphicLoader<TBaseType, TDiscriminator>
        {
            private readonly Func<TDiscriminator, Type> _typeTest;
            private readonly string _column;

            /// <summary>
            /// Creates a polymorphic loader which can load base type and all sub types. 
            /// </summary>
            /// <param name="discriminatorColumn">The name of the column which will be used discriminate between types</param>
            /// <param name="typeTest">mapping from discriminant to the concrete type to deserialize</param>
            public PolymorphicLoader(string discriminatorColumn, Func<TDiscriminator, Type> typeTest)
            {
                _typeTest = typeTest;
                _column = discriminatorColumn;
            }

            /// <summary>
            /// returns a polymorphic deserializer for the type.
            /// </summary>
            internal Func<IDataReader, object> GetDeserializer(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
            {
                if (type != typeof(TBaseType))
                    throw new ArgumentException($"Cannot deserialize type {type.Name}", nameof(type));

                if (length == -1)
                    length = reader.FieldCount - startBound;

                return r =>
                {
                    int idx = r.GetOrdinal(_column);
                    object discriminant = null; ;
                    // make sure GetOrdinal returns a column in the bounds
                    if (idx < startBound)
                    {
                        for (int i = startBound; i < startBound + length; i++)
                        {
                            string name = r.GetName(i);
                            if (_column.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                discriminant = r.GetValue(i);
                                break;
                            }
                        }
                    }
                    else
                    {
                        discriminant = r.GetValue(idx);
                    }

                    if (discriminant == DBNull.Value)
                        return default(TBaseType);

                    Type childType = _typeTest((TDiscriminator)discriminant);
                    if (childType == null)
                        throw new InvalidOperationException($"cannot find deserializer for {typeof(TBaseType).Name}, val: {discriminant}");

                    var deserializer = GetTypeDeserializer(childType, reader, startBound, length, returnNullIfFirstMissing);
                    return deserializer(reader);
                };
            }
        }
    }
}
