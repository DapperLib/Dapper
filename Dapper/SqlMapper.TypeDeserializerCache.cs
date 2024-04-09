using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Dapper
{
    public static partial class SqlMapper
    {
        private class TypeDeserializerCache
        {
            private TypeDeserializerCache(Type type)
            {
                this.type = type;
            }

            private static readonly Hashtable byType = new();
            private readonly Type type;
            internal static void Purge(Type type)
            {
                lock (byType)
                {
                    byType.Remove(type);
                }
            }

            internal static void Purge()
            {
                lock (byType)
                {
                    byType.Clear();
                }
            }

            internal static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
            {
                var found = (TypeDeserializerCache?)byType[type];
                if (found is null)
                {
                    lock (byType)
                    {
                        found = (TypeDeserializerCache?)byType[type];
                        if (found is null)
                        {
                            byType[type] = found = new TypeDeserializerCache(type);
                        }
                    }
                }
                return found.GetReader(reader, startBound, length, returnNullIfFirstMissing);
            }

            private readonly Dictionary<DeserializerKey, Func<DbDataReader, object>> readers = new();

            private readonly struct DeserializerKey : IEquatable<DeserializerKey>
            {
                private readonly int startBound, length;
                private readonly bool returnNullIfFirstMissing;
                private readonly DbDataReader? reader;
                private readonly string[]? names;
                private readonly Type[]? types;
                private readonly int hashCode;

                public DeserializerKey(int hashCode, int startBound, int length, bool returnNullIfFirstMissing, DbDataReader reader, bool copyDown)
                {
                    this.hashCode = hashCode;
                    this.startBound = startBound;
                    this.length = length;
                    this.returnNullIfFirstMissing = returnNullIfFirstMissing;

                    if (copyDown)
                    {
                        this.reader = null;
                        names = new string[length];
                        types = new Type[length];
                        int index = startBound;
                        for (int i = 0; i < length; i++)
                        {
                            names[i] = reader.GetName(index);
                            types[i] = reader.GetFieldType(index++);
                        }
                    }
                    else
                    {
                        this.reader = reader;
                        names = null;
                        types = null;
                    }
                }

                public override int GetHashCode() => hashCode;

                public override string ToString()
                { // only used in the debugger
                    if (names is not null)
                    {
                        return string.Join(", ", names);
                    }
                    if (reader is not null)
                    {
                        var sb = new StringBuilder();
                        int index = startBound;
                        for (int i = 0; i < length; i++)
                        {
                            if (i != 0) sb.Append(", ");
                            sb.Append(reader.GetName(index++));
                        }
                        return sb.ToString();
                    }
                    return base.ToString() ?? "";
                }

                public override bool Equals(object? obj)
                    => obj is DeserializerKey key && Equals(key);

                public bool Equals(DeserializerKey other)
                {
                    if (hashCode != other.hashCode
                        || startBound != other.startBound
                        || length != other.length
                        || returnNullIfFirstMissing != other.returnNullIfFirstMissing)
                    {
                        return false; // clearly different
                    }
                    for (int i = 0; i < length; i++)
                    {
                        if ((names?[i] ?? reader?.GetName(startBound + i)) != (other.names?[i] ?? other.reader?.GetName(startBound + i))
                            ||
                            (types?[i] ?? reader?.GetFieldType(startBound + i)) != (other.types?[i] ?? other.reader?.GetFieldType(startBound + i))
                            )
                        {
                            return false; // different column name or type
                        }
                    }
                    return true;
                }
            }

            private Func<DbDataReader, object> GetReader(DbDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
            {
                if (length < 0) length = reader.FieldCount - startBound;
                int hash = GetColumnHash(reader, startBound, length);
                if (returnNullIfFirstMissing) hash *= -27;
                // get a cheap key first: false means don't copy the values down
                var key = new DeserializerKey(hash, startBound, length, returnNullIfFirstMissing, reader, false);
                Func<DbDataReader, object>? deser;
                lock (readers)
                {
                    if (readers.TryGetValue(key, out deser)) return deser!;
                }
                deser = GetTypeDeserializerImpl(type, reader, startBound, length, returnNullIfFirstMissing);
                // get a more expensive key: true means copy the values down so it can be used as a key later
                key = new DeserializerKey(hash, startBound, length, returnNullIfFirstMissing, reader, true);
                lock (readers)
                {
                    return readers[key] = deser;
                }
            }
        }
    }
}
