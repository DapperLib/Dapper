using System.Collections;

namespace Dapper
{
    partial class DynamicParameters
    {
        // The type here is used to differentiate the cache by type via generics
        // ReSharper disable once UnusedTypeParameter
        internal static class CachedOutputSetters<T>
        {
            // Intentional, abusing generics to get our cache splits
            // ReSharper disable once StaticMemberInGenericType
            public static readonly Hashtable Cache = new Hashtable();
        }
    }
}
