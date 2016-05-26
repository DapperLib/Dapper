using System;
using System.Data;

namespace Dapper
{
    partial class SqlMapper
    {
        struct DeserializerState
        {
            public readonly int Hash;
            public readonly Func<IDataReader, object> Func;

            public DeserializerState(int hash, Func<IDataReader, object> func)
            {
                Hash = hash;
                Func = func;
            }
        }
    }
}
