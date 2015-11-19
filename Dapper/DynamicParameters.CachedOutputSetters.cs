using System;
using System.Collections;
using System.Collections.Generic;

namespace Dapper
{
    partial class DynamicParameters
    {
        internal static class CachedOutputSetters<T>
        {
#if COREFX
            public static readonly Dictionary<string, Action<object, DynamicParameters>> Cache = new Dictionary<string, Action<object, DynamicParameters>>();
#else
            public static readonly Hashtable Cache = new Hashtable();
#endif
        }
    }
}
