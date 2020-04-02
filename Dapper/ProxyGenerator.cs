using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    /// <summary>
    /// Class to Generate Proxy Classes from Interfaces
    /// </summary>
    public static class ProxyGenerator
    {
        /// <summary>
        /// Interface that will be implemented additionally and contains the IsDirty stuff.
        /// </summary>
        public interface IProxy //must be kept public
        {
            /// <summary>
            /// Is true if changes have occurred. Only here for backwards compatibility
            /// </summary>
            bool IsDirty { get; }
            /// <summary>
            /// Hashmap of all field names that have been changed
            /// </summary>
            HashSet<string> DirtyFields { get; }
            /// <summary>
            /// Reset the DirtyFields Hashmap. IsDirty will be true afterwards, and DirtyFields will be empty.
            /// </summary>
            void MarkAsClean();
        }
        private static readonly Dictionary<Type, Type> TypeCache = new Dictionary<Type, Type>();

        private static AssemblyBuilder GetAsmBuilder(string name)
        {
#if NETSTANDARD2_0
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
#else
            return Thread.GetDomain().DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
#endif
        }

        /// <summary>
        /// Returns an instance of the generated Interface Proxy
        /// </summary>
        public static T GetInterfaceProxy<T>()
        {
            return (T)Activator.CreateInstance(GetInterfaceProxyType<T>());
        }

        /// <summary>
        /// Returns the Type of the generated Interface Proxy
        /// </summary>
        public static Type GetInterfaceProxyType<T>()
        {
            Type typeOfT = typeof(T);

            if (TypeCache.TryGetValue(typeOfT, out Type k))
            {
                return k;
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
                object[] tmp = property.GetCustomAttributes(true);
                List<Attribute> custAttr = new List<Attribute>(tmp.Count());
                foreach (var t in tmp)
                {
                    custAttr.Add((Attribute)t);
                }
                CreateProperty<T>(typeBuilder, property.Name, property.PropertyType, setIsDirtyMethod, custAttr);
            }

#if NETSTANDARD2_0
                var generatedType = typeBuilder.CreateTypeInfo().AsType();
#else
            var generatedType = typeBuilder.CreateType();
#endif

            TypeCache.Add(typeOfT, generatedType);
            return generatedType;
        }

        private static MethodInfo CreateIsDirtyProperty(TypeBuilder typeBuilder)
        {
            var DirtyFieldsType = typeof(HashSet<string>);
            var DirtyFieldsField = typeBuilder.DefineField("_" + nameof(IProxy.DirtyFields), DirtyFieldsType, FieldAttributes.Private);
            var DirtyFieldsProp = typeBuilder.DefineProperty(nameof(IProxy.DirtyFields),
                                           System.Reflection.PropertyAttributes.None,
                                           DirtyFieldsType,
                                           new[] { DirtyFieldsType });
            const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.SpecialName
                                              | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig;

            //Dirty fields is roughly equivalent to:
            // Hashset<string> DirtyFields { get; private set; }
            //getter for DirtyFields
            var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + nameof(IProxy.DirtyFields),
                                         getSetAttr,
                                         DirtyFieldsType,
                                         Type.EmptyTypes);
            var currGetIl = currGetPropMthdBldr.GetILGenerator();
            currGetIl.Emit(OpCodes.Ldarg_0);
            currGetIl.Emit(OpCodes.Ldfld, DirtyFieldsField);
            currGetIl.Emit(OpCodes.Ret);
            DirtyFieldsProp.SetGetMethod(currGetPropMthdBldr);
            var getMethod = typeof(IProxy).GetMethod("get_" + nameof(IProxy.DirtyFields));
            typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);

            //setter for DirtyFields
            var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + nameof(IProxy.DirtyFields),
                                         (getSetAttr & ~MethodAttributes.Public) | MethodAttributes.Private,
                                         null,
                                         new[] { DirtyFieldsType });
            var currSetIl = currSetPropMthdBldr.GetILGenerator();
            currSetIl.Emit(OpCodes.Ldarg_0);
            currSetIl.Emit(OpCodes.Ldarg_1);
            currSetIl.Emit(OpCodes.Stfld, DirtyFieldsField);
            currSetIl.Emit(OpCodes.Ret);
            DirtyFieldsProp.SetSetMethod(currSetPropMthdBldr);
            var setMethod = typeof(IProxy).GetMethod("set_" + nameof(IProxy.DirtyFields));

            // void MarkAsClean() {
            //  this.DirtyFields = new HashSet<string>();
            // }
            var markAsCleanMethBldr = typeBuilder.DefineMethod(nameof(IProxy.MarkAsClean),
                                         getSetAttr,
                                         typeof(void),
                                         Type.EmptyTypes);
            var markAsCleanIl = markAsCleanMethBldr.GetILGenerator();
            markAsCleanIl.Emit(OpCodes.Ldarg_0);
            markAsCleanIl.Emit(OpCodes.Newobj, typeof(HashSet<string>).GetConstructor(new Type[0]));
            markAsCleanIl.Emit(OpCodes.Stfld, DirtyFieldsField);
            markAsCleanIl.Emit(OpCodes.Ret);

            //Constructor. This is needed to initialize DirtyFields
            // public ClassName() {
            //   MarkAsClean();
            // }
            ConstructorInfo objCtor = typeof(object).GetConstructor(new Type[0]);
            ConstructorBuilder constrBuilder = typeBuilder.DefineConstructor(
                           MethodAttributes.Public,
                           CallingConventions.Standard,
                           Type.EmptyTypes);

            ILGenerator ctorIL = constrBuilder.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, objCtor);
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, markAsCleanMethBldr);
            ctorIL.Emit(OpCodes.Ret);


            //getter for bool IsDirty. Equivalent to:
            //public bool IsDirty { get { return this.DirtyFields.Count() > 0; } }
            var currGetIsDirtyMthdBldr = typeBuilder.DefineMethod("get_" + nameof(IProxy.IsDirty),
                                         getSetAttr,
                                         typeof(bool),
                                         Type.EmptyTypes);
            var currGetIsDirtyIl = currGetIsDirtyMthdBldr.GetILGenerator();
            var countMethod = typeof(System.Linq.Enumerable).GetMethods().Where(x => x.Name == "Count" && x.GetParameters().Count() == 1).First();
            countMethod = countMethod.MakeGenericMethod(new Type[] { typeof(string) });
            currGetIsDirtyIl.Emit(OpCodes.Ldarg_0);
            currGetIsDirtyIl.Emit(OpCodes.Ldfld, DirtyFieldsField);
            currGetIsDirtyIl.Emit(OpCodes.Call, countMethod);
            currGetIsDirtyIl.Emit(OpCodes.Ldc_I4_0);
            currGetIsDirtyIl.Emit(OpCodes.Cgt);
            currGetIsDirtyIl.Emit(OpCodes.Ret);


            //function to mark a Field as dirty
            // private void add_DirtyFields(string fieldName) {
            //   this.DirtyFields.Add(fieldName);
            // }
            var addIsDirtyPropMethBldr = typeBuilder.DefineMethod("add_" + nameof(IProxy.DirtyFields),
                                         MethodAttributes.Private | MethodAttributes.NewSlot | MethodAttributes.SpecialName
                                              | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                                         typeof(void),
                                         new Type[] { typeof(string) });
            var addIsDirtyIl = addIsDirtyPropMethBldr.GetILGenerator();
            addIsDirtyIl.Emit(OpCodes.Nop);
            addIsDirtyIl.Emit(OpCodes.Ldarg_0);
            addIsDirtyIl.Emit(OpCodes.Ldfld, DirtyFieldsField);
            addIsDirtyIl.Emit(OpCodes.Ldarg_1);
            addIsDirtyIl.Emit(OpCodes.Callvirt, DirtyFieldsType.GetMethod("Add", new Type[] { typeof(string) }));
            addIsDirtyIl.Emit(OpCodes.Pop);
            addIsDirtyIl.Emit(OpCodes.Ret);
            return addIsDirtyPropMethBldr;
        }

        private static void CreateProperty<T>(TypeBuilder typeBuilder, string propertyName, Type propType, MethodInfo setIsDirtyMethod, IEnumerable<Attribute> custAttributes)
        {
            //Define the field and the property 
            var field = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);
            var property = typeBuilder.DefineProperty(propertyName,
                                           System.Reflection.PropertyAttributes.None,
                                           propType,
                                           new[] { propType });

            const MethodAttributes getSetAttr = MethodAttributes.Public
                                                | MethodAttributes.Virtual
                                                | MethodAttributes.HideBySig;

            // Define the "get" and "set" accessor methods
            var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName,
                                         getSetAttr,
                                         propType,
                                         Type.EmptyTypes);

            var currGetIl = currGetPropMthdBldr.GetILGenerator();
            currGetIl.Emit(OpCodes.Ldarg_0);
            currGetIl.Emit(OpCodes.Ldfld, field);
            currGetIl.Emit(OpCodes.Ret);

            var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                                         getSetAttr,
                                         null,
                                         new[] { propType });

            //store value in private field and set the isdirty flag
            var currSetIl = currSetPropMthdBldr.GetILGenerator();
            currSetIl.Emit(OpCodes.Ldarg_0);
            currSetIl.Emit(OpCodes.Ldarg_1);
            currSetIl.Emit(OpCodes.Stfld, field);
            currSetIl.Emit(OpCodes.Ldarg_0);
            currSetIl.Emit(OpCodes.Ldstr, propertyName);
            currSetIl.Emit(OpCodes.Call, setIsDirtyMethod);
            currSetIl.Emit(OpCodes.Ret);

            //TODO: this copy of attributes does not work for all cases, but I'm not sure if such a copy can be done.
            //this however works for all Dapper.Contrib's Attributes
            foreach (var a in custAttributes)
            {
                var attrType = a.GetType();
                var constructors = attrType.GetConstructors();
                Array.Sort(constructors, (x, y) => x.GetParameters().Count().CompareTo(y.GetParameters().Count()));
                var constructor = constructors[0];
                var constParams = constructor.GetParameters();
                object[] constArgs = new object[constParams.Count()];
                bool success = true;
                for (int i = 0; i < constParams.Count(); i++)
                {
                    var method = attrType.GetMethod("get_" + char.ToUpper(constParams[i].Name[0]) + constParams[i].Name.Substring(1));
                    if (method != null)
                    {
                        constArgs[i] = method.Invoke(a, new object[0]);
                    }
                    else
                    {
                        success = false;
                    }
                }
                if (success)
                {
                    var attributeBuilder = new CustomAttributeBuilder(constructor, constArgs);
                    property.SetCustomAttribute(attributeBuilder);
                }
            }

            property.SetGetMethod(currGetPropMthdBldr);
            property.SetSetMethod(currSetPropMthdBldr);
            var getMethod = typeof(T).GetMethod("get_" + propertyName);
            var setMethod = typeof(T).GetMethod("set_" + propertyName);
            typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
            typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);
        }
    }
}
