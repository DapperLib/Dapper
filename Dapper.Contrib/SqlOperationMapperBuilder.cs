using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Dapper.Contrib.Extensions;

namespace Dapper.Contrib
{
    static class SqlOperationMapperBuilder
    {
        private static AssemblyBuilder assemblyBuilder = null;
        private static ModuleBuilder moduleBuilder = null;
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, Type> mapperTypeCache = new ConcurrentDictionary<RuntimeTypeHandle, Type>();

        private static AssemblyBuilder GetAssemblyBuilder()
        {
            AssemblyName assemblyName = new AssemblyName("Dapper.Contrib.MyMapper");
            if (assemblyBuilder == null)
            {
#if NETSTANDARD1_3 || NETSTANDARD2_0
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#else
                assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
            }
            return assemblyBuilder;
        }

        private static ModuleBuilder GetModuleBuilder(AssemblyBuilder assemblyBuilder)
        {
            if (moduleBuilder == null)
            {
                moduleBuilder = assemblyBuilder.DefineDynamicModule("SqlOperationMapperProvider" + Guid.NewGuid().ToString());
            }
            return moduleBuilder;
        }

        private static TypeBuilder GetTypeBuilder(Type type, ModuleBuilder moduleBuilder)
        {
            var implementTypeName = $"{type.Namespace}.{type.Name.TrimStart('I')}";
            var typeBuilder = moduleBuilder.DefineType(implementTypeName, TypeAttributes.Public, typeof(object), new Type[] { type, typeof(ISqlOperationMapper) });

            return typeBuilder;
        }

        private static FieldBuilder DefineFieldConnection(TypeBuilder typeBuilder)
        {
            return typeBuilder.DefineField("connection", typeof(IDbConnection), FieldAttributes.Private);
        }

        private static void DefineConstructor(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
        {
            // define constructor
            Type[] constructorArgs = { typeof(IDbConnection) };
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorArgs);
            var ilGen = constructorBuilder.GetILGenerator();
            Type objType = typeof(object);
            ConstructorInfo objCtor = objType.GetConstructor(Type.EmptyTypes);
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Call, objCtor);
            //ilGen.Emit(OpCodes.Nop);
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, fieldBuilder);
            ilGen.Emit(OpCodes.Ret);
        }

        private static void DefineExcuteSqlMethod(TypeBuilder typeBuilder, FieldBuilder fieldBuilder, MethodInfo methodInfo, ExcuteSqlAttribute attr)
        {
            var parameters = methodInfo.GetParameters();
            var paramTypes = GetParameterTypes(parameters);
            ValidateParameterTypes(paramTypes, methodInfo.Name);
            ValidateExcuteReturnParameterType(methodInfo.ReturnType, methodInfo.Name);

            var sql = attr.SqlFormat;
            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot, methodInfo.ReturnType, paramTypes);
            var ilGen = methodBuilder.GetILGenerator();

            ilGen.DeclareLocal(typeof(string));
            ilGen.DeclareLocal(typeof(int?));
            ilGen.DeclareLocal(typeof(CommandType?));


            ilGen.Emit(OpCodes.Ldstr, sql);
            ilGen.Emit(OpCodes.Stloc_0);

            //this
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, fieldBuilder);

            //sql string
            ilGen.Emit(OpCodes.Ldloc_0);

            //object
#if NETSTANDARD1_3
            if (paramTypes.Length > 0 && paramTypes.All(p => p.GetTypeInfo().IsValueType || p == typeof(string)))
#else
            if (paramTypes.Length > 0 && paramTypes.All(p => p.IsValueType || p == typeof(string)))
#endif
            {
                var dicType = typeof(Dictionary<string, object>);
                ilGen.DeclareLocal(dicType);
                ilGen.Emit(OpCodes.Newobj, dicType.GetConstructor(Type.EmptyTypes));
                ilGen.Emit(OpCodes.Stloc_3);
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    ilGen.Emit(OpCodes.Ldloc_3);
                    ilGen.Emit(OpCodes.Ldstr, parameters[i].Name);
#if NETSTANDARD1_3
                    if (paramTypes[i].GetTypeInfo().IsValueType)
#else
                    if (paramTypes[i].IsValueType)
#endif
                    {
                        ilGen.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                        ilGen.Emit(OpCodes.Box, paramTypes[i]);
                    }
                    else
                    {
                        ilGen.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                    }
                    ilGen.Emit(OpCodes.Callvirt, dicType.GetMethod("Add", new[] { typeof(string), typeof(object) }));
                }
                ilGen.Emit(OpCodes.Ldloc_3);
            }
#if NETSTANDARD1_3
            else if (paramTypes.Length == 1 && paramTypes[0].GetTypeInfo().IsClass)
