using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Dapper
{
    internal static class TypeExtensions
    {
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "All callers use framework types whose public methods are preserved.")]
        public static MethodInfo? GetPublicInstanceMethod(this Type type, string name, Type[] types)
            => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, types, null);
    }
}
