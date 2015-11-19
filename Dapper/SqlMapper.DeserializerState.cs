using System;
using System.Data;

#if DOTNET5_2
using IDataReader = System.Data.Common.DbDataReader;
#endif

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
