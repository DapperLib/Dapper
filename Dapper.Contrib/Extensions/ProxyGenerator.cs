using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Dapper.Contrib.Extensions
{
    //TODO: Try hiding this interface
    public interface IProxy
    {
        bool IsDirty { get; set; }
    }

    class ProxyGenerator
    {
        private static readonly Dictionary<Type, object> TypeCache = new Dictionary<Type, object>();

        private static AssemblyBuilder GetAsmBuilder(string name)
        {
            var assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(new AssemblyName { Name = name }, 
                AssemblyBuilderAccess.Run);       //NOTE: to save, use RunAndSave

            return assemblyBuilder;
        }

        public static T GetClassProxy<T>()
        {
            // A class proxy could be implemented if all properties are virtual
            //  otherwise there is a pretty dangerous case where internal actions will not update dirty tracking
            throw new NotImplementedException();
        }


        public static T GetInterfaceProxy<T>()
        {
            Type typeOfT = typeof(T);

            if (TypeCache.ContainsKey(typeOfT))
            {
                return (T)TypeCache[typeOfT];
            }
            var assemblyBuilder = GetAsmBuilder(typeOfT.Name);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("SqlMapperExtensions." + typeOfT.Name); //NOTE: to save, add "asdasd.dll" parameter

            var interfaceType = typeof(IProxy);
            var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "_" + Guid.NewGuid(), 
                TypeAttributes.Public | TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(typeOfT);
            typeBuilder.AddInterfaceImplementation(interfaceType);

            //create our _isDirty field, which implements IProxy
            var setIsDirtyMethod = CreateIsDirtyProperty(typeBuilder);

            // Generate a field for each property, which implements the T
            foreach (var property in typeof(T).GetProperties())
            {
                var isId = property.GetCustomAttributes(true).Any(a => a is KeyAttribute);
                CreateProperty<T>(typeBuilder, property.Name, property.PropertyType, setIsDirtyMethod, isId);
            }

            var generatedType = typeBuilder.CreateType();

            //assemblyBuilder.Save(name + ".dll");  //NOTE: to save, uncomment

            var generatedObject = Activator.CreateInstance(generatedType);

            TypeCache.Add(typeOfT, generatedObject);
            return (T)generatedObject;
        }

      
        private static MethodInfo CreateIsDirtyProperty(TypeBuilder typeBuilder)
        {
            Type propType = typeof(bool);
            FieldBuilder field = typeBuilder.DefineField("_" + "IsDirty", propType, FieldAttributes.Private);
            // Generate a public property
            PropertyBuilder property =
                typeBuilder.DefineProperty("IsDirty",
                                           PropertyAttributes.None,
                                           propType,
                                           new Type[] { propType });

            // The property set and property get methods require a special set of attributes:

            MethodAttributes GetSetAttr =
                MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.SpecialName |
                MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig;

            // Define the "get" accessor method for current private field.
            MethodBuilder currGetPropMthdBldr =
                typeBuilder.DefineMethod("get_" + "IsDirty",
                                         GetSetAttr,
                                         propType,
                                         Type.EmptyTypes);

            // Intermediate Language stuff...
            ILGenerator currGetIL = currGetPropMthdBldr.GetILGenerator();
            currGetIL.Emit(OpCodes.Ldarg_0);
            currGetIL.Emit(OpCodes.Ldfld, field);
            currGetIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for current private field.
            MethodBuilder currSetPropMthdBldr =
                typeBuilder.DefineMethod("set_" + "IsDirty",
                                         GetSetAttr,
                                         null,
                                         new Type[] { propType });

            // Again some Intermediate Language stuff...
            ILGenerator currSetIL = currSetPropMthdBldr.GetILGenerator();
            currSetIL.Emit(OpCodes.Ldarg_0);
            currSetIL.Emit(OpCodes.Ldarg_1);
            currSetIL.Emit(OpCodes.Stfld, field);
            currSetIL.Emit(OpCodes.Ret);

            // Last, we must map the two methods created above to our PropertyBuilder to 
            // their corresponding behaviors, "get" and "set" respectively. 
            property.SetGetMethod(currGetPropMthdBldr);
            property.SetSetMethod(currSetPropMthdBldr);

            MethodInfo getMethod = typeof(IProxy).GetMethod("get_" + "IsDirty");
            MethodInfo setMethod = typeof(IProxy).GetMethod("set_" + "IsDirty");
            typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
            typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);

            return currSetPropMthdBldr;
        }

        private static void CreateProperty<T>(TypeBuilder typeBuilder, string propertyName, Type propType, MethodInfo setIsDirtyMethod, bool isIdentity)
        {
            FieldBuilder field = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);
            // Generate a public property
            PropertyBuilder property =
                typeBuilder.DefineProperty(propertyName,
                                           PropertyAttributes.None,
                                           propType,
                                           new Type[] { propType });

            // The property set and property get methods require a special set of attributes:

            MethodAttributes GetSetAttr =
                MethodAttributes.Public | MethodAttributes.Virtual |
                MethodAttributes.HideBySig;

            // Define the "get" accessor method for current private field.
            MethodBuilder currGetPropMthdBldr =
                typeBuilder.DefineMethod("get_" + propertyName,
                                         GetSetAttr,
                                         propType,
                                         Type.EmptyTypes);

            // Intermediate Language stuff...
            ILGenerator currGetIL = currGetPropMthdBldr.GetILGenerator();
            currGetIL.Emit(OpCodes.Ldarg_0);
            currGetIL.Emit(OpCodes.Ldfld, field);
            currGetIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for current private field.
            MethodBuilder currSetPropMthdBldr =
                typeBuilder.DefineMethod("set_" + propertyName,
                                         GetSetAttr,
                                         null,
                                         new Type[] { propType });

            // Again some Intermediate Language stuff...
            ILGenerator currSetIL = currSetPropMthdBldr.GetILGenerator();
            currSetIL.Emit(OpCodes.Ldarg_0);
            currSetIL.Emit(OpCodes.Ldarg_1);
            currSetIL.Emit(OpCodes.Stfld, field);
            currSetIL.Emit(OpCodes.Ldarg_0);
            currSetIL.Emit(OpCodes.Ldc_I4_1);
            currSetIL.Emit(OpCodes.Call, setIsDirtyMethod);
            currSetIL.Emit(OpCodes.Ret);

            if (isIdentity)
            {
                Type keyAttribute = typeof(KeyAttribute);
                // Create a Constructorinfo object for attribute 'MyAttribute1'.
                ConstructorInfo myConstructorInfo = keyAttribute.GetConstructor(new Type[] { });
                // Create the CustomAttribute instance of attribute of type 'MyAttribute1'.
                CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(myConstructorInfo, new object[] { });
                property.SetCustomAttribute(attributeBuilder);
            }

            // Last, we must map the two methods created above to our PropertyBuilder to 
            // their corresponding behaviors, "get" and "set" respectively. 
            property.SetGetMethod(currGetPropMthdBldr);
            property.SetSetMethod(currSetPropMthdBldr);

            MethodInfo getMethod = typeof(T).GetMethod("get_" + propertyName);
            MethodInfo setMethod = typeof(T).GetMethod("set_" + propertyName);
            typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
            typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);
        }

    }
}
