#if COREFX
using IDbCommand = System.Data.Common.DbCommand;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace Fix
{
    public interface IDbHook
    {
        T CommandExecute<T>(IDbCommand command, Func<IDbCommand, T> executeDelegate);
    }
}
