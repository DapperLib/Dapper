using System;
using System.Collections.Concurrent;
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
        /// Returns all Properties of "inherited" Interfaces
        /// </summary>
        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Returns all Properties of "inherited" Interfaces
        /// </summary>
        public static MethodInfo GetPublicMethod(this Type type, string name)
        {
            if (type.IsInterface)
            {
                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var method = subType.GetMethod(name, 
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    if (method != null) return method;
                }
            }

            return type.GetMethod(name, BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }

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
        private static readonly ConcurrentDictionary<Type, Type> TypeCache = new ConcurrentDictionary<Type, Type>();

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
            foreach (var a in typeOfT.GetCustomAttributes(true))
            {
                var attrBuilder = GenerateAttributeBuilder((Attribute)a);
                if (attrBuilder != null)
                    typeBuilder.SetCustomAttribute(attrBuilder);
            }
            

            //create our _isDirty field, which implements IProxy
            var setIsDirtyMethod = CreateIsDirtyProperty(typeBuilder);

            // Generate a field for each property, which implements the T
            foreach (var property in typeof(T).GetPublicProperties())
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

            if (!TypeCache.TryAdd(typeOfT, generatedType))
            {
                if (TypeCache.TryGetValue(typeOfT, out Type k2))
                {
                    return k2;
                }
            }
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
                var attrBuilder = GenerateAttributeBuilder(a);
                if (attrBuilder != null)
                    property.SetCustomAttribute(attrBuilder);
            }

            property.SetGetMethod(currGetPropMthdBldr);
            property.SetSetMethod(currSetPropMthdBldr);
            var getMethod = typeof(T).GetPublicMethod("get_" + propertyName);
            var setMethod = typeof(T).GetPublicMethod("set_" + propertyName);
            typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
            typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);
        }

        private static CustomAttributeBuilder GenerateAttributeBuilder(Attribute a)
        {
            var attrType = a.GetType();
            var constructors = attrType.GetConstructors();
            var props = attrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(p => p.CanRead).ToList();
            var fields = attrType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).ToList();
            Array.Sort(constructors, (x, y) => y.GetParameters().Count().CompareTo(x.GetParameters().Count()));
            foreach (var constructor in constructors)
            {
                var constrParamInfos = constructor.GetParameters();
                var constructorArgs = new object[constrParamInfos.Length];
                bool allMatched = true;
                for (var i = 0; i < constructorArgs.Length && allMatched; i++)
                {
                    var prop = props.FirstOrDefault(x => x.Name.Equals(constrParamInfos[i].Name, StringComparison.OrdinalIgnoreCase) && x.PropertyType == constrParamInfos[i].ParameterType);
                    if (prop != null)
                    {
                        constructorArgs[i] = prop.GetValue(a);
                        props.Remove(prop);
                    }
                    else
                    {
                        var field = fields.FirstOrDefault(x => x.Name.Equals(constrParamInfos[i].Name, StringComparison.OrdinalIgnoreCase) && x.FieldType == constrParamInfos[i].ParameterType);
                        if (field != null)
                        {
                            constructorArgs[i] = field.GetValue(a);
                            fields.Remove(field);
                        }
                        else
                        {
                            allMatched = false;
                        }
                    }
                }
                if (allMatched)
                {
                    var propsArr = props.Where(p => p.CanRead && p.CanWrite && (p.GetMethod?.GetParameters().Length ?? 1) == 0).ToArray();
                    object[] propValues = new object[propsArr.Length];
                    for (var i = 0; i < propsArr.Length; i++)
                        propValues[i] = propsArr[i].GetMethod.Invoke(a, new object[0] { });

                    var fieldsArr = fields.ToArray();
                    object[] fieldValues = new object[fieldsArr.Length];
                    for (var i = 0; i < fieldsArr.Length; i++)
                        fieldValues[i] = fieldsArr[i].GetValue(a);

                    var attributeBuilder = new CustomAttributeBuilder(constructor, constructorArgs, propsArr, propValues, fieldsArr, fieldValues);
                    return attributeBuilder;
                }
            }
            return null;
        }
    }
}
