using System;
using System.Collections;
using System.Collections.Generic;

namespace Dapper
{
    partial class DynamicParameters
    {
        internal static class CachedOutputSetters<T>
        {
            public static readonly Hashtable Cache = new Hashtable();
        }
    }
}