#else
            else if (paramTypes.Length == 1 && paramTypes[0].IsClass)
#endif
            {
                ilGen.Emit(OpCodes.Ldarg_1);
            }
            else
            {
                ilGen.Emit(OpCodes.Ldnull);
            }

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldloca_S, (byte)1);
            ilGen.Emit(OpCodes.Initobj, typeof(int?));
            ilGen.Emit(OpCodes.Ldloc_1);
            ilGen.Emit(OpCodes.Ldloca_S, (byte)2);
            ilGen.Emit(OpCodes.Initobj, typeof(CommandType?));
            ilGen.Emit(OpCodes.Ldloc_2);
#if NETSTANDARD1_3
            var excuteMethod = typeof(SqlMapper).GetMethod("Execute", new[] { typeof(IDbConnection), typeof(string), typeof(object), typeof(IDbTransaction), typeof(int?), typeof(CommandType?) });
#else
            var excuteMethod = typeof(SqlMapper).GetMethod("Execute", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(IDbConnection), typeof(string), typeof(object), typeof(IDbTransaction), typeof(int?), typeof(CommandType?) }, null);
#endif
            ilGen.Emit(OpCodes.Call, excuteMethod);
            if (methodInfo.ReturnType == typeof(void))
            {
                ilGen.Emit(OpCodes.Pop);
            }
            ilGen.Emit(OpCodes.Ret);
            return;
        }

        private static void DefineQuerySqlMethod(TypeBuilder typeBuilder, FieldBuilder fieldBuilder, MethodInfo methodInfo, QuerySqlAttribute attr)
        {
            var parameters = methodInfo.GetParameters();
            var paramTypes = GetParameterTypes(parameters);
            ValidateParameterTypes(paramTypes, methodInfo.Name);
            ValidateQueryReturnParameterType(methodInfo.ReturnType, methodInfo.Name);
            var elementType = methodInfo.ReturnType.IsArray ? methodInfo.ReturnType.GetElementType() : methodInfo.ReturnType.GetGenericArguments()[0];

            var sql = attr.SqlFormat;
            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot, methodInfo.ReturnType, paramTypes);
            var ilGen = methodBuilder.GetILGenerator();

            ilGen.DeclareLocal(typeof(string));
            //ilGen.DeclareLocal(typeof(bool));
            ilGen.DeclareLocal(typeof(int?));
            ilGen.DeclareLocal(typeof(CommandType?));


            ilGen.Emit(OpCodes.Ldstr, sql);
            ilGen.Emit(OpCodes.Stloc_0);

            //this
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, fieldBuilder);

            //sql string
            ilGen.Emit(OpCodes.Ldloc_0);

            //object
#if NETSTANDARD1_3
            if (paramTypes.Length > 0 && paramTypes.All(p => p.GetTypeInfo().IsValueType || p == typeof(string)))
#else
            if (paramTypes.Length > 0 && paramTypes.All(p => p.IsValueType || p == typeof(string)))
#endif
            {
                var dicType = typeof(Dictionary<string, object>);
                ilGen.DeclareLocal(dicType);
                ilGen.Emit(OpCodes.Newobj, dicType.GetConstructor(Type.EmptyTypes));
                ilGen.Emit(OpCodes.Stloc_3);
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    ilGen.Emit(OpCodes.Ldloc_3);
                    ilGen.Emit(OpCodes.Ldstr, parameters[i].Name);
#if NETSTANDARD1_3
                    if (paramTypes[i].GetTypeInfo().IsValueType)
#else
                    if (paramTypes[i].IsValueType)
#endif
                    {
                        ilGen.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                        ilGen.Emit(OpCodes.Box, paramTypes[i]);
                    }
                    else
                    {
                        ilGen.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                    }
                    ilGen.Emit(OpCodes.Callvirt, dicType.GetMethod("Add", new[] { typeof(string), typeof(object) }));
                }
                ilGen.Emit(OpCodes.Ldloc_3);
            }
#if NETSTANDARD1_3
            else if (paramTypes.Length == 1 && paramTypes[0].GetTypeInfo().IsClass)
#else
            else if (paramTypes.Length == 1 && paramTypes[0].IsClass)
