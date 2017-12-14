using System;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper
{
    internal static class TypeExtensions
    {
        public static string Name(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().Name;
#else
            type.Name;
#endif

        public static bool IsValueType(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().IsValueType;
#else
            type.IsValueType;
#endif

        public static bool IsEnum(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().IsEnum;
#else
            type.IsEnum;
#endif

        public static bool IsGenericType(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().IsGenericType;
#else
            type.IsGenericType;
#endif

        public static bool IsInterface(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().IsInterface;
#else
            type.IsInterface;
#endif

#if NETSTANDARD1_3 || NETCOREAPP1_0
        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(inherit);
        }

        public static TypeCode GetTypeCode(Type type)
        {
            if (type == null) return TypeCode.Empty;
            if (typeCodeLookup.TryGetValue(type, out TypeCode result)) return result;

            if (type.IsEnum())
            {
                type = Enum.GetUnderlyingType(type);
                if (typeCodeLookup.TryGetValue(type, out result)) return result;
            }
            return TypeCode.Object;
        }

        private static readonly Dictionary<Type, TypeCode> typeCodeLookup = new Dictionary<Type, TypeCode>
        {
            [typeof(bool)] = TypeCode.Boolean,
            [typeof(byte)] = TypeCode.Byte,
            [typeof(char)] = TypeCode.Char,
            [typeof(DateTime)] = TypeCode.DateTime,
            [typeof(decimal)] = TypeCode.Decimal,
            [typeof(double)] = TypeCode.Double,
            [typeof(short)] = TypeCode.Int16,
            [typeof(int)] = TypeCode.Int32,
            [typeof(long)] = TypeCode.Int64,
            [typeof(object)] = TypeCode.Object,
            [typeof(sbyte)] = TypeCode.SByte,
            [typeof(float)] = TypeCode.Single,
            [typeof(string)] = TypeCode.String,
            [typeof(ushort)] = TypeCode.UInt16,
            [typeof(uint)] = TypeCode.UInt32,
            [typeof(ulong)] = TypeCode.UInt64,
        };
#else
        public static TypeCode GetTypeCode(Type type) => Type.GetTypeCode(type);
#endif

        public static MethodInfo GetPublicInstanceMethod(this Type type, string name, Type[] types)
        {
#if NETSTANDARD1_3 || NETCOREAPP1_0
            var method = type.GetMethod(name, types);
            return (method?.IsPublic == true && !method.IsStatic) ? method : null;
#else
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, types, null);
#endif
        }
    }
}
