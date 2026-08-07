using System;
using System.Data;
using System.Data.Common;

namespace Dapper
{
    public static partial class SqlMapper
    {
        // Reference type on purpose: this is published into CacheInfo by a plain field write,
        // so it must be a single atomic reference store. A multi-field struct tears, pairing a
        // Hash with the Func compiled for a different result shape.
        private sealed class DeserializerState
        {
            public readonly int Hash;
            public readonly Func<DbDataReader, object> Func;

            public DeserializerState(int hash, Func<DbDataReader, object> func)
            {
                Hash = hash;
                Func = func;
            }
        }
    }
}
