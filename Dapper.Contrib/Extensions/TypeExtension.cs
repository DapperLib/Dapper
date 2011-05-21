using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dapper.Contrib
{
    public static class TypeExtension
    {
        public static Boolean IsAnonymousType(this Type type)
        {
            if (type == null) return false;

            var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Count() > 0;
            var nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            var isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

            return isAnonymousType;
        }
    }
}