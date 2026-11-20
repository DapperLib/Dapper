using System;
using System.Data;
using System.Data.Common;

namespace Dapper
{
    public static partial class SqlMapper
    {
        // Reference type on purpose: this is published into CacheInfo by a plain field write,
        // so it must be a single atomic reference store. A multi-field struct tears, pairing a
        // Hash with the Func compiled for a different result shape. OtherDeserializers lives
        // here rather than in a second CacheInfo field for the same reason - two fields cannot
        // be updated together, so a reader could pair one shape's Func with another's rest-set.
        private sealed class DeserializerState
        {
            public readonly int Hash;
            public readonly Func<DbDataReader, object> Func;
            public readonly Func<DbDataReader, object>[]? OtherDeserializers;

            public DeserializerState(int hash, Func<DbDataReader, object> func, Func<DbDataReader, object>[]? otherDeserializers = null)
            {
                Hash = hash;
                Func = func;
                OtherDeserializers = otherDeserializers;
            }
        }
    }
}
