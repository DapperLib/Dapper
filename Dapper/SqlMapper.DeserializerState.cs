using System;
using System.Data;
using System.Data.Common;

namespace Dapper
{
    public static partial class SqlMapper
    {
        private readonly struct DeserializerState
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