#endif
            {
                ilGen.Emit(OpCodes.Ldarg_1);
            }
            else
            {
                ilGen.Emit(OpCodes.Ldnull);
            }

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldc_I4_1);
            ilGen.Emit(OpCodes.Ldloca_S, (byte)1);
            ilGen.Emit(OpCodes.Initobj, typeof(int?));
            ilGen.Emit(OpCodes.Ldloc_1);
            ilGen.Emit(OpCodes.Ldloca_S, (byte)2);
            ilGen.Emit(OpCodes.Initobj, typeof(CommandType?));
            ilGen.Emit(OpCodes.Ldloc_2);

            var excuteMethod = typeof(SqlMapper).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name == "Query" && x.IsGenericMethodDefinition && x.GetParameters().Length == 7).MakeGenericMethod(new[] { elementType });
            ilGen.Emit(OpCodes.Call, excuteMethod);
            if (methodInfo.ReturnType.IsArray)
            {
                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(new[] { elementType });
                ilGen.Emit(OpCodes.Call, toArrayMethod);
            }
            ilGen.DeclareLocal(methodInfo.ReturnType);
            ilGen.Emit(OpCodes.Stloc_S, (byte)4);
            ilGen.Emit(OpCodes.Ldloc_S, (byte)4);
            ilGen.Emit(OpCodes.Ret);
            return;
        }

        private static Type[] GetParameterTypes(ParameterInfo[] parameters)
        {
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                paramTypes[i] = parameters[i].ParameterType;
            };
            return paramTypes;
        }

        private static void ValidateParameterTypes(Type[] paramTypes, string methodName)
        {
#if NETSTANDARD1_3
            if (paramTypes.Length > 1 && paramTypes.All(p => !p.GetTypeInfo().IsValueType & p != typeof(string)))
#else
            if (paramTypes.Length > 1 && paramTypes.All(p => !p.IsValueType && p != typeof(string)))
#endif
            {
                throw new ArgumentException($"The method \"{methodName}\" only supports multiple(>0) value/string types or single(=1) class type");
            }
        }

        private static void ValidateExcuteReturnParameterType(Type returnType, string methodName)
        {
            if (returnType != typeof(int) && returnType != typeof(void))
            {
                throw new ArgumentException($"The method \"{methodName}\" only supports int/void return type");
            }
        }

        private static void ValidateQueryReturnParameterType(Type returnType, string methodName)
        {
            if (!returnType.IsArray && returnType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                throw new ArgumentException($"The method \"{methodName}\" only supports array/IEnumerable<> return type");
            }
        }

        private static void DefineMethod(TypeBuilder typeBuilder, FieldBuilder fieldBuilder, MethodInfo methodInfo)
        {
            var attrExcute = methodInfo.GetCustomAttribute<ExcuteSqlAttribute>();
            if (attrExcute != null)
            {
                DefineExcuteSqlMethod(typeBuilder, fieldBuilder, methodInfo, attrExcute);
                return;
            }

            var attrQuery = methodInfo.GetCustomAttribute<QuerySqlAttribute>();
            if (attrQuery != null)
            {
                DefineQuerySqlMethod(typeBuilder, fieldBuilder, methodInfo, attrQuery);
            }
        }

        private static Type GetMapperType<T>()
        {
            Type type = typeof(T);
#if NETSTANDARD1_3 || NETSTANDARD2_0
            if (!type.GetTypeInfo().IsInterface)
#else
            if (!type.IsInterface)
#endif
            {
                throw new ArgumentException($"The mapper only supports interface type defined that inherited ISqlOperationMapper");
            }

            if (mapperTypeCache.TryGetValue(type.TypeHandle, out Type mapperType))
            {
                return mapperType;
            }

            var assemblyBuilder = GetAssemblyBuilder();
            var moduleBuilder = GetModuleBuilder(assemblyBuilder);
            var typeBuilder = GetTypeBuilder(type, moduleBuilder);

            var fieldBuilder = DefineFieldConnection(typeBuilder);
            DefineConstructor(typeBuilder, fieldBuilder);

            var methods = type.GetMethods();
            foreach (MethodInfo methodInfo in methods)
            {
                DefineMethod(typeBuilder, fieldBuilder, methodInfo);
            }

#if NETSTANDARD1_3 || NETSTANDARD2_0
            mapperType = typeBuilder.CreateTypeInfo().AsType();
#else
            mapperType = typeBuilder.CreateType();
#endif
            mapperTypeCache[type.TypeHandle] = mapperType;
            return mapperType;
        }


        public static T GetMapperInstance<T>(IDbConnection connection)
        {
            var mapperType = GetMapperType<T>();
#if NETSTANDARD1_3
            var constructor = mapperType.GetConstructor(new[] { typeof(IDbConnection) });
#else
            var constructor = mapperType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(IDbConnection) }, null);
#endif

            var param0 = Expression.Parameter(typeof(IDbConnection));
            var source = Expression.New(constructor, param0);
            var expr = Expression.Lambda<Func<IDbConnection, T>>(source, param0).Compile();
            return expr.Invoke(connection);
        }
    }
}
